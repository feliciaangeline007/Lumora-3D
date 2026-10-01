using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoinConvoy
{
    /// <summary>
    /// Controller for the main title screen. Displays high score trophy badge,
    /// launches gameplay, triggers the how-to-play tutorial dialog, and manages quit.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private TutorialDialog tutorialDialog;

        private void Start()
        {
            RefreshHighScore();
            Time.timeScale = 1f;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (GetComponent<ReleaseUpdater>() == null)
            {
                gameObject.AddComponent<ReleaseUpdater>();
            }
#endif
        }

        public void Play()
        {
            Time.timeScale = 1f;
            int gameIndex = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/Game.unity");
            if (gameIndex >= 0)
            {
                SceneManager.LoadScene(gameIndex);
            }
            else
            {
                SceneManager.LoadScene("Game");
            }
        }

        public void OpenTutorial()
        {
            if (tutorialDialog != null)
            {
                tutorialDialog.Open();
            }
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void RefreshHighScore()
        {
            if (highScoreText == null) return;
            try
            {
                int best = PlayerPrefs.GetInt("HighScore", 0);
                highScoreText.text = $"★ BEST SCORE: {best} PTS";
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MainMenu] Failed to read HighScore: " + e.Message);
                highScoreText.text = "★ BEST SCORE: 0 PTS";
            }
        }
    }
}
