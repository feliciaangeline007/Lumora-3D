using UnityEngine;

namespace EnchantedForest
{
    /// <summary>
    /// Player Controller 3D lengkap untuk menjelajahi level design.
    /// Kompatibel dengan Unity 6 Input System & Legacy Input.
    /// Mendukung jalan, lari, lompat, pantulan jamur (bounce pad), efek suara, dan respawn jika jatuh.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [SelectionBase]
    public class PlayerController3D : MonoBehaviour
    {
        [Header("Kecepatan & Pergerakan")]
        public float walkSpeed = 6.0f;
        public float sprintSpeed = 9.5f;
        public float rotationSpeed = 12.0f;
        public float gravity = 22.0f;

        [Header("Lompatan")]
        public float jumpForce = 8.5f;
        public float coyoteTime = 0.2f;
        public float jumpBufferTime = 0.16f;

        [Header("Audio & Langkah")]
        public float stepIntervalWalk = 0.5f;
        public float stepIntervalSprint = 0.32f;

        [Header("Respawn Saat Jatuh")]
        public float fallThresholdY = -12f;
        public Vector3 spawnPoint;

        [Header("Kamera Follow (Opsional)")]
        public Transform cameraTransform;

        private CharacterController _controller;
        private Vector3 _velocity;
        private float _coyoteCounter;
        private float _jumpBufferCounter;
        private bool _wasGrounded;
        private float _stepTimer;
        private static Material _runtimeSkybox;
        private TouchGameControls _touchControls;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            spawnPoint = transform.position;
            _touchControls = TouchGameControls.CreateFor(this);
            EnsureForestSkybox();
            EnsureCharacterVisuals();
            LevelVisualPolish.Apply(this);
        }

        private void EnsureForestSkybox()
        {
            Material sky = RenderSettings.skybox;
            if (sky == null || !sky.name.Contains("ForestSkybox"))
            {
                Shader shader = Shader.Find("Skybox/Procedural");
                if (shader == null)
                {
                    Debug.LogError("[PlayerController3D] Shader Skybox/Procedural tidak ditemukan; skybox hutan tidak dapat dibuat.", this);
                    return;
                }

                if (_runtimeSkybox == null)
                {
                    _runtimeSkybox = new Material(shader) { name = "Runtime_ForestSkybox" };
                }
                sky = _runtimeSkybox;
            }

            sky.SetFloat("_SunSize", 0.035f);
            sky.SetFloat("_AtmosphereThickness", 1.2f);
            sky.SetFloat("_Exposure", 1.15f);
            sky.SetColor("_SkyTint", Color.Lerp(Color.white, RenderSettings.ambientSkyColor, 0.35f));
            sky.SetColor("_GroundColor", RenderSettings.fogColor);
            RenderSettings.skybox = sky;

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.Skybox;
            }

