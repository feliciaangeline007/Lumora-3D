using UnityEngine;
using UnityEngine.SceneManagement;

namespace EnchantedForest
{
    /// <summary>
    /// Mengelola progres level, state permainan, kamera, HUD, jeda, dan hasil level.
    /// </summary>
    public class GameModeManager : MonoBehaviour
    {
        public static GameModeManager Instance { get; private set; }

        public enum CameraMode
        {
            ShowcaseTour,
            PlayerThirdPerson
        }

        [Header("Mode Kamera")]
        public CameraMode currentMode = CameraMode.ShowcaseTour;

        [Header("Referensi Objek")]
        public GameObject showcaseCameraObj;
        public GameObject playerCameraObj;
        public GameObject playerCharacterObj;

        [Header("Status Level")]
        public int collectedCoins = 0;
        public int totalCoinsInLevel = 0;

        [Header("Tujuan Level")]
        [Min(30f)] public float levelTimeLimit = 240f;

        private PlayerController3D _playerController;
        private bool _hasStarted;
        private float _timeRemaining;
        private float _toastUntil;
        private string _toastMessage;
        private GameState _gameState;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _mutedStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _coinStyle;
        private bool _stylesInitialized;

        private enum GameState
        {
            Ready,
            Playing,
            Paused,
            Won,
            Lost
        }

        public bool HasStarted => _hasStarted;
        public bool IsPlaying => _gameState == GameState.Playing;

        public static void EnsurePlayableScene(GameObject showcaseCamera)
        {
            if (Object.FindAnyObjectByType<GameModeManager>() != null) return;

            Vector3 spawn = GetRuntimeSpawnPosition();
            GameObject player = CreateRuntimePlayer(spawn);
            GameObject managerObject = new GameObject("GameModeManager");
            GameModeManager manager = managerObject.AddComponent<GameModeManager>();
            manager.showcaseCameraObj = showcaseCamera;
            manager.playerCameraObj = player.GetComponentInChildren<ThirdPersonCameraFollow>(true).gameObject;
            manager.playerCharacterObj = player;
            manager.currentMode = CameraMode.PlayerThirdPerson;
        }

        private static GameObject CreateRuntimePlayer(Vector3 spawn)
        {
            GameObject player = new GameObject("Player_Adventurer");
            player.tag = "Player";
            player.transform.position = spawn;

            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.4f;
            characterController.center = new Vector3(0f, 0.9f, 0f);

            Transform visuals = new GameObject("Visuals").transform;
            visuals.SetParent(player.transform, false);
            AddRuntimeVisual(PrimitiveType.Capsule, "Body", visuals, new Vector3(0f, 0.75f, 0f), new Vector3(0.55f, 0.7f, 0.45f), new Color(0.12f, 0.38f, 0.72f));
            AddRuntimeVisual(PrimitiveType.Sphere, "Head", visuals, new Vector3(0f, 1.55f, 0f), Vector3.one * 0.44f, new Color(0.92f, 0.72f, 0.59f));
            AddRuntimeVisual(PrimitiveType.Cylinder, "Hat", visuals, new Vector3(0f, 1.78f, 0f), new Vector3(0.72f, 0.06f, 0.72f), new Color(0.28f, 0.16f, 0.08f));
            AddRuntimeVisual(PrimitiveType.Cube, "Backpack", visuals, new Vector3(0f, 0.8f, -0.3f), new Vector3(0.45f, 0.5f, 0.25f), new Color(0.36f, 0.22f, 0.11f));

            GameObject cameraObject = new GameObject("PlayerThirdPersonCamera");
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 2.8f, -5.5f);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            ThirdPersonCameraFollow follow = cameraObject.AddComponent<ThirdPersonCameraFollow>();
            follow.target = player.transform;
            cameraObject.SetActive(false);

            PlayerController3D controller = player.AddComponent<PlayerController3D>();
            controller.cameraTransform = cameraObject.transform;
            controller.spawnPoint = spawn;
            controller.fallThresholdY = spawn.y - 12f;
            controller.enabled = false;
            return player;
        }

