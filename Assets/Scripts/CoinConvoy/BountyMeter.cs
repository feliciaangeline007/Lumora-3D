using TMPro;
using UnityEngine;

namespace CoinConvoy
{
    // TWIST C - BURONAN BERNILAI
    // Ditempel ke: GameObject "BountyMeter"
    // Menampilkan level buronan dan (opsional) panah arah musuh terdekat
    public class BountyMeter : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text label;          // "BURONAN LV 3"
        [SerializeField] private int coinsPerLevel = 3;   // naik level tiap 3 coin
        [SerializeField] private int maxLevel = 6;

        [Header("Panah bahaya (opsional)")]
        [SerializeField] private Transform dangerArrow;   // anak Main Camera
        [SerializeField] private float refreshInterval = 1f;
        [SerializeField] private float rotateSpeed = 8f;
        [SerializeField] private int arrowShowLevel = 3;  // panah muncul mulai level ini

        private int lastLevel = -1;
        private float refreshTimer;
        private Transform nearestEnemy;
        private Renderer dangerArrowRenderer;
        private bool dangerArrowRendererCached;

        public int Level { get; private set; }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.IsEnded) return;

            // Hitung level dari coin terkumpul
            Level = Mathf.Min(Mathf.Max(1, maxLevel), 1 + gm.CollectedCoins / Mathf.Max(1, coinsPerLevel));
            if (Level != lastLevel && label != null)
            {
                lastLevel = Level;
                label.text = $"BURONAN  LV {Level}";
            }

            UpdateDangerArrow();
        }

        private void UpdateDangerArrow()
        {
            if (dangerArrow == null) return;
            // Refresh daftar musuh hanya tiap 1 detik (bukan tiap frame)
            refreshTimer -= Time.deltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = Mathf.Max(0.1f, refreshInterval);
                nearestEnemy = FindNearestEnemy();
            }
            bool show = Level >= arrowShowLevel && nearestEnemy != null;
            if (!dangerArrowRendererCached)
            {
                dangerArrowRenderer = dangerArrow.GetComponentInChildren<Renderer>(true);
                dangerArrowRendererCached = true;
            }
            if (dangerArrowRenderer != null) dangerArrowRenderer.enabled = show;
            if (!show) return;

            Vector3 direction = nearestEnemy.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) return;
            Quaternion look = Quaternion.LookRotation(direction.normalized);
            dangerArrow.rotation = Quaternion.Slerp(dangerArrow.rotation, look, rotateSpeed * Time.deltaTime);
        }

        // Cari musuh terdekat sesekali saja agar hemat
        private Transform FindNearestEnemy()
        {
            Transform nearest = null;
            float best = float.MaxValue;
            for (int i = 0; i < EnemyChaser.ActiveChasers.Count; i++)
            {
                EnemyChaser enemy = EnemyChaser.ActiveChasers[i];
                if (enemy == null) continue;
                float distance = (enemy.transform.position - transform.position).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = enemy.transform;
                }
            }
            for (int i = 0; i < PatrolEnemy.ActivePatrols.Count; i++)
            {
                PatrolEnemy enemy = PatrolEnemy.ActivePatrols[i];
                if (enemy == null) continue;
                float distance = (enemy.transform.position - transform.position).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = enemy.transform;
                }
            }
            return nearest;
        }
    }
}
