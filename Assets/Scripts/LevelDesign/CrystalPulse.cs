using UnityEngine;

namespace EnchantedForest
{
    /// <summary>
    /// Efek emissive berdenyut (pulse) pada Kristal Lumora.
    /// Cocok untuk kristal checkpoint, kristal dinding gua, dan kristal tujuan akhir.
    /// Tidak memerlukan Point Light tambahan — emissive sendiri sudah cukup untuk tampilan video.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class CrystalPulse : MonoBehaviour
    {
        // ── Warna & Intensitas ────────────────────────────────────────────────
        [Header("Warna Kristal")]
        [Tooltip("Warna dasar kristal Lumora (default cyan #4FF5D8)")]
        public Color crystalColor = new Color(0.31f, 0.96f, 0.85f); // #4FF5D8

        [Tooltip("Intensitas emissive minimum saat denyutan paling redup")]
        [Range(0.5f, 3f)]
        public float minEmission = 0.8f;

        [Tooltip("Intensitas emissive maksimum saat denyutan paling terang")]
        [Range(1f, 8f)]
        public float maxEmission = 3.5f;

        // ── Kecepatan Denyut ─────────────────────────────────────────────────
        [Header("Kecepatan Denyut")]
        [Tooltip("Kecepatan denyutan. Nilai lebih tinggi = lebih cepat berdenyut.")]
        [Range(0.3f, 5f)]
        public float pulseSpeed = 1.4f;

        [Tooltip("Acak offset waktu agar kristal-kristal yang berdekatan tidak berdenyut bersamaan")]
        public bool randomizeOffset = true;

        // ── Rotasi Pelan (opsional) ───────────────────────────────────────────
        [Header("Rotasi Lambat (Opsional)")]
        [Tooltip("Aktifkan agar kristal berputar pelan menambah efek visual")]
        public bool enableSlowRotation = true;

        [Tooltip("Kecepatan rotasi Y (derajat per detik)")]
        [Range(0f, 60f)]
        public float rotationSpeed = 12f;

        // ── Ukuran Bernapas (skala naik-turun kecil) ─────────────────────────
        [Header("Efek Bernapas (Scale)")]
        [Tooltip("Aktifkan efek skala naik-turun kecil seperti kristal bernapas")]
        public bool enableBreathing = true;

        [Tooltip("Amplitudo perubahan skala (nilai kecil = halus)")]
        [Range(0f, 0.15f)]
        public float breathingScale = 0.05f;

        // ── Private ──────────────────────────────────────────────────────────
        private Renderer _renderer;
        private MaterialPropertyBlock _mpb;
        private Vector3 _baseScale;
        private float _timeOffset;

        // Nama property emissive di shader URP Lit dan Standard
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();
            _baseScale = transform.localScale;

            if (randomizeOffset)
            {
                // Offset berbeda per posisi agar tiap kristal terasa hidup sendiri
                _timeOffset = (transform.position.x * 1.37f + transform.position.z * 0.83f) % (2f * Mathf.PI);
            }

            // Pastikan emissive keyword aktif pada material
            foreach (Material mat in _renderer.sharedMaterials)
            {
                if (mat != null)
                {
                    mat.EnableKeyword("_EMISSION");
                }
            }
        }

        private void Update()
        {
            float t = Mathf.Sin((Time.time + _timeOffset) * pulseSpeed);
            // Normalisasi sin dari [-1,1] ke [0,1]
            float normalized = (t + 1f) * 0.5f;
            float emission = Mathf.Lerp(minEmission, maxEmission, normalized);

            // Terapkan warna emissive via MaterialPropertyBlock (tidak memodifikasi asset material)
            _mpb.SetColor(EmissionColorID, crystalColor * emission);
            _renderer.SetPropertyBlock(_mpb);

            // Efek bernapas skala
            if (enableBreathing)
            {
                float breathe = 1f + Mathf.Sin((Time.time + _timeOffset) * pulseSpeed * 0.7f) * breathingScale;
                transform.localScale = _baseScale * breathe;
            }

            // Rotasi lambat
            if (enableSlowRotation && rotationSpeed > 0f)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
            }
        }

        private void OnDisable()
        {
            // Kembalikan emissive ke minimum saat dinonaktifkan agar tidak "ngehang" terang
            if (_renderer != null && _mpb != null)
            {
                _mpb.SetColor(EmissionColorID, crystalColor * minEmission);
                _renderer.SetPropertyBlock(_mpb);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Tampilkan range visual di editor
            Gizmos.color = new Color(crystalColor.r, crystalColor.g, crystalColor.b, 0.25f);
            Gizmos.DrawWireSphere(transform.position, 2.5f);

            UnityEditor.Handles.color = new Color(0.31f, 0.96f, 0.85f, 0.8f);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, "✦ Kristal Lumora");
        }
#endif
    }
}
