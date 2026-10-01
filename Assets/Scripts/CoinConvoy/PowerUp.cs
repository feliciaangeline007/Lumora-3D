using System.Collections.Generic;
using UnityEngine;

namespace CoinConvoy
{
    public class PowerUp : MonoBehaviour
    {
        public static readonly List<PowerUp> ActivePowerUps = new List<PowerUp>();

        public enum PowerType { Nitro, Shield, Magnet, ExtraLife }

        [Header("Jenis")]
        [SerializeField] private PowerType type = PowerType.Nitro;

        [Header("Efek")]
        [SerializeField] private float nitroBoost = 1.8f;      // Pengali kecepatan Nitro
        [SerializeField] private float nitroDuration = 6f;
        [SerializeField] private float magnetRange = 14f;       // Jarak magnet menarik coin
        [SerializeField] private float magnetDuration = 8f;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private ParticleSystem pickupEffect;

        [Header("Visual Floating")]
        [SerializeField] private float rotateSpeed = 100f;
        [SerializeField] private float bobHeight = 0.22f;
        [SerializeField] private float bobSpeed = 3f;

        private Collider triggerCollider;
        private Vector3 basePosition;
        private float randomOffset;
        private bool collected;

        public PowerType Type => type;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null) triggerCollider.isTrigger = true;
            basePosition = transform.position;
            randomOffset = Random.Range(0f, Mathf.PI * 2f);

            Light[] localLights = GetComponentsInChildren<Light>(true);
            foreach (Light localLight in localLights)
            {
                localLight.enabled = false;
                localLight.shadows = LightShadows.None;
            }

            Renderer visualRenderer = GetComponentInChildren<Renderer>();
            if (visualRenderer != null && visualRenderer.sharedMaterial != null)
            {
                Material material = visualRenderer.sharedMaterial;
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", material.color * 1.25f);
                }
            }
        }

        private void OnEnable()
        {
            if (!ActivePowerUps.Contains(this))
                ActivePowerUps.Add(this);
        }

        private void OnDisable()
        {
            ActivePowerUps.Remove(this);
        }

        private void Update()
        {
            if (collected) return;
            transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
            float newY = basePosition.y + Mathf.Sin(Time.time * bobSpeed + randomOffset) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            if (!other.CompareTag("Player")) return;

            CarController car = other.GetComponentInParent<CarController>();
            if (car == null) return;

            collected = true;
            ActivePowerUps.Remove(this);

            Apply(car, car.gameObject);

            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, 0.9f);

            if (pickupEffect != null)
                Instantiate(pickupEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }

        private void Apply(CarController car, GameObject player)
        {
            string powerName = "";
            Color notificationColor = Color.white;

            switch (type)
            {
                case PowerType.Nitro:
                    car.StartNitro(nitroBoost, nitroDuration);
                    powerName = "NITRO BOOST ACTIVATED!";
                    notificationColor = UITheme.ColorAccentGold;
                    break;

                case PowerType.Shield:
                    DamageFeedback feedback = player.GetComponentInChildren<DamageFeedback>();
                    if (feedback != null) feedback.ActivateShield();
                    powerName = "ENERGY SHIELD ACTIVE!";
                    notificationColor = UITheme.ColorTechCyan;
                    break;

                case PowerType.Magnet:
                    car.StartMagnet(magnetRange, magnetDuration);
                    powerName = "COIN MAGNET ONLINE!";
                    notificationColor = new Color(0.85f, 0.4f, 1f);
                    break;

                case PowerType.ExtraLife:
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.AddLife(1);
                        powerName = "EXTRA LIFE RESTORED!";
                        notificationColor = UITheme.ColorPrimaryGreen;
                    }
                    break;
            }

            if (HudManager.Instance != null && !string.IsNullOrEmpty(powerName))
            {
                HudManager.Instance.ShowPowerUpNotification(powerName, notificationColor);
            }
        }
    }
}
