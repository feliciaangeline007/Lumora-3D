using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoinConvoy
{
    /// <summary>
    /// Core gameplay loop controller. Handles lives, score, escape timer, coin targets,
    /// high score persistence, and interfaces smoothly with both legacy HUD texts and
    /// the modernized HudManager component.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Aturan")]
        [SerializeField] private int startingLives = 3;
        [SerializeField] private int pointsPerCoin = 10;
        [SerializeField] private int targetCoins = 15;

        [Header("Misi Escape")]
        [SerializeField] private bool useTimer = true;
        [SerializeField] private float timeLimit = 180f;      // Detik
        [SerializeField] private int bonusPerSecond = 2;       // Bonus skor per detik sisa
        [SerializeField] private EscapeGate escapeGate;        // Seret gate (boleh null)

        [Header("UI (Legacy & Direct Bindings)")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject pauseButton;

        [Header("Modern HUD Extension (Optional)")]
        [SerializeField] private HudManager hudManager;
        [SerializeField] private ScreenFlash screenFlash;

        private int score;
        private int lives;
        private int collectedCoins;
        private float timeRemaining;
        private bool ended;
        private bool gateOpen;
        private int lastCeilTime = -1;

        public bool IsEnded => ended;
        public bool IsPaused => !ended && Time.timeScale < 0.01f;
        public int CollectedCoins => collectedCoins;
        public int Score => score;
        public int TargetCoins => targetCoins;
        public float TimeRemaining => timeRemaining;
        public bool GateOpen => gateOpen;
        public int StartingLives => startingLives;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            lives = startingLives;
            timeRemaining = timeLimit;
            Time.timeScale = 1f;

            if (hudManager == null) hudManager = FindFirstObjectByType<HudManager>();
            if (screenFlash == null) screenFlash = FindFirstObjectByType<ScreenFlash>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (targetCoins <= 0)
                targetCoins = FindObjectsByType<CoinPickup>(FindObjectsSortMode.None).Length;

            RefreshUi();
            SetPanel(pausePanel, false);
            SetPanel(winPanel, false);
            SetPanel(gameOverPanel, false);
            SetPanel(pauseButton, true);
            if (escapeGate != null) escapeGate.Close();
        }

        private void Update()
        {
            if (ended || Time.timeScale < 0.01f) return;
            if (!useTimer) return;

            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                RefreshUi();
                EndGame(false, "TIME EXPIRED! Failed to reach escape gate.");
                return;
            }

            int currentCeil = Mathf.CeilToInt(timeRemaining);
            if (currentCeil != lastCeilTime)
            {
                lastCeilTime = currentCeil;
                RefreshUi();
            }
        }

        public void CollectCoin()
        {
            if (ended) return;
            collectedCoins++;
            score += pointsPerCoin;
            RefreshUi();

            if (collectedCoins >= targetCoins && !gateOpen)
            {
                if (escapeGate != null)
                {
                    gateOpen = true;
                    escapeGate.Open();   // Gerbang terbuka, pemain harus masuk
                    RefreshUi();
                }
                else
                {
                    EndGame(true);       // Tanpa gerbang: semua coin = menang
                }
            }
        }

        public void ReachEscape()
        {
            if (ended || !gateOpen) return;
            int timeBonus = 0;
            if (useTimer)
            {
                timeBonus = Mathf.CeilToInt(timeRemaining) * bonusPerSecond;
                score += timeBonus;
            }
            RefreshUi();
            EndGame(true, null, timeBonus);
        }

        public void TakeDamage(int amount = 1)
        {
            if (ended) return;
            lives = Mathf.Max(0, lives - amount);

            if (screenFlash != null)
                screenFlash.Flash();

            RefreshUi();
            if (lives == 0)
            {
                EndGame(false, "VEHICLE DESTROYED! Patrol intercepted you.");
            }
        }

        public void AddScore(int amount)
        {
            if (ended) return;
            score = Mathf.Max(0, score + amount);
            RefreshUi();
        }

        public void AddLife(int amount = 1)
        {
            if (ended) return;
            lives = Mathf.Min(startingLives, lives + amount);
            RefreshUi();
        }

        public void TogglePause()
        {
            if (ended) return;
            bool paused = Time.timeScale > 0.01f;
            Time.timeScale = paused ? 0f : 1f;
            SetPanel(pausePanel, paused);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            int menuIndex = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/MainMenu.unity");
            if (menuIndex >= 0)
                SceneManager.LoadScene(menuIndex);
            else
                SceneManager.LoadScene("MainMenu");
        }

        private void EndGame(bool won, string reason = null, int timeBonus = 0)
        {
            ended = true;
            Time.timeScale = 0f;
            SetPanel(pauseButton, false);
            SetPanel(pausePanel, false);

            bool isNewRecord = false;
            if (won)
            {
                int oldHigh = GetHighScore();
                if (score > oldHigh)
                {
                    isNewRecord = true;
                    SaveHighScore();
                }
            }

            if (hudManager != null)
            {
                if (won)
                {
                    int coinScore = collectedCoins * pointsPerCoin;
                    hudManager.ShowVictory(coinScore, collectedCoins, targetCoins, timeBonus, score, isNewRecord);
                }
                else
                {
                    hudManager.ShowGameOver(reason ?? "MISSION FAILED", collectedCoins, targetCoins, score);
                }
            }
            else
            {
                SetPanel(won ? winPanel : gameOverPanel, true);
            }
        }

        private void SaveHighScore()
        {
            try
            {
                if (score > PlayerPrefs.GetInt("HighScore", 0))
                {
                    PlayerPrefs.SetInt("HighScore", score);
                    PlayerPrefs.Save();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GameManager] Could not save high score: " + e.Message);
            }
        }

        public int GetHighScore() => PlayerPrefs.GetInt("HighScore", 0);

        private void RefreshUi()
        {
            // Update legacy text fields if bound
            if (scoreText != null) scoreText.text = $"SKOR  {score}";
            if (livesText != null) livesText.text = $"NYAWA  {lives}";
            if (coinsText != null) coinsText.text = $"COIN  {collectedCoins}/{targetCoins}";
            if (timerText != null && useTimer) timerText.text = $"WAKTU  {Mathf.CeilToInt(timeRemaining)}";

            // Update modern HUD manager if present
            if (hudManager != null)
            {
                hudManager.UpdateScore(score);
                hudManager.UpdateLives(lives, startingLives);
                hudManager.UpdateCoins(collectedCoins, targetCoins, gateOpen);
                if (useTimer) hudManager.UpdateTimer(timeRemaining);
            }
        }

        private static void SetPanel(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }
    }
}
