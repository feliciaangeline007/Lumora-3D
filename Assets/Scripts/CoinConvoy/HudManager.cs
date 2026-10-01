using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoinConvoy
{
    /// <summary>
    /// Coordinates the in-game HUD visual presentation, including responsive safe-area insets,
    /// animated coin progress bar, urgent timer pulsing, formatted hearts, bounty threat badge,
    /// and comprehensive stat breakdowns for victory and game-over screens.
    /// </summary>
    public class HudManager : MonoBehaviour
    {
        public static HudManager Instance { get; private set; }

        [Header("Root & Safe Area")]
        [SerializeField] private RectTransform safeAreaRoot;

        [Header("Top-Left: Player Status")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text bountyBadgeText;
        [SerializeField] private Image bountyBadgeBg;

        [Header("Top-Center: Mission Objective")]
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private Image coinsProgressBarFill;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Image timerBadgeBg;

        [Header("Top-Right: Quick Actions")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button helpButton;

        [Header("Dialogs & Modals")]
        [SerializeField] private TutorialDialog tutorialDialog;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("Win Modal Breakdown")]
        [SerializeField] private TMP_Text winScoreText;
        [SerializeField] private TMP_Text winCoinsText;
        [SerializeField] private TMP_Text winTimeBonusText;
        [SerializeField] private TMP_Text winTotalScoreText;
        [SerializeField] private GameObject winNewRecordBadge;

        [Header("Game Over Breakdown")]
        [SerializeField] private TMP_Text loseReasonText;
        [SerializeField] private TMP_Text loseScoreText;
        [SerializeField] private TMP_Text loseCoinsText;

        private Rect lastSafeArea;
        private float timerWarningPulse;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ApplySafeArea();
        }

        private void Start()
        {
            if (helpButton != null && tutorialDialog != null)
            {
                helpButton.onClick.RemoveAllListeners();
                helpButton.onClick.AddListener(() => tutorialDialog.Open());
            }
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea)
            {
                ApplySafeArea();
            }

            // Pulse timer badge if time is critical (< 30s)
            if (GameManager.Instance != null && !GameManager.Instance.IsEnded)
            {
                float time = GameManager.Instance.TimeRemaining;
                if (time <= 30f && time > 0f)
                {
                    timerWarningPulse += Time.deltaTime * 5f;
                    float wave = Mathf.Abs(Mathf.Sin(timerWarningPulse));
                    if (timerBadgeBg != null)
                        timerBadgeBg.color = Color.Lerp(UITheme.ColorDangerRedDark, UITheme.ColorDangerRed, wave);
                    if (timerText != null)
                        timerText.color = Color.Lerp(Color.white, UITheme.ColorDangerRed, wave);
                }
            }
        }

        /// <summary>
        /// Adjusts safeAreaRoot anchors according to the physical device screen safe area (notches, punch-holes).
        /// </summary>
        private void ApplySafeArea()
        {
            if (safeAreaRoot == null) return;
            Rect safe = Screen.safeArea;
            lastSafeArea = safe;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;

            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            safeAreaRoot.anchorMin = min;
            safeAreaRoot.anchorMax = max;
        }

        public void UpdateScore(int score)
        {
            if (scoreText != null) scoreText.text = $"★ {score}";
        }

        public void UpdateLives(int lives, int maxLives)
        {
            if (livesText == null) return;
            string hearts = "";
            for (int i = 0; i < maxLives; i++)
            {
                hearts += (i < lives) ? "<color=#EF4444>♥</color> " : "<color=#475569>♡</color> ";
            }
            livesText.text = $"HP  {hearts.TrimEnd()}";
        }

        public void UpdateCoins(int collected, int target, bool gateOpen)
        {
            if (coinsText != null)
            {
                if (gateOpen)
                    coinsText.text = "<color=#10B981>ESCAPE GATE OPEN!</color>";
                else
                    coinsText.text = $"COINS  {collected}/{target}";
            }

            if (coinsProgressBarFill != null && target > 0)
            {
                float progress = Mathf.Clamp01((float)collected / target);
                coinsProgressBarFill.fillAmount = progress;
                coinsProgressBarFill.color = gateOpen ? UITheme.ColorPrimaryGreen : UITheme.ColorAccentGold;
            }
        }

        public void UpdateTimer(float seconds)
        {
            if (timerText == null) return;
            int totalSec = Mathf.Max(0, Mathf.CeilToInt(seconds));
            int min = totalSec / 60;
            int sec = totalSec % 60;
            timerText.text = $"⏱ {min:00}:{sec:00}";
        }

        public void UpdateBounty(int level)
        {
            if (bountyBadgeText == null) return;
            string levelStr = $"BURONAN LV {level}";
            Color badgeColor;

            if (level >= 5)
            {
                levelStr += " [CRITICAL]";
                badgeColor = UITheme.ColorDangerRed;
            }
            else if (level >= 3)
            {
                levelStr += " [HIGH]";
                badgeColor = UITheme.ColorAccentGold;
            }
            else
            {
                levelStr += " [PATROL]";
                badgeColor = UITheme.ColorTechCyan;
            }

            bountyBadgeText.text = levelStr;
            if (bountyBadgeBg != null) bountyBadgeBg.color = badgeColor;
        }

        [Header("Power-Up Notification Toast")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private TMP_Text notificationText;
        [SerializeField] private Image notificationBg;
        private Coroutine notificationRoutine;

        public void ShowPowerUpNotification(string text, Color highlightColor)
        {
            if (notificationPanel == null || notificationText == null) return;
            if (notificationRoutine != null) StopCoroutine(notificationRoutine);
            notificationRoutine = StartCoroutine(NotificationAnimation(text, highlightColor));
        }

        private System.Collections.IEnumerator NotificationAnimation(string text, Color highlightColor)
        {
            notificationPanel.SetActive(true);
            notificationText.text = text;
            if (notificationBg != null)
                notificationBg.color = new Color(highlightColor.r * 0.2f, highlightColor.g * 0.2f, highlightColor.b * 0.2f, 0.9f);

            CanvasGroup group = notificationPanel.GetComponent<CanvasGroup>();
            if (group == null) group = notificationPanel.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            float fadeIn = 0.25f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Clamp01(elapsed / fadeIn);
                yield return null;
            }

            yield return new WaitForSeconds(1.8f);

            elapsed = 0f;
            float fadeOut = 0.4f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.deltaTime;
                group.alpha = 1f - Mathf.Clamp01(elapsed / fadeOut);
                yield return null;
            }

            notificationPanel.SetActive(false);
            notificationRoutine = null;
        }

        public void ShowVictory(int coinScore, int coins, int targetCoins, int timeBonus, int totalScore, bool isNewRecord)
        {
            if (winPanel != null) winPanel.SetActive(true);
            if (winCoinsText != null) winCoinsText.text = $"{coins} / {targetCoins}";
            if (winScoreText != null) winScoreText.text = $"+{coinScore} pts";
            if (winTimeBonusText != null) winTimeBonusText.text = $"+{timeBonus} pts";
            if (winTotalScoreText != null) winTotalScoreText.text = $"{totalScore} PTS";
            if (winNewRecordBadge != null) winNewRecordBadge.SetActive(isNewRecord);
        }

        public void ShowGameOver(string reason, int coins, int targetCoins, int totalScore)
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (loseReasonText != null) loseReasonText.text = reason;
            if (loseCoinsText != null) loseCoinsText.text = $"{coins} / {targetCoins}";
            if (loseScoreText != null) loseScoreText.text = $"{totalScore} PTS";
        }

        public void ToggleTutorial()
        {
            if (tutorialDialog != null) tutorialDialog.Open();
        }
    }
}

