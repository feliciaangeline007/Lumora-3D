using UnityEngine;

namespace CoinConvoy
{
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        [Header("Mobil")]
        [SerializeField] private float acceleration = 16f;   // Akselerasi maju
        [SerializeField] private float reverseSpeed = 6f;    // Kecepatan mundur maksimum
        [SerializeField] private float maxSpeed = 12f;       // Kecepatan maju maksimum
        [SerializeField] private float steering = 90f;       // Kekuatan belok
        [SerializeField] private float brakeStrength = 22f;  // Kekuatan rem
        [SerializeField] private float flipRecoverY = -0.6f; // Batas mobil terbalik
        [SerializeField] private float inputResponse = 8f;
        [SerializeField] private float lateralGrip = 4f;
        [SerializeField] private Transform visual;

        [Header("Audio & Juice")]
        [SerializeField] private bool enableEngineSound = true;
        [SerializeField] private AudioClip skidSound;

        private Rigidbody body;
        private float gasInput;
        private float steerInput;
        private bool brakeInput;
        private float touchGasInput;
        private float externalSteerInput;
        private bool touchGasActive;
        private bool touchBrakeActive;
        private bool touchLeftActive;
        private bool touchRightActive;
        private bool externalSteerActive;
        private bool externalBrakeInput;
        private float currentSteer;
        private float smoothedGas;
        private float nitroMultiplier = 1f;   // Pengali kecepatan sementara
        private float nitroTimer;
        private float magnetTimer;
        private float magnetRange;

        private AudioSource engineSource;
        private AudioSource skidSource;
        private GameObject[] exhaustFlames;
        private ParticleSystem skidParticle;
        private ParticleSystem.EmissionModule skidEmission;
        private static AudioClip engineClip;
        private static Material skidParticleMaterial;
        private static Material nitroMaterial;

        public bool IsNitroActive => nitroTimer > 0f;
        public bool IsMagnetActive => magnetTimer > 0f;
        public float NitroRemaining => nitroTimer;
        public float MagnetRemaining => magnetTimer;
        public float CurrentSpeed => body != null ? body.linearVelocity.magnitude : 0f;
        public float SpeedRatio => maxSpeed > 0f ? Mathf.Clamp01(CurrentSpeed / maxSpeed) : 0f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.centerOfMass = new Vector3(0f, -0.35f, 0f);
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            SetupAudioAndVfx();
        }

        private void SetupAudioAndVfx()
        {
            // Audio Sources
            engineSource = gameObject.AddComponent<AudioSource>();
            engineSource.playOnAwake = false;
            engineSource.loop = true;
            engineSource.volume = 0.25f;
            engineSource.spatialBlend = 0f; // 2D crisp sound
            if (enableEngineSound)
                engineSource.clip = GetEngineClip();
            if (enableEngineSound) engineSource.Play();

            skidSource = gameObject.AddComponent<AudioSource>();
            skidSource.playOnAwake = false;
            skidSource.loop = true;
            skidSource.volume = 0.35f;
            skidSource.clip = skidSound;

            // Exhaust flame visuals for Nitro
            CreateExhaustFlames();
            CreateSkidParticles();
        }

        private void CreateSkidParticles()
        {
            GameObject dustObject = new GameObject("SkidDust");
            dustObject.transform.SetParent(transform, false);
            dustObject.transform.localPosition = new Vector3(0f, 0.08f, -0.65f);
            skidParticle = dustObject.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = skidParticle.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = 64;
            main.startLifetime = 0.45f;
            main.startSpeed = 0.8f;
            main.startSize = 0.2f;
            main.startColor = new Color(0.62f, 0.55f, 0.43f, 0.48f);

            skidEmission = skidParticle.emission;
            skidEmission.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = skidParticle.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.1f, 0.1f, 0.25f);

            ParticleSystemRenderer particleRenderer = dustObject.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleRenderer != null && shader != null)
            {
                if (skidParticleMaterial == null)
                    skidParticleMaterial = new Material(shader);
                particleRenderer.sharedMaterial = skidParticleMaterial;
            }

            skidParticle.Play();
        }

        private void CreateExhaustFlames()
        {
            exhaustFlames = new GameObject[2];
            for (int i = 0; i < 2; i++)
            {
                float xOffset = i == 0 ? -0.45f : 0.45f;
                GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                flame.name = "NitroFlame_" + i;
                flame.transform.SetParent(transform, false);
                flame.transform.localPosition = new Vector3(xOffset, 0.25f, -1.05f);
                flame.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                flame.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);

                Collider col = flame.GetComponent<Collider>();
                if (col != null) Destroy(col);

                Renderer ren = flame.GetComponent<Renderer>();
                if (ren != null)
                {
                    if (nitroMaterial == null)
                    {
                        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                                        Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                        nitroMaterial = new Material(shader)
                        {
                            color = new Color(0.1f, 0.8f, 1f, 0.9f),
                            enableInstancing = true
                        };
                    }
                    ren.sharedMaterial = nitroMaterial;
                }

                flame.SetActive(false);
                exhaustFlames[i] = flame;
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsEnded)
            {
                if (engineSource != null && engineSource.isPlaying) engineSource.Stop();
                if (skidSource != null && skidSource.isPlaying) skidSource.Stop();
                if (skidParticle != null) skidEmission.rateOverTime = 0f;
                return;
            }

            float keyboardGas = 0f;
            float keyboardSteer = 0f;
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) keyboardGas += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) keyboardGas -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) keyboardSteer -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) keyboardSteer += 1f;
            }
