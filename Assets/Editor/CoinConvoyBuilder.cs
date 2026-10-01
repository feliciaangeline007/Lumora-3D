using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CoinConvoy;

namespace CoinConvoy.Editor
{
    /// <summary>
    /// Automated builder for Coin Convoy. Constructs the complete 3D gameplay scene and
    /// main menu scene with modern responsive UI/UX, safe-area insets, interactive tutorial,
    /// and cohesive design tokens.
    /// </summary>
    public static class CoinConvoyBuilder
    {
        private const string SpriteDir = "Assets/Sprite/UI";
        private const string CardSpritePath = SpriteDir + "/CC_Card.png";
        private const string ButtonSpritePath = SpriteDir + "/CC_Button.png";
        private const string PillSpritePath = SpriteDir + "/CC_Pill.png";
        private const string CircleSpritePath = SpriteDir + "/CC_Circle.png";
        private static readonly Dictionary<Color, Material> SharedColorMaterials = new Dictionary<Color, Material>();

        [MenuItem("Tools/Coin Convoy/Create Playable Demo")]
        public static void CreatePlayableDemo()
        {
            if (!EditorUtility.DisplayDialog("Coin Convoy", "Buat ulang scene demo mandiri dengan UI modern? Scene Game & MainMenu saat ini akan diperbarui.", "Buat", "Batal")) return;
            BuildDemo(true);
        }

        [MenuItem("Tools/Coin Convoy/Upgrade UI in Active Scene")]
        public static void UpgradeUIInActiveSceneMenu()
        {
            EnsureUISprites();
            CarController car = Object.FindFirstObjectByType<CarController>();
            GameManager manager = Object.FindFirstObjectByType<GameManager>();

            if (manager == null)
            {
                EditorUtility.DisplayDialog("Coin Convoy", "GameManager tidak ditemukan di scene aktif.", "OK");
                return;
            }

            // Remove existing Canvas if present
            Canvas oldCanvas = Object.FindFirstObjectByType<Canvas>();
            if (oldCanvas != null)
            {
                Undo.DestroyObjectImmediate(oldCanvas.gameObject);
            }

            CreateModernHud(manager, car);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Coin Convoy", "UI modern berhasil dipasang di scene aktif!", "OK");
        }

        public static void CreatePlayableDemoBatch()
        {
            BuildDemo(false);
        }

        private static void BuildDemo(bool showDialog)
        {
            EnsureUISprites();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting
            GameObject light = new GameObject("Directional Light");
            Light directional = light.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Arena Ground
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Arena (NavMesh Ground)";
            floor.transform.localScale = new Vector3(6f, 1f, 6f);
            NavMeshSurface surface = floor.AddComponent<NavMeshSurface>();

            // Player Car
            GameObject player = CreateCar("PlayerCar", new Vector3(0f, 0.7f, -18f), new Color(0.1f, 0.45f, 0.95f));
            player.tag = "Player";
            CarController car = player.AddComponent<CarController>();
            player.AddComponent<DamageFeedback>();
            Camera mainCam = CreateCamera(player.transform);

            // Primary Enemy Chaser
            GameObject enemy = CreateCar("EnemyChaser", new Vector3(0f, 0.7f, 14f), new Color(0.85f, 0.12f, 0.1f));
            enemy.tag = "Enemy";
            NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
            agent.speed = 3.5f;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            enemy.AddComponent<EnemyChaser>();

            // Patrol Enemy with Waypoints
            Vector3[] patrolWps = new Vector3[]
            {
                new Vector3(-16f, 0.7f, -12f),
                new Vector3(-16f, 0.7f, 12f),
                new Vector3(16f, 0.7f, 12f),
                new Vector3(16f, 0.7f, -12f)
            };
            CreatePatrolEnemy(new Vector3(-16f, 0.7f, 0f), patrolWps);

            // Convoy Spawner (reinforcements every 5 coins)
            CreateConvoySpawner(enemy);

            // Dynamic Moving Obstacles (hazard roadblocks)
            CreateMovingObstacle("MovingObstacle_1", new Vector3(-8f, 1f, 4f), new Vector3(8f, 1f, 4f), 4f);
            CreateMovingObstacle("MovingObstacle_2", new Vector3(8f, 1f, -4f), new Vector3(-8f, 1f, -4f), 3.5f);

            // 4 Distinct Power-Ups
            CreatePowerUp("PowerUp_Nitro", new Vector3(-14f, 1.2f, -5f), PowerUp.PowerType.Nitro, new Color(0.1f, 0.85f, 1f));
            CreatePowerUp("PowerUp_Shield", new Vector3(14f, 1.2f, -5f), PowerUp.PowerType.Shield, new Color(0.2f, 0.55f, 1f));
            CreatePowerUp("PowerUp_Magnet", new Vector3(-14f, 1.2f, 15f), PowerUp.PowerType.Magnet, new Color(0.85f, 0.35f, 1f));
            CreatePowerUp("PowerUp_ExtraLife", new Vector3(14f, 1.2f, 15f), PowerUp.PowerType.ExtraLife, new Color(0.1f, 0.95f, 0.4f));

            // Escape Gate
            GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gate.name = "EscapeGate";
            gate.transform.position = new Vector3(0f, 1.5f, 28f);
            gate.transform.localScale = new Vector3(8f, 3f, 1f);
            gate.GetComponent<Collider>().isTrigger = true;
            EscapeGate escapeGate = gate.AddComponent<EscapeGate>();
            SetColor(gate, new Color(0.7f, 0.15f, 0.15f, 0.8f));
            SetEmission(gate, new Color(0.7f, 0.15f, 0.15f), 0.55f);

            // Path Lights leading to Escape Gate
            CreatePathLights(new Vector3(0f, 0.3f, -2f), new Vector3(0f, 0.3f, 26f), 8);

            // Arrow Indicator (points to EscapeGate)
            GameObject arrowObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arrowObj.name = "EscapeArrow";
            arrowObj.transform.SetParent(mainCam.transform);
            arrowObj.transform.localPosition = new Vector3(0f, 1.2f, 3.5f);
            arrowObj.transform.localScale = new Vector3(0.2f, 0.05f, 0.6f);
            arrowObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Object.DestroyImmediate(arrowObj.GetComponent<Collider>());
            SetColor(arrowObj, new Color(0.1f, 0.8f, 0.4f, 0.9f));
            ArrowIndicator arrowIndicator = arrowObj.AddComponent<ArrowIndicator>();
            SerializedObject arrowSo = new SerializedObject(arrowIndicator);
            arrowSo.FindProperty("target").objectReferenceValue = gate.transform;
            arrowSo.ApplyModifiedPropertiesWithoutUndo();

            // 15 Energy Coins scattered in challenging patterns
            GameObject coinGroup = new GameObject("CoinGroup");
            for (int i = 0; i < 15; i++)
            {
                float x = (i % 5 - 2) * 5f;
                float z = -12f + (i / 5) * 11f;
                GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coin.name = "Coin_" + (i + 1);
                coin.transform.SetParent(coinGroup.transform);
                coin.transform.position = new Vector3(x, 1.2f, z);
                coin.transform.localScale = new Vector3(0.55f, 0.12f, 0.55f);
                coin.GetComponent<Collider>().isTrigger = true;
                SetColor(coin, new Color(1f, 0.75f, 0.05f));
                SetEmission(coin, new Color(1f, 0.75f, 0.05f), 0.9f);
                coin.AddComponent<CoinPickup>();
            }

            // Bounty Meter
            GameObject bountyObj = new GameObject("BountyMeter");
            bountyObj.AddComponent<BountyMeter>();

            // Game Manager
            GameObject managerObj = new GameObject("GameManager");
            GameManager gameManager = managerObj.AddComponent<GameManager>();
            SerializedObject gmSo = new SerializedObject(gameManager);
            gmSo.FindProperty("escapeGate").objectReferenceValue = escapeGate;
            gmSo.ApplyModifiedPropertiesWithoutUndo();

            // Music Manager & Mobile Optimizer
            CreateAudioAndOptimizer();

            // Modern HUD Canvas with Radar & PowerUp Toast
            CreateModernHud(gameManager, car);

            // Apply complete scenery (Skybox, lighting, mountains, invisible walls, rocks, fog, wind)
            SceneryEnhancer.EnhanceActiveSceneMenu();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
            CreateMainMenuScene();

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Coin Convoy", "Demo game, AI Patrol, PowerUps, Mini-Radar HUD, dan Scenery berhasil dibangun!\nBuka MainMenu untuk memulai atau main langsung di Game.", "OK");
            }
        }


