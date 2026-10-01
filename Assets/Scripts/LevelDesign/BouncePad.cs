using UnityEngine;

namespace EnchantedForest
{
    /// <summary>
    /// Jamur Pegas (Spore Bounce Pad) - Melontarkan karakter ke atas dengan animasi squish-and-stretch yang memukau.
    /// </summary>
    [SelectionBase]
    public class BouncePad : MonoBehaviour
    {
        [Header("Pengaturan Lontaran")]
        public float bounceForce = 15f;

        [Header("Animasi Pantulan")]
        public float squishDuration = 0.4f;
        public Vector3 squishScale = new Vector3(1.3f, 0.5f, 1.3f);

        private Vector3 _baseScale;
        private float _animTimer;
        private bool _isBouncing = false;

        private void Start()
        {
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            if (_isBouncing)
            {
                _animTimer += Time.deltaTime;
                float progress = _animTimer / squishDuration;

                if (progress <= 0.4f)
                {
                    // Menekan ke bawah (Squish)
                    float t = progress / 0.4f;
                    transform.localScale = Vector3.Lerp(_baseScale, squishScale, t);
                }
                else if (progress <= 1f)
                {
                    // Memantul kembali (Overshoot & Normal)
                    float t = (progress - 0.4f) / 0.6f;
                    Vector3 stretch = new Vector3(_baseScale.x * 0.9f, _baseScale.y * 1.25f, _baseScale.z * 0.9f);
                    transform.localScale = Vector3.Lerp(stretch, _baseScale, t);
                }
                else
                {
                    transform.localScale = _baseScale;
                    _isBouncing = false;
                }
            }
        }

        public void TriggerBounce()
        {
            _isBouncing = true;
            _animTimer = 0f;

            if (GameAudioManager.Instance != null)
            {
                GameAudioManager.Instance.PlayBounceSound(transform.position);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TriggerBounce();

            var player = other.GetComponent<PlayerController3D>();
            if (player != null)
            {
                player.LaunchUpward(bounceForce);
                return;
            }

            var rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, bounceForce, rb.linearVelocity.z);
            }
        }
    }
}