#else
            keyboardGas = Input.GetAxisRaw("Vertical");
            keyboardSteer = Input.GetAxisRaw("Horizontal");
#endif
            gasInput = touchGasActive ? touchGasInput : keyboardGas;
            steerInput = touchLeftActive || touchRightActive
                ? (touchRightActive ? 1f : 0f) - (touchLeftActive ? 1f : 0f)
                : externalSteerActive ? externalSteerInput : keyboardSteer;
#if ENABLE_INPUT_SYSTEM
            bool keyboardBrake = UnityEngine.InputSystem.Keyboard.current != null &&
                                 UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed;
#else
            bool keyboardBrake = Input.GetKey(KeyCode.Space);
#endif
            brakeInput = touchBrakeActive || externalBrakeInput || keyboardBrake;
            currentSteer = Mathf.Lerp(currentSteer, steerInput, 12f * Time.deltaTime);
            if (visual != null) visual.localRotation = Quaternion.Euler(0f, currentSteer * 14f, -currentSteer * 6f);
            if (engineSource != null)
            {
                engineSource.pitch = Mathf.Lerp(0.8f, 1.45f, SpeedRatio) *
                                     (IsNitroActive ? 1.12f : 1f);
                engineSource.volume = 0.18f + SpeedRatio * 0.12f;
            }
            UpdateSkidFeedback();

            // Nitro Timer & VFX
            if (nitroTimer > 0f)
            {
                nitroTimer -= Time.deltaTime;
                if (nitroTimer <= 0f)
                {
                    nitroMultiplier = 1f;
                    ToggleExhaustFlames(false);
                }
                else
                {
                    AnimateExhaustFlames();
                }
            }

            // Magnet Attraction Loop
            if (magnetTimer > 0f)
            {
                magnetTimer -= Time.deltaTime;
                AttractNearbyCoins();
            }

            RecoverIfFlipped();
        }

        private void FixedUpdate()
        {
            Vector3 flatVelocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
            float forwardSpeed = Vector3.Dot(flatVelocity, transform.forward);
            smoothedGas = Mathf.MoveTowards(smoothedGas,
                gasInput, Mathf.Max(0.1f, inputResponse) * Time.fixedDeltaTime);

            // Gas / mundur dengan kecepatan maksimal yang dibatasi
            body.AddForce(transform.forward * smoothedGas * acceleration, ForceMode.Acceleration);
            float currentMax = maxSpeed * nitroMultiplier;
            if (forwardSpeed > currentMax && smoothedGas > 0f)
                body.AddForce(-transform.forward * (forwardSpeed - currentMax) * 3f, ForceMode.Acceleration);
            if (forwardSpeed < -reverseSpeed && smoothedGas < 0f)
                body.AddForce(-transform.forward * (forwardSpeed + reverseSpeed) * 3f, ForceMode.Acceleration);

            // Rem mengurangi kecepatan, lalu mobil bisa mundur pelan
            if (brakeInput && flatVelocity.sqrMagnitude > 0.5f)
                body.AddForce(-flatVelocity * (brakeStrength / Mathf.Max(flatVelocity.magnitude, 1f)), ForceMode.Acceleration);

            float lateralSpeed = Vector3.Dot(flatVelocity, transform.right);
            body.AddForce(-transform.right * lateralSpeed * Mathf.Max(0f, lateralGrip), ForceMode.Acceleration);

            // Belok hanya stabil saat ada kecepatan, tidak muter di tempat
            float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 4f);
            float turnAmount = currentSteer * steering * Time.fixedDeltaTime * speedFactor;
            if (Mathf.Sign(forwardSpeed) < 0f && Mathf.Abs(forwardSpeed) > 0.5f) turnAmount = -turnAmount;
            body.MoveRotation(body.rotation * Quaternion.Euler(0f, turnAmount, 0f));

        }

        private void AttractNearbyCoins()
        {
            Vector3 playerPos = transform.position;
            var activeCoins = CoinPickup.ActiveCoins;
            for (int i = 0; i < activeCoins.Count; i++)
            {
                if (activeCoins[i] == null) continue;
                float dist = Vector3.Distance(playerPos, activeCoins[i].transform.position);
                if (dist <= magnetRange)
                {
                    float pullSpeed = Mathf.Lerp(22f, 10f, dist / magnetRange);
                    activeCoins[i].PullTowards(playerPos, pullSpeed);
                }
            }
        }

        private void ToggleExhaustFlames(bool active)
        {
            if (exhaustFlames == null) return;
            foreach (var flame in exhaustFlames)
            {
                if (flame != null) flame.SetActive(active);
            }
        }

        private void AnimateExhaustFlames()
        {
            if (exhaustFlames == null) return;
            float flicker = 0.35f + Mathf.Sin(Time.time * 30f) * 0.1f;
            foreach (var flame in exhaustFlames)
            {
                if (flame != null)
                {
                    flame.transform.localScale = new Vector3(0.12f, flicker, 0.12f);
                }
            }
        }

        private void RecoverIfFlipped()
        {
            if (transform.rotation.eulerAngles.z > 25f && transform.rotation.eulerAngles.z < 335f)
            {
                transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                body.linearVelocity = Vector3.zero;
            }
        }

        private void UpdateSkidFeedback()
        {
            if (skidParticle == null) return;
            Vector3 velocity = body.linearVelocity;
            float sidewaysSpeed = Mathf.Abs(Vector3.Dot(velocity, transform.right));
            float intensity = Mathf.Clamp01((sidewaysSpeed - 1.2f) / 5f) *
                              Mathf.Clamp01(new Vector2(velocity.x, velocity.z).magnitude / 4f);
            skidEmission.rateOverTime = intensity * 24f;

            if (skidSource == null || skidSound == null) return;
            bool shouldPlay = intensity > 0.18f;
            if (shouldPlay && !skidSource.isPlaying)
                skidSource.Play();
            else if (!shouldPlay && skidSource.isPlaying)
                skidSource.Stop();
            skidSource.volume = 0.35f * intensity;
        }

        public void SetGas(float value) => SetTouchGas(value);
        public void SetSteer(float value)
        {
            externalSteerInput = Mathf.Clamp(value, -1f, 1f);
            externalSteerActive = Mathf.Abs(externalSteerInput) > 0.01f;
        }
        public void SetBrake(bool value) => externalBrakeInput = value;

        public void SetTouchGas(float value)
        {
            touchGasInput = Mathf.Clamp(value, -1f, 1f);
            touchGasActive = Mathf.Abs(touchGasInput) > 0.01f;
        }

        public void SetTouchBrake(bool pressed) => touchBrakeActive = pressed;

        public void SetTouchSteer(float direction, bool pressed)
        {
            if (direction < 0f) touchLeftActive = pressed;
            else if (direction > 0f) touchRightActive = pressed;
        }

        public void StartNitro(float multiplier, float duration)
        {
            nitroMultiplier = Mathf.Max(1f, multiplier);
            nitroTimer = duration;
            body.AddForce(transform.forward * 9f, ForceMode.VelocityChange);
            ToggleExhaustFlames(true);
        }

        public void StartMagnet(float range, float duration)
        {
            magnetRange = range;
            magnetTimer = duration;
        }

        private static AudioClip GetEngineClip()
        {
            if (engineClip != null) return engineClip;

            const int sampleRate = 22050;
            const int sampleCount = sampleRate;
            const float baseFrequency = 48f;
            float[] samples = new float[sampleCount];
            for (int i = 0; i < samples.Length; i++)
            {
                float phase = i * baseFrequency * (2f * Mathf.PI / sampleRate);
                samples[i] = (Mathf.Sin(phase) * 0.55f + Mathf.Sin(phase * 2f) * 0.28f +
                              Mathf.Sin(phase * 3f) * 0.1f) * 0.12f;
            }
            engineClip = AudioClip.Create("CoinConvoyEngine", sampleCount, 1, sampleRate, false);
            engineClip.SetData(samples, 0);
            return engineClip;
        }
    }
}
