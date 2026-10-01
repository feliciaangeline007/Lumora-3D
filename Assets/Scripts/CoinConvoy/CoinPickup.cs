using System.Collections.Generic;
using UnityEngine;

namespace CoinConvoy
{
    public class CoinPickup : MonoBehaviour
    {
        public static readonly List<CoinPickup> ActiveCoins = new List<CoinPickup>();
        private const int PickupEffectPoolSize = 2;
        private static readonly ParticleSystem[] InspectorEffectPool = new ParticleSystem[PickupEffectPoolSize];
        private static readonly ParticleSystem.Burst[] PickupBursts =
        {
            new ParticleSystem.Burst(0f, 12)
        };
        private static readonly ParticleSystem[] PickupEffectPool = new ParticleSystem[PickupEffectPoolSize];
        private static AudioClip defaultPickupSound;
        private static Material pickupEffectMaterial;
        private static bool defaultPickupSoundLoaded;
        private static int nextPickupEffect;

        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private ParticleSystem pickupEffect;
        [SerializeField] private float rotateSpeed = 140f;
        [SerializeField] private float bobHeight = 0.18f;
        [SerializeField] private float bobSpeed = 3.5f;

        private Vector3 basePosition;
        private float randomOffset;
        private bool collected;

        private void Awake()
        {
            basePosition = transform.position;
            randomOffset = Random.Range(0f, Mathf.PI * 2f);

            Renderer coinRenderer = GetComponent<Renderer>();
            if (coinRenderer != null && coinRenderer.sharedMaterial != null &&
                coinRenderer.sharedMaterial.HasProperty("_EmissionColor"))
            {
                Material material = coinRenderer.sharedMaterial;
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", material.color * 0.9f);
            }
        }

        private void OnEnable()
        {
            if (!ActiveCoins.Contains(this))
                ActiveCoins.Add(this);
        }

        private void OnDisable()
        {
            ActiveCoins.Remove(this);
        }

        private void Update()
        {
            if (collected) return;

            // Smooth 3D rotation and vertical floating wave
            transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
            float newY = basePosition.y + Mathf.Sin(Time.time * bobSpeed + randomOffset) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        public void PullTowards(Vector3 targetPosition, float pullSpeed)
        {
            if (collected) return;
            Vector3 dir = (targetPosition - transform.position).normalized;
            transform.position += dir * pullSpeed * Time.deltaTime;
            basePosition = new Vector3(transform.position.x, basePosition.y, transform.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            if (!other.CompareTag("Player")) return;

            collected = true;
            ActiveCoins.Remove(this);

            if (GameManager.Instance != null)
                GameManager.Instance.CollectCoin();

            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, 0.9f);
            else
            {
                if (!defaultPickupSoundLoaded)
                {
                    defaultPickupSound = Resources.Load<AudioClip>("CoinPickup");
                    defaultPickupSoundLoaded = true;
                }
                if (defaultPickupSound != null)
                    AudioSource.PlayClipAtPoint(defaultPickupSound, transform.position, 0.9f);
            }

            PlayPickupEffect();

            Destroy(gameObject);
        }

        private void PlayPickupEffect()
        {
            if (pickupEffect != null)
            {
                for (int i = 0; i < InspectorEffectPool.Length; i++)
                {
                    ParticleSystem effect = InspectorEffectPool[i];
                    if (effect == null)
                    {
                        effect = Instantiate(pickupEffect, transform.position, Quaternion.identity);
                        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        InspectorEffectPool[i] = effect;
                    }
                    else if (effect.IsAlive(true))
                    {
                        continue;
                    }

                    effect.transform.position = transform.position;
                    effect.transform.rotation = Quaternion.identity;
                    effect.Play(true);
                    return;
                }
            }

            PlayDefaultPickupEffect();
        }

        private void PlayDefaultPickupEffect()
        {
            ParticleSystem effect = GetNextPickupEffect();
            if (effect == null) return;

            effect.transform.position = transform.position;
            effect.Clear(true);
            effect.Play(true);
        }

        private static ParticleSystem GetNextPickupEffect()
        {
            for (int attempt = 0; attempt < PickupEffectPool.Length; attempt++)
            {
                int index = (nextPickupEffect + attempt) % PickupEffectPool.Length;
                if (PickupEffectPool[index] == null)
                    PickupEffectPool[index] = CreatePickupEffect(index);
                if (PickupEffectPool[index] != null)
                {
                    nextPickupEffect = (index + 1) % PickupEffectPool.Length;
                    return PickupEffectPool[index];
                }
            }
            return null;
        }

        private static ParticleSystem CreatePickupEffect(int index)
        {
            GameObject effectObject = new GameObject("CoinPickupBurst_" + index);
            ParticleSystem effect = effectObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = effect.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.25f;
            main.maxParticles = 24;
            main.startLifetime = 0.45f;
            main.startSpeed = 2.4f;
            main.startSize = 0.12f;
            main.startColor = UITheme.ColorAccentGold;

            ParticleSystem.EmissionModule emission = effect.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(PickupBursts);

            ParticleSystem.ShapeModule shape = effect.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            ParticleSystemRenderer particleRenderer = effectObject.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleRenderer != null && shader != null)
            {
                if (pickupEffectMaterial == null)
                    pickupEffectMaterial = new Material(shader) { enableInstancing = true };
                particleRenderer.sharedMaterial = pickupEffectMaterial;
            }

            return effect;
        }
    }
}
