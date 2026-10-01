using UnityEngine;

namespace EnchantedForest
{
    /// <summary>
    /// Animasi koin berputar dan mengambang halus (bobbing).
    /// Sangat cocok untuk collectible di level design 3D.
    /// </summary>
    [SelectionBase]
    public class CoinRotator : MonoBehaviour
    {
        [Header("Rotasi")]
        [Tooltip("Kecepatan putaran koin (derajat per detik)")]
        public float rotateSpeed = 120f;
        public Vector3 rotateAxis = Vector3.up;

        [Header("Floating / Bobbing")]
        [Tooltip("Tinggi ayunan naik-turun")]
        public float bobHeight = 0.25f;
        [Tooltip("Kecepatan ayunan naik-turun")]
        public float bobSpeed = 2.5f;

        [Header("Partikel / Efek (Opsional)")]
        public ParticleSystem collectEffectPrefab;

        private Vector3 _startPosition;
        private float _timeOffset;

        private void Start()
        {
            _startPosition = transform.position;
            // Acak offset agar kumpulan koin tidak bergerak barengan secara kaku
            _timeOffset = (transform.position.x + transform.position.z) * 0.5f;
        }

        private void Update()
        {
            // 1. Rotasi koin
            transform.Rotate(rotateAxis, rotateSpeed * Time.deltaTime, Space.World);

            // 2. Mengambang naik turun (Sine wave)
            float newY = _startPosition.y + Mathf.Sin((Time.time + _timeOffset) * bobSpeed) * bobHeight;
            transform.position = new Vector3(_startPosition.x, newY, _startPosition.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Jika tersentuh player
            if (other.CompareTag("Player") || other.GetComponent<PlayerController3D>() != null)
            {
                if (GameAudioManager.Instance != null)
                {
                    GameAudioManager.Instance.PlayCoinSound(transform.position);
                }

                if (GameModeManager.Instance != null)
                {
                    GameModeManager.Instance.AddCoin();
                }

                if (collectEffectPrefab != null)
                {
                    Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
                }
                gameObject.SetActive(false);
            }
        }
    }
}
