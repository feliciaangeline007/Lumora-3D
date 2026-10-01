using UnityEngine;

namespace CoinConvoy
{
    // TWIST A - KONVOI BERBAHAYA
    // Ditempel ke: GameObject "ConvoySpawner"
    // Setiap N coin, satu musuh baru ditambahkan sampai batas maksimum
    public class ConvoySpawner : MonoBehaviour
    {
        [Header("Musuh yang di-spawn")]
        [SerializeField] private GameObject enemyPrefab;    // seret prefab EnemyCar
        [SerializeField] private Transform[] spawnPoints;   // titik spawn musuh tambahan

        [Header("Aturan konvoi")]
        [SerializeField] private int coinPerEnemy = 5;      // musuh baru tiap 5 coin
        [SerializeField] private int maxEnemies = 4;        // batas musuh

        private int spawnedCount;
        private int lastThreshold;

        private void Update()
        {
            if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0) return;
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.IsEnded) return;

            // Cek threshold baru setiap kali jumlah coin melewati kelipatan N
            int reached = gm.CollectedCoins / Mathf.Max(1, coinPerEnemy);
            if (reached <= lastThreshold) return;
            lastThreshold = reached;

            int activeEnemies = EnemyChaser.ActiveChasers.Count + PatrolEnemy.ActivePatrols.Count;
            if (activeEnemies >= Mathf.Max(1, maxEnemies)) return;
            if (spawnedCount >= spawnPoints.Length) return;

            Vector3 position = spawnPoints[spawnedCount].position;
            Quaternion rotation = spawnPoints[spawnedCount].rotation;
            Instantiate(enemyPrefab, position, rotation, transform);
            spawnedCount++;
        }
    }
}