        private static void CreateMainMenuScene()
        {
            EnsureUISprites();
            Scene menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera & Light
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 2f, -8f);
            camera.transform.LookAt(Vector3.zero);

            GameObject light = new GameObject("Menu Light");
            Light menuLight = light.AddComponent<Light>();
            menuLight.type = LightType.Directional;
            menuLight.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(35f, -25f, 0f);

            // Backdrop
            GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backdrop.name = "MenuBackdrop";
            backdrop.transform.position = new Vector3(0f, 0f, 4f);
            backdrop.transform.localScale = new Vector3(20f, 12f, 0.5f);
            SetColor(backdrop, new Color(0.02f, 0.05f, 0.10f));

            // Main Menu Manager
            GameObject menuObject = new GameObject("MainMenu");
            MainMenu mainMenu = menuObject.AddComponent<MainMenu>();

            // Canvas
            GameObject canvasObject = new GameObject("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            // Main Menu Card (Center container)
            GameObject menuCard = CreatePanelObject(canvasObject.transform, "MenuCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 580f), GetCardSprite(), UITheme.ColorCardSurface);

            // Title Header
            TMP_Text title = CreateText(menuCard.transform, "Title", "COIN CONVOY", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -65f), new Vector2(500f, 60f), 52f, FontStyles.Bold, UITheme.ColorAccentGold);
            title.alignment = TextAlignmentOptions.Center;

            TMP_Text subtitle = CreateText(menuCard.transform, "Subtitle", "ESCAPE ROUTE • MISSION CONTROL", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -115f), new Vector2(500f, 32f), 20f, FontStyles.Normal, UITheme.ColorTextSecondary);
            subtitle.alignment = TextAlignmentOptions.Center;

