using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EnchantedForest;

namespace EnchantedForest.Editor
{
    public class LevelDesignAutoBuilder : EditorWindow
    {
        private const string MaterialsPath = "Assets/Materials/EnchantedForest";
        private const string ScenesPath = "Assets/Scenes";

        [InitializeOnLoadMethod]
        private static void AutoRunOnFirstLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorPrefs.GetInt("EnchantedForest_Version", 0) < 3)
                {
                    EditorPrefs.SetInt("EnchantedForest_Version", 3);
                    Debug.Log("[Enchanted Forest] Generating all 3 levels with Player & Sound System...");
                    GenerateAll(showDialog: false);
                    Debug.Log("[Enchanted Forest] All 3 levels with Player & Sound updated successfully!");
                }
            };
        }

        [MenuItem("Tools/Enchanted Forest/Level Design Control Panel", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelDesignAutoBuilder>("Enchanted Forest Studio");
            window.minSize = new Vector2(420, 520);
        }

        [MenuItem("Tools/Enchanted Forest/⚡ Generate All 3 Levels Now", false, 2)]
        public static void GenerateAllLevelsMenu()
        {
            GenerateAll(showDialog: true);
        }

        public static void GenerateAllBatch()
        {
            GenerateAll(showDialog: false);
        }

        private void OnGUI()
        {
            GUILayout.Space(15);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.2f, 0.85f, 0.5f) }
            };

            GUILayout.Label("🌲 ENCHANTED FOREST 🌲", headerStyle);
            GUILayout.Label("Automated 3D Level Design Studio (Unity 6 / URP)", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(15);

            EditorGUILayout.HelpBox(
                "Tool ini akan secara otomatis membuat:\n" +
                "1. Karakter Player Playable (WASD + Lompat + Audio Langkah/Lompat)\n" +
                "2. Audio Manager & Sound Effects (Koin, Bounce Pad, Lompat, Mendarat, BGM Hutan)\n" +
                "3. Palet Material Enchanted Forest & 3 Level Lengkap\n" +
                "4. Kamera Ganda: Drone Sinematik (Rekam Video) & Third-Person Player (Tekan C/Tab)",
                MessageType.Info);

            GUILayout.Space(15);

            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.4f);
            if (GUILayout.Button("⚡ GENERATE ALL 3 LEVELS NOW", GUILayout.Height(45)))
            {
                GenerateAll();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(20);
            EditorGUILayout.LabelField("Buka & Preview Level:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Level 1\nPinggir Hutan", GUILayout.Height(38)))
            {
                OpenScene("Level_1_PinggirHutan.unity");
            }
            if (GUILayout.Button("Level 2\nJurang Akar", GUILayout.Height(38)))
            {
                OpenScene("Level_2_JurangAkar.unity");
            }
            if (GUILayout.Button("Level 3\nPuncak Pohon", GUILayout.Height(38)))
            {
                OpenScene("Level_3_PuncakPohon.unity");
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);
            EditorGUILayout.HelpBox(
                "TIPS KONTROL & REKAM VIDEO:\n" +
                "• Tekan [PLAY (▶)] di Unity Editor.\n" +
                "• Mode bawaan: Kamera Drone Otomatis (Sangat pas untuk rekam video 3 menit!).\n" +
                "• Tekan tombol [C] atau [Tab]: Beralih ke Karakter Pemain untuk dimainkan langsung dengan suara!",
                MessageType.None);
        }

        private static void OpenScene(string sceneFileName)
        {
            string fullPath = Path.Combine(ScenesPath, sceneFileName);
            if (File.Exists(fullPath))
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(fullPath);
            }
            else
            {
                EditorUtility.DisplayDialog("Scene Belum Dibuat",
                    $"File {sceneFileName} belum ada. Silakan klik tombol 'GENERATE ALL 3 LEVELS NOW' terlebih dahulu.", "OK");
            }
        }

        public static void GenerateAll(bool showDialog = true)
        {
            if (showDialog) EditorUtility.DisplayProgressBar("Enchanted Forest", "Membuat folder dan material...", 0.1f);
            EnsureFolders();
            var mats = CreatePaletteMaterials();

            if (showDialog) EditorUtility.DisplayProgressBar("Enchanted Forest", "Membangun Level 1: Pinggir Hutan...", 0.35f);
            BuildLevel1(mats);

            if (showDialog) EditorUtility.DisplayProgressBar("Enchanted Forest", "Membangun Level 2: Jurang Akar...", 0.65f);
            BuildLevel2(mats);

            if (showDialog) EditorUtility.DisplayProgressBar("Enchanted Forest", "Membangun Level 3: Puncak Pohon Raksasa...", 0.9f);
            BuildLevel3(mats);

            RegisterScenesInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Sukses!",
                    "Semua 3 Level Enchanted Forest berhasil dibuat dan disimpan di Assets/Scenes/!\n\n" +
                    "Kini sudah dilengkapi:\n" +
                    "✔ Karakter Player Playable (WASD + Jump)\n" +
                    "✔ Sistem Audio Lengkap (SFX Koin, Bounce Pad, Footstep, & BGM Hutan)\n" +
                    "✔ Mode Kamera Ganda (Tekan C/Tab untuk switch Drone / Player)\n\n" +
                    "Level 1: Level_1_PinggirHutan.unity\n" +
                    "Level 2: Level_2_JurangAkar.unity\n" +
                    "Level 3: Level_3_PuncakPohon.unity", "Keren Banget!");
            }

            // Buka Level 1 sebagai default tampilan
            OpenScene("Level_1_PinggirHutan.unity");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(MaterialsPath))
                AssetDatabase.CreateFolder("Assets/Materials", "EnchantedForest");
            if (!AssetDatabase.IsValidFolder(ScenesPath))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        public class ForestMaterials
        {
            public Material Grass;
            public Material DarkWood;
            public Material AncientStone;
            public Material GoldCoin;
            public Material HazardThorn;
            public Material CrystalCyan;
            public Material EnemyArmor;
            public Material EnemyGlow;
            public Material FinishPortal;
            public Material TreeBark;
            public Material Foliage;
            public Material SkyMountain;
            public Material SkyCloud;
            public Material SwampWater;
            public Material PlayerTunic;
            public Material PlayerSkin;
            public Material PlayerLeather;
        }

        private static ForestMaterials CreatePaletteMaterials()
        {
            var mats = new ForestMaterials();
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            mats.Grass = GetOrCreateMaterial("Mat_LushGrass", litShader, new Color(0.18f, 0.48f, 0.25f), 0.1f, 0.2f);
            mats.DarkWood = GetOrCreateMaterial("Mat_DarkWood", litShader, new Color(0.28f, 0.16f, 0.08f), 0.0f, 0.35f);
            mats.AncientStone = GetOrCreateMaterial("Mat_AncientStone", litShader, new Color(0.38f, 0.42f, 0.44f), 0.1f, 0.15f);
            mats.GoldCoin = GetOrCreateMaterial("Mat_GoldCoin", litShader, new Color(1.0f, 0.82f, 0.25f), 0.9f, 0.85f, new Color(1.0f, 0.72f, 0.1f) * 0.8f);
            mats.HazardThorn = GetOrCreateMaterial("Mat_HazardThorn", litShader, new Color(0.42f, 0.06f, 0.15f), 0.2f, 0.4f, new Color(0.7f, 0.1f, 0.2f) * 0.5f);
            mats.CrystalCyan = GetOrCreateMaterial("Mat_CrystalCyan", litShader, new Color(0.0f, 0.95f, 0.85f), 0.3f, 0.9f, new Color(0.0f, 0.95f, 0.85f) * 1.5f);
            mats.EnemyArmor = GetOrCreateMaterial("Mat_EnemyArmor", litShader, new Color(0.65f, 0.12f, 0.16f), 0.5f, 0.6f);
            mats.EnemyGlow = GetOrCreateMaterial("Mat_EnemyGlow", litShader, new Color(1.0f, 0.2f, 0.1f), 0.1f, 0.9f, new Color(1.0f, 0.3f, 0.1f) * 2.0f);
            mats.FinishPortal = GetOrCreateMaterial("Mat_FinishPortal", litShader, new Color(1.0f, 0.88f, 0.35f), 0.2f, 0.9f, new Color(1.0f, 0.75f, 0.1f) * 2.5f);
            mats.TreeBark = GetOrCreateMaterial("Mat_TreeBark", litShader, new Color(0.22f, 0.13f, 0.07f), 0.0f, 0.15f);
            mats.Foliage = GetOrCreateMaterial("Mat_Foliage", litShader, new Color(0.12f, 0.38f, 0.2f), 0.0f, 0.1f);
            mats.SkyMountain = GetOrCreateMaterial("Mat_SkyMountain", litShader, new Color(0.28f, 0.38f, 0.43f), 0.0f, 0.05f);
            mats.SkyCloud = GetOrCreateMaterial("Mat_SkyCloud", litShader, new Color(0.88f, 0.91f, 0.90f), 0.0f, 0.05f);
            mats.SwampWater = GetOrCreateMaterial("Mat_SwampWater", litShader, new Color(0.06f, 0.18f, 0.22f, 0.9f), 0.1f, 0.95f);
            mats.PlayerTunic = GetOrCreateMaterial("Mat_PlayerTunic", litShader, new Color(0.15f, 0.45f, 0.85f), 0.0f, 0.3f);
            mats.PlayerSkin = GetOrCreateMaterial("Mat_PlayerSkin", litShader, new Color(0.95f, 0.76f, 0.65f), 0.0f, 0.2f);
            mats.PlayerLeather = GetOrCreateMaterial("Mat_PlayerLeather", litShader, new Color(0.35f, 0.2f, 0.1f), 0.0f, 0.25f);

            return mats;
        }

        private static Material GetOrCreateMaterial(string name, Shader shader, Color color, float metallic, float smoothness, Color? emission = null)
        {
            string assetPath = $"{MaterialsPath}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, assetPath);
            }

            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);

            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ==========================================
        // LEVEL 1: PINGGIR HUTAN
        // ==========================================
        private static void BuildLevel1(ForestMaterials mats)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting & Atmosphere
            SetupLighting(new Color(1f, 0.95f, 0.85f), 1.2f, new Vector3(45, -30, 0), new Color(0.6f, 0.8f, 0.7f), 0.012f, "ForestDawn");

            // Roots
            GameObject envRoot = new GameObject("--- ENVIRONMENT & PLATFORMS ---");
            GameObject coinsRoot = new GameObject("--- COINS ---");
            GameObject hazardsRoot = new GameObject("--- HAZARDS ---");
            GameObject enemiesRoot = new GameObject("--- ENEMIES ---");
            GameObject decorsRoot = new GameObject("--- DECORATIONS ---");
            GameObject camRoot = new GameObject("--- SHOWCASE CAMERA ---");

            // 1. Start Platform
            CreatePlatform("Platform_0_Start", new Vector3(0, 0, 0), new Vector3(10, 1, 10), mats.Grass, envRoot);
            CreateStoneArch("StartArch", new Vector3(0, 0.5f, -3.5f), mats.AncientStone, envRoot);
            CreateSpawnBeacon(new Vector3(0, 0.6f, 0), mats.CrystalCyan, envRoot);

            // 2. Main Walkway with Low Brambles & Pulsing Poison Fungus
            CreatePlatform("Platform_1_Walkway", new Vector3(0, 0, 11), new Vector3(8, 1, 10), mats.Grass, envRoot);
            CreateThornHazard(new Vector3(-2.5f, 0.8f, 11), new Vector3(1.0f, 0.6f, 4f), mats.HazardThorn, hazardsRoot);
            CreatePulsingPoisonFungus(new Vector3(3.0f, 0.5f, 13), mats, hazardsRoot);

            // 3. Stepping Stones
            CreatePlatform("StepStone_1", new Vector3(-2.2f, 1.0f, 20), new Vector3(2.5f, 0.8f, 2.5f), mats.AncientStone, envRoot);
            CreatePlatform("StepStone_2", new Vector3(2.2f, 1.8f, 25), new Vector3(2.5f, 0.8f, 2.5f), mats.AncientStone, envRoot);

            // 4. Spore Bounce Pad (Melontarkan pemain tinggi ke atas jembatan!)
            CreateBouncePad(new Vector3(0, 1.5f, 32), mats, envRoot);

            // 5. Elevated Wooden Bridge (Jalur Tinggi setelah Bounce Pad)
            CreatePlatform("Platform_2_ElevatedBridge", new Vector3(0, 4.5f, 44), new Vector3(5, 1, 14), mats.DarkWood, envRoot);
            CreateEnemyPlaceholder("Enemy_WoodlingScout", new Vector3(0, 5.5f, 44), mats, enemiesRoot, EnemyPlaceholder.EnemyType.WoodlingScout, true, 3.5f, Vector3.forward);

            // 6. Finish Sanctuary
            CreatePlatform("Platform_3_FinishSanctuary", new Vector3(0, 5.5f, 62), new Vector3(12, 1.2f, 12), mats.Grass, envRoot);
            CreateFinishPortal(new Vector3(0, 6.1f, 65), mats, envRoot);

            // Coins
            // Koin busur lompat
            CreateCoin(new Vector3(-2.2f, 2.2f, 20), mats.GoldCoin, coinsRoot);
            CreateCoin(new Vector3(2.2f, 3.0f, 25), mats.GoldCoin, coinsRoot);
            // Koin busur peluncuran di atas Bounce Pad
            for (int i = 0; i < 4; i++)
            {
                float t = i / 3.0f;
                float z = Mathf.Lerp(32f, 38f, t);
                float y = Mathf.Lerp(3.5f, 6.5f, Mathf.Sin(t * Mathf.PI)) + 0.5f;
                CreateCoin(new Vector3(0, y, z), mats.GoldCoin, coinsRoot);
            }
            // Koin di sepanjang jembatan elevated
            CreateCoin(new Vector3(0, 5.8f, 40), mats.GoldCoin, coinsRoot);
            CreateCoin(new Vector3(0, 5.8f, 44), mats.GoldCoin, coinsRoot);
            CreateCoin(new Vector3(0, 5.8f, 48), mats.GoldCoin, coinsRoot);
            // Secret Coin near start
            CreateCoin(new Vector3(4f, 1.5f, 1), mats.GoldCoin, coinsRoot);

            // Ornaments
            CreateStylizedTree(new Vector3(-4.5f, 0.5f, -2f), 1.2f, mats, decorsRoot);
            CreateStylizedTree(new Vector3(4.5f, 0.5f, 3f), 1.0f, mats, decorsRoot);
            CreateStylizedTree(new Vector3(-5f, 0.5f, 12f), 1.4f, mats, decorsRoot);
            CreateStylizedTree(new Vector3(5f, 4.5f, 44f), 1.2f, mats, decorsRoot);
            CreateGlowingMushroom(new Vector3(-3f, 0.5f, 8f), mats, decorsRoot);
            CreateGlowingMushroom(new Vector3(3.2f, 0.5f, 10f), mats, decorsRoot);
            CreateGlowingMushroom(new Vector3(-3.5f, 5.5f, 60f), mats, decorsRoot);
            CreateSkyBackdrop(new Vector3(0, 0, 32), 0f, 42f, mats, decorsRoot);

            // Showcase Camera setup
            List<Vector3> camWaypoints = new List<Vector3>
            {
                new Vector3(-4, 4f, -6f),
                new Vector3(-3, 3.5f, 10f),
                new Vector3(0, 4.5f, 25f),
                new Vector3(0, 6.5f, 35f),
                new Vector3(-3, 7.5f, 46f),
                new Vector3(0, 8f, 58f)
            };
            GameObject scCam = SetupShowcaseCamera(camWaypoints, camRoot, 3.2f);
            CreateGameAudioManager(envRoot);
            CreatePlayerCharacter(new Vector3(0, 0.6f, 0), mats, envRoot, scCam);

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/Level_1_PinggirHutan.unity");
        }

        // ==========================================
        // LEVEL 2: JURANG AKAR
        // ==========================================
        private static void BuildLevel2(ForestMaterials mats)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Deep Twilight Fog & Lighting
            SetupLighting(new Color(0.9f, 0.8f, 0.95f), 0.9f, new Vector3(35, 45, 0), new Color(0.25f, 0.2f, 0.35f), 0.022f, "Twilight");

            GameObject envRoot = new GameObject("--- ENVIRONMENT & PLATFORMS ---");
            GameObject coinsRoot = new GameObject("--- COINS ---");
            GameObject hazardsRoot = new GameObject("--- HAZARDS ---");
            GameObject enemiesRoot = new GameObject("--- ENEMIES ---");
            GameObject decorsRoot = new GameObject("--- DECORATIONS ---");
            GameObject camRoot = new GameObject("--- SHOWCASE CAMERA ---");

            // Swamp / Chasm Bed far below
            GameObject bog = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bog.name = "ChasmBogFloor";
            bog.transform.SetParent(envRoot.transform);
            bog.transform.position = new Vector3(0, -5, 40);
            bog.transform.localScale = new Vector3(60, 1, 100);
            bog.GetComponent<Renderer>().sharedMaterial = mats.SwampWater;

            // 1. High Start Cliff
            CreatePlatform("Cliff_Start", new Vector3(0, 10, 0), new Vector3(9, 2, 8), mats.AncientStone, envRoot);
            CreateSpawnBeacon(new Vector3(0, 11.2f, -1f), mats.CrystalCyan, envRoot);

            // DUAL PATH FORK:
            // JALUR ATAS (High Risk, High Reward): Moving Platform X melintasi jurang
            GameObject movePlat1 = CreatePlatform("UpperPath_MovingPlatX", new Vector3(4f, 10.5f, 16), new Vector3(4.0f, 0.8f, 4.0f), mats.DarkWood, envRoot);
            var fp1 = movePlat1.AddComponent<FloatingPlatform>();
            fp1.direction = FloatingPlatform.MoveDirection.HorizontalX;
            fp1.travelDistance = 5f;
            fp1.moveDuration = 3f;

            // Pendulum di atas jalur bergerak
            CreatePendulumHazard(new Vector3(6.5f, 15f, 16), mats, hazardsRoot);

            // JALUR BAWAH (Aman tapi Berliku): Jembatan Akar Rawa
            CreatePlatform("LowerPath_RootBridge1", new Vector3(-3.5f, 8.5f, 10), new Vector3(2.5f, 1f, 8f), mats.DarkWood, envRoot);
            CreatePlatform("LowerPath_RootBridge2", new Vector3(-3.5f, 8.0f, 20), new Vector3(2.5f, 1f, 8f), mats.DarkWood, envRoot);

            // Musuh Laba-laba menggantung di jalur bawah
            CreateEnemyPlaceholder("Enemy_ArachnidWeaver", new Vector3(-3.5f, 8.8f, 15), mats, enemiesRoot, EnemyPlaceholder.EnemyType.ArachnidWeaver, false, 2.5f);

            // 3. Mid Plateau Sanctuary (Titik Temu Kedua Jalur)
            CreatePlatform("Platform_MidIsle", new Vector3(0, 8.5f, 32), new Vector3(9, 1.5f, 9), mats.Grass, envRoot);

            // Tanaman Penembak Spora di tebing samping
            CreateEnemyPlaceholder("Enemy_ThornSpitter", new Vector3(-4.5f, 9.5f, 32), mats, enemiesRoot, EnemyPlaceholder.EnemyType.ThornSpitter, false);

            // Spinning Log Hazard di tengah Mid Isle
            GameObject spikeLog = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spikeLog.name = "SpinningLogHazard";
            spikeLog.transform.SetParent(hazardsRoot.transform);
            spikeLog.transform.position = new Vector3(0, 9.8f, 36.5f);
            spikeLog.transform.localScale = new Vector3(0.6f, 4f, 0.6f);
            spikeLog.transform.rotation = Quaternion.Euler(0, 0, 90);
            spikeLog.GetComponent<Renderer>().sharedMaterial = mats.HazardThorn;
            var trap = spikeLog.AddComponent<HazardTrap>();
            trap.trapType = HazardTrap.TrapType.ContinuousRotation;
            trap.rotationAxis = Vector3.forward;
            trap.rotationSpeed = 120f;

            // 4. Moving Platform 2 (Vertical Elevator Climb)
            GameObject movePlat2 = CreatePlatform("MovingPlatform_Y", new Vector3(0, 9.0f, 45), new Vector3(4f, 0.8f, 4f), mats.DarkWood, envRoot);
            var fp2 = movePlat2.AddComponent<FloatingPlatform>();
            fp2.direction = FloatingPlatform.MoveDirection.VerticalY;
            fp2.travelDistance = 5f;
            fp2.moveDuration = 3.5f;

            // 5. Suspended High Root Span with Pendulum
            CreatePlatform("RootBridge_FinalSpan", new Vector3(0, 14f, 57), new Vector3(2.5f, 1f, 12f), mats.DarkWood, envRoot);
            CreatePendulumHazard(new Vector3(0, 18f, 57), mats, hazardsRoot);

            // 6. Ancient Sanctuary Summit (Finish)
            CreatePlatform("Platform_FinishSanctuary", new Vector3(0, 14.5f, 72), new Vector3(12, 2f, 12), mats.AncientStone, envRoot);
            CreateFinishPortal(new Vector3(0, 15.7f, 75), mats, envRoot);
            CreateStoneArch("FinishSanctuaryArch", new Vector3(0, 15.5f, 72), mats.AncientStone, envRoot);

            // Coins
            // Koin reward banyak di jalur atas
            for (int i = 0; i < 4; i++)
            {
                CreateCoin(new Vector3(3f + (i * 1.5f), 12f, 16), mats.GoldCoin, coinsRoot);
            }
            // Koin di jalur bawah
            CreateCoin(new Vector3(-3.5f, 9.5f, 10), mats.GoldCoin, coinsRoot);
            CreateCoin(new Vector3(-3.5f, 9.0f, 20), mats.GoldCoin, coinsRoot);

            // Koin di elevator vertikal
            CreateCoin(new Vector3(0, 11f, 45), mats.GoldCoin, coinsRoot);
            CreateCoin(new Vector3(0, 13f, 45), mats.GoldCoin, coinsRoot);

            // Koin melalui pendulum
            CreateCoin(new Vector3(0, 15.5f, 54), mats.GoldCoin, coinsRoot);
            CreateCoin(new Vector3(0, 15.5f, 57), mats.GoldCoin, coinsRoot);
            CreateCoin(new Vector3(0, 15.5f, 60), mats.GoldCoin, coinsRoot);

            // Cincin koin di finish
            for (int i = 0; i < 6; i++)
            {
                float ang = i * Mathf.PI * 2f / 6f;
                CreateCoin(new Vector3(Mathf.Cos(ang) * 3.5f, 16.5f, 72 + Mathf.Sin(ang) * 3.5f), mats.GoldCoin, coinsRoot);
            }

            // Ornaments
            CreateGiantRootArch(new Vector3(-8, 5, 20), new Vector3(8, 5, 20), mats.TreeBark, decorsRoot);
            CreateGiantRootArch(new Vector3(-9, 8, 48), new Vector3(9, 8, 48), mats.TreeBark, decorsRoot);
            CreateGlowingMushroom(new Vector3(2.5f, 10.5f, 1), mats, decorsRoot);
            CreateGlowingMushroom(new Vector3(-4.5f, 15.5f, 70), mats, decorsRoot);
            CreateGlowingMushroom(new Vector3(4.5f, 15.5f, 70), mats, decorsRoot);
            CreateSkyBackdrop(new Vector3(0, 0, 36), -10f, 42f, mats, decorsRoot);

            // Showcase Camera setup
            List<Vector3> camWaypoints = new List<Vector3>
            {
                new Vector3(-5, 14f, -4f),
                new Vector3(6, 13f, 16f),
                new Vector3(-3, 11f, 26f),
                new Vector3(3, 11.5f, 35f),
                new Vector3(0, 14f, 45f),
                new Vector3(0, 17f, 57f),
                new Vector3(0, 18f, 70f)
            };
            GameObject scCam = SetupShowcaseCamera(camWaypoints, camRoot, 3.4f);
            CreateGameAudioManager(envRoot);
            CreatePlayerCharacter(new Vector3(0, 11f, 0), mats, envRoot, scCam);

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/Level_2_JurangAkar.unity");
        }

        // ==========================================
        // LEVEL 3: PUNCAK POHON RAKSASA
        // ==========================================
        private static void BuildLevel3(ForestMaterials mats)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Golden Sunset & High Altitude Lighting
            SetupLighting(new Color(1f, 0.75f, 0.5f), 1.35f, new Vector3(25, -60, 0), new Color(0.8f, 0.5f, 0.4f), 0.008f, "GoldenSunset");

            GameObject envRoot = new GameObject("--- ENVIRONMENT & PLATFORMS ---");
            GameObject coinsRoot = new GameObject("--- COINS ---");
            GameObject hazardsRoot = new GameObject("--- HAZARDS ---");
            GameObject enemiesRoot = new GameObject("--- ENEMIES ---");
            GameObject decorsRoot = new GameObject("--- DECORATIONS ---");
            GameObject camRoot = new GameObject("--- SHOWCASE CAMERA ---");

            Vector3 trunkCenter = new Vector3(0, 20, 25);

            // Colossal Giant Tree Trunk
            GameObject giantTrunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            giantTrunk.name = "ColossalTreeTrunk";
            giantTrunk.transform.SetParent(envRoot.transform);
            giantTrunk.transform.position = trunkCenter;
            giantTrunk.transform.localScale = new Vector3(12, 25, 12);
            giantTrunk.GetComponent<Renderer>().sharedMaterial = mats.TreeBark;

            // Massive Tree Crown / Canopy at top
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "GreatTreeCanopy";
            canopy.transform.SetParent(decorsRoot.transform);
            canopy.transform.position = new Vector3(0, 48, 25);
            canopy.transform.localScale = new Vector3(45, 18, 45);
            canopy.GetComponent<Renderer>().sharedMaterial = mats.Foliage;

            // 1. Base Starting Platform
            CreatePlatform("Platform_BaseStart", new Vector3(0, 1, 10), new Vector3(8, 1, 8), mats.Grass, envRoot);
            CreateSpawnBeacon(new Vector3(0, 1.6f, 9), mats.CrystalCyan, envRoot);

            // 2. Spiral Steps hugging the giant trunk (Dengan Variasi Platform Rapuh & Dahan Putar)
            float radius = 8.5f;
            int stepCount = 8;
            for (int i = 0; i < stepCount; i++)
            {
                float angle = (i * 40f) * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle) * radius;
                float z = 25f - Mathf.Cos(angle) * radius;
                float y = 3f + (i * 3.5f);

                GameObject step;
                // Step 4 & 5 adalah Crumbling Leaf Platform (Daun Rapuh Bergoyang)
                if (i == 3 || i == 4)
                {
                    step = CreateCrumblingPlatform(new Vector3(x, y, z), new Vector3(4f, 0.4f, 4f), mats.Grass, envRoot);
                }
                else
                {
                    step = CreatePlatform($"SpiralStep_{i + 1}", new Vector3(x, y, z), new Vector3(4f, 0.8f, 4f), mats.DarkWood, envRoot);
                }
                step.transform.LookAt(new Vector3(trunkCenter.x, y, trunkCenter.z));

                // Koin di setiap tangga
                CreateCoin(new Vector3(x, y + 1.2f, z), mats.GoldCoin, coinsRoot);

                // Dahan Duri Berputar 360° di step 3 & 6
                if (i == 2 || i == 5)
                {
                    GameObject branchHazard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    branchHazard.name = $"SweepingSpikeBranch_Step{i + 1}";
                    branchHazard.transform.SetParent(hazardsRoot.transform);
                    branchHazard.transform.position = new Vector3(x, y + 1.4f, z);
                    branchHazard.transform.localScale = new Vector3(0.4f, 3.0f, 0.4f);
                    branchHazard.transform.rotation = Quaternion.Euler(90, 0, 0);
                    branchHazard.GetComponent<Renderer>().sharedMaterial = mats.HazardThorn;

                    var ht = branchHazard.AddComponent<HazardTrap>();
                    ht.trapType = HazardTrap.TrapType.ContinuousRotation;
                    ht.rotationAxis = Vector3.forward;
                    ht.rotationSpeed = 85f;
                }
            }

            // 3. Ascending High Elevator Platform
            float topSpiralY = 3f + ((stepCount - 1) * 3.5f);
            float lastAngle = ((stepCount - 1) * 40f) * Mathf.Deg2Rad;
            Vector3 elevatorPos = new Vector3(Mathf.Sin(lastAngle) * radius + 5f, topSpiralY, 25f - Mathf.Cos(lastAngle) * radius);

            GameObject elevator = CreatePlatform("Platform_HighElevator", elevatorPos, new Vector3(5, 0.8f, 5), mats.DarkWood, envRoot);
            var fpElevator = elevator.AddComponent<FloatingPlatform>();
            fpElevator.direction = FloatingPlatform.MoveDirection.VerticalY;
            fpElevator.travelDistance = 8f;
            fpElevator.moveDuration = 4f;

            CreateCoin(elevatorPos + new Vector3(0, 3f, 0), mats.GoldCoin, coinsRoot);
            CreateCoin(elevatorPos + new Vector3(0, 6f, 0), mats.GoldCoin, coinsRoot);

            // 4. Precision Jumping Pillars at High Altitude
            float pillarBaseY = topSpiralY + 8f;
            Vector3 p1 = elevatorPos + new Vector3(0, 1f, 8f);
            Vector3 p2 = p1 + new Vector3(-4f, 1.5f, 6f);
            Vector3 p3 = p2 + new Vector3(4f, 2.5f, 6f);

            CreatePlatform("Pillar_1", p1, new Vector3(2.0f, 15f, 2.0f), mats.AncientStone, envRoot);
            CreatePlatform("Pillar_2", p2, new Vector3(1.8f, 16f, 1.8f), mats.AncientStone, envRoot);
            CreatePlatform("Pillar_3", p3, new Vector3(1.6f, 17f, 1.6f), mats.AncientStone, envRoot);

            CreateCoin(p1 + new Vector3(0, 8.5f, 0), mats.GoldCoin, coinsRoot);
            CreateCoin(p2 + new Vector3(0, 9f, 0), mats.GoldCoin, coinsRoot);
            CreateCoin(p3 + new Vector3(0, 9.5f, 0), mats.GoldCoin, coinsRoot);

            // 5. Grand Sun Altar Summit (Finish) dengan Ancient Colossus Guardian!
            Vector3 summitPos = p3 + new Vector3(0, 3f, 12f);
            GameObject summit = CreatePlatform("Platform_SunAltarSummit", summitPos, new Vector3(15, 2f, 15), mats.Grass, envRoot);

            CreateFinishPortal(summitPos + new Vector3(0, 1.2f, 4f), mats, envRoot);
            CreateStoneArch("SunTempleArch", summitPos + new Vector3(0, 1f, 0), mats.AncientStone, envRoot);

            // Ancient Colossus Guardian (Mini-Boss Pelindung Altar)
            CreateEnemyPlaceholder("Enemy_AncientColossusGuardian", summitPos + new Vector3(0, 1.2f, -1.5f), mats, enemiesRoot, EnemyPlaceholder.EnemyType.AncientColossus);

            // Ring of 8 Gold Coins surrounding the altar
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                CreateCoin(summitPos + new Vector3(Mathf.Cos(a) * 4.8f, 2f, Mathf.Sin(a) * 4.8f), mats.GoldCoin, coinsRoot);
            }

            // Ornaments
            CreateGlowingMushroom(summitPos + new Vector3(-5f, 1.1f, -4f), mats, decorsRoot);
            CreateGlowingMushroom(summitPos + new Vector3(5f, 1.1f, -4f), mats, decorsRoot);
            CreateGlowingMushroom(summitPos + new Vector3(-5f, 1.1f, 4f), mats, decorsRoot);
            CreateGlowingMushroom(summitPos + new Vector3(5f, 1.1f, 4f), mats, decorsRoot);
            CreateSkyBackdrop(new Vector3(0, 0, 28), -10f, 68f, mats, decorsRoot);

            // Showcase Camera setup (Epic vertical spiral)
            List<Vector3> camWaypoints = new List<Vector3>
            {
                new Vector3(-6, 3f, 4f),
                new Vector3(12, 10f, 16f),
                new Vector3(10, 18f, 34f),
                new Vector3(-8, 25f, 32f),
                new Vector3(elevatorPos.x - 4f, topSpiralY + 4f, elevatorPos.z - 4f),
                new Vector3(p2.x - 5f, pillarBaseY + 6f, p2.z),
                new Vector3(summitPos.x, summitPos.y + 8f, summitPos.z - 8f),
                new Vector3(summitPos.x, summitPos.y + 4.5f, summitPos.z + 1f)
            };
            GameObject scCam = SetupShowcaseCamera(camWaypoints, camRoot, 4.0f);
            CreateGameAudioManager(envRoot);
            CreatePlayerCharacter(new Vector3(0, 1.6f, 10), mats, envRoot, scCam);

            EditorSceneManager.SaveScene(scene, $"{ScenesPath}/Level_3_PuncakPohon.unity");
        }

        // ==========================================
        // HELPER BUILDERS
        // ==========================================

        private static GameObject CreatePlatform(string name, Vector3 pos, Vector3 scale, Material mat, GameObject parent)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent.transform);
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = mat;
            return obj;
        }

        private static void CreateCoin(Vector3 pos, Material mat, GameObject parent)
        {
            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "GoldCoin";
            coin.tag = "Untagged";
            coin.transform.SetParent(parent.transform);
            coin.transform.position = pos;
            coin.transform.localScale = new Vector3(0.8f, 0.1f, 0.8f);
            coin.transform.rotation = Quaternion.Euler(90, 0, 0);
            coin.GetComponent<Renderer>().sharedMaterial = mat;
            coin.GetComponent<Collider>().isTrigger = true;

            var rotator = coin.AddComponent<CoinRotator>();
            rotator.rotateSpeed = 140f;
            rotator.bobHeight = 0.2f;
        }

        private static void CreateThornHazard(Vector3 pos, Vector3 scale, Material mat, GameObject parent)
        {
            GameObject thornZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            thornZone.name = "ThornHazardZone";
            thornZone.transform.SetParent(parent.transform);
            thornZone.transform.position = pos;
            thornZone.transform.localScale = scale;
            thornZone.GetComponent<Renderer>().sharedMaterial = mat;

            // Tambahkan cone / pyramid visual sebagai duri tajam
            for (int i = 0; i < 3; i++)
            {
                GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                spike.name = "Spike";
                spike.transform.SetParent(thornZone.transform);
                spike.transform.localPosition = new Vector3(0, 0.8f, -0.3f + (i * 0.3f));
                spike.transform.localScale = new Vector3(0.3f, 0.8f, 0.3f);
                spike.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        private static void CreatePendulumHazard(Vector3 pivotPos, ForestMaterials mats, GameObject parent)
        {
            GameObject pivot = new GameObject("PendulumPivot");
            pivot.transform.SetParent(parent.transform);
            pivot.transform.position = pivotPos;

            // Tali / rantai pendulum
            GameObject rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rod.name = "Rod";
            rod.transform.SetParent(pivot.transform);
            rod.transform.localPosition = new Vector3(0, -2f, 0);
            rod.transform.localScale = new Vector3(0.15f, 2f, 0.15f);
            rod.GetComponent<Renderer>().sharedMaterial = mats.AncientStone;

            // Kepala duri / bola berduri di ujung pendulum
            GameObject spikeBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spikeBall.name = "SpikeBall";
            spikeBall.transform.SetParent(pivot.transform);
            spikeBall.transform.localPosition = new Vector3(0, -4f, 0);
            spikeBall.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
            spikeBall.GetComponent<Renderer>().sharedMaterial = mats.HazardThorn;

            var trap = pivot.AddComponent<HazardTrap>();
            trap.trapType = HazardTrap.TrapType.PendulumSwing;
            trap.swingAxis = Vector3.forward;
            trap.swingAngle = 55f;
            trap.swingSpeed = 2.2f;
        }

        private static GameObject CreateBouncePad(Vector3 pos, ForestMaterials mats, GameObject parent)
        {
            GameObject pad = new GameObject("SporeBouncePad");
            pad.transform.SetParent(parent.transform);
            pad.transform.position = pos;

            // Batang Jamur
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "Stem";
            stem.transform.SetParent(pad.transform);
            stem.transform.localPosition = new Vector3(0, 0.4f, 0);
            stem.transform.localScale = new Vector3(1.2f, 0.4f, 1.2f);
            stem.GetComponent<Renderer>().sharedMaterial = mats.AncientStone;

            // Kubah Jamur Pegas
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "BounceCap";
            cap.transform.SetParent(pad.transform);
            cap.transform.localPosition = new Vector3(0, 0.9f, 0);
            cap.transform.localScale = new Vector3(3.2f, 1.2f, 3.2f);
            cap.GetComponent<Renderer>().sharedMaterial = mats.CrystalCyan;

            var collider = cap.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;

            var bounce = pad.AddComponent<BouncePad>();
            bounce.bounceForce = 16f;

            // Cahaya pendar cyan
            GameObject glow = new GameObject("BounceLight");
            glow.transform.SetParent(pad.transform);
            glow.transform.localPosition = new Vector3(0, 1.2f, 0);
            Light l = glow.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0f, 0.95f, 0.85f);
            l.range = 5f;
            l.intensity = 2.5f;

            return pad;
        }

        private static GameObject CreateCrumblingPlatform(Vector3 pos, Vector3 scale, Material mat, GameObject parent)
        {
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = "CrumblingLeafPlatform";
            leaf.transform.SetParent(parent.transform);
            leaf.transform.position = pos;
            leaf.transform.localScale = scale;
            leaf.GetComponent<Renderer>().sharedMaterial = mat;

            leaf.AddComponent<CrumblingPlatform>();
            return leaf;
        }

        private static GameObject CreatePulsingPoisonFungus(Vector3 pos, ForestMaterials mats, GameObject parent)
        {
            GameObject fungus = new GameObject("PulsingPoisonFungus");
            fungus.transform.SetParent(parent.transform);
            fungus.transform.position = pos;

            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "PoisonCap";
            cap.transform.SetParent(fungus.transform);
            cap.transform.localPosition = new Vector3(0, 0.8f, 0);
            cap.transform.localScale = new Vector3(1.6f, 1.2f, 1.6f);
            cap.GetComponent<Renderer>().sharedMaterial = mats.HazardThorn;

            var trap = cap.AddComponent<HazardTrap>();
            trap.trapType = HazardTrap.TrapType.SporePuff;
            trap.sporeScaleMultiplier = 1.7f;
            trap.sporePulseSpeed = 2f;

            return fungus;
        }

        private static void CreateEnemyPlaceholder(string name, Vector3 pos, ForestMaterials mats, GameObject parent, EnemyPlaceholder.EnemyType type, bool patrol = false, float dist = 0, Vector3 axis = default)
        {
            GameObject enemy = new GameObject(name);
            enemy.transform.SetParent(parent.transform);
            enemy.transform.position = pos;

            float scaleMult = (type == EnemyPlaceholder.EnemyType.AncientColossus) ? 2.5f : 1.0f;

            // Badan
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(enemy.transform);
            body.transform.localPosition = new Vector3(0, 0.7f * scaleMult, 0);
            body.transform.localScale = new Vector3(1.2f * scaleMult, 0.7f * scaleMult, 1.2f * scaleMult);
            body.GetComponent<Renderer>().sharedMaterial = (type == EnemyPlaceholder.EnemyType.AncientColossus) ? mats.AncientStone : mats.EnemyArmor;

            // Kepala / Mata Bercahaya
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(enemy.transform);
            head.transform.localPosition = new Vector3(0, 1.5f * scaleMult, 0);
            head.transform.localScale = new Vector3(0.9f * scaleMult, 0.9f * scaleMult, 0.9f * scaleMult);
            head.GetComponent<Renderer>().sharedMaterial = (type == EnemyPlaceholder.EnemyType.AncientColossus) ? mats.AncientStone : mats.EnemyArmor;

            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "GlowingEye";
            eye.transform.SetParent(head.transform);
            eye.transform.localPosition = new Vector3(0, 0.1f * scaleMult, 0.4f * scaleMult);
            eye.transform.localScale = new Vector3(0.35f * scaleMult, 0.35f * scaleMult, 0.35f * scaleMult);
            eye.GetComponent<Renderer>().sharedMaterial = mats.EnemyGlow;

            // Jika tipe Colossus, tambahkan tanduk batu megah
            if (type == EnemyPlaceholder.EnemyType.AncientColossus)
            {
                GameObject hornL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hornL.transform.SetParent(head.transform);
                hornL.transform.localPosition = new Vector3(-0.6f * scaleMult, 0.8f * scaleMult, 0);
                hornL.transform.localScale = new Vector3(0.2f * scaleMult, 0.8f * scaleMult, 0.2f * scaleMult);
                hornL.transform.rotation = Quaternion.Euler(0, 0, 30);
                hornL.GetComponent<Renderer>().sharedMaterial = mats.AncientStone;

                GameObject hornR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hornR.transform.SetParent(head.transform);
                hornR.transform.localPosition = new Vector3(0.6f * scaleMult, 0.8f * scaleMult, 0);
                hornR.transform.localScale = new Vector3(0.2f * scaleMult, 0.8f * scaleMult, 0.2f * scaleMult);
                hornR.transform.rotation = Quaternion.Euler(0, 0, -30);
                hornR.GetComponent<Renderer>().sharedMaterial = mats.AncientStone;
            }

            // Jika Arachnid, tambahkan tali sutra gantung di atasnya
            if (type == EnemyPlaceholder.EnemyType.ArachnidWeaver)
            {
                GameObject silk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                silk.name = "SpiderSilkThread";
                silk.transform.SetParent(enemy.transform);
                silk.transform.localPosition = new Vector3(0, 4.5f, 0);
                silk.transform.localScale = new Vector3(0.06f, 4f, 0.06f);
                silk.GetComponent<Renderer>().sharedMaterial = mats.AncientStone;
            }

            var script = enemy.AddComponent<EnemyPlaceholder>();
            script.enemyType = type;
            script.canPatrol = patrol;
            script.patrolDistance = dist;
            script.patrolAxis = axis;
        }

        private static void CreateSpawnBeacon(Vector3 pos, Material glowMat, GameObject parent)
        {
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beacon.name = "SpawnBeaconPlatform";
            beacon.transform.SetParent(parent.transform);
            beacon.transform.position = pos;
            beacon.transform.localScale = new Vector3(2.5f, 0.1f, 2.5f);
            beacon.GetComponent<Renderer>().sharedMaterial = glowMat;

            GameObject lightObj = new GameObject("SpawnLight");
            lightObj.transform.SetParent(beacon.transform);
            lightObj.transform.localPosition = new Vector3(0, 1.5f, 0);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0f, 0.9f, 1f);
            l.range = 5f;
            l.intensity = 2f;
        }

        private static void CreateFinishPortal(Vector3 pos, ForestMaterials mats, GameObject parent)
        {
            GameObject portal = new GameObject("VictoryFinishPortal");
            portal.transform.SetParent(parent.transform);
            portal.transform.position = pos;

            // Tiang Kiri & Kanan
            GameObject pL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pL.transform.SetParent(portal.transform);
            pL.transform.localPosition = new Vector3(-2f, 2f, 0);
            pL.transform.localScale = new Vector3(0.8f, 4f, 0.8f);
            pL.GetComponent<Renderer>().sharedMaterial = mats.AncientStone;

            GameObject pR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pR.transform.SetParent(portal.transform);
            pR.transform.localPosition = new Vector3(2f, 2f, 0);
            pR.transform.localScale = new Vector3(0.8f, 4f, 0.8f);
            pR.GetComponent<Renderer>().sharedMaterial = mats.AncientStone;

            // Lintel Atas
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.transform.SetParent(portal.transform);
            top.transform.localPosition = new Vector3(0, 4.2f, 0);
            top.transform.localScale = new Vector3(5.2f, 0.8f, 1f);
            top.GetComponent<Renderer>().sharedMaterial = mats.AncientStone;

            // Portal Glow Quad / Ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "PortalEnergy";
            ring.transform.SetParent(portal.transform);
            ring.transform.localPosition = new Vector3(0, 2f, 0);
            ring.transform.localScale = new Vector3(3f, 0.1f, 3f);
            ring.transform.rotation = Quaternion.Euler(90, 0, 0);
            ring.GetComponent<Renderer>().sharedMaterial = mats.FinishPortal;
            ring.GetComponent<Collider>().isTrigger = true;

            // Cahaya Portal
            GameObject lightObj = new GameObject("PortalLight");
            lightObj.transform.SetParent(portal.transform);
            lightObj.transform.localPosition = new Vector3(0, 2f, 0);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.8f, 0.2f);
            l.range = 8f;
            l.intensity = 4f;
        }

        private static void CreateStoneArch(string name, Vector3 pos, Material stoneMat, GameObject parent)
        {
            GameObject arch = new GameObject(name);
            arch.transform.SetParent(parent.transform);
            arch.transform.position = pos;

            GameObject col1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            col1.transform.SetParent(arch.transform);
            col1.transform.localPosition = new Vector3(-2f, 1.5f, 0);
            col1.transform.localScale = new Vector3(0.7f, 3f, 0.7f);
            col1.GetComponent<Renderer>().sharedMaterial = stoneMat;

            GameObject col2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            col2.transform.SetParent(arch.transform);
            col2.transform.localPosition = new Vector3(2f, 1.5f, 0);
            col2.transform.localScale = new Vector3(0.7f, 3f, 0.7f);
            col2.GetComponent<Renderer>().sharedMaterial = stoneMat;

            GameObject lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lintel.transform.SetParent(arch.transform);
            lintel.transform.localPosition = new Vector3(0, 3.2f, 0);
            lintel.transform.localScale = new Vector3(5f, 0.6f, 0.9f);
            lintel.GetComponent<Renderer>().sharedMaterial = stoneMat;
        }

        private static void CreateStylizedTree(Vector3 pos, float scale, ForestMaterials mats, GameObject parent)
        {
            GameObject tree = new GameObject("StylizedTree");
            tree.transform.SetParent(parent.transform);
            tree.transform.position = pos;

            // Trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = new Vector3(0, 1.5f * scale, 0);
            trunk.transform.localScale = new Vector3(0.5f * scale, 1.5f * scale, 0.5f * scale);
            trunk.GetComponent<Renderer>().sharedMaterial = mats.TreeBark;

            // Crown 1
            GameObject crown1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown1.name = "Canopy_Base";
            crown1.transform.SetParent(tree.transform);
            crown1.transform.localPosition = new Vector3(0, 3.2f * scale, 0);
            crown1.transform.localScale = new Vector3(2.5f * scale, 2.2f * scale, 2.5f * scale);
            crown1.GetComponent<Renderer>().sharedMaterial = mats.Foliage;

            // Crown 2
            GameObject crown2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown2.name = "Canopy_Top";
            crown2.transform.SetParent(tree.transform);
            crown2.transform.localPosition = new Vector3(0, 4.4f * scale, 0);
            crown2.transform.localScale = new Vector3(1.8f * scale, 1.8f * scale, 1.8f * scale);
            crown2.GetComponent<Renderer>().sharedMaterial = mats.Foliage;
        }

        private static void CreateGlowingMushroom(Vector3 pos, ForestMaterials mats, GameObject parent)
        {
            GameObject shroom = new GameObject("GlowingMushroom");
            shroom.transform.SetParent(parent.transform);
            shroom.transform.position = pos;

            // Stem
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.transform.SetParent(shroom.transform);
            stem.transform.localPosition = new Vector3(0, 0.35f, 0);
            stem.transform.localScale = new Vector3(0.2f, 0.35f, 0.2f);
            stem.GetComponent<Renderer>().sharedMaterial = mats.AncientStone;

            // Cap
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.transform.SetParent(shroom.transform);
            cap.transform.localPosition = new Vector3(0, 0.75f, 0);
            cap.transform.localScale = new Vector3(0.8f, 0.4f, 0.8f);
            cap.GetComponent<Renderer>().sharedMaterial = mats.CrystalCyan;

            // PointLight
            GameObject lightObj = new GameObject("ShroomGlow");
            lightObj.transform.SetParent(shroom.transform);
            lightObj.transform.localPosition = new Vector3(0, 0.8f, 0);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0f, 0.95f, 0.85f);
            l.range = 3.5f;
            l.intensity = 1.8f;
        }

        private static void CreateGiantRootArch(Vector3 p1, Vector3 p2, Material barkMat, GameObject parent)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "GiantCrossingRoot";
            root.transform.SetParent(parent.transform);

            Vector3 mid = (p1 + p2) * 0.5f + Vector3.up * 3f;
            float dist = Vector3.Distance(p1, p2);

            root.transform.position = mid;
            root.transform.localScale = new Vector3(1.8f, dist * 0.5f, 1.8f);
            root.transform.rotation = Quaternion.FromToRotation(Vector3.up, (p2 - p1).normalized);
            root.GetComponent<Renderer>().sharedMaterial = barkMat;
        }

        private static void CreateSkyBackdrop(Vector3 center, float mountainBaseY, float cloudBaseY, ForestMaterials mats, GameObject parent)
        {
            const int mountainCount = 12;
            for (int i = 0; i < mountainCount; i++)
            {
                float angle = i * Mathf.PI * 2f / mountainCount;
                float distance = 105f + Mathf.Sin(i * 2.17f) * 12f;
                float width = 34f + Mathf.Abs(Mathf.Sin(i * 1.73f)) * 24f;
                float height = 24f + Mathf.Abs(Mathf.Cos(i * 2.41f)) * 20f;
                Vector3 basePosition = center + new Vector3(Mathf.Cos(angle) * distance, mountainBaseY, Mathf.Sin(angle) * distance);

                CreateSkyPrimitive("Sky_Mountain", PrimitiveType.Sphere, basePosition + Vector3.up * (height * 0.5f),
                    new Vector3(width, height, width * 0.7f), mats.SkyMountain, parent);
                CreateSkyPrimitive("Sky_MountainPeak", PrimitiveType.Sphere,
                    basePosition + new Vector3(Mathf.Sin(angle * 3f) * width * 0.18f, height * 0.68f, 0f),
                    new Vector3(width * 0.48f, height * 0.62f, width * 0.4f), mats.SkyMountain, parent);
            }

            for (int i = 0; i < 7; i++)
            {
                float angle = i * Mathf.PI * 2f / 7f + 0.35f;
                float distance = 38f + (i % 3) * 11f;
                Vector3 cloudCenter = center + new Vector3(
                    Mathf.Cos(angle) * distance,
                    cloudBaseY + (i % 2) * 9f,
                    Mathf.Sin(angle) * distance);

                CreateSkyPrimitive("Sky_Cloud", PrimitiveType.Sphere, cloudCenter,
                    new Vector3(18f, 4.5f, 9f), mats.SkyCloud, parent);
                CreateSkyPrimitive("Sky_CloudPuff", PrimitiveType.Sphere, cloudCenter + new Vector3(-5f, 2f, 0f),
                    new Vector3(9f, 5.5f, 8f), mats.SkyCloud, parent);
                CreateSkyPrimitive("Sky_CloudPuff", PrimitiveType.Sphere, cloudCenter + new Vector3(5f, 1.5f, 1f),
                    new Vector3(10f, 5f, 8f), mats.SkyCloud, parent);
            }
        }

        private static void CreateSkyPrimitive(string name, PrimitiveType shape, Vector3 position, Vector3 scale, Material material, GameObject parent)
        {
            GameObject backdrop = GameObject.CreatePrimitive(shape);
            backdrop.name = name;
            backdrop.transform.SetParent(parent.transform);
            backdrop.transform.position = position;
            backdrop.transform.localScale = scale;
            Renderer renderer = backdrop.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Object.DestroyImmediate(backdrop.GetComponent<Collider>());
        }

        private static void SetupLighting(Color sunColor, float sunIntensity, Vector3 sunRot, Color fogColor, float fogDensity, string skyboxPreset)
        {
            // Directional Light
            GameObject sunObj = new GameObject("Directional Light (Sun)");
            Light sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = sunColor;
            sun.intensity = sunIntensity;
            sunObj.transform.rotation = Quaternion.Euler(sunRot);
            RenderSettings.sun = sun;

            // Fog settings
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sunColor * 0.8f;
            RenderSettings.ambientEquatorColor = fogColor * 0.6f;
            RenderSettings.ambientGroundColor = new Color(0.1f, 0.1f, 0.05f);
            SetupForestSkybox(sunColor, fogColor, skyboxPreset);
        }

        private static void SetupForestSkybox(Color skyTint, Color groundColor, string preset)
        {
            string path = $"Assets/Materials/EnchantedForest/Mat_ForestSkybox_{preset}.mat";
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sky == null)
            {
                Shader shader = Shader.Find("Skybox/Procedural");
                if (shader == null)
                {
                    Debug.LogError("[Enchanted Forest] Shader Skybox/Procedural tidak ditemukan.");
                    return;
                }
                sky = new Material(shader) { name = $"Mat_ForestSkybox_{preset}" };
                AssetDatabase.CreateAsset(sky, path);
            }
            sky.SetFloat("_SunSize", 0.035f);
            sky.SetFloat("_SunSizeConvergence", 4f);
            sky.SetFloat("_AtmosphereThickness", 1.25f);
            sky.SetFloat("_Exposure", 1.1f);
            sky.SetColor("_SkyTint", Color.Lerp(Color.white, skyTint, 0.55f));
            sky.SetColor("_GroundColor", groundColor);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
        }

        private static void CreateCharacterPart(Transform parent, string partName, PrimitiveType shape, Vector3 position, Vector3 scale, Material material, float xRotation = 0f, float yRotation = 0f, float zRotation = 0f)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
            part.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(part.GetComponent<Collider>());
        }

        private static GameObject SetupShowcaseCamera(List<Vector3> waypoints, GameObject parent, float speed)
        {
            GameObject camObj = new GameObject("ShowcaseCamera");
            camObj.tag = "MainCamera";
            camObj.transform.SetParent(parent.transform);
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            camObj.AddComponent<AudioListener>();

            var showcase = camObj.AddComponent<ShowcaseCamera>();
            showcase.moveSpeed = speed;
            showcase.rotationSmoothness = 2.5f;
            showcase.loop = true;
            showcase.playOnStart = true;

            GameObject wpRoot = new GameObject("Waypoints");
            wpRoot.transform.SetParent(parent.transform);

            for (int i = 0; i < waypoints.Count; i++)
            {
                GameObject wp = new GameObject($"WP_{i + 1}");
                wp.transform.SetParent(wpRoot.transform);
                wp.transform.position = waypoints[i];
                showcase.waypoints.Add(wp.transform);
            }

            return camObj;
        }

        private static GameObject CreateGameAudioManager(GameObject parent)
        {
            GameObject audioObj = new GameObject("GameAudioManager");
            audioObj.transform.SetParent(parent.transform);
            var manager = audioObj.AddComponent<GameAudioManager>();

            manager.coinClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/CoinPickup.wav");
            manager.bounceClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BouncePad.wav");
            manager.victoryClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VictoryFanfare.wav");
            manager.ambientMusicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ForestAmbience.wav");
            manager.jumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Standard Assets/Characters/FirstPersonCharacter/Audio/Jump.wav");
            manager.landClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Standard Assets/Characters/FirstPersonCharacter/Audio/Land.wav");

            List<AudioClip> steps = new List<AudioClip>();
            for (int i = 1; i <= 4; i++)
            {
                var step = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Standard Assets/Characters/FirstPersonCharacter/Audio/Footstep0{i}.wav");
                if (step != null) steps.Add(step);
            }
            manager.footstepClips = steps.ToArray();

            return audioObj;
        }

        private static GameObject CreatePlayerCharacter(Vector3 spawnPos, ForestMaterials mats, GameObject parent, GameObject showcaseCamObj)
        {
            GameObject player = new GameObject("Player_Adventurer");
            player.tag = "Player";
            player.transform.SetParent(parent.transform);
            player.transform.position = spawnPos + Vector3.up * 0.1f;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0, 0.9f, 0);

            // Visual model (Humanoid Adventurer)
            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(player.transform);
            visuals.transform.localPosition = Vector3.zero;

            // Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(visuals.transform);
            body.transform.localPosition = new Vector3(0, 0.78f, 0);
            body.transform.localScale = new Vector3(0.58f, 0.72f, 0.4f);
            body.GetComponent<Renderer>().sharedMaterial = mats.PlayerTunic;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            CreateCharacterPart(visuals.transform, "LeftArm", PrimitiveType.Capsule, new Vector3(-0.39f, 0.78f, 0.015f), new Vector3(0.19f, 0.55f, 0.19f), mats.PlayerTunic, 0f, 0f, -12f);
            CreateCharacterPart(visuals.transform, "RightArm", PrimitiveType.Capsule, new Vector3(0.39f, 0.78f, 0.015f), new Vector3(0.19f, 0.55f, 0.19f), mats.PlayerTunic, 0f, 0f, 12f);
            CreateCharacterPart(visuals.transform, "LeftGlove", PrimitiveType.Sphere, new Vector3(-0.46f, 0.49f, 0.02f), new Vector3(0.17f, 0.17f, 0.17f), mats.PlayerLeather);
            CreateCharacterPart(visuals.transform, "RightGlove", PrimitiveType.Sphere, new Vector3(0.46f, 0.49f, 0.02f), new Vector3(0.17f, 0.17f, 0.17f), mats.PlayerLeather);
            CreateCharacterPart(visuals.transform, "LeftLeg", PrimitiveType.Capsule, new Vector3(-0.16f, 0.25f, 0f), new Vector3(0.22f, 0.42f, 0.23f), mats.PlayerLeather);
            CreateCharacterPart(visuals.transform, "RightLeg", PrimitiveType.Capsule, new Vector3(0.16f, 0.25f, 0f), new Vector3(0.22f, 0.42f, 0.23f), mats.PlayerLeather);
            CreateCharacterPart(visuals.transform, "LeftBoot", PrimitiveType.Cube, new Vector3(-0.16f, 0.08f, 0.07f), new Vector3(0.25f, 0.15f, 0.38f), mats.DarkWood);
            CreateCharacterPart(visuals.transform, "RightBoot", PrimitiveType.Cube, new Vector3(0.16f, 0.08f, 0.07f), new Vector3(0.25f, 0.15f, 0.38f), mats.DarkWood);

            // Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(visuals.transform);
            head.transform.localPosition = new Vector3(0, 1.4f, 0);
            head.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            head.GetComponent<Renderer>().sharedMaterial = mats.PlayerSkin;
            Object.DestroyImmediate(head.GetComponent<Collider>());

            // Explorer Hat
            GameObject hatRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hatRim.name = "HatRim";
            hatRim.transform.SetParent(visuals.transform);
            hatRim.transform.localPosition = new Vector3(0, 1.55f, 0);
            hatRim.transform.localScale = new Vector3(0.8f, 0.05f, 0.8f);
            hatRim.GetComponent<Renderer>().sharedMaterial = mats.PlayerLeather;
            Object.DestroyImmediate(hatRim.GetComponent<Collider>());

            // Backpack
            GameObject pack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pack.name = "Backpack";
            pack.transform.SetParent(visuals.transform);
            pack.transform.localPosition = new Vector3(0, 0.8f, -0.25f);
            pack.transform.localScale = new Vector3(0.45f, 0.5f, 0.25f);
            pack.GetComponent<Renderer>().sharedMaterial = mats.PlayerLeather;
            Object.DestroyImmediate(pack.GetComponent<Collider>());

            // Magic Crystal Torch in Hand
            GameObject torch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            torch.name = "Torch";
            torch.transform.SetParent(visuals.transform);
            torch.transform.localPosition = new Vector3(0.4f, 0.6f, 0.25f);
            torch.transform.localScale = new Vector3(0.08f, 0.35f, 0.08f);
            torch.GetComponent<Renderer>().sharedMaterial = mats.DarkWood;
            Object.DestroyImmediate(torch.GetComponent<Collider>());

            GameObject torchGlow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            torchGlow.name = "TorchCrystal";
            torchGlow.transform.SetParent(torch.transform);
            torchGlow.transform.localPosition = new Vector3(0, 1.1f, 0);
            torchGlow.transform.localScale = new Vector3(2.5f, 1.5f, 2.5f);
            torchGlow.GetComponent<Renderer>().sharedMaterial = mats.CrystalCyan;
            Object.DestroyImmediate(torchGlow.GetComponent<Collider>());

            GameObject torchLight = new GameObject("TorchLight");
            torchLight.transform.SetParent(torch.transform);
            torchLight.transform.localPosition = new Vector3(0, 1.1f, 0);
            Light tl = torchLight.AddComponent<Light>();
            tl.type = LightType.Point;
            tl.color = new Color(0f, 0.95f, 0.85f);
            tl.range = 4.5f;
            tl.intensity = 1.5f;

            // Player Camera
            GameObject playerCamObj = new GameObject("PlayerThirdPersonCamera");
            playerCamObj.transform.SetParent(player.transform);
            playerCamObj.transform.localPosition = new Vector3(0, 3.0f, -6f);
            var cam = playerCamObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            playerCamObj.AddComponent<AudioListener>();
            var follow = playerCamObj.AddComponent<ThirdPersonCameraFollow>();
            follow.target = player.transform;

            // Player Controller
            var controller = player.AddComponent<PlayerController3D>();
            controller.cameraTransform = playerCamObj.transform;
            controller.spawnPoint = spawnPos + Vector3.up * 0.5f;
            controller.fallThresholdY = spawnPos.y - 12f;

            // Game Mode Manager (Toggles between Showcase Drone and Playable Player)
            GameObject gmmObj = new GameObject("GameModeManager");
            gmmObj.transform.SetParent(parent.transform);
            var gmm = gmmObj.AddComponent<GameModeManager>();
            gmm.showcaseCameraObj = showcaseCamObj;
            gmm.playerCameraObj = playerCamObj;
            gmm.playerCharacterObj = player;
            gmm.currentMode = GameModeManager.CameraMode.ShowcaseTour;

            return player;
        }

        private static void RegisterScenesInBuildSettings()
        {
            string[] sceneFiles = {
                $"{ScenesPath}/Level_1_PinggirHutan.unity",
                $"{ScenesPath}/Level_2_JurangAkar.unity",
                $"{ScenesPath}/Level_3_PuncakPohon.unity"
            };

            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
            foreach (var path in sceneFiles)
            {
                if (File.Exists(path))
                {
                    buildScenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
        }
    }
}