            if (RenderSettings.sun == null)
            {
                foreach (Light sceneLight in FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (sceneLight.type != LightType.Directional) continue;
                    RenderSettings.sun = sceneLight;
                    break;
                }
            }
        }

        private void EnsureCharacterVisuals()
        {
            Transform visuals = transform.Find("Visuals");
            if (visuals == null)
            {
                Debug.LogWarning("[PlayerController3D] Child Visuals tidak ditemukan; detail karakter dilewati.", this);
                return;
            }

            Material tunic = FindPartMaterial(visuals, "Body");
            Material skin = FindPartMaterial(visuals, "Head");
            Material leather = FindPartMaterial(visuals, "Hat") ?? tunic;
            Material crystal = FindPartMaterial(visuals, "CrystalStaff/LumoraGem") ??
                               LevelVisualPolish.CrystalMaterial ??
                               leather;
            if (tunic == null || skin == null)
            {
                Debug.LogWarning("[PlayerController3D] Material Body atau Head tidak ditemukan; detail karakter dilewati.", this);
                return;
            }

            AddVisualPart(visuals, "LeftArm", PrimitiveType.Capsule, new Vector3(-0.39f, 0.78f, 0.015f), new Vector3(0.19f, 0.55f, 0.19f), tunic, 0f, 0f, -12f);
            AddVisualPart(visuals, "RightArm", PrimitiveType.Capsule, new Vector3(0.39f, 0.78f, 0.015f), new Vector3(0.19f, 0.55f, 0.19f), tunic, 0f, 0f, 12f);
            AddVisualPart(visuals, "LeftGlove", PrimitiveType.Sphere, new Vector3(-0.46f, 0.49f, 0.02f), Vector3.one * 0.17f, leather);
            AddVisualPart(visuals, "RightGlove", PrimitiveType.Sphere, new Vector3(0.46f, 0.49f, 0.02f), Vector3.one * 0.17f, leather);
            AddVisualPart(visuals, "LeftLeg", PrimitiveType.Capsule, new Vector3(-0.16f, 0.25f, 0f), new Vector3(0.22f, 0.42f, 0.23f), leather);
            AddVisualPart(visuals, "RightLeg", PrimitiveType.Capsule, new Vector3(0.16f, 0.25f, 0f), new Vector3(0.22f, 0.42f, 0.23f), leather);
            AddVisualPart(visuals, "LeftBoot", PrimitiveType.Cube, new Vector3(-0.16f, 0.08f, 0.07f), new Vector3(0.25f, 0.15f, 0.38f), leather);
            AddVisualPart(visuals, "RightBoot", PrimitiveType.Cube, new Vector3(0.16f, 0.08f, 0.07f), new Vector3(0.25f, 0.15f, 0.38f), leather);
            AddVisualPart(visuals, "Belt", PrimitiveType.Cube, new Vector3(0f, 0.57f, 0f), new Vector3(0.53f, 0.12f, 0.41f), leather);
            AddVisualPart(visuals, "LeftEye", PrimitiveType.Sphere, new Vector3(-0.075f, 1.42f, 0.19f), Vector3.one * 0.045f, leather);
            AddVisualPart(visuals, "RightEye", PrimitiveType.Sphere, new Vector3(0.075f, 1.42f, 0.19f), Vector3.one * 0.045f, leather);
            AddVisualPart(visuals, "Nose", PrimitiveType.Sphere, new Vector3(0f, 1.37f, 0.215f), new Vector3(0.07f, 0.09f, 0.07f), skin);
            AddVisualPart(visuals, "ShoulderMantle", PrimitiveType.Capsule, new Vector3(0f, 1.08f, -0.12f), new Vector3(0.64f, 0.2f, 0.43f), leather);
            AddVisualPart(visuals, "ChestEmblem", PrimitiveType.Sphere, new Vector3(0f, 0.91f, 0.235f), new Vector3(0.18f, 0.22f, 0.08f), crystal);
            AddVisualPart(visuals, "ScarfKnot", PrimitiveType.Sphere, new Vector3(0f, 1.16f, 0.2f), new Vector3(0.2f, 0.16f, 0.14f), crystal);
        }

        private static Material FindPartMaterial(Transform parent, string partName)
        {
            Transform part = parent.Find(partName);
            return part != null && part.TryGetComponent(out Renderer partRenderer)
                ? partRenderer.sharedMaterial
                : null;
        }

        private static void AddVisualPart(
            Transform parent,
            string partName,
            PrimitiveType shape,
            Vector3 position,
            Vector3 scale,
            Material material,
            float xRotation = 0f,
            float yRotation = 0f,
            float zRotation = 0f)
        {
            if (parent.Find(partName) != null) return;

            GameObject part = GameObject.CreatePrimitive(shape);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
            part.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(part.GetComponent<Collider>());
        }

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            HandleMovement();
            CheckFallRespawn();
        }

        private void HandleMovement()
        {
            bool isGrounded = _controller.isGrounded;

            // Efek suara saat mendarat
            if (isGrounded && !_wasGrounded && _velocity.y < -3.0f)
            {
                if (GameAudioManager.Instance != null)
                {
                    GameAudioManager.Instance.PlayLandSound();
                }
            }
            _wasGrounded = isGrounded;

            if (isGrounded)
            {
                _coyoteCounter = coyoteTime;
                if (_velocity.y < 0)
                {
                    _velocity.y = -2f; // Menjaga player tetap menempel di tanah
                }
            }
            else
            {
                _coyoteCounter -= Time.deltaTime;
            }

            // 1. Baca Input Gerakan (Support New Input System & Legacy)
            Vector2 inputDir = ReadMovementInput();
            bool isSprinting = ReadSprintInput();
            bool jumpPressed = ReadJumpInput();
            if (jumpPressed)
            {
                _jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                _jumpBufferCounter = Mathf.Max(0f, _jumpBufferCounter - Time.deltaTime);
            }

            // 2. Arah Gerak Relatif terhadap Kamera
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * inputDir.y + right * inputDir.x).normalized;

            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
            Vector3 horizontalMove = moveDirection * currentSpeed;

            // 3. Putar Karakter menghadap arah jalan
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

                // Efek suara langkah kaki
                if (isGrounded)
                {
                    _stepTimer += Time.deltaTime;
                    float interval = isSprinting ? stepIntervalSprint : stepIntervalWalk;
                    if (_stepTimer >= interval)
                    {
                        _stepTimer = 0f;
                        if (GameAudioManager.Instance != null)
                        {
                            GameAudioManager.Instance.PlayFootstep();
                        }
                    }
                }
            }
            else
            {
                _stepTimer = 0f;
            }

            // 4. Lompat
            if (_jumpBufferCounter > 0f && _coyoteCounter > 0f)
            {
                _velocity.y = jumpForce;
                _coyoteCounter = 0f;
                _jumpBufferCounter = 0f;

                if (GameAudioManager.Instance != null)
                {
                    GameAudioManager.Instance.PlayJumpSound();
                }
            }

            // 5. Gravitasi
            _velocity.y -= gravity * Time.deltaTime;

            // 6. Eksekusi Gerakan
            Vector3 totalMove = horizontalMove + Vector3.up * _velocity.y;
            _controller.Move(totalMove * Time.deltaTime);
        }

        public void LaunchUpward(float force)
        {
            _velocity.y = force;
            _coyoteCounter = 0f;
            _jumpBufferCounter = 0f;
        }

        private void CheckFallRespawn()
        {
            if (transform.position.y < fallThresholdY)
            {
                _controller.enabled = false;
                transform.position = spawnPoint + Vector3.up * 1.0f;
                _velocity = Vector3.zero;
                _jumpBufferCounter = 0f;
                _controller.enabled = true;
            }
        }

        private Vector2 ReadMovementInput()
        {
            float x = 0f;
            float y = 0f;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            x = Input.GetAxisRaw("Horizontal");
            y = Input.GetAxisRaw("Vertical");
#endif
            Vector2 touchInput = _touchControls != null ? _touchControls.Movement : Vector2.zero;
            return Vector2.ClampMagnitude(new Vector2(x, y) + touchInput, 1f);
        }

        private bool ReadSprintInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftShift);
#else
            return false;
#endif
        }

        private bool ReadJumpInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            bool keyboardPressed = kb != null && kb.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            bool keyboardPressed = Input.GetKeyDown(KeyCode.Space);
#else
            bool keyboardPressed = false;
#endif
            bool touchPressed = _touchControls != null && _touchControls.ConsumeJumpPressed();
            return keyboardPressed || touchPressed;
        }
    }
}