            // Best Score Trophy Chip
            GameObject bestChip = CreatePanelObject(menuCard.transform, "BestScoreChip", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(360f, 48f), GetPillSprite(), UITheme.ColorCardBorder);
            TMP_Text bestText = CreateText(bestChip.transform, "BestScoreText", "★ BEST SCORE: 0 PTS", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 40f), 22f, FontStyles.Bold, UITheme.ColorAccentGold);
            bestText.alignment = TextAlignmentOptions.Center;

            // Buttons
            GameObject playBtn = CreateStyledButton(menuCard.transform, "PlayButton", "PLAY MISSION", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(380f, 66f), UITheme.ColorPrimaryGreen, 26f);
            playBtn.GetComponent<Button>().onClick.AddListener(mainMenu.Play);

            GameObject guideBtn = CreateStyledButton(menuCard.transform, "GuideButton", "HOW TO PLAY", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(380f, 56f), UITheme.ColorSlateButton, 22f);

            GameObject quitBtn = CreateStyledButton(menuCard.transform, "QuitButton", "QUIT GAME", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -405f), new Vector2(380f, 52f), UITheme.ColorCardBorder, 20f);
            quitBtn.GetComponent<Button>().onClick.AddListener(mainMenu.Quit);

            // Controls Hint Footer
            TMP_Text hint = CreateText(menuCard.transform, "HintFooter", "Mobile Touch Pedals  •  Desktop W/A/S/D + Space", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(500f, 30f), 16f, FontStyles.Italic, UITheme.ColorTextSecondary);
            hint.alignment = TextAlignmentOptions.Center;

            // Tutorial Dialog Modal in Main Menu
            GameObject tutObj = CreateTutorialModal(canvasObject.transform);
            TutorialDialog tutorialDialog = tutObj.GetComponent<TutorialDialog>();
            guideBtn.GetComponent<Button>().onClick.AddListener(tutorialDialog.Open);

            // Wire MainMenu references
            SerializedObject menuSo = new SerializedObject(mainMenu);
            menuSo.FindProperty("highScoreText").objectReferenceValue = bestText;
            menuSo.FindProperty("tutorialDialog").objectReferenceValue = tutorialDialog;
            menuSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(menu, "Assets/Scenes/MainMenu.unity");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true)
            };
            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        }

        private static void CreateModernHud(GameManager manager, CarController car)
        {
            GameObject canvasObject = new GameObject("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            // Safe Area Root
            GameObject safeRoot = new GameObject("SafeAreaRoot");
            safeRoot.transform.SetParent(canvasObject.transform, false);
            RectTransform safeRect = safeRoot.AddComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;

            HudManager hud = canvasObject.AddComponent<HudManager>();
            SerializedObject hudSo = new SerializedObject(hud);
            hudSo.FindProperty("safeAreaRoot").objectReferenceValue = safeRect;

            // ---------------------------------------------------------
            // TOP BAR (Status & Objective)
            // ---------------------------------------------------------
            // Left Status Card (Score, Lives, Bounty)
            GameObject leftCard = CreatePanelObject(safeRoot.transform, "LeftStatusCard", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(230f, -70f), new Vector2(400f, 84f), GetCardSprite(), UITheme.ColorCardSurface);
            
            TMP_Text scoreText = CreateText(leftCard.transform, "ScoreText", "★ 0", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(75f, 14f), new Vector2(140f, 36f), 30f, FontStyles.Bold, UITheme.ColorAccentGold);
            scoreText.gameObject.AddComponent<ScorePop>();

            TMP_Text livesText = CreateText(leftCard.transform, "LivesText", "HP  ♥ ♥ ♥", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(245f, 14f), new Vector2(170f, 36f), 26f, FontStyles.Bold, Color.white);

            GameObject bountyPill = CreatePanelObject(leftCard.transform, "BountyPill", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(360f, 26f), GetPillSprite(), UITheme.ColorSlateButton);
            TMP_Text bountyText = CreateText(bountyPill.transform, "BountyText", "BURONAN LV 1 [PATROL]", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 24f), 18f, FontStyles.Bold, UITheme.ColorTechCyan);
            bountyText.alignment = TextAlignmentOptions.Center;

            // Center Objective Card (Coins, Progress Bar, Timer)
            GameObject centerCard = CreatePanelObject(safeRoot.transform, "CenterObjectiveCard", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -75f), new Vector2(480f, 96f), GetCardSprite(), UITheme.ColorCardSurface);

            TMP_Text coinsText = CreateText(centerCard.transform, "CoinsText", "COINS  0/15", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(125f, -28f), new Vector2(220f, 34f), 26f, FontStyles.Bold, Color.white);

            GameObject timerBadge = CreatePanelObject(centerCard.transform, "TimerBadge", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-105f, -28f), new Vector2(170f, 34f), GetPillSprite(), UITheme.ColorSlateButton);
            TMP_Text timerText = CreateText(timerBadge.transform, "TimerText", "⏱ 03:00", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 30f), 22f, FontStyles.Bold, UITheme.ColorTechCyan);
            timerText.alignment = TextAlignmentOptions.Center;

            // Coin Progress Bar
            GameObject barTrack = CreatePanelObject(centerCard.transform, "ProgressBarTrack", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(430f, 16f), GetPillSprite(), new Color(0.08f, 0.12f, 0.20f, 0.95f));
            GameObject barFill = CreatePanelObject(barTrack.transform, "ProgressBarFill", new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, GetPillSprite(), UITheme.ColorAccentGold);
            RectTransform fillRect = barFill.GetComponent<RectTransform>();
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            Image fillImg = barFill.GetComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;

            // Right Quick Actions (Pause & Help)
            GameObject rightCard = CreatePanelObject(safeRoot.transform, "RightUtilityBar", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, -70f), new Vector2(250f, 84f), GetCardSprite(), UITheme.ColorCardSurface);
            GameObject pauseBtn = CreateStyledButton(rightCard.transform, "PauseButton", "⏸ PAUSE", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-60f, 0f), new Vector2(110f, 56f), UITheme.ColorSlateButton, 18f);
            pauseBtn.GetComponent<Button>().onClick.AddListener(manager.TogglePause);

            GameObject helpBtn = CreateStyledButton(rightCard.transform, "HelpButton", "? GUIDE", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60f, 0f), new Vector2(100f, 56f), UITheme.ColorSlateButton, 18f);

            // ---------------------------------------------------------
            // MINIMAP / RADAR HUD (Top-Right under Utility Bar)
            // ---------------------------------------------------------
            GameObject radarCard = CreatePanelObject(safeRoot.transform, "RadarCard", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-115f, -195f), new Vector2(170f, 170f), GetCardSprite(), UITheme.ColorCardSurface);
            
            // Radar circular background
            GameObject radarCircle = CreatePanelObject(radarCard.transform, "RadarScope", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f), GetCircleSprite(), new Color(0.04f, 0.08f, 0.15f, 0.95f));

            // Blip container
            GameObject blipContainer = new GameObject("BlipContainer");
            blipContainer.transform.SetParent(radarCircle.transform, false);
            RectTransform blipRect = blipContainer.AddComponent<RectTransform>();
            blipRect.anchorMin = Vector2.zero;
            blipRect.anchorMax = Vector2.one;
            blipRect.offsetMin = Vector2.zero;
            blipRect.offsetMax = Vector2.zero;

            // Player heading arrow in center
            GameObject arrow = CreatePanelObject(radarCircle.transform, "PlayerArrow", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 18f), GetPillSprite(), UITheme.ColorTechCyan);

            // Gate marker
            GameObject gateMarker = CreatePanelObject(radarCircle.transform, "GateMarker", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 14f), GetButtonSprite(), UITheme.ColorPrimaryGreen);

            MinimapRadar radar = radarCard.AddComponent<MinimapRadar>();
            SerializedObject radarSo = new SerializedObject(radar);
            radarSo.FindProperty("blipContainer").objectReferenceValue = blipRect;
            radarSo.FindProperty("playerArrow").objectReferenceValue = arrow.GetComponent<RectTransform>();
            radarSo.FindProperty("gateMarker").objectReferenceValue = gateMarker.GetComponent<Image>();
            radarSo.ApplyModifiedPropertiesWithoutUndo();

            // ---------------------------------------------------------
            // POWER-UP TOAST BANNER (Top-Center below Objective Card)
            // ---------------------------------------------------------
            GameObject toastObj = CreatePanelObject(safeRoot.transform, "PowerUpToast", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -135f), new Vector2(400f, 40f), GetPillSprite(), UITheme.ColorCardBorder);
            TMP_Text toastText = CreateText(toastObj.transform, "ToastText", "POWER-UP READY!", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 32f), 19f, FontStyles.Bold, Color.white);
            toastText.alignment = TextAlignmentOptions.Center;
            toastObj.SetActive(false);

            hudSo.FindProperty("notificationPanel").objectReferenceValue = toastObj;
            hudSo.FindProperty("notificationText").objectReferenceValue = toastText;
            hudSo.FindProperty("notificationBg").objectReferenceValue = toastObj.GetComponent<Image>();


            // ---------------------------------------------------------
            // BOTTOM CONTROLS (Ergonomic Touch Zones)
            // ---------------------------------------------------------
            // Left Steer Zone
            GameObject steerDock = new GameObject("SteerDock");
            steerDock.transform.SetParent(safeRoot.transform, false);
            RectTransform steerRect = steerDock.AddComponent<RectTransform>();
            steerRect.anchorMin = new Vector2(0f, 0f);
            steerRect.anchorMax = new Vector2(0f, 0f);
            steerRect.anchoredPosition = new Vector2(190f, 110f);
            steerRect.sizeDelta = new Vector2(340f, 140f);

            CreatePedalButton(steerDock.transform, "LeftButton", "◀\nLEFT", new Vector2(-80f, 0f), new Vector2(145f, 115f), TouchButton.ActionType.Left, car, UITheme.ColorSlateButton, 26f);
            CreatePedalButton(steerDock.transform, "RightButton", "▶\nRIGHT", new Vector2(80f, 0f), new Vector2(145f, 115f), TouchButton.ActionType.Right, car, UITheme.ColorSlateButton, 26f);

            // Right Drive Zone
            GameObject driveDock = new GameObject("DriveDock");
            driveDock.transform.SetParent(safeRoot.transform, false);
            RectTransform driveRect = driveDock.AddComponent<RectTransform>();
            driveRect.anchorMin = new Vector2(1f, 0f);
            driveRect.anchorMax = new Vector2(1f, 0f);
            driveRect.anchoredPosition = new Vector2(-190f, 110f);
            driveRect.sizeDelta = new Vector2(340f, 140f);

            CreatePedalButton(driveDock.transform, "BrakeButton", "▼\nBRAKE", new Vector2(-80f, 0f), new Vector2(145f, 115f), TouchButton.ActionType.Brake, car, UITheme.ColorDangerRedDark, 26f);
            CreatePedalButton(driveDock.transform, "GasButton", "▲\nGAS", new Vector2(80f, 0f), new Vector2(145f, 115f), TouchButton.ActionType.Gas, car, UITheme.ColorPrimaryGreenDark, 26f);

            // Screen Damage Flash Overlay
            GameObject flashObj = new GameObject("ScreenDamageFlash");
            flashObj.transform.SetParent(canvasObject.transform, false);
            RectTransform flashRect = flashObj.AddComponent<RectTransform>();
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;
            Image flashImg = flashObj.AddComponent<Image>();
            flashImg.color = new Color(0.9f, 0.1f, 0.1f, 0f);
            flashImg.raycastTarget = false;
            ScreenFlash screenFlash = flashObj.AddComponent<ScreenFlash>();

            // ---------------------------------------------------------
            // MODALS & DIALOGS
            // ---------------------------------------------------------
            GameObject tutModal = CreateTutorialModal(canvasObject.transform);
            TutorialDialog tutorialDialog = tutModal.GetComponent<TutorialDialog>();
            helpBtn.GetComponent<Button>().onClick.AddListener(tutorialDialog.Open);

            GameObject pausePanel = CreatePauseModal(canvasObject.transform, manager, tutorialDialog);
            GameObject winPanel = CreateWinModal(canvasObject.transform, manager, hudSo);
            GameObject gameOverPanel = CreateGameOverModal(canvasObject.transform, manager, hudSo);

            // Wire HudManager fields
            hudSo.FindProperty("scoreText").objectReferenceValue = scoreText;
            hudSo.FindProperty("livesText").objectReferenceValue = livesText;
            hudSo.FindProperty("bountyBadgeText").objectReferenceValue = bountyText;
            hudSo.FindProperty("bountyBadgeBg").objectReferenceValue = bountyPill.GetComponent<Image>();
            hudSo.FindProperty("coinsText").objectReferenceValue = coinsText;
            hudSo.FindProperty("coinsProgressBarFill").objectReferenceValue = fillImg;
            hudSo.FindProperty("timerText").objectReferenceValue = timerText;
            hudSo.FindProperty("timerBadgeBg").objectReferenceValue = timerBadge.GetComponent<Image>();
            hudSo.FindProperty("pauseButton").objectReferenceValue = pauseBtn.GetComponent<Button>();
            hudSo.FindProperty("helpButton").objectReferenceValue = helpBtn.GetComponent<Button>();
            hudSo.FindProperty("tutorialDialog").objectReferenceValue = tutorialDialog;
            hudSo.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            hudSo.FindProperty("winPanel").objectReferenceValue = winPanel;
            hudSo.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            // Wire GameManager fields
            SerializedObject gmSo = new SerializedObject(manager);
            gmSo.FindProperty("scoreText").objectReferenceValue = scoreText;
            gmSo.FindProperty("livesText").objectReferenceValue = livesText;
            gmSo.FindProperty("coinsText").objectReferenceValue = coinsText;
            gmSo.FindProperty("timerText").objectReferenceValue = timerText;
            gmSo.FindProperty("pauseButton").objectReferenceValue = pauseBtn;
            gmSo.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            gmSo.FindProperty("winPanel").objectReferenceValue = winPanel;
            gmSo.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            gmSo.FindProperty("hudManager").objectReferenceValue = hud;
            gmSo.FindProperty("screenFlash").objectReferenceValue = screenFlash;
            gmSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateTutorialModal(Transform parent)
        {
            GameObject overlay = CreatePanelObject(parent, "TutorialModal", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null, UITheme.ColorBgOverlay);
            overlay.AddComponent<CanvasGroup>();
            TutorialDialog dialog = overlay.AddComponent<TutorialDialog>();
            SerializedObject dSo = new SerializedObject(dialog);

            GameObject card = CreatePanelObject(overlay.transform, "TutorialCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 540f), GetCardSprite(), UITheme.ColorCardSurface);

            TMP_Text stepCounter = CreateText(card.transform, "StepCounter", "STEP 1 OF 3", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(300f, 30f), 18f, FontStyles.Bold, UITheme.ColorTechCyan);
            stepCounter.alignment = TextAlignmentOptions.Center;

            TMP_Text title = CreateText(card.transform, "Title", "VEHICLE CONTROLS", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(760f, 48f), 38f, FontStyles.Bold, Color.white);
            title.alignment = TextAlignmentOptions.Center;

            TMP_Text subtitle = CreateText(card.transform, "Subtitle", "Master steering & throttle response", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -125f), new Vector2(760f, 30f), 20f, FontStyles.Normal, UITheme.ColorTextSecondary);
            subtitle.alignment = TextAlignmentOptions.Center;

            GameObject badgeObj = CreatePanelObject(card.transform, "GraphicBadge", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(560f, 48f), GetPillSprite(), UITheme.ColorSlateButton);
            TMP_Text badgeText = CreateText(badgeObj.transform, "BadgeText", "◀ STEER ▶  |  GAS ▲ • BRAKE ▼", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 40f), 22f, FontStyles.Bold, UITheme.ColorAccentGold);
            badgeText.alignment = TextAlignmentOptions.Center;

            TMP_Text desc = CreateText(card.transform, "Description", "Controls description", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(760f, 120f), 22f, FontStyles.Normal, Color.white);
            desc.alignment = TextAlignmentOptions.TopLeft;

            TMP_Text tip = CreateText(card.transform, "Tip", "TIP: Hold gas and steer together", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 105f), new Vector2(760f, 32f), 18f, FontStyles.Italic, UITheme.ColorAccentGold);
            tip.alignment = TextAlignmentOptions.Center;

            // Dots Container
            GameObject dotsObj = new GameObject("DotsContainer");
            dotsObj.transform.SetParent(card.transform, false);
            RectTransform dotsRect = dotsObj.AddComponent<RectTransform>();
            dotsRect.anchorMin = new Vector2(0.5f, 0f);
            dotsRect.anchorMax = new Vector2(0.5f, 0f);
            dotsRect.anchoredPosition = new Vector2(0f, 65f);
            dotsRect.sizeDelta = new Vector2(100f, 20f);

            Image[] dots = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject dot = CreatePanelObject(dotsObj.transform, "Dot_" + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 30f, 0f), new Vector2(14f, 14f), GetCircleSprite(), (i == 0) ? UITheme.ColorAccentGold : UITheme.ColorCardBorder);
                dots[i] = dot.GetComponent<Image>();
            }

            // Navigation Buttons
            GameObject skipBtn = CreateStyledButton(card.transform, "SkipButton", "SKIP", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(100f, 45f), new Vector2(120f, 50f), UITheme.ColorSlateButton, 18f);
            GameObject prevBtn = CreateStyledButton(card.transform, "PrevButton", "◀ BACK", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-120f, 45f), new Vector2(140f, 50f), UITheme.ColorSlateButton, 18f);
            GameObject nextBtn = CreateStyledButton(card.transform, "NextButton", "NEXT ➔", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-120f, 45f), new Vector2(180f, 52f), UITheme.ColorPrimaryGreen, 20f);

            dSo.FindProperty("stepCounterText").objectReferenceValue = stepCounter;
            dSo.FindProperty("titleText").objectReferenceValue = title;
            dSo.FindProperty("subtitleText").objectReferenceValue = subtitle;
            dSo.FindProperty("badgeText").objectReferenceValue = badgeText;
            dSo.FindProperty("descriptionText").objectReferenceValue = desc;
            dSo.FindProperty("tipText").objectReferenceValue = tip;
            dSo.FindProperty("prevButton").objectReferenceValue = prevBtn.GetComponent<Button>();
            dSo.FindProperty("nextButton").objectReferenceValue = nextBtn.GetComponent<Button>();
            dSo.FindProperty("nextButtonText").objectReferenceValue = nextBtn.GetComponentInChildren<TMP_Text>();
            dSo.FindProperty("skipButton").objectReferenceValue = skipBtn.GetComponent<Button>();

            SerializedProperty dotsProp = dSo.FindProperty("dotIndicators");
            dotsProp.arraySize = dots.Length;
            for (int i = 0; i < dots.Length; i++)
                dotsProp.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];

            dSo.ApplyModifiedPropertiesWithoutUndo();
            overlay.SetActive(false);
            return overlay;
        }

        private static GameObject CreatePauseModal(Transform parent, GameManager manager, TutorialDialog tutorialDialog)
        {
            GameObject overlay = CreatePanelObject(parent, "PausePanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null, UITheme.ColorBgOverlay);
            GameObject card = CreatePanelObject(overlay.transform, "PauseCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 480f), GetCardSprite(), UITheme.ColorCardSurface);

            TMP_Text header = CreateText(card.transform, "Header", "GAME PAUSED", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(460f, 50f), 42f, FontStyles.Bold, UITheme.ColorAccentGold);
            header.alignment = TextAlignmentOptions.Center;

            TMP_Text sub = CreateText(card.transform, "Subtitle", "Misi ditangguhkan sementara", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(460f, 30f), 18f, FontStyles.Normal, UITheme.ColorTextSecondary);
            sub.alignment = TextAlignmentOptions.Center;

            GameObject resumeBtn = CreateStyledButton(card.transform, "ResumeBtn", "RESUME MISSION", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(360f, 60f), UITheme.ColorPrimaryGreen, 22f);
            resumeBtn.GetComponent<Button>().onClick.AddListener(manager.TogglePause);

            GameObject guideBtn = CreateStyledButton(card.transform, "GuideBtn", "HOW TO PLAY / GUIDE", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -245f), new Vector2(360f, 54f), UITheme.ColorSlateButton, 20f);
            if (tutorialDialog != null) guideBtn.GetComponent<Button>().onClick.AddListener(tutorialDialog.Open);

            GameObject restartBtn = CreateStyledButton(card.transform, "RestartBtn", "RESTART MISSION", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -315f), new Vector2(360f, 54f), UITheme.ColorSlateButton, 20f);
            restartBtn.GetComponent<Button>().onClick.AddListener(manager.Restart);

            GameObject menuBtn = CreateStyledButton(card.transform, "MenuBtn", "RETURN TO MENU", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -385f), new Vector2(360f, 54f), UITheme.ColorCardBorder, 18f);
            menuBtn.GetComponent<Button>().onClick.AddListener(manager.ReturnToMenu);

            overlay.SetActive(false);
            return overlay;
        }

        private static GameObject CreateWinModal(Transform parent, GameManager manager, SerializedObject hudSo)
        {
            GameObject overlay = CreatePanelObject(parent, "WinPanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null, UITheme.ColorBgOverlay);
            GameObject card = CreatePanelObject(overlay.transform, "WinCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660f, 560f), GetCardSprite(), UITheme.ColorCardSurface);

            TMP_Text header = CreateText(card.transform, "Header", "★ MISSION COMPLETE! ★", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(600f, 50f), 38f, FontStyles.Bold, UITheme.ColorAccentGold);
            header.alignment = TextAlignmentOptions.Center;

            TMP_Text sub = CreateText(card.transform, "Subtitle", "Escape Gate berhasil ditembus!", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -95f), new Vector2(600f, 30f), 20f, FontStyles.Normal, UITheme.ColorPrimaryGreen);
            sub.alignment = TextAlignmentOptions.Center;

            // Stats breakdown container
            GameObject statsBox = CreatePanelObject(card.transform, "StatsBox", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(560f, 200f), GetCardSprite(), UITheme.ColorCardBorder);

            CreateText(statsBox.transform, "L_Coins", "Coins Harvested", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -35f), new Vector2(250f, 30f), 20f, FontStyles.Normal, UITheme.ColorTextSecondary);
            TMP_Text winCoins = CreateText(statsBox.transform, "V_Coins", "15 / 15", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-120f, -35f), new Vector2(180f, 30f), 22f, FontStyles.Bold, Color.white);
            winCoins.alignment = TextAlignmentOptions.Right;

            CreateText(statsBox.transform, "L_Score", "Coin Score", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -75f), new Vector2(250f, 30f), 20f, FontStyles.Normal, UITheme.ColorTextSecondary);
            TMP_Text winScore = CreateText(statsBox.transform, "V_Score", "+150 pts", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-120f, -75f), new Vector2(180f, 30f), 22f, FontStyles.Bold, Color.white);
            winScore.alignment = TextAlignmentOptions.Right;

            CreateText(statsBox.transform, "L_Bonus", "Time Escape Bonus", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -115f), new Vector2(250f, 30f), 20f, FontStyles.Normal, UITheme.ColorTextSecondary);
            TMP_Text winBonus = CreateText(statsBox.transform, "V_Bonus", "+60 pts", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-120f, -115f), new Vector2(180f, 30f), 22f, FontStyles.Bold, UITheme.ColorPrimaryGreen);
            winBonus.alignment = TextAlignmentOptions.Right;

            // Total Score Row
            CreateText(statsBox.transform, "L_Total", "TOTAL MISSION SCORE", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 30f), new Vector2(280f, 35f), 22f, FontStyles.Bold, UITheme.ColorAccentGold);
            TMP_Text winTotal = CreateText(statsBox.transform, "V_Total", "210 PTS", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-120f, 30f), new Vector2(200f, 35f), 30f, FontStyles.Bold, UITheme.ColorAccentGold);
            winTotal.alignment = TextAlignmentOptions.Right;

            // New High Score Badge
            GameObject newRecord = CreatePanelObject(card.transform, "NewRecordBadge", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 115f), new Vector2(300f, 36f), GetPillSprite(), UITheme.ColorAccentGold);
            TMP_Text recordText = CreateText(newRecord.transform, "RecordText", "★ NEW HIGH SCORE! ★", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 30f), 18f, FontStyles.Bold, Color.black);
            recordText.alignment = TextAlignmentOptions.Center;

            // Action Buttons
            GameObject replayBtn = CreateStyledButton(card.transform, "ReplayBtn", "PLAY AGAIN", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-130f, 50f), new Vector2(240f, 56f), UITheme.ColorPrimaryGreen, 20f);
            replayBtn.GetComponent<Button>().onClick.AddListener(manager.Restart);

            GameObject menuBtn = CreateStyledButton(card.transform, "MenuBtn", "MAIN MENU", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(130f, 50f), new Vector2(240f, 56f), UITheme.ColorSlateButton, 20f);
            menuBtn.GetComponent<Button>().onClick.AddListener(manager.ReturnToMenu);

            hudSo.FindProperty("winScoreText").objectReferenceValue = winScore;
            hudSo.FindProperty("winCoinsText").objectReferenceValue = winCoins;
            hudSo.FindProperty("winTimeBonusText").objectReferenceValue = winBonus;
            hudSo.FindProperty("winTotalScoreText").objectReferenceValue = winTotal;
            hudSo.FindProperty("winNewRecordBadge").objectReferenceValue = newRecord;

            overlay.SetActive(false);
            return overlay;
        }

        private static GameObject CreateGameOverModal(Transform parent, GameManager manager, SerializedObject hudSo)
        {
            GameObject overlay = CreatePanelObject(parent, "GameOverPanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null, UITheme.ColorBgOverlay);
            GameObject card = CreatePanelObject(overlay.transform, "GameOverCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 500f), GetCardSprite(), UITheme.ColorCardSurface);

            TMP_Text header = CreateText(card.transform, "Header", "MISSION FAILED", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(560f, 50f), 42f, FontStyles.Bold, UITheme.ColorDangerRed);
            header.alignment = TextAlignmentOptions.Center;

            TMP_Text reason = CreateText(card.transform, "ReasonText", "VEHICLE DESTROYED BY PATROL", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(560f, 40f), 20f, FontStyles.Normal, UITheme.ColorTextSecondary);
            reason.alignment = TextAlignmentOptions.Center;

            // Stats box
            GameObject statsBox = CreatePanelObject(card.transform, "StatsBox", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(500f, 140f), GetCardSprite(), UITheme.ColorCardBorder);

            CreateText(statsBox.transform, "L_Coins", "Coins Retrieved", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -35f), new Vector2(250f, 30f), 20f, FontStyles.Normal, UITheme.ColorTextSecondary);
            TMP_Text loseCoins = CreateText(statsBox.transform, "V_Coins", "8 / 15", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-120f, -35f), new Vector2(180f, 30f), 22f, FontStyles.Bold, Color.white);
            loseCoins.alignment = TextAlignmentOptions.Right;

            CreateText(statsBox.transform, "L_Score", "Final Score Attained", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 30f), new Vector2(280f, 35f), 22f, FontStyles.Bold, UITheme.ColorAccentGold);
            TMP_Text loseScore = CreateText(statsBox.transform, "V_Score", "80 PTS", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-120f, 30f), new Vector2(180f, 35f), 28f, FontStyles.Bold, UITheme.ColorAccentGold);
            loseScore.alignment = TextAlignmentOptions.Right;

            // Action Buttons
            GameObject retryBtn = CreateStyledButton(card.transform, "RetryBtn", "TRY AGAIN", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-125f, 50f), new Vector2(230f, 56f), UITheme.ColorDangerRed, 20f);
            retryBtn.GetComponent<Button>().onClick.AddListener(manager.Restart);

            GameObject menuBtn = CreateStyledButton(card.transform, "MenuBtn", "MAIN MENU", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(125f, 50f), new Vector2(230f, 56f), UITheme.ColorSlateButton, 20f);
            menuBtn.GetComponent<Button>().onClick.AddListener(manager.ReturnToMenu);

            hudSo.FindProperty("loseReasonText").objectReferenceValue = reason;
            hudSo.FindProperty("loseCoinsText").objectReferenceValue = loseCoins;
            hudSo.FindProperty("loseScoreText").objectReferenceValue = loseScore;

            overlay.SetActive(false);
            return overlay;
        }

        // -------------------------------------------------------------
        // UI ELEMENT HELPERS
        // -------------------------------------------------------------
        private static GameObject CreatePanelObject(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Sprite sprite, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = pos;
            if (size != Vector2.zero) rect.sizeDelta = size;
            Image img = obj.AddComponent<Image>();
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            return obj;
        }

        private static GameObject CreateStyledButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color color, float fontSize)
        {
            GameObject obj = CreatePanelObject(parent, name, anchorMin, anchorMax, pos, size, GetButtonSprite(), color);
            Button btn = obj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(color.r * 1.15f, color.g * 1.15f, color.b * 1.15f, color.a);
            cb.pressedColor = new Color(color.r * 0.85f, color.g * 0.85f, color.b * 0.85f, color.a);
            btn.colors = cb;

            TMP_Text txt = CreateText(obj.transform, "Label", label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fontSize, FontStyles.Bold, Color.white);
            txt.alignment = TextAlignmentOptions.Center;
            return obj;
        }

        private static GameObject CreatePedalButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, TouchButton.ActionType action, CarController car, Color color, float fontSize)
        {
            GameObject obj = CreatePanelObject(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size, GetButtonSprite(), color);
            obj.AddComponent<Button>();

            TouchButton touch = obj.AddComponent<TouchButton>();
            SerializedObject s = new SerializedObject(touch);
            s.FindProperty("action").enumValueIndex = (int)action;
            s.FindProperty("car").objectReferenceValue = car;
            s.ApplyModifiedPropertiesWithoutUndo();

            TMP_Text txt = CreateText(obj.transform, "Label", label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fontSize, FontStyles.Bold, Color.white);
            txt.alignment = TextAlignmentOptions.Center;
            return obj;
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, float fontSize, FontStyles style, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = pos;
            if (size != Vector2.zero) rect.sizeDelta = size;
            TMP_Text text = obj.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.enableWordWrapping = false;
            return text;
        }

        private static GameObject CreateCar(string name, Vector3 position, Color color)
        {
            GameObject car = GameObject.CreatePrimitive(PrimitiveType.Cube);
            car.name = name;
            car.transform.position = position;
            car.transform.localScale = new Vector3(1.8f, 0.7f, 3f);
            SetColor(car, color);
            Rigidbody body = car.AddComponent<Rigidbody>();
            body.mass = 1100f;

            // Cabin Roof
            GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin";
            cabin.transform.SetParent(car.transform, false);
            cabin.transform.localPosition = new Vector3(0f, 0.65f, -0.15f);
            cabin.transform.localScale = new Vector3(0.85f, 0.6f, 0.55f);
            SetColor(cabin, new Color(color.r * 0.35f, color.g * 0.35f, color.b * 0.35f));
            Object.DestroyImmediate(cabin.GetComponent<Collider>());

            // Front Headlights
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -0.35f : 0.35f;
                GameObject light = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                light.name = "Headlight_" + i;
                light.transform.SetParent(car.transform, false);
                light.transform.localPosition = new Vector3(x, 0.1f, 0.52f);
                light.transform.localScale = new Vector3(0.18f, 0.18f, 0.1f);
                SetColor(light, new Color(1f, 0.95f, 0.7f));
                Object.DestroyImmediate(light.GetComponent<Collider>());
            }

            // 4 Wheels
            Vector3[] wheelPos = new Vector3[]
            {
                new Vector3(-0.55f, -0.3f, 0.35f),
                new Vector3(0.55f, -0.3f, 0.35f),
                new Vector3(-0.55f, -0.3f, -0.35f),
                new Vector3(0.55f, -0.3f, -0.35f)
            };
            for (int i = 0; i < wheelPos.Length; i++)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Wheel_" + i;
                wheel.transform.SetParent(car.transform, false);
                wheel.transform.localPosition = wheelPos[i];
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.transform.localScale = new Vector3(0.42f, 0.12f, 0.42f);
                SetColor(wheel, new Color(0.12f, 0.12f, 0.15f));
                Object.DestroyImmediate(wheel.GetComponent<Collider>());
            }

            return car;
        }

        private static GameObject CreatePowerUp(string name, Vector3 pos, PowerUp.PowerType type, Color color)
        {
            GameObject root = new GameObject(name);
            root.transform.position = pos;

            // Rotating floating gemstone
            GameObject gem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gem.name = "Gem";
            gem.transform.SetParent(root.transform, false);
            gem.transform.localScale = new Vector3(0.7f, 0.4f, 0.7f);
            gem.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            SetColor(gem, color);
            SetEmission(gem, color, 1.25f);

            // Trigger sphere collider
            SphereCollider col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 1.3f;

            PowerUp power = root.AddComponent<PowerUp>();
            SerializedObject pSo = new SerializedObject(power);
            pSo.FindProperty("type").enumValueIndex = (int)type;
            pSo.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static GameObject CreatePatrolEnemy(Vector3 pos, Vector3[] waypoints)
        {
            GameObject enemy = CreateCar("EnemyPatrol", pos, new Color(0.95f, 0.65f, 0.1f));
            enemy.tag = "Enemy";
            NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
            agent.speed = 2.5f;
            agent.angularSpeed = 360f;
            agent.acceleration = 10f;

            Transform[] wpTransforms = new Transform[waypoints.Length];
            GameObject wpGroup = new GameObject("PatrolWaypoints");
            for (int i = 0; i < waypoints.Length; i++)
            {
                GameObject wp = new GameObject("WP_" + (i + 1));
                wp.transform.SetParent(wpGroup.transform, false);
                wp.transform.position = waypoints[i];
                wpTransforms[i] = wp.transform;
            }

            PatrolEnemy patrol = enemy.AddComponent<PatrolEnemy>();
            SerializedObject so = new SerializedObject(patrol);
            var arrProp = so.FindProperty("waypoints");
            arrProp.arraySize = wpTransforms.Length;
            for (int i = 0; i < wpTransforms.Length; i++)
            {
                arrProp.GetArrayElementAtIndex(i).objectReferenceValue = wpTransforms[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return enemy;
        }

        private static GameObject CreateConvoySpawner(GameObject enemyTemplate)
        {
            GameObject spawnerObj = new GameObject("ConvoySpawner");
            ConvoySpawner spawner = spawnerObj.AddComponent<ConvoySpawner>();

            Vector3[] spawnPos = new Vector3[]
            {
                new Vector3(-22f, 0.7f, -18f),
                new Vector3(22f, 0.7f, -18f),
                new Vector3(-22f, 0.7f, 18f),
                new Vector3(22f, 0.7f, 18f)
            };

            Transform[] spTransforms = new Transform[spawnPos.Length];
            GameObject spGroup = new GameObject("ConvoySpawnPoints");
            spGroup.transform.SetParent(spawnerObj.transform, false);
            for (int i = 0; i < spawnPos.Length; i++)
            {
                GameObject sp = new GameObject("SpawnPoint_" + (i + 1));
                sp.transform.SetParent(spGroup.transform, false);
                sp.transform.position = spawnPos[i];
                spTransforms[i] = sp.transform;
            }

            SerializedObject so = new SerializedObject(spawner);
            so.FindProperty("enemyPrefab").objectReferenceValue = enemyTemplate;
            var arrProp = so.FindProperty("spawnPoints");
            arrProp.arraySize = spTransforms.Length;
            for (int i = 0; i < spTransforms.Length; i++)
            {
                arrProp.GetArrayElementAtIndex(i).objectReferenceValue = spTransforms[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return spawnerObj;
        }

        private static GameObject CreateMovingObstacle(string name, Vector3 posA, Vector3 posB, float speed)
        {
            GameObject root = new GameObject(name);
            root.transform.position = posA;

            GameObject pA = new GameObject(name + "_PointA");
            pA.transform.position = posA;
            GameObject pB = new GameObject(name + "_PointB");
            pB.transform.position = posB;

            GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = "BarrierVisual";
            barrier.tag = "Enemy";
            barrier.transform.SetParent(root.transform, false);
            barrier.transform.localScale = new Vector3(3.5f, 1.2f, 0.8f);
            SetColor(barrier, new Color(0.95f, 0.45f, 0.1f));

            Rigidbody rb = barrier.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            MovingObstacle mo = root.AddComponent<MovingObstacle>();
            SerializedObject so = new SerializedObject(mo);
            so.FindProperty("pointA").objectReferenceValue = pA.transform;
            so.FindProperty("pointB").objectReferenceValue = pB.transform;
            so.FindProperty("speed").floatValue = speed;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static GameObject CreatePathLights(Vector3 start, Vector3 end, int count)
        {
            GameObject root = new GameObject("PathLights");
            GameObject[] lamps = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                Vector3 pos = Vector3.Lerp(start, end, t);

                GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "RunwayLamp_" + (i + 1);
                pole.transform.SetParent(root.transform, false);
                pole.transform.position = pos;
                pole.transform.localScale = new Vector3(0.2f, 0.8f, 0.2f);
                SetColor(pole, new Color(0.2f, 0.2f, 0.25f));

                GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bulb.name = "Bulb";
                bulb.transform.SetParent(pole.transform, false);
                bulb.transform.localPosition = new Vector3(0f, 1f, 0f);
                bulb.transform.localScale = new Vector3(1.4f, 0.35f, 1.4f);
                SetColor(bulb, new Color(0.1f, 0.95f, 0.4f));
                SetEmission(bulb, new Color(0.1f, 0.95f, 0.4f), 1.4f);

                lamps[i] = bulb;
            }

            PathLights pathLights = root.AddComponent<PathLights>();
            SerializedObject so = new SerializedObject(pathLights);
            var arrProp = so.FindProperty("lights");
            arrProp.arraySize = lamps.Length;
            for (int i = 0; i < lamps.Length; i++)
            {
                arrProp.GetArrayElementAtIndex(i).objectReferenceValue = lamps[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static void CreateAudioAndOptimizer()
        {
            GameObject audioObj = new GameObject("MusicManager");
            audioObj.AddComponent<AudioSource>();
            MusicManager mm = audioObj.AddComponent<MusicManager>();

            AudioClip ambience = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ForestAmbience.wav");
            AudioClip danger = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Fire.wav");

            SerializedObject so = new SerializedObject(mm);
            if (ambience != null) so.FindProperty("normalTrack").objectReferenceValue = ambience;
            if (danger != null) so.FindProperty("dangerTrack").objectReferenceValue = danger;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject optObj = new GameObject("MobileOptimizer");
            optObj.AddComponent<MobileOptimizer>();
        }


        private static Camera CreateCamera(Transform target)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            FollowCamera follow = cameraObject.AddComponent<FollowCamera>();
            SerializedObject serialized = new SerializedObject(follow);
            serialized.FindProperty("target").objectReferenceValue = target;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return camera;
        }

        private static void SetColor(GameObject obj, Color color)
        {
            if (!SharedColorMaterials.TryGetValue(color, out Material material) || material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { color = color, enableInstancing = true };
                SharedColorMaterials[color] = material;
            }
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }

        private static void SetEmission(GameObject obj, Color color, float intensity)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterial == null ||
                !renderer.sharedMaterial.HasProperty("_EmissionColor")) return;

            Material material = renderer.sharedMaterial;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * intensity);
        }

        // -------------------------------------------------------------
        // ASSET SPRITE CREATION & 9-SLICE IMPORT
        // -------------------------------------------------------------
        public static void EnsureUISprites()
        {
            if (!Directory.Exists(SpriteDir))
            {
                Directory.CreateDirectory(SpriteDir);
            }

            CreateAndSavePng(CardSpritePath, 64, 64, 18, 2, new Vector4(20, 20, 20, 20));
            CreateAndSavePng(ButtonSpritePath, 48, 48, 14, 2, new Vector4(16, 16, 16, 16));
            CreateAndSavePng(PillSpritePath, 48, 48, 23, 2, new Vector4(24, 24, 24, 24));
            CreateAndSaveCirclePng(CircleSpritePath, 64);
            AssetDatabase.Refresh();
        }

        private static void CreateAndSavePng(string path, int width, int height, int radius, int border, Vector4 spriteBorder)
        {
            if (File.Exists(path)) return;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = 0f;
                    if (x < radius) dx = radius - (x + 0.5f);
                    else if (x >= width - radius) dx = (x + 0.5f) - (width - radius);

                    float dy = 0f;
                    if (y < radius) dy = radius - (y + 0.5f);
                    else if (y >= height - radius) dy = (y + 0.5f) - (height - radius);

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float outerAlpha = Mathf.Clamp01(radius + 0.5f - dist);
                    if (outerAlpha <= 0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float innerRadius = radius - border;
                        float innerAlpha = Mathf.Clamp01(innerRadius + 0.5f - dist);
                        Color c = Color.Lerp(new Color(1f, 1f, 1f, 0.7f), Color.white, innerAlpha);
                        c.a *= outerAlpha;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteBorder = spriteBorder;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        private static void CreateAndSaveCirclePng(string path, int size)
        {
            if (File.Exists(path)) return;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = (size - 4) * 0.5f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(radius + 0.5f - dist);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        private static Sprite GetCardSprite() => AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath) ?? UITheme.GetCardSprite();
        private static Sprite GetButtonSprite() => AssetDatabase.LoadAssetAtPath<Sprite>(ButtonSpritePath) ?? UITheme.GetButtonSprite();
        private static Sprite GetPillSprite() => AssetDatabase.LoadAssetAtPath<Sprite>(PillSpritePath) ?? UITheme.GetPillSprite();
        private static Sprite GetCircleSprite() => AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath) ?? UITheme.GetCircleSprite();
    }
}
