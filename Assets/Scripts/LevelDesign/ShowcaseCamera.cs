using System.Collections.Generic;
using UnityEngine;

namespace EnchantedForest
{
    /// <summary>
    /// Kamera showcase sinematik otomatis untuk merekam video tugas level design.
    /// Kamera akan terbang halus (smooth flythrough) melewati setiap rute level dari Start ke Finish.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ShowcaseCamera : MonoBehaviour
    {
        [Header("Waypoints Rute")]
        [Tooltip("Daftar titik koordinat yang akan dilalui kamera secara berurutan.")]
        public List<Transform> waypoints = new List<Transform>();

        [Tooltip("Target fokus tatapan kamera (opsional, jika kosong kamera melihat ke arah waypoint berikutnya)")]
        public Transform lookTarget;

        [Header("Kecepatan & Durasi")]
        [Tooltip("Kecepatan gerak terbang kamera")]
        public float moveSpeed = 3.5f;

        [Tooltip("Kehalusan rotasi kamera")]
        public float rotationSmoothness = 3.0f;

        [Tooltip("Apakah kamera mengulang rute jika sudah sampai ujung?")]
        public bool loop = true;

        [Tooltip("Otomatis jalan saat tombol Play ditekan")]
        public bool playOnStart = true;

        private int _currentIndex = 0;
        private bool _isPlaying = false;

        private void Start()
        {
            GameModeManager.EnsurePlayableScene(gameObject);

            if (waypoints.Count > 0)
            {
                transform.position = waypoints[0].position;
                transform.rotation = waypoints[0].rotation;
            }

            if (playOnStart)
            {
                _isPlaying = true;
            }
        }

        private void Update()
        {
            // Tombol Space untuk pause/play kamera saat preview
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                _isPlaying = !_isPlaying;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _isPlaying = !_isPlaying;
            }
#endif

            if (!_isPlaying || waypoints == null || waypoints.Count < 2) return;

            Transform targetWaypoint = waypoints[_currentIndex];
            Vector3 targetPos = targetWaypoint.position;

            // 1. Gerakkan kamera menuju target waypoint saat ini
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

            // 2. Rotasi kamera halus ke arah target tatapan atau waypoint berikutnya
            Vector3 lookDirection;
            if (lookTarget != null)
            {
                lookDirection = lookTarget.position - transform.position;
            }
            else
            {
                lookDirection = targetPos - transform.position;
                if (lookDirection.sqrMagnitude < 0.1f && _currentIndex + 1 < waypoints.Count)
                {
                    lookDirection = waypoints[_currentIndex + 1].position - transform.position;
                }
            }

            if (lookDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmoothness * Time.deltaTime);
            }

            // 3. Cek jika sudah mendekati waypoint saat ini
            if (Vector3.Distance(transform.position, targetPos) < 0.3f)
            {
                _currentIndex++;
                if (_currentIndex >= waypoints.Count)
                {
                    if (loop)
                    {
                        _currentIndex = 0;
                    }
                    else
                    {
                        _isPlaying = false;
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Count < 2) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                if (waypoints[i] != null && waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                    Gizmos.DrawWireSphere(waypoints[i].position, 0.4f);
                }
            }
            if (waypoints.Count > 0 && waypoints[waypoints.Count - 1] != null)
            {
                Gizmos.DrawWireSphere(waypoints[waypoints.Count - 1].position, 0.4f);
            }
        }
    }
}
