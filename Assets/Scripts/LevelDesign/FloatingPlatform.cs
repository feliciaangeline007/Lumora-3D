using UnityEngine;

namespace EnchantedForest
{
    /// <summary>
    /// Platform bergerak (horizontal / vertikal / floating) dengan transisi halus (SmoothStep / Lerp).
    /// </summary>
    [SelectionBase]
    public class FloatingPlatform : MonoBehaviour
    {
        public enum MoveDirection
        {
            HorizontalX,
            HorizontalZ,
            VerticalY,
            CustomOffset
        }

        [Header("Pergerakan")]
        [Tooltip("Arah pergerakan platform")]
        public MoveDirection direction = MoveDirection.HorizontalX;

        [Tooltip("Jarak tempuh dari titik awal")]
        public float travelDistance = 6f;

        [Tooltip("Waktu tempuh dari ujung ke ujung (detik)")]
        public float moveDuration = 3f;

        [Tooltip("Jeda berhenti di setiap ujung (detik)")]
        public float waitTimeAtEnds = 0.5f;

        [Header("Custom Offset (Bila memilih tipe Custom)")]
        public Vector3 customOffset = new Vector3(0, 5f, 0);

        private Vector3 _startPos;
        private Vector3 _targetPos;
        private float _timer;
        private bool _movingToTarget = true;
        private float _pauseTimer;

        private void Start()
        {
            _startPos = transform.position;

            Vector3 delta = Vector3.zero;
            switch (direction)
            {
                case MoveDirection.HorizontalX:
                    delta = Vector3.right * travelDistance;
                    break;
                case MoveDirection.HorizontalZ:
                    delta = Vector3.forward * travelDistance;
                    break;
                case MoveDirection.VerticalY:
                    delta = Vector3.up * travelDistance;
                    break;
                case MoveDirection.CustomOffset:
                    delta = customOffset;
                    break;
            }
            _targetPos = _startPos + delta;
        }

        private void Update()
        {
            if (_pauseTimer > 0f)
            {
                _pauseTimer -= Time.deltaTime;
                return;
            }

            _timer += Time.deltaTime;
            float progress = Mathf.Clamp01(_timer / moveDuration);

            // Gerakan halus percepatan-perlambatan (SmoothStep)
            float smooth = Mathf.SmoothStep(0f, 1f, progress);

            if (_movingToTarget)
            {
                transform.position = Vector3.Lerp(_startPos, _targetPos, smooth);
            }
            else
            {
                transform.position = Vector3.Lerp(_targetPos, _startPos, smooth);
            }

            if (progress >= 1f)
            {
                _timer = 0f;
                _movingToTarget = !_movingToTarget;
                _pauseTimer = waitTimeAtEnds;
            }
        }

        // Supaya player / objek di atas platform ikut bergerak
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                collision.transform.SetParent(transform);
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                collision.transform.SetParent(null);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 start = Application.isPlaying ? _startPos : transform.position;
            Vector3 delta = Vector3.zero;
            switch (direction)
            {
                case MoveDirection.HorizontalX: delta = Vector3.right * travelDistance; break;
                case MoveDirection.HorizontalZ: delta = Vector3.forward * travelDistance; break;
                case MoveDirection.VerticalY: delta = Vector3.up * travelDistance; break;
                case MoveDirection.CustomOffset: delta = customOffset; break;
            }
            Vector3 target = start + delta;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(start, target);
            Gizmos.DrawWireCube(start, transform.localScale);
            Gizmos.DrawWireCube(target, transform.localScale);
        }
    }
}
