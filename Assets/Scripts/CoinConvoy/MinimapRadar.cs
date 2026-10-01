using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoinConvoy
{
    /// <summary>
    /// High-performance, zero-allocation circular mini-radar for Coin Convoy.
    /// Plots the player (center arrow), coins (gold), enemies (pulsing red),
    /// powerups (color-coded), and the escape gate (clamped beacon).
    /// </summary>
    public class MinimapRadar : MonoBehaviour
    {
        public static MinimapRadar Instance { get; private set; }

        [Header("Konfigurasi Radar")]
        [SerializeField] private float worldRadarRadius = 38f; // Jarak pandang dunia dalam radius radar
        [SerializeField] private RectTransform blipContainer;
        [SerializeField] private RectTransform playerArrow;
        [SerializeField] private Image gateMarker;

        private Transform playerTransform;
        private EscapeGate escapeGate;
        private float radarPixelRadius = 75f;
        private float entitySearchTimer;
        [SerializeField] private float entitySearchInterval = 1f;

        // Pooled blip image objects
        private readonly List<Image> coinBlips = new List<Image>();
        private readonly List<Image> enemyBlips = new List<Image>();
        private readonly List<Image> powerUpBlips = new List<Image>();

        private Sprite circleSprite;

        private void Awake()
        {
            Instance = this;
            entitySearchTimer = entitySearchInterval;
            circleSprite = Resources.Load<Sprite>("UI/CC_Circle") ??
                           Resources.Load<Sprite>("CC_Circle");
            if (blipContainer != null)
                radarPixelRadius = blipContainer.rect.width * 0.45f;
        }

        private void Start()
        {
            FindEntities();
        }

        private void FindEntities()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
            if (escapeGate == null)
            {
                escapeGate = FindFirstObjectByType<EscapeGate>();
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                entitySearchTimer -= Time.deltaTime;
                if (entitySearchTimer <= 0f)
                {
                    entitySearchTimer = Mathf.Max(0.1f, entitySearchInterval);
                    FindEntities();
                }
                if (playerTransform == null) return;
            }

            // Update player heading arrow
            if (playerArrow != null)
            {
                float heading = -playerTransform.eulerAngles.y;
                playerArrow.localRotation = Quaternion.Euler(0f, 0f, heading);
            }

            Vector3 playerPos = playerTransform.position;

            // Render Coins
            RenderCoinBlips(playerPos);

            // Render Enemies
            RenderEnemyBlips(playerPos);

            // Render PowerUps
            RenderPowerUpBlips(playerPos);

            // Render Escape Gate
            RenderEscapeGate(playerPos);
        }

        private void RenderCoinBlips(Vector3 playerPos)
        {
            var coins = CoinPickup.ActiveCoins;
            EnsureBlipPool(coinBlips, coins.Count, UITheme.ColorAccentGold, 7f);

            for (int i = 0; i < coinBlips.Count; i++)
            {
                if (i < coins.Count && coins[i] != null)
                {
                    Vector2 radarPos = WorldToRadarPos(coins[i].transform.position, playerPos, out bool isClamped);
                    coinBlips[i].gameObject.SetActive(true);
                    coinBlips[i].rectTransform.anchoredPosition = radarPos;
                    coinBlips[i].rectTransform.sizeDelta = isClamped ? new Vector2(5f, 5f) : new Vector2(8f, 8f);
                }
                else
                {
                    coinBlips[i].gameObject.SetActive(false);
                }
            }
        }

        private void RenderEnemyBlips(Vector3 playerPos)
        {
            var chasers = EnemyChaser.ActiveChasers;
            var patrols = PatrolEnemy.ActivePatrols;
            int totalEnemies = chasers.Count + patrols.Count;

            EnsureBlipPool(enemyBlips, totalEnemies, UITheme.ColorDangerRed, 10f);

            int index = 0;
            float pulseScale = 1f + Mathf.Sin(Time.time * 8f) * 0.2f;

            for (int i = 0; i < chasers.Count; i++)
            {
                if (chasers[i] != null)
                {
                    Vector2 radarPos = WorldToRadarPos(chasers[i].transform.position, playerPos, out bool isClamped);
                    enemyBlips[index].gameObject.SetActive(true);
                    enemyBlips[index].rectTransform.anchoredPosition = radarPos;
                    enemyBlips[index].color = UITheme.ColorDangerRed;
                    enemyBlips[index].rectTransform.sizeDelta = Vector2.one * (10f * (isClamped ? 0.8f : pulseScale));
                    index++;
                }
            }

            for (int i = 0; i < patrols.Count; i++)
            {
                if (patrols[i] != null)
                {
                    Vector2 radarPos = WorldToRadarPos(patrols[i].transform.position, playerPos, out bool isClamped);
                    enemyBlips[index].gameObject.SetActive(true);
                    enemyBlips[index].rectTransform.anchoredPosition = radarPos;
                    enemyBlips[index].color = patrols[i].IsChasing ? UITheme.ColorDangerRed : UITheme.ColorAccentGold;
                    enemyBlips[index].rectTransform.sizeDelta = Vector2.one * (9f * (isClamped ? 0.8f : 1f));
                    index++;
                }
            }

            for (int i = index; i < enemyBlips.Count; i++)
            {
                enemyBlips[i].gameObject.SetActive(false);
            }
        }

        private void RenderPowerUpBlips(Vector3 playerPos)
        {
            var powers = PowerUp.ActivePowerUps;
            EnsureBlipPool(powerUpBlips, powers.Count, UITheme.ColorTechCyan, 9f);

            for (int i = 0; i < powerUpBlips.Count; i++)
            {
                if (i < powers.Count && powers[i] != null)
                {
                    Vector2 radarPos = WorldToRadarPos(powers[i].transform.position, playerPos, out bool isClamped);
                    powerUpBlips[i].gameObject.SetActive(true);
                    powerUpBlips[i].rectTransform.anchoredPosition = radarPos;

                    Color blipCol = UITheme.ColorTechCyan;
                    switch (powers[i].Type)
                    {
                        case PowerUp.PowerType.Nitro: blipCol = UITheme.ColorAccentGold; break;
                        case PowerUp.PowerType.Shield: blipCol = UITheme.ColorTechCyan; break;
                        case PowerUp.PowerType.Magnet: blipCol = new Color(0.85f, 0.4f, 1f); break;
                        case PowerUp.PowerType.ExtraLife: blipCol = UITheme.ColorPrimaryGreen; break;
                    }
                    powerUpBlips[i].color = blipCol;
                }
                else
                {
                    powerUpBlips[i].gameObject.SetActive(false);
                }
            }
        }

        private void RenderEscapeGate(Vector3 playerPos)
        {
            if (gateMarker == null || escapeGate == null) return;

            Vector2 radarPos = WorldToRadarPos(escapeGate.transform.position, playerPos, out bool isClamped);
            gateMarker.rectTransform.anchoredPosition = radarPos;

            bool isOpen = GameManager.Instance != null && GameManager.Instance.GateOpen;
            if (isOpen)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.25f;
                gateMarker.color = UITheme.ColorPrimaryGreen;
                gateMarker.rectTransform.sizeDelta = Vector2.one * (16f * pulse);
            }
            else
            {
                gateMarker.color = new Color(0.6f, 0.65f, 0.75f, 0.7f);
                gateMarker.rectTransform.sizeDelta = Vector2.one * 12f;
            }
        }

        private Vector2 WorldToRadarPos(Vector3 worldTarget, Vector3 worldCenter, out bool clamped)
        {
            float dx = worldTarget.x - worldCenter.x;
            float dz = worldTarget.z - worldCenter.z;

            float factor = radarPixelRadius / worldRadarRadius;
            Vector2 local = new Vector2(dx * factor, dz * factor);

            float dist = local.magnitude;
            if (dist > radarPixelRadius)
            {
                local = local.normalized * radarPixelRadius;
                clamped = true;
            }
            else
            {
                clamped = false;
            }
            return local;
        }

        private void EnsureBlipPool(List<Image> pool, int needed, Color defaultColor, float defaultSize)
        {
            while (pool.Count < needed)
            {
                GameObject blipObj = new GameObject("Blip_" + pool.Count);
                blipObj.transform.SetParent(blipContainer, false);
                Image img = blipObj.AddComponent<Image>();
                if (circleSprite != null) img.sprite = circleSprite;
                img.color = defaultColor;
                img.raycastTarget = false;
                img.rectTransform.sizeDelta = Vector2.one * defaultSize;
                pool.Add(img);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
