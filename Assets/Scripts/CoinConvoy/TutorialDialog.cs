using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoinConvoy
{
    /// <summary>
    /// Interactive 3-step onboarding / tutorial modal.
    /// Manages step navigation, progress indicator dots, PlayerPrefs first-launch logic,
    /// and smooth animated fade transitions.
    /// </summary>
    public class TutorialDialog : MonoBehaviour
    {
        public const string PrefKeyTutorialCompleted = "CC_TutorialCompleted";

        [System.Serializable]
        public struct TutorialStep
        {
            public string title;
            public string subtitle;
            public string badge;
            [TextArea(3, 5)] public string description;
            public string tip;
        }

        [Header("Settings")]
        [SerializeField] private bool autoShowOnFirstLaunch = true;
        [SerializeField] private bool pauseGameWhileOpen = true;

        [Header("UI References (Assigned in Builder or Inspector)")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text stepCounterText;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text badgeText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text tipText;
        [SerializeField] private Image[] dotIndicators;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text nextButtonText;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button closeButton;

        private int currentStep = 0;
        private bool isOpen;
        private float savedTimeScale = 1f;

        private readonly TutorialStep[] steps = new[]
        {
            new TutorialStep
            {
                title = "VEHICLE CONTROLS",
                subtitle = "Master steering & throttle response",
                badge = "◀  STEER  ▶    |    GAS ▲  •  BRAKE ▼",
                description = "• <b>Mobile</b>: Tap & hold the Left/Right steer pads and Gas/Brake pedals.\n" +
                              "• <b>Desktop</b>: Drive using <b>W/A/S/D</b> or <b>Arrow Keys</b>.\n" +
                              "• Hold <b>BRAKE</b> (or <b>Spacebar</b>) to stop swiftly and reverse.",
                tip = "TIP: You can hold Gas and Steer simultaneously for smooth drifting!"
            },
            new TutorialStep
            {
                title = "ENERGY HARVEST",
                subtitle = "Collect 15 power cores to activate the exit",
                badge = "★  OBJECTIVE: COLLECT 15 COINS  ★",
                description = "• Explore the forest arena to retrieve all <b>15 glowing energy coins</b>.\n" +
                              "• Each coin awards +10 points to your mission score.\n" +
                              "• <b>Caution</b>: Every 3 coins escalates your <b>Bounty Level</b>, making AI chasers faster and more aggressive!",
                tip = "TIP: Watch the top progress bar to track remaining cores."
            },
            new TutorialStep
            {
                title = "EVADE & ESCAPE",
                subtitle = "Survive the patrol and reach the gate",
                badge = "⏱  180s TIMER   ➔   ESCAPE GATE",
                description = "• Evade hostile patrol interceptors. You have <b>3 Lives</b>—don't get wrecked!\n" +
                              "• Once 15 coins are secured, the <b>Escape Gate turns GREEN</b>.\n" +
                              "• Rush into the portal before time runs out to earn a huge <b>Time Bonus</b>!",
                tip = "TIP: High scores are only recorded when you successfully escape!"
            }
        };

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            WireButtons();
        }

        private void Start()
        {
            if (autoShowOnFirstLaunch)
            {
                bool shown = false;
                try
                {
                    shown = PlayerPrefs.GetInt(PrefKeyTutorialCompleted, 0) == 1;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[TutorialDialog] PlayerPrefs access error: " + e.Message);
                }

                if (!shown)
                {
                    Open();
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void WireButtons()
        {
            if (prevButton != null)
            {
                prevButton.onClick.RemoveAllListeners();
                prevButton.onClick.AddListener(OnPrevClicked);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextClicked);
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(CloseAndSave);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseAndSave);
            }
        }

        public void Open()
        {
            isOpen = true;
            gameObject.SetActive(true);

            if (pauseGameWhileOpen)
            {
                savedTimeScale = Time.timeScale > 0.01f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
            }

            currentStep = 0;
            RenderStep();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void Close()
        {
            isOpen = false;
            gameObject.SetActive(false);

            if (pauseGameWhileOpen)
            {
                // Restore game time if GameManager is not ended or paused
                if (GameManager.Instance == null || (!GameManager.Instance.IsEnded && !GameManager.Instance.IsPaused))
                {
                    Time.timeScale = savedTimeScale;
                }
            }
        }

        public void CloseAndSave()
        {
            try
            {
                PlayerPrefs.SetInt(PrefKeyTutorialCompleted, 1);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[TutorialDialog] Could not save tutorial completion: " + e.Message);
            }

            Close();
        }

        private void OnPrevClicked()
        {
            if (currentStep > 0)
            {
                currentStep--;
                RenderStep();
            }
        }

        private void OnNextClicked()
        {
            if (currentStep < steps.Length - 1)
            {
                currentStep++;
                RenderStep();
            }
            else
            {
                // Reached end step -> Start playing
                CloseAndSave();
            }
        }

        private void RenderStep()
        {
            if (currentStep < 0 || currentStep >= steps.Length) return;
            TutorialStep step = steps[currentStep];

            if (stepCounterText != null)
                stepCounterText.text = $"STEP {currentStep + 1} OF {steps.Length}";

            if (titleText != null) titleText.text = step.title;
            if (subtitleText != null) subtitleText.text = step.subtitle;
            if (badgeText != null) badgeText.text = step.badge;
            if (descriptionText != null) descriptionText.text = step.description;
            if (tipText != null) tipText.text = step.tip;

            // Update Progress Dots
            if (dotIndicators != null)
            {
                for (int i = 0; i < dotIndicators.Length; i++)
                {
                    if (dotIndicators[i] == null) continue;
                    bool isActive = (i == currentStep);
                    dotIndicators[i].color = isActive ? UITheme.ColorAccentGold : new Color(0.3f, 0.4f, 0.55f, 0.5f);
                    dotIndicators[i].transform.localScale = isActive ? new Vector3(1.3f, 1.3f, 1f) : Vector3.one;
                }
            }

            // Button States
            if (prevButton != null)
                prevButton.gameObject.SetActive(currentStep > 0);

            if (nextButtonText != null)
                nextButtonText.text = (currentStep == steps.Length - 1) ? "START PLAYING" : "NEXT ➔";
        }
    }
}
