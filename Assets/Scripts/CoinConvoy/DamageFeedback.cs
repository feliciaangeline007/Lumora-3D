using System.Collections;
using UnityEngine;

namespace CoinConvoy
{
    public class DamageFeedback : MonoBehaviour
    {
        [Header("Kebal Setelah Kena")]
        [SerializeField] private float invulnerabilityDuration = 2f; // Jeda kebal
        [SerializeField] private float flashInterval = 0.12f;        // Kecepatan kedip

        [Header("Efek")]
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip shieldAbsorbSound;
        [SerializeField] private GameObject shieldVisual;

        private Renderer[] renderers;
        private bool[] rendererInitialStates;
        private bool invulnerable;
        private bool shielded;   // Perisai menahan 1 damage
        private GameObject proceduralShield;

        public bool IsShielded => shielded;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            rendererInitialStates = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                rendererInitialStates[i] = renderers[i] != null && renderers[i].enabled;
        }

        private void Update()
        {
            // Spin shield visual if active
            if (shielded)
            {
                GameObject activeShield = shieldVisual != null ? shieldVisual : proceduralShield;
                if (activeShield != null)
                {
                    activeShield.transform.Rotate(30f * Time.deltaTime, 60f * Time.deltaTime, 15f * Time.deltaTime, Space.Self);
                    float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.08f;
                    activeShield.transform.localScale = Vector3.one * (2.8f * pulse);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (invulnerable) return;
            if (!collision.collider.CompareTag("Enemy")) return;

            invulnerable = true;
            if (FollowCamera.Instance != null) FollowCamera.Instance.Shake();
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif

            if (shielded)
            {
                // Shield absorbs complete damage
                shielded = false;
                if (shieldVisual != null) shieldVisual.SetActive(false);
                if (proceduralShield != null) proceduralShield.SetActive(false);

                if (shieldAbsorbSound != null)
                    AudioSource.PlayClipAtPoint(shieldAbsorbSound, transform.position, 1f);
                else if (hitSound != null)
                    AudioSource.PlayClipAtPoint(hitSound, transform.position, 0.7f);

                if (HudManager.Instance != null)
                    HudManager.Instance.ShowPowerUpNotification("SHIELD BROKEN!", UITheme.ColorDangerRed);

                StartCoroutine(FlashRoutine(1f));
                return;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TakeDamage();
            }

            if (hitSound != null)
                AudioSource.PlayClipAtPoint(hitSound, transform.position, 0.9f);

            StartCoroutine(FlashRoutine(invulnerabilityDuration));
        }

        public void ActivateShield()
        {
            shielded = true;
            EnsureShieldVisual();
            if (shieldVisual != null) shieldVisual.SetActive(true);
            if (proceduralShield != null) proceduralShield.SetActive(true);
        }

        private void EnsureShieldVisual()
        {
            if (shieldVisual != null || proceduralShield != null) return;

            proceduralShield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proceduralShield.name = "ShieldBubbleVisual";
            proceduralShield.transform.SetParent(transform, false);
            proceduralShield.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            proceduralShield.transform.localScale = Vector3.one * 2.8f;

            Collider col = proceduralShield.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer ren = proceduralShield.GetComponent<Renderer>();
            if (ren != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
                mat.color = new Color(0.2f, 0.8f, 1f, 0.35f);
                // Set transparent render mode if standard
                mat.SetFloat("_Surface", 1); // Transparent in URP
                ren.material = mat;
            }
        }

        private IEnumerator FlashRoutine(float duration)
        {
            float elapsed = 0f;
            bool visible = false;
            while (elapsed < duration)
            {
                elapsed += flashInterval;
                visible = !visible;
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer item = renderers[i];
                    if (item != null && item.gameObject != proceduralShield && item.gameObject != shieldVisual)
                        item.enabled = rendererInitialStates[i] && visible;
                }
                yield return new WaitForSecondsRealtime(flashInterval);
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].enabled = rendererInitialStates[i];
            }
            invulnerable = false;
        }
    }
}