        private static void AddRuntimeVisual(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            GameObject visual = GameObject.CreatePrimitive(type);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = position;
            visual.transform.localScale = scale;
            Object.Destroy(visual.GetComponent<Collider>());

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = color;
                visual.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        private static Vector3 GetRuntimeSpawnPosition()
        {
            switch (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
            {
                case "Level_2_JurangAkar": return new Vector3(0f, 11f, 0f);
                case "Level_3_PuncakPohon": return new Vector3(0f, 1.6f, 10f);
                default: return new Vector3(0f, 0.6f, 0f);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            CoinRotator[] allCoins = Object.FindObjectsByType<CoinRotator>(FindObjectsSortMode.None);
            totalCoinsInLevel = allCoins.Length;
            collectedCoins = 0;
            _timeRemaining = levelTimeLimit;
            _gameState = GameState.Ready;

            currentMode = CameraMode.PlayerThirdPerson;
            _playerController = playerCharacterObj != null ? playerCharacterObj.GetComponent<PlayerController3D>() : null;
            if (_playerController != null) _playerController.enabled = false;

            if (showcaseCameraObj != null) showcaseCameraObj.SetActive(false);
            if (playerCameraObj != null) playerCameraObj.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            AttachFinishPortal();
        }

        private void Update()
        {
            if (!_hasStarted)
            {
                bool startPressed = false;

#if ENABLE_INPUT_SYSTEM
                var startKeyboard = UnityEngine.InputSystem.Keyboard.current;
                startPressed = startKeyboard != null && (startKeyboard.enterKey.wasPressedThisFrame || startKeyboard.spaceKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
                startPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
#endif

                if (startPressed) StartGame();
                return;
            }

            if (_gameState == GameState.Paused)
            {
                if (ReadPauseInput()) ResumeGame();
                return;
            }

            if (_gameState != GameState.Playing) return;

            _timeRemaining = Mathf.Max(0f, _timeRemaining - Time.deltaTime);
            if (_timeRemaining <= 0f)
            {
                EndGame(false);
                return;
            }

            if (ReadPauseInput())
            {
                PauseGame();
                return;
            }

            // Tekan tombol C atau V atau Tab untuk ganti mode kamera
            bool switchPressed = false;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.cKey.wasPressedThisFrame || kb.vKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame))
            {
                switchPressed = true;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.V) || Input.GetKeyDown(KeyCode.Tab))
            {
                switchPressed = true;
            }
#endif

            if (switchPressed)
            {
                currentMode = (currentMode == CameraMode.ShowcaseTour) ? CameraMode.PlayerThirdPerson : CameraMode.ShowcaseTour;
                ApplyCameraMode();
            }
        }

        private static bool ReadPauseInput()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        private void StartGame()
        {
            if (_gameState != GameState.Ready) return;
            _hasStarted = true;
            _gameState = GameState.Playing;
            _timeRemaining = levelTimeLimit;
            if (_playerController != null) _playerController.enabled = true;
            ApplyCameraMode();
        }

        public void StartGameFromTouch()
        {
            if (!_hasStarted) StartGame();
        }

        public void AddCoin()
        {
            if (_gameState != GameState.Playing) return;
            collectedCoins = Mathf.Min(collectedCoins + 1, totalCoinsInLevel);
        }

        public bool TryFinishLevel()
        {
            if (_gameState != GameState.Playing) return false;
            if (collectedCoins < totalCoinsInLevel)
            {
                int remaining = totalCoinsInLevel - collectedCoins;
                ShowToast($"Kumpulkan {remaining} koin lagi sebelum masuk portal.");
                return false;
            }

            EndGame(true);
            return true;
        }

        public void PauseGame()
        {
            if (_gameState != GameState.Playing) return;
            _gameState = GameState.Paused;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ResumeGame()
        {
            if (_gameState != GameState.Paused) return;
            _gameState = GameState.Playing;
            Time.timeScale = 1f;
            ApplyCameraMode();
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadNextLevel()
        {
            Time.timeScale = 1f;
            int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
            int nextBuildIndex = currentBuildIndex + 1;
            if (nextBuildIndex >= SceneManager.sceneCountInBuildSettings) nextBuildIndex = 0;
            SceneManager.LoadScene(nextBuildIndex);
        }

        private void EndGame(bool won)
        {
            _gameState = won ? GameState.Won : GameState.Lost;
            Time.timeScale = 0f;
            if (_playerController != null) _playerController.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (won)
            {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.PlayVictorySound();
                int bestTime = PlayerPrefs.GetInt(GetBestTimeKey(), 0);
                int completedTime = Mathf.CeilToInt(_timeRemaining);
                if (completedTime > bestTime)
                {
                    PlayerPrefs.SetInt(GetBestTimeKey(), completedTime);
                    PlayerPrefs.Save();
                }
            }
        }

        private string GetBestTimeKey()
        {
            return $"Lumora.BestTime.{SceneManager.GetActiveScene().name}";
        }

        private void ShowToast(string message)
        {
            _toastMessage = message;
            _toastUntil = Time.unscaledTime + 2.5f;
        }

        private void AttachFinishPortal()
        {
            Collider[] colliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider portalCollider = colliders[i];
                if (portalCollider.gameObject.name != "PortalEnergy") continue;

                portalCollider.isTrigger = true;
                if (portalCollider.GetComponent<FinishPortalTrigger>() == null)
                {
                    portalCollider.gameObject.AddComponent<FinishPortalTrigger>();
                }
            }
        }

        private void ApplyCameraMode()
        {
            bool isShowcase = (currentMode == CameraMode.ShowcaseTour);

            if (showcaseCameraObj != null) showcaseCameraObj.SetActive(isShowcase);
            if (playerCameraObj != null) playerCameraObj.SetActive(!isShowcase);

            Cursor.lockState = isShowcase ? CursorLockMode.None : CursorLockMode.Confined;
            Cursor.visible = isShowcase;
        }

        private void OnGUI()
        {
            EnsureGUIStyles();

            if (_gameState == GameState.Ready)
            {
                DrawStartScreen();
                return;
            }

            if (_gameState == GameState.Playing || _gameState == GameState.Paused)
            {
                DrawHUD();
            }

            if (_gameState == GameState.Paused) DrawPauseScreen();
            else if (_gameState == GameState.Won) DrawEndScreen(true);
            else if (_gameState == GameState.Lost) DrawEndScreen(false);

            if (!string.IsNullOrEmpty(_toastMessage) && Time.unscaledTime < _toastUntil)
            {
                DrawPanel(new Rect((Screen.width - 520f) * 0.5f, Screen.height - 108f, 520f, 56f));
                GUI.Label(new Rect((Screen.width - 500f) * 0.5f, Screen.height - 101f, 500f, 42f),
                    _toastMessage, _bodyStyle);
            }
        }

        private void DrawStartScreen()
        {
            Rect panel = CenteredPanel(330f);
            DrawPanel(panel);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 24f, panel.width - 40f, 58f), "LUMORA", _titleStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 88f, panel.width - 48f, 44f),
                "Kumpulkan semua biji cahaya, lalu capai portal.", _bodyStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 136f, panel.width - 48f, 28f),
                $"{totalCoinsInLevel} koin  ·  {Mathf.CeilToInt(levelTimeLimit)} detik", _mutedStyle);
            if (GUI.Button(new Rect(panel.x + 36f, panel.y + 181f, panel.width - 72f, 52f),
                    "MULAI PETUALANGAN", _buttonStyle))
            {
                StartGame();
            }
        }

        private void DrawHUD()
        {
            bool compact = Screen.width < 720f;
            Rect timer = new Rect(Screen.width - 178f, compact ? 12f : 20f, 102f, 52f);
            DrawPanel(timer);
            GUI.Label(new Rect(timer.x + 8f, timer.y + 9f, timer.width - 16f, 32f),
                FormatTime(_timeRemaining), _coinStyle);

            Rect pause = new Rect(Screen.width - 66f, compact ? 12f : 20f, 46f, 52f);
            if (GUI.Button(pause, "Ⅱ", _buttonStyle)) PauseGame();

            Rect panel = compact
                ? new Rect(12f, 72f, Screen.width - 24f, 82f)
                : new Rect(20f, 20f, Mathf.Min(350f, Screen.width * 0.42f), 105f);
            DrawPanel(panel);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 7f, panel.width - 28f, 25f),
                $"BIJI CAHAYA     {collectedCoins:00} / {totalCoinsInLevel:00}", _coinStyle);

            Rect progress = new Rect(panel.x + 14f, panel.y + 38f, panel.width - 28f, 8f);
            DrawProgressBar(progress, totalCoinsInLevel > 0 ? (float)collectedCoins / totalCoinsInLevel : 0f);
            string objective = collectedCoins >= totalCoinsInLevel
                ? "Portal terbuka — lanjut ke tujuan!"
                : "Kumpulkan semua koin untuk membuka portal";
            GUI.Label(new Rect(panel.x + 14f, panel.y + 49f, panel.width - 28f, 25f), objective, _mutedStyle);
        }

        private void DrawPauseScreen()
        {
            Rect panel = CenteredPanel(260f);
            DrawPanel(panel);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 28f, panel.width - 40f, 50f), "JEDA", _titleStyle);
            if (GUI.Button(new Rect(panel.x + 30f, panel.y + 94f, panel.width - 60f, 48f),
                    "LANJUTKAN", _buttonStyle))
            {
                ResumeGame();
            }

            if (GUI.Button(new Rect(panel.x + 30f, panel.y + 152f, panel.width - 60f, 42f),
                    "ULANGI LEVEL", _mutedStyle))
            {
                RestartLevel();
            }
        }

        private void DrawEndScreen(bool won)
        {
            Rect panel = CenteredPanel(300f);
            DrawPanel(panel);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 26f, panel.width - 40f, 54f),
                won ? "LEVEL SELESAI!" : "WAKTU HABIS", _titleStyle);

            string result = won
                ? $"Semua {totalCoinsInLevel} koin terkumpul.\nSisa waktu: {FormatTime(_timeRemaining)}"
                : $"{collectedCoins} dari {totalCoinsInLevel} koin terkumpul.";
            GUI.Label(new Rect(panel.x + 20f, panel.y + 90f, panel.width - 40f, 52f), result, _bodyStyle);

            if (won)
            {
                float best = PlayerPrefs.GetInt(GetBestTimeKey(), 0);
                GUI.Label(new Rect(panel.x + 20f, panel.y + 141f, panel.width - 40f, 25f),
                    $"Rekor sisa waktu: {FormatTime(best)}", _mutedStyle);
            }

            if (GUI.Button(new Rect(panel.x + 24f, panel.y + 182f, panel.width - 48f, 46f),
                    won ? "LEVEL BERIKUTNYA" : "COBA LAGI", _buttonStyle))
            {
                if (won) LoadNextLevel();
                else RestartLevel();
            }
        }

        private void EnsureGUIStyles()
        {
            if (_stylesInitialized) return;
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Screen.height / 22, 26, 40),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.84f, 0.42f) }
            };
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Screen.height / 75, 15, 22),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            _mutedStyle = new GUIStyle(_bodyStyle)
            {
                fontSize = Mathf.Clamp(Screen.height / 95, 12, 17),
                normal = { textColor = new Color(0.66f, 0.83f, 0.81f) }
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.Clamp(Screen.height / 70, 16, 22),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _coinStyle = new GUIStyle(_bodyStyle)
            {
                fontSize = Mathf.Clamp(Screen.height / 65, 17, 24),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1f, 0.84f, 0.42f) }
            };
            _stylesInitialized = true;
        }

        private static Rect CenteredPanel(float height)
        {
            float width = Mathf.Min(520f, Screen.width - 32f);
            float boundedHeight = Mathf.Min(height, Screen.height - 32f);
            return new Rect((Screen.width - width) * 0.5f, (Screen.height - boundedHeight) * 0.5f,
                width, boundedHeight);
        }

        private static void DrawPanel(Rect rect)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.018f, 0.055f, 0.062f, 0.94f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.31f, 0.96f, 0.85f, 0.92f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawProgressBar(Rect rect, float progress)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.12f, 0.2f, 0.2f, 1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.31f, 0.96f, 0.85f, 1f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(progress), rect.height),
                Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static string FormatTime(float seconds)
        {
            int roundedSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{roundedSeconds / 60:00}:{roundedSeconds % 60:00}";
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }
    }
}
