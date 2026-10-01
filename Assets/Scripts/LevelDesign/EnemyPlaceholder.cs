using UnityEngine;

namespace EnchantedForest
{
    /// <summary>
    /// Script visual untuk penempatan musuh (patroli bolak-balik sederhana + animasi idle bernapas/berputar).
    /// </summary>
    [SelectionBase]
    public class EnemyPlaceholder : MonoBehaviour
    {
        public enum EnemyType
        {
            WoodlingScout,
            ArachnidWeaver,
            ThornSpitter,
            AncientColossus
        }

        [Header("Tipe Musuh")]
        public EnemyType enemyType = EnemyType.WoodlingScout;

        [Header("Patroli / Gerakan")]
        public bool canPatrol = true;
        public float patrolDistance = 4f;
        public float patrolSpeed = 2f;
        public Vector3 patrolAxis = Vector3.forward;

        [Header("Animasi Idle / Tampilan")]
        public float breathingSpeed = 3f;
        public float breathingScale = 0.05f;
        public float scanRotateSpeed = 45f;

        private Vector3 _startPos;
        private Vector3 _baseScale;
        private bool _movingPositive = true;

        private void Start()
        {
            _startPos = transform.position;
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            switch (enemyType)
            {
                case EnemyType.WoodlingScout:
                    UpdateWoodling();
                    break;
                case EnemyType.ArachnidWeaver:
                    UpdateArachnid();
                    break;
                case EnemyType.ThornSpitter:
                    UpdateThornSpitter();
                    break;
                case EnemyType.AncientColossus:
                    UpdateColossus();
                    break;
            }
        }

        private void UpdateWoodling()
        {
            float breathe = 1f + Mathf.Sin(Time.time * breathingSpeed) * breathingScale;
            transform.localScale = new Vector3(_baseScale.x * breathe, _baseScale.y, _baseScale.z * breathe);

            if (canPatrol && patrolDistance > 0f)
            {
                Vector3 target = _startPos + (patrolAxis.normalized * (_movingPositive ? patrolDistance : -patrolDistance));
                transform.position = Vector3.MoveTowards(transform.position, target, patrolSpeed * Time.deltaTime);

                Vector3 moveDir = (target - transform.position).normalized;
                if (moveDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.deltaTime);
                }

                if (Vector3.Distance(transform.position, target) < 0.1f)
                {
                    _movingPositive = !_movingPositive;
                }
            }
        }

        private void UpdateArachnid()
        {
            // Laba-laba turun-naik perlahan di benang sutra
            float yOffset = Mathf.Sin(Time.time * patrolSpeed) * (patrolDistance * 0.5f);
            transform.position = _startPos + Vector3.up * yOffset;
            transform.Rotate(Vector3.up, 30f * Time.deltaTime);
        }

        private void UpdateThornSpitter()
        {
            // Tanaman penembak mengembang lalu mengempis seolah siap menyembur
            float spitCharge = Mathf.PingPong(Time.time * 2f, 1f);
            transform.localScale = new Vector3(_baseScale.x * (1f + spitCharge * 0.15f), _baseScale.y * (1f + spitCharge * 0.25f), _baseScale.z * (1f + spitCharge * 0.15f));
            transform.Rotate(Vector3.up, scanRotateSpeed * Time.deltaTime);
        }

        private void UpdateColossus()
        {
            // Golem kolosal bergetar megah dengan stomping rhythm
            float stomp = Mathf.Abs(Mathf.Sin(Time.time * 1.5f));
            transform.localScale = new Vector3(_baseScale.x, _baseScale.y * (0.95f + stomp * 0.05f), _baseScale.z);
            transform.Rotate(Vector3.up, 15f * Time.deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = Application.isPlaying ? _startPos : transform.position;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, 0.5f);
            if (canPatrol)
            {
                Gizmos.DrawLine(center - patrolAxis.normalized * patrolDistance, center + patrolAxis.normalized * patrolDistance);
            }
        }
    }
}
