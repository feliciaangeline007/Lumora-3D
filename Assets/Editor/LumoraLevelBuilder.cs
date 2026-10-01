using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EnchantedForest;

namespace EnchantedForest.Editor
{
    /// <summary>
    /// LUMORA: HUTAN YANG TERLUPAKAN
    /// Editor tool untuk generate otomatis 3 level sesuai rancangan tugas:
    ///   Level 1 — Hutan Fajar      (Mudah,  ~65 detik)
    ///   Level 2 — Gua Lumut        (Sedang, ~80 detik)
    ///   Level 3 — Kuil Reruntuhan  (Sulit,  ~100 detik, Mini-Boss Golem)
    ///
    /// Akses via: Tools → Lumora → Level Builder
    ///
    /// PERBEDAAN DARI LevelDesignAutoBuilder (EnchantedForest):
    ///   - Tema cerita Lumora (kristal, biji cahaya, Riko)
    ///   - Nama scene baru: Level_1_HutanFajar, Level_2_GuaLumut, Level_3_KuilReruntuhan
    ///   - CrystalPulse dipasang ke semua kristal
    ///   - LevelTitleUI dipasang otomatis di tiap scene
    ///   - Desain level sesuai spesifikasi rancangan (platform, obstacle, enemy, coin tersembunyi)
    /// </summary>
    public class LumoraLevelBuilder : EditorWindow
    {
        // ── Konstanta Path ────────────────────────────────────────────────────
        private const string MatPath    = "Assets/Materials/EnchantedForest";
        private const string ScenePath  = "Assets/Scenes";
        private const string BuilderKey = "Lumora_BuilderVersion";
        private const int    BuilderVer = 1;

        // ── Auto-generate sekali saat pertama kali ────────────────────────────
        [InitializeOnLoadMethod]
        private static void AutoRunOnce()
        {
            EditorApplication.delayCall += () =>
            {
                // Hanya generate jika belum pernah (key belum ada / versi lama)
                if (EditorPrefs.GetInt(BuilderKey, 0) < BuilderVer)
                {
                    // Jangan auto-generate: biarkan user klik manual agar tidak overwrite scene L1/L2/L3 yang sudah ada
                    Debug.Log("[Lumora] LumoraLevelBuilder siap. Buka: Tools → Lumora → Level Builder");
                }
            };
        }

        // ── Menu Items ────────────────────────────────────────────────────────
        [MenuItem("Tools/Lumora/Level Builder", priority = 1)]
        public static void ShowWindow()
        {
            var w = GetWindow<LumoraLevelBuilder>("Lumora Level Builder");
            w.minSize = new Vector2(440, 560);
        }

        [MenuItem("Tools/Lumora/⚡ Generate Semua Level Lumora", priority = 2)]
        public static void GenerateAllMenu() => GenerateAll(showDialog: true);

        // ── GUI Window ────────────────────────────────────────────────────────
        private void OnGUI()
        {
            GUILayout.Space(12);

            // Header
            GUIStyle header = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 19,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.31f, 0.96f, 0.85f) }
            };
            GUILayout.Label("✦  LUMORA: HUTAN YANG TERLUPAKAN  ✦", header);
            GUILayout.Label("3D Level Design Builder — Tugas Android Game Dev", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(12);

            EditorGUILayout.HelpBox(
                "Tool ini membuat 3 scene level baru:\n" +
                "  🌅  Level 1 — Hutan Fajar      (Mudah, ~65 detik)\n" +
                "  🕳️  Level 2 — Gua Lumut         (Sedang, ~80 detik)\n" +
                "  ⚔️  Level 3 — Kuil Reruntuhan   (Sulit, ~100 detik, Golem)\n\n" +
                "Tiap scene sudah berisi:\n" +
                "  ✔  Platform, obstacle, enemy, coin Biji Cahaya\n" +
                "  ✔  Kristal Lumora dengan efek CrystalPulse\n" +
                "  ✔  Title card LevelTitleUI untuk rekaman\n" +
                "  ✔  Kamera Showcase otomatis (tekan [C/Tab] ganti ke Player)",
                MessageType.Info);

            GUILayout.Space(12);

            // Tombol utama
            GUI.backgroundColor = new Color(0.31f, 0.96f, 0.85f);
            if (GUILayout.Button("⚡  GENERATE SEMUA LEVEL LUMORA", GUILayout.Height(46)))
            {
                GenerateAll(showDialog: true);
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(16);
            EditorGUILayout.LabelField("Buka Level Individual:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🌅  Hutan Fajar\n(Level 1)", GUILayout.Height(40)))
                OpenScene("Level_1_HutanFajar.unity");
            if (GUILayout.Button("🕳️  Gua Lumut\n(Level 2)", GUILayout.Height(40)))
                OpenScene("Level_2_GuaLumut.unity");
            if (GUILayout.Button("⚔️  Kuil Reruntuhan\n(Level 3)", GUILayout.Height(40)))
                OpenScene("Level_3_KuilReruntuhan.unity");
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(18);
            EditorGUILayout.HelpBox(
                "TIPS REKAMAN VIDEO:\n" +
                "1. Tekan ▶ Play — kamera showcase otomatis berjalan.\n" +
                "2. Tekan [SPACE] untuk pause/resume kamera.\n" +
                "3. Tekan [C] atau [Tab] untuk ganti ke mode Player (WASD+Lompat).\n" +
                "4. Rekam layar dengan screen recorder HP atau OBS.\n" +
                "5. Total target: < 3 menit untuk 3 level.",
                MessageType.None);
        }

        // ── Generate All ──────────────────────────────────────────────────────
        public static void GenerateAll(bool showDialog = true)
        {
            try
            {
                if (showDialog) EditorUtility.DisplayProgressBar("Lumora Level Builder", "Menyiapkan folder & material...", 0.05f);
                EnsureFolders();
                var mats = CreateLumoraMaterials();

                if (showDialog) EditorUtility.DisplayProgressBar("Lumora Level Builder", "🌅  Membangun Level 1: Hutan Fajar...", 0.30f);
                BuildLevel1(mats);

                if (showDialog) EditorUtility.DisplayProgressBar("Lumora Level Builder", "🕳️  Membangun Level 2: Gua Lumut Bercahaya...", 0.62f);
                BuildLevel2(mats);

                if (showDialog) EditorUtility.DisplayProgressBar("Lumora Level Builder", "⚔️  Membangun Level 3: Kuil Reruntuhan...", 0.88f);
                BuildLevel3(mats);

                RegisterScenes();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorPrefs.SetInt(BuilderKey, BuilderVer);

                if (showDialog)
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog(
                        "✦ Lumora — Selesai!",
                        "Semua 3 level berhasil dibuat!\n\n" +
                        "🌅  Level_1_HutanFajar.unity\n" +
                        "🕳️  Level_2_GuaLumut.unity\n" +
                        "⚔️  Level_3_KuilReruntuhan.unity\n\n" +
                        "Tekan ▶ Play untuk preview.\n" +
                        "Tekan [C] atau [Tab] untuk ganti mode kamera/player.",
                        "Siap Rekam! 🎥");
                }

                OpenScene("Level_1_HutanFajar.unity");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[Lumora] Error saat generate level: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Error", $"Gagal generate level:\n{e.Message}", "OK");
            }
        }

        // ── Folder Setup ──────────────────────────────────────────────────────
        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(MatPath))
                AssetDatabase.CreateFolder("Assets/Materials", "EnchantedForest");
            if (!AssetDatabase.IsValidFolder(ScenePath))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        // ── Material Container ────────────────────────────────────────────────
        private class LumoraMats
        {
            public Material Grass, DarkWood, Stone, AncientStone;
            public Material GoldCoin, Crystal, Hazard;
            public Material EnemyArmor, EnemyGlow;
            public Material Portal, TreeBark, Foliage, Water;
            public Material PlayerTunic, PlayerSkin, PlayerLeather;
        }

        // ── Buat Material Lumora ───────────────────────────────────────────────
        private static LumoraMats CreateLumoraMaterials()
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new LumoraMats();

            // Palet sesuai spec: hijau tua, cokelat batu, cyan #4FF5D8, kuning #FFD84A, merah-ungu #8B1A4A
            m.Grass        = Mat("Mat_LushGrass",    sh, new Color(0.18f, 0.48f, 0.25f), 0.0f, 0.2f);
            m.DarkWood     = Mat("Mat_DarkWood",     sh, new Color(0.28f, 0.16f, 0.08f), 0.0f, 0.3f);
            m.Stone        = Mat("Mat_Stone",        sh, new Color(0.45f, 0.45f, 0.42f), 0.1f, 0.2f);
            m.AncientStone = Mat("Mat_AncientStone", sh, new Color(0.38f, 0.36f, 0.30f), 0.1f, 0.15f);
            // Cyan kristal: #4FF5D8 → (0.310, 0.961, 0.847)
            m.Crystal      = Mat("Mat_CrystalCyan",  sh, new Color(0.31f, 0.96f, 0.85f), 0.3f, 0.9f,
                                  new Color(0.31f, 0.96f, 0.85f) * 2.0f);
            // Kuning koin: #FFD84A → (1.0, 0.847, 0.29)
            m.GoldCoin     = Mat("Mat_GoldCoin",     sh, new Color(1.0f, 0.847f, 0.29f), 0.9f, 0.85f,
                                  new Color(1.0f, 0.72f, 0.1f) * 0.9f);
            // Merah-ungu hazard: #8B1A4A → (0.545, 0.102, 0.290)
            m.Hazard       = Mat("Mat_HazardThorn",  sh, new Color(0.42f, 0.06f, 0.15f), 0.2f, 0.4f,
                                  new Color(0.65f, 0.08f, 0.18f) * 0.6f);
            m.EnemyArmor   = Mat("Mat_EnemyArmor",   sh, new Color(0.50f, 0.10f, 0.12f), 0.5f, 0.5f);
            m.EnemyGlow    = Mat("Mat_EnemyGlow",    sh, new Color(1.0f, 0.2f, 0.1f),   0.1f, 0.9f,
                                  new Color(1.0f, 0.3f, 0.1f) * 2.0f);
            m.Portal       = Mat("Mat_FinishPortal", sh, new Color(1.0f, 0.88f, 0.35f), 0.2f, 0.9f,
                                  new Color(1.0f, 0.75f, 0.1f) * 2.5f);
            m.TreeBark     = Mat("Mat_TreeBark",     sh, new Color(0.22f, 0.13f, 0.07f), 0.0f, 0.15f);
            m.Foliage      = Mat("Mat_Foliage",      sh, new Color(0.09f, 0.30f, 0.13f), 0.0f, 0.1f);
            m.Water        = Mat("Mat_SwampWater",   sh, new Color(0.06f, 0.15f, 0.20f, 0.88f), 0.1f, 0.95f);
            m.PlayerTunic  = Mat("Mat_PlayerTunic",  sh, new Color(0.15f, 0.45f, 0.85f), 0.0f, 0.3f);
            m.PlayerSkin   = Mat("Mat_PlayerSkin",   sh, new Color(0.95f, 0.76f, 0.65f), 0.0f, 0.2f);
            m.PlayerLeather= Mat("Mat_PlayerLeather",sh, new Color(0.35f, 0.20f, 0.10f), 0.0f, 0.25f);

            return m;
        }

        private static Material Mat(string name, Shader sh, Color col, float metal, float smooth, Color? emit = null)
        {
            string path = $"{MatPath}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", col);
            mat.SetColor("_Color", col);
            mat.SetFloat("_Metallic", metal);
            mat.SetFloat("_Smoothness", smooth);
            if (emit.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emit.Value);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ╔══════════════════════════════════════════════════════════════════╗
        // ║  LEVEL 1 — HUTAN FAJAR                                          ║
        // ║  Alur: Lurus ke depan (+Z), naik bertahap, akhir di bukit       ║
        // ╚══════════════════════════════════════════════════════════════════╝
        private static void BuildLevel1(LumoraMats m)
        {
            Scene sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Lighting: Fajar (Oranye-kuning, kabut krem tipis) ─────────────
            SetupLighting(
                new Color(1.00f, 0.93f, 0.78f), 1.2f, new Vector3(30f, -30f, 0f),
                new Color(0.78f, 0.88f, 0.80f), 0.012f,
                new Color(0.55f, 0.70f, 0.85f), new Color(0.42f, 0.52f, 0.48f), new Color(0.08f, 0.10f, 0.05f));

            // ── Root GameObjects (organisasi hierarchy) ────────────────────────
            var env  = new GameObject("--- ENVIRONMENT ---");
            var coins = new GameObject("--- COINS ---");
            var haz  = new GameObject("--- HAZARDS ---");
            var npc  = new GameObject("--- ENEMIES ---");
            var deco = new GameObject("--- DECORATIONS ---");
            var cam  = new GameObject("--- SHOWCASE CAMERA ---");

            // ── Lantai bawah (tanah datar) ─────────────────────────────────────
            MkPlatform("Ground_Floor", new Vector3(0,-0.5f,30), new Vector3(30,1,80), m.Grass, env);

            // ── Platform P0: Start ─────────────────────────────────────────────
            MkPlatform("P0_Start", V(0,0,0), V(10,1,10), m.Grass, env);
            MkArch("StartArch", V(0,0.5f,-3f), m.AncientStone, env);
            MkCrystal("Crystal_Spawn", V(0,1.0f,0), V(0.6f,1.2f,0.6f), m.Crystal, env, isMajor:false);
            MkSpawnBeacon(V(0,0.6f,0), m.Crystal, env);

            // ── Platform P1–P2: Walkway datar ─────────────────────────────────
            // Gap P0→P1 = 0 (menyambung), P2 naik 1.5m
            MkPlatform("P1_Walkway", V(0,0,11), V(8,1,10), m.Grass, env);
            MkPlatform("P2_Raised",  V(0,1.5f,22), V(6,1,8), m.Stone, env);

            // ── Obstacle 1: Duri statis di P1 ─────────────────────────────────
            // 2 thorn zone kiri & kanan, celah 1.5m di tengah
            MkThorn("Thorn_L", V(-2.8f,1.4f,12), V(1.2f,0.7f,3f), m.Hazard, haz);
            MkThorn("Thorn_R", V( 2.8f,1.4f,12), V(1.2f,0.7f,3f), m.Hazard, haz);

            // ── Obstacle 2: Pohon tumbang di P2 ───────────────────────────────
            // Silinder horizontal memaksa lompat/lewat kiri
            var log = MkObj("FallenLog", V(0,2.5f,24), V(7f,0.5f,0.5f), m.TreeBark, env);
            log.transform.rotation = Quaternion.Euler(0,90,90);

            // ── Platform P3–P5 ─────────────────────────────────────────────────
            MkPlatform("P3",  V(-1.5f, 1.5f, 30), V(4,1,4), m.Stone,    env);
            MkPlatform("P4",  V( 1.5f, 1.5f, 35), V(4,1,4), m.Stone,    env);
            MkPlatform("P5",  V( 0,    1.5f, 42), V(6,1,8), m.Grass,    env);

            // Kristal checkpoint di P5
            MkCrystal("Crystal_Checkpoint", V(0,2.8f,42), V(0.8f,1.8f,0.8f), m.Crystal, env, isMajor:false);

            // ── Platform P6–P7 ─────────────────────────────────────────────────
            MkPlatform("P6",  V(0, 0.5f, 50), V(5,1,6), m.Grass, env);
            MkPlatform("P7",  V(0, 0.5f, 58), V(5,1,8), m.Grass, env);

            // ── Enemy: Penjaga Hutan di P7 ────────────────────────────────────
            MkEnemy("Penjaga_Hutan", V(0, 1.8f, 60), 1.0f, m, EnemyPlaceholder.EnemyType.WoodlingScout,
                    patrol:true, dist:2.5f, axis:Vector3.right, npc);

            // ── Platform P8–P9: Bukit Finish ──────────────────────────────────
            MkPlatform("P8_HillBase",  V(0, 2.0f, 67), V(6,1,6), m.Grass, env);
            MkPlatform("P9_HillTop",   V(0, 4.0f, 74), V(8,1.5f,10), m.Grass, env);

            // Kristal terakhir (besar) di altar P9
            MkCrystal("Crystal_Lumora_L1", V(0,5.7f,77), V(1.2f,2.5f,1.2f), m.Crystal, env, isMajor:true);
            MkFinishPortal(V(0,5.2f,79), m, env);

            // ── Coins: Jalur Pemandu (8 coin) ─────────────────────────────────
            MkCoin(V(0,   1.5f,  5),  m.GoldCoin, coins); // dekat start
            MkCoin(V(0,   1.5f, 14),  m.GoldCoin, coins);
            MkCoin(V(0,   3.0f, 22),  m.GoldCoin, coins);
            MkCoin(V(-1.5f,3.0f,30),  m.GoldCoin, coins);
            MkCoin(V( 1.5f,3.0f,35),  m.GoldCoin, coins);
            MkCoin(V(0,   3.0f, 44),  m.GoldCoin, coins);
            MkCoin(V(0,   2.0f, 52),  m.GoldCoin, coins);
            MkCoin(V(0,   2.0f, 60),  m.GoldCoin, coins);

            // ── Coins Tersembunyi (3 coin) ─────────────────────────────────────
            MkCoin(V( 4f,  1.5f,  8), m.GoldCoin, coins); // C-H1: balik pohon dekat P1
            MkCoin(V(-3f,  0.8f, 52), m.GoldCoin, coins); // C-H2: tepi kiri P6, turun sedikit
            MkCoin(V( 3f,  4.8f, 73), m.GoldCoin, coins); // C-H3: sudut kanan P9

            // ── Dekorasi ───────────────────────────────────────────────────────
            MkTree(V(-5f, 0, -3f), 1.2f, m, deco);
            MkTree(V( 5f, 0,  3f), 1.0f, m, deco);
            MkTree(V(-6f, 0, 12f), 1.4f, m, deco);
            MkTree(V( 6f, 0, 25f), 1.1f, m, deco);
            MkTree(V(-5f, 0, 50f), 1.3f, m, deco);
            MkTree(V( 5f, 0, 58f), 1.1f, m, deco);
            MkMushroom(V(-3f, 0, 8f), m, deco);
            MkMushroom(V( 3f, 0, 42f), m, deco);

            // ── Showcase Camera ────────────────────────────────────────────────
            var waypoints = new List<Vector3>
            {
                V(-5f,  4.5f, -6f),
                V( 0f,  3.5f,  8f),
                V(-2f,  4.0f, 22f),
                V( 2f,  4.0f, 36f),
                V( 0f,  5.0f, 50f),
                V(-2f,  5.5f, 62f),
                V( 0f,  7.5f, 75f),
            };
            var scCam = SetupCamera(waypoints, cam, 3.2f);
            MkAudioManager(env);
            MkPlayer(V(0,0.6f,0), m, env, scCam);
            MkTitleUI(env, "Hutan Fajar", "Level 1  •  Mudah", 1);

            EditorSceneManager.SaveScene(sc, $"{ScenePath}/Level_1_HutanFajar.unity");
        }

        // ╔══════════════════════════════════════════════════════════════════╗
        // ║  LEVEL 2 — GUA LUMUT BERCAHAYA                                  ║
        // ║  Alur: Masuk gua → turun → zigzag → naik lagi → keluar         ║
        // ╚══════════════════════════════════════════════════════════════════╝
        private static void BuildLevel2(LumoraMats m)
        {
            Scene sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Lighting: Gua gelap biru, fog pekat ───────────────────────────
            SetupLighting(
                new Color(0.55f, 0.60f, 0.85f), 0.3f, new Vector3(35f, 45f, 0f),
                new Color(0.04f, 0.06f, 0.12f), 0.06f,
                new Color(0.04f, 0.06f, 0.12f), new Color(0.02f, 0.03f, 0.06f), new Color(0.01f, 0.01f, 0.02f));

            var env  = new GameObject("--- ENVIRONMENT ---");
            var coins = new GameObject("--- COINS ---");
            var haz  = new GameObject("--- HAZARDS ---");
            var npc  = new GameObject("--- ENEMIES ---");
            var deco = new GameObject("--- DECORATIONS ---");
            var cam  = new GameObject("--- SHOWCASE CAMERA ---");

            // Lantai/dasar gua jauh di bawah (seperti jurang)
            MkPlatform("CaveBedrock", V(0,-8,30), V(40,1,80), m.AncientStone, env);
            // Genangan air bercahaya di dasar
            var pool = MkObj("SwampPool", V(0,-7.4f,30), V(20f,0.3f,40f), m.Water, env);
            pool.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // ── Dinding gua kiri & kanan (backdrop) ───────────────────────────
            MkPlatform("WallLeft",  V(-12,2,35), V(2,20,80), m.AncientStone, env);
            MkPlatform("WallRight", V( 12,2,35), V(2,20,80), m.AncientStone, env);
            MkPlatform("CeilingBg", V(0,12,35),  V(26,2,80), m.AncientStone, env);

            // ── P0: Entrance (terang, transisi luar-gua) ──────────────────────
            MkPlatform("P0_Entrance", V(0,0,0), V(10,1,10), m.Stone, env);
            MkCrystal("Crystal_Entry_L", V(-3,2,2), V(0.5f,1.0f,0.5f), m.Crystal, env, false);
            MkCrystal("Crystal_Entry_R", V( 3,2,2), V(0.5f,1.0f,0.5f), m.Crystal, env, false);
            MkSpawnBeacon(V(0,0.6f,0), m.Crystal, env);

            // ── P1–P2: Turun ke gua ────────────────────────────────────────────
            MkPlatform("P1", V(0,-1.5f,10), V(7,1,8),  m.Stone, env);
            MkPlatform("P2", V(0,-2.5f,20), V(5,1,6),  m.Stone, env);

            // Kristal dinding gua kiri (sumber cahaya utama area ini)
            MkCrystal("Crystal_Wall_L1", V(-8,-1,12), V(0.6f,1.4f,0.6f), m.Crystal, env, false);
            MkCrystal("Crystal_Wall_R1", V( 8,-1,18), V(0.4f,1.0f,0.4f), m.Crystal, env, false);

            // ── P3–P4: Sempit + Duri kiri-kanan ───────────────────────────────
            MkPlatform("P3_Narrow", V(0,-2.5f,28), V(4,1,6), m.AncientStone, env);
            // Obstacle 1: duri gua kiri-kanan (celah 1.2m di tengah)
            MkThorn("CaveThorn_L", V(-2f,-1.8f,29), V(1.0f,0.8f,3f), m.Hazard, haz);
            MkThorn("CaveThorn_R", V( 2f,-1.8f,29), V(1.0f,0.8f,3f), m.Hazard, haz);

            MkPlatform("P4", V(0,-2.0f,36), V(5,1,7), m.Stone, env);
            MkCrystal("Crystal_Checkpoint_2", V(0,-0.5f,36), V(0.9f,2.0f,0.9f), m.Crystal, env, false);

            // ── P5: Platform sangat sempit ────────────────────────────────────
            MkPlatform("P5_VeryNarrow", V(0,-2.0f,43), V(2.5f,1,4), m.AncientStone, env);

            // ── P6–P8: Naik menuju exit ────────────────────────────────────────
            // Gap P5→P6 = 3m (lebar, perlu lompat penuh)
            MkPlatform("P6",  V(0,-1.5f,50), V(3,1,5), m.Stone, env);
            MkPlatform("P7",  V(0,-1.0f,57), V(3,1,5), m.Stone, env);
            MkPlatform("P8",  V(0,-0.5f,63), V(4,1,6), m.Stone, env);

            // Obstacle 2: Stalaktit rendah di P6–P8 (cone terbalik menggantung)
            MkStalactite("Stalactite_1", V(-0.5f,4.5f,51), V(0.8f,2.0f,0.8f), m.AncientStone, env);
            MkStalactite("Stalactite_2", V( 0.5f,4.5f,57), V(0.8f,2.0f,0.8f), m.AncientStone, env);
            MkStalactite("Stalactite_3", V( 0.0f,4.5f,63), V(0.6f,1.5f,0.6f), m.AncientStone, env);

            // ── P9–P10: Menuju area enemy ─────────────────────────────────────
            MkPlatform("P9",   V(0, 0,70), V(6,1,7), m.Stone, env);
            MkPlatform("P10",  V(0, 0,79), V(7,1,8), m.Stone, env);

            // Enemy: Lumut Raksasa (2 musuh, bercahaya emissive)
            MkEnemy("LumutRaksasa_1", V(-1.5f,1.5f,72), 1.2f, m, EnemyPlaceholder.EnemyType.ThornSpitter, false, 0, Vector3.forward, npc);
            MkEnemy("LumutRaksasa_2", V( 1.5f,1.5f,80), 1.2f, m, EnemyPlaceholder.EnemyType.ThornSpitter, false, 0, Vector3.forward, npc);

            // ── P11: Altar Exit + Kristal Lumora ──────────────────────────────
            MkPlatform("P11_AltarExit", V(0,0,88), V(10,1,10), m.AncientStone, env);
            MkCrystal("Crystal_Lumora_L2", V(0,2.5f,91), V(1.5f,3.0f,1.5f), m.Crystal, env, isMajor:true);
            MkFinishPortal(V(0,1.5f,93), m, env);

            // ── Coins: Jalur Pemandu (10 coin) — rapat di area gelap ──────────
            MkCoin(V(0,  1.0f,  4),  m.GoldCoin, coins);
            MkCoin(V(0, -0.7f, 12),  m.GoldCoin, coins);
            MkCoin(V(0, -1.5f, 22),  m.GoldCoin, coins);
            MkCoin(V(0, -1.5f, 28),  m.GoldCoin, coins);
            MkCoin(V(0, -1.0f, 36),  m.GoldCoin, coins);
            MkCoin(V(0, -1.0f, 43),  m.GoldCoin, coins);
            // Coin sebagai "umpan" di tepi P6 setelah gap 3m
            MkCoin(V(0, -0.5f, 51),  m.GoldCoin, coins);
            MkCoin(V(0, -0.5f, 57),  m.GoldCoin, coins);
            MkCoin(V(0,  0.5f, 70),  m.GoldCoin, coins);
            MkCoin(V(0,  0.5f, 80),  m.GoldCoin, coins);

            // ── Coins Tersembunyi (3 coin) ─────────────────────────────────────
            MkCoin(V(-7.5f,-0.5f, 15), m.GoldCoin, coins); // C-H1: ceruk dinding kiri
            MkCoin(V( 0,   -4.5f, 50), m.GoldCoin, coins); // C-H2: bawah P8 (platform mini tersembunyi)
            MkCoin(V( 4f,   0.8f, 90), m.GoldCoin, coins); // C-H3: balik pilar batu dekat exit

            // ── Dekorasi Gua ───────────────────────────────────────────────────
            // Kristal tambahan di dinding
            MkCrystal("CrystalDeco_1", V(-9,-2,30), V(0.4f,0.8f,0.4f), m.Crystal, env, false);
            MkCrystal("CrystalDeco_2", V( 9,-2,42), V(0.3f,0.6f,0.3f), m.Crystal, env, false);
            MkCrystal("CrystalDeco_3", V(-8,-1,56), V(0.5f,1.0f,0.5f), m.Crystal, env, false);
            MkCrystal("CrystalDeco_4", V( 7, 0,68), V(0.4f,0.9f,0.4f), m.Crystal, env, false);
            // Stalagmit dekoratif (silinder kecil dari bawah)
            MkStalagmite("Stalagmite_1", V(-4,-7,28), m.AncientStone, env);
            MkStalagmite("Stalagmite_2", V( 3,-7,38), m.AncientStone, env);
            MkStalagmite("Stalagmite_3", V(-5,-7,55), m.AncientStone, env);
            MkMushroom(V(3,-1.5f,36), m, deco);
            MkMushroom(V(-3,-1,80), m, deco);

            // ── Showcase Camera ────────────────────────────────────────────────
            var waypoints = new List<Vector3>
            {
                V(-4f,  4.0f, -5f),
                V( 0f,  1.5f,  8f),
                V( 3f, -0.5f, 22f),
                V(-2f, -0.5f, 32f),
                V( 0f,  0.5f, 44f),
                V( 0f,  1.5f, 58f),  // tunjukkan gap lebar
                V( 0f,  2.0f, 72f),
                V( 0f,  3.5f, 88f),
            };
            var scCam = SetupCamera(waypoints, cam, 3.0f);
            MkAudioManager(env);
            MkPlayer(V(0,0.5f,0), m, env, scCam);
            MkTitleUI(env, "Gua Lumut Bercahaya", "Level 2  •  Sedang", 2);

            EditorSceneManager.SaveScene(sc, $"{ScenePath}/Level_2_GuaLumut.unity");
        }

        // ╔══════════════════════════════════════════════════════════════════╗
        // ║  LEVEL 3 — KUIL RERUNTUHAN                                      ║
        // ║  Alur: Gerbang → lompatan batu → jurang → tangga → arena Golem ║
        // ╚══════════════════════════════════════════════════════════════════╝
        private static void BuildLevel3(LumoraMats m)
        {
            Scene sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Lighting: Senja oranye-merah, fog ringan ──────────────────────
            SetupLighting(
                new Color(1.00f, 0.65f, 0.35f), 0.8f, new Vector3(15f, -60f, 0f),
                new Color(0.58f, 0.28f, 0.16f), 0.025f,
                new Color(0.60f, 0.35f, 0.20f), new Color(0.40f, 0.22f, 0.12f), new Color(0.08f, 0.05f, 0.03f));

            var env  = new GameObject("--- ENVIRONMENT ---");
            var coins = new GameObject("--- COINS ---");
            var haz  = new GameObject("--- HAZARDS ---");
            var npc  = new GameObject("--- ENEMIES ---");
            var deco = new GameObject("--- DECORATIONS ---");
            var cam  = new GameObject("--- SHOWCASE CAMERA ---");

            // Jurang di bawah (lantai jauh)
            MkPlatform("AbyssFloor", V(0,-10,40), V(50,1,100), m.AncientStone, env);

            // ── Backdrop: Tembok kuil reruntuhan di latar ─────────────────────
            MkPlatform("RuinWall_Back",  V(0,5,88), V(20,15,2), m.AncientStone, env);
            MkPlatform("RuinWall_Left",  V(-12,3,50), V(2,10,50), m.AncientStone, env);
            MkPlatform("RuinWall_Right", V( 12,3,50), V(2,10,50), m.AncientStone, env);

            // ── P0: Gerbang Runtuh (Start) ────────────────────────────────────
            MkPlatform("P0_Gate", V(0,0,0), V(10,1,10), m.AncientStone, env);
            MkArch("GateArch_Ruin", V(0,0.5f,-2f), m.AncientStone, env);
            MkArch("GateArch_Broken", V(5,0.5f,-1f), m.AncientStone, env); // arch kedua miring sedikit
            MkSpawnBeacon(V(0,0.6f,0), m.Crystal, env);

            // ── P1–P2: Platform batu awal, sedikit miring ─────────────────────
            MkPlatform("P1", V(-1f,0,11), V(5,0.8f,4), m.AncientStone, env);
            MkPlatform("P2", V( 1f,0,16), V(4,0.8f,4), m.AncientStone, env);

            // ── P3: Lompat lebar (3.5m) ke platform sempit ────────────────────
            MkPlatform("P3_Narrow", V(0,2,24), V(2.5f,1,4), m.Stone, env);

            // ── P4–P5: Platform sempit, naik ──────────────────────────────────
            MkPlatform("P4", V(-1f,2,29), V(3,1,4), m.Stone, env);
            MkPlatform("P5", V( 1f,2,34), V(3,1,4), m.Stone, env);

            // Enemy penjaga di P5
            MkEnemy("Penjaga_Kuil_1", V(1.5f,3.2f,35), 1.0f, m, EnemyPlaceholder.EnemyType.WoodlingScout, false, 0, Vector3.forward, npc);

            // ── Obstacle 1: Duri di P4 (sisi kanan, celah di kiri) ────────────
            MkThorn("Thorn_P4", V(1.2f,3.2f,29), V(1.0f,0.8f,2.5f), m.Hazard, haz);

            // ── P6: Platform sempit + duri ─────────────────────────────────────
            MkPlatform("P6_Narrow", V(0,1.5f,40), V(2.5f,1,3), m.Stone, env);
            MkThorn("Thorn_P6_R", V(1.2f,2.7f,40), V(0.8f,0.7f,2f), m.Hazard, haz);

            // ── Jurang: Batu Loncatan B1–B2 ───────────────────────────────────
            // Gap 3m antar batu, tidak ada lantai di bawah
            MkPlatform("BoulderB1", V(-1f,1.5f,46), V(2,1,2), m.AncientStone, env);
            MkPlatform("BoulderB2", V( 1f,1.5f,51), V(2,1,2), m.AncientStone, env);

            // ── P7–P8: Mendekati tangga ────────────────────────────────────────
            MkPlatform("P7", V(0,1.5f,57), V(5,1,5), m.AncientStone, env);
            MkPlatform("P8", V(0,1.5f,64), V(5,1,6), m.AncientStone, env);

            // Enemy penjaga kedua di P8
            MkEnemy("Penjaga_Kuil_2", V(1f,3.0f,66), 1.0f, m, EnemyPlaceholder.EnemyType.WoodlingScout, false, 0, Vector3.forward, npc);

            // ── Obstacle 2: Tembok Runtuh Penghalang (2 cube selang-seling) ────
            var wall1 = MkObj("RuinWallBlock_1", V(-1.5f,2.5f,60), V(3f,2.5f,0.5f), m.AncientStone, haz);
            var wall2 = MkObj("RuinWallBlock_2", V( 1.5f,2.5f,63), V(3f,2.5f,0.5f), m.AncientStone, haz);

            // ── Obstacle 3: Kolom patah dekoratif (visual hazard) ─────────────
            var brokenCol = MkObj("BrokenColumn_Deco", V(2f,3.5f,68), V(1.2f,5f,1.2f), m.AncientStone, deco);
            brokenCol.transform.rotation = Quaternion.Euler(0,0,15f); // miring

            // ── Tangga × 5 anak menuju Arena ──────────────────────────────────
            for (int i = 0; i < 5; i++)
            {
                float z = 70f + i * 2.0f;
                float y = 1.5f + i * 0.5f;
                MkPlatform($"Stair_{i+1}", V(0,y,z), V(5,0.4f,2.2f), m.AncientStone, env);
            }
            // Obor di tangga
            MkTorch("Torch_Stair_L", V(-3f,4.0f,70), m, deco);
            MkTorch("Torch_Stair_R", V( 3f,4.5f,75), m, deco);

            // ── P9: Arena Golem (10×10, lantai batu) ──────────────────────────
            float arenaY = 4.0f;
            MkPlatform("Arena_Floor",   V(0,arenaY,88),   V(12,1.5f,14), m.AncientStone, env);
            // Tembok arena
            MkPlatform("ArenaWall_L",  V(-7,arenaY+3,88), V(1,6,14), m.AncientStone, env);
            MkPlatform("ArenaWall_R",  V( 7,arenaY+3,88), V(1,6,14), m.AncientStone, env);
            MkPlatform("ArenaWall_Back",V(0,arenaY+3,95), V(14,6,1), m.AncientStone, env);

            // MINI-BOSS: Golem Penjaga (skala 3×)
            MkEnemy("Golem_MinionBoss", V(0,arenaY+3.0f,88), 2.5f, m, EnemyPlaceholder.EnemyType.AncientColossus, false, 0, Vector3.forward, npc);

            // Kristal Lumora terakhir (besar, di altar belakang Golem)
            MkPlatform("Altar_Base",  V(0,arenaY+0.5f,93), V(5,1,4), m.AncientStone, env);
            MkCrystal("Crystal_Lumora_L3", V(0,arenaY+3.5f,93), V(2.0f,4.0f,2.0f), m.Crystal, env, isMajor:true);
            MkFinishPortal(V(0,arenaY+1.5f,96), m, env);

            // ── Coins: Jalur Pemandu (12 coin) ────────────────────────────────
            MkCoin(V(0,    1.0f, 4),   m.GoldCoin, coins);
            MkCoin(V(-1f,  1.0f,12),   m.GoldCoin, coins);
            MkCoin(V( 1f,  1.0f,17),   m.GoldCoin, coins);
            MkCoin(V(0,    3.5f,25),   m.GoldCoin, coins);
            MkCoin(V(-1f,  3.5f,30),   m.GoldCoin, coins);
            MkCoin(V( 1f,  3.5f,36),   m.GoldCoin, coins);
            MkCoin(V(0,    3.0f,42),   m.GoldCoin, coins);
            MkCoin(V(-1f,  3.0f,47),   m.GoldCoin, coins);
            MkCoin(V( 1f,  3.0f,52),   m.GoldCoin, coins);
            MkCoin(V(0,    3.0f,58),   m.GoldCoin, coins);
            MkCoin(V(0,    3.5f,66),   m.GoldCoin, coins);
            MkCoin(V(0,    5.5f,72),   m.GoldCoin, coins);

            // ── Coins Tersembunyi (3 coin) ─────────────────────────────────────
            MkCoin(V(4f,   2.5f,  1),  m.GoldCoin, coins); // C-H1: atas gerbang
            MkCoin(V(-2f,  4.0f, 70),  m.GoldCoin, coins); // C-H2: balik kolom patah
            MkCoin(V(-3f,  arenaY+2f,93), m.GoldCoin, coins); // C-H3: belakang Golem

            // ── Dekorasi Kuil ──────────────────────────────────────────────────
            // Pilar batu kuno berdiri dan rubuh
            MkPillar("Pillar_Stand_L", V(-4f,0,20), 3.0f, m.AncientStone, deco, tipped:false);
            MkPillar("Pillar_Stand_R", V( 4f,0,20), 3.0f, m.AncientStone, deco, tipped:false);
            MkPillar("Pillar_Fallen_1",V(-5f,0,45), 1.5f, m.AncientStone, deco, tipped:true);
            MkPillar("Pillar_Fallen_2",V( 5f,0,55), 1.5f, m.AncientStone, deco, tipped:true);
            // Reruntuhan tembok latar
            MkObj("RuinBlock_1", V(-6f,1.5f,30), V(3f,3f,3f), m.AncientStone, deco);
            MkObj("RuinBlock_2", V( 7f,1.0f,48), V(2f,2f,2f), m.AncientStone, deco);
            // Obor tambahan di arena
            MkTorch("Torch_Arena_L", V(-5f,arenaY+2.5f,82), m, deco);
            MkTorch("Torch_Arena_R", V( 5f,arenaY+2.5f,82), m, deco);
            // Sulur/lumut di dinding (plane tipis)
            MkMoss("Moss_1", V(-6f,arenaY+2f,88), deco, m.Foliage);
            MkMoss("Moss_2", V( 6f,arenaY+2f,88), deco, m.Foliage);

            // ── Showcase Camera ────────────────────────────────────────────────
            var waypoints = new List<Vector3>
            {
                V(-6f, 5.0f, -5f),   // aerial view gerbang
                V( 0f, 4.0f, 15f),
                V( 2f, 4.0f, 30f),
                V(-2f, 4.0f, 48f),   // tunjukkan batu loncatan
                V( 0f, 5.0f, 64f),
                V( 0f, 7.0f, 74f),   // kamera menengadah ke tangga
                V( 0f, arenaY+6f,83f), // aerial arena — Golem terlihat
                V( 0f, arenaY+3f,91f), // close ke kristal terakhir
            };
            var scCam = SetupCamera(waypoints, cam, 4.0f);
            MkAudioManager(env);
            MkPlayer(V(0,0.6f,0), m, env, scCam);
            MkTitleUI(env, "Kuil Reruntuhan", "Level 3  •  Sulit  •  Mini-Boss Golem", 3);

            EditorSceneManager.SaveScene(sc, $"{ScenePath}/Level_3_KuilReruntuhan.unity");
        }

        // ╔══════════════════════════════════════════════════════════════════╗
        // ║  HELPER METHODS                                                  ║
        // ╚══════════════════════════════════════════════════════════════════╝

        // Shorthand Vector3
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        private static GameObject MkPlatform(string name, Vector3 pos, Vector3 scale, Material mat, GameObject parent)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent.transform);
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = mat;
            return obj;
        }

        private static GameObject MkObj(string name, Vector3 pos, Vector3 scale, Material mat, GameObject parent)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent.transform);
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = mat;
            // Collider tidak diperlukan untuk dekorasi — tapi biarkan ada (engine handle)
            return obj;
        }

        private static void MkCoin(Vector3 pos, Material mat, GameObject parent)
        {
            var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "Coin_BijicahaYa";
            coin.transform.SetParent(parent.transform);
            coin.transform.position = pos;
            coin.transform.localScale = new Vector3(0.55f, 0.08f, 0.55f);
            coin.transform.rotation = Quaternion.Euler(90, 0, 0);
            coin.GetComponent<Renderer>().sharedMaterial = mat;
            coin.GetComponent<Collider>().isTrigger = true;
            var rot = coin.AddComponent<CoinRotator>();
            rot.rotateSpeed = 140f;
            rot.bobHeight = 0.18f;
            rot.bobSpeed = 2.2f;
        }

        private static void MkCrystal(string name, Vector3 pos, Vector3 scale, Material mat, GameObject parent, bool isMajor)
        {
            // Kristal dari 2 cube rotasi 45° untuk tampilan faceted
            var root = new GameObject(name);
            root.transform.SetParent(parent.transform);
            root.transform.position = pos;

            var shard1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard1.name = "Shard_A";
            shard1.transform.SetParent(root.transform);
            shard1.transform.localPosition = Vector3.zero;
            shard1.transform.localScale = scale;
            shard1.transform.rotation = Quaternion.Euler(0, 45f, 15f);
            shard1.GetComponent<Renderer>().sharedMaterial = mat;

            var shard2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard2.name = "Shard_B";
            shard2.transform.SetParent(root.transform);
            shard2.transform.localPosition = Vector3.zero;
            shard2.transform.localScale = scale * 0.7f;
            shard2.transform.rotation = Quaternion.Euler(0, -30f, -10f);
            shard2.GetComponent<Renderer>().sharedMaterial = mat;

            // Pasang CrystalPulse
            var pulse = root.AddComponent<CrystalPulse>();
            pulse.crystalColor = new Color(0.31f, 0.96f, 0.85f);
            pulse.minEmission = isMajor ? 1.5f : 0.8f;
            pulse.maxEmission = isMajor ? 5.0f : 3.0f;
            pulse.pulseSpeed = isMajor ? 1.8f : 1.2f;
            pulse.enableSlowRotation = true;
            pulse.rotationSpeed = isMajor ? 20f : 8f;

            // Point Light kecil baked untuk kristal mayor
            if (isMajor)
            {
                var lightObj = new GameObject("CrystalLight");
                lightObj.transform.SetParent(root.transform);
                lightObj.transform.localPosition = Vector3.zero;
                var l = lightObj.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(0.31f, 0.96f, 0.85f);
                l.range = 8f;
                l.intensity = 3.0f;
                l.lightmapBakeType = LightmapBakeType.Baked;
            }
        }

        private static void MkThorn(string name, Vector3 pos, Vector3 scale, Material mat, GameObject parent)
        {
            var zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zone.name = name;
            zone.transform.SetParent(parent.transform);
            zone.transform.position = pos;
            zone.transform.localScale = scale;
            zone.GetComponent<Renderer>().sharedMaterial = mat;
            // Spike visual
            for (int i = 0; i < 3; i++)
            {
                var sp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                sp.name = $"Spike_{i}";
                sp.transform.SetParent(zone.transform);
                sp.transform.localPosition = new Vector3(-0.2f + i * 0.2f, 0.8f, 0);
                sp.transform.localScale = new Vector3(0.25f, 0.7f, 0.25f);
                sp.GetComponent<Renderer>().sharedMaterial = mat;
                Object.DestroyImmediate(sp.GetComponent<Collider>());
            }
        }

        private static void MkStalactite(string name, Vector3 pos, Vector3 scale, Material mat, GameObject parent)
        {
            // Cone terbalik menggantung dari ceiling
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent.transform);
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            obj.transform.rotation = Quaternion.Euler(180, 0, 0); // terbalik
            obj.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void MkStalagmite(string name, Vector3 pos, Material mat, GameObject parent)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent.transform);
            obj.transform.position = pos;
            obj.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
            obj.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void MkSpawnBeacon(Vector3 pos, Material mat, GameObject parent)
        {
            var beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beacon.name = "SpawnBeacon";
            beacon.transform.SetParent(parent.transform);
            beacon.transform.position = pos;
            beacon.transform.localScale = new Vector3(2.5f, 0.1f, 2.5f);
            beacon.GetComponent<Renderer>().sharedMaterial = mat;

            var lg = new GameObject("BeaconLight");
            lg.transform.SetParent(beacon.transform);
            lg.transform.localPosition = new Vector3(0, 2f, 0);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.31f, 0.96f, 0.85f);
            l.range = 6f;
            l.intensity = 2f;
            l.lightmapBakeType = LightmapBakeType.Baked;
        }

        private static void MkArch(string name, Vector3 pos, Material mat, GameObject parent)
        {
            var arch = new GameObject(name);
            arch.transform.SetParent(parent.transform);
            arch.transform.position = pos;

            var c1 = GameObject.CreatePrimitive(PrimitiveType.Cube); c1.transform.SetParent(arch.transform);
            c1.transform.localPosition = new Vector3(-2.2f, 1.5f, 0);
            c1.transform.localScale = new Vector3(0.7f, 3f, 0.7f);
            c1.GetComponent<Renderer>().sharedMaterial = mat;

            var c2 = GameObject.CreatePrimitive(PrimitiveType.Cube); c2.transform.SetParent(arch.transform);
            c2.transform.localPosition = new Vector3( 2.2f, 1.5f, 0);
            c2.transform.localScale = new Vector3(0.7f, 3f, 0.7f);
            c2.GetComponent<Renderer>().sharedMaterial = mat;

            var top = GameObject.CreatePrimitive(PrimitiveType.Cube); top.transform.SetParent(arch.transform);
            top.transform.localPosition = new Vector3(0, 3.2f, 0);
            top.transform.localScale = new Vector3(5.2f, 0.6f, 0.9f);
            top.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void MkFinishPortal(Vector3 pos, LumoraMats m, GameObject parent)
        {
            var portal = new GameObject("FinishPortal_KristalLumora");
            portal.transform.SetParent(parent.transform);
            portal.transform.position = pos;

            var pL = GameObject.CreatePrimitive(PrimitiveType.Cube); pL.transform.SetParent(portal.transform);
            pL.transform.localPosition = new Vector3(-2f, 2f, 0); pL.transform.localScale = new Vector3(0.8f, 4f, 0.8f);
            pL.GetComponent<Renderer>().sharedMaterial = m.AncientStone;

            var pR = GameObject.CreatePrimitive(PrimitiveType.Cube); pR.transform.SetParent(portal.transform);
            pR.transform.localPosition = new Vector3( 2f, 2f, 0); pR.transform.localScale = new Vector3(0.8f, 4f, 0.8f);
            pR.GetComponent<Renderer>().sharedMaterial = m.AncientStone;

            var top = GameObject.CreatePrimitive(PrimitiveType.Cube); top.transform.SetParent(portal.transform);
            top.transform.localPosition = new Vector3(0, 4.2f, 0); top.transform.localScale = new Vector3(5.2f, 0.8f, 1f);
            top.GetComponent<Renderer>().sharedMaterial = m.AncientStone;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder); ring.name = "PortalEnergy";
            ring.transform.SetParent(portal.transform);
            ring.transform.localPosition = new Vector3(0, 2f, 0);
            ring.transform.localScale = new Vector3(3f, 0.08f, 3f);
            ring.transform.rotation = Quaternion.Euler(90, 0, 0);
            ring.GetComponent<Renderer>().sharedMaterial = m.Portal;
            ring.GetComponent<Collider>().isTrigger = true;
            ring.AddComponent<CrystalPulse>(); // portal juga berdenyut

            var lightObj = new GameObject("PortalLight"); lightObj.transform.SetParent(portal.transform);
            lightObj.transform.localPosition = new Vector3(0, 2f, 0);
            var l = lightObj.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.8f, 0.2f);
            l.range = 8f; l.intensity = 4f; l.lightmapBakeType = LightmapBakeType.Baked;
        }

        private static void MkEnemy(string name, Vector3 pos, float scaleMult, LumoraMats m,
            EnemyPlaceholder.EnemyType type, bool patrol, float dist, Vector3 axis, GameObject parent)
        {
            var enemy = new GameObject(name);
            enemy.transform.SetParent(parent.transform);
            enemy.transform.position = pos;

            bool isGolem = (type == EnemyPlaceholder.EnemyType.AncientColossus);
            Material bodyMat = isGolem ? m.AncientStone : m.EnemyArmor;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body"; body.transform.SetParent(enemy.transform);
            body.transform.localPosition = new Vector3(0, 0.7f * scaleMult, 0);
            body.transform.localScale = new Vector3(1.2f * scaleMult, 0.7f * scaleMult, 1.2f * scaleMult);
            body.GetComponent<Renderer>().sharedMaterial = bodyMat;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head"; head.transform.SetParent(enemy.transform);
            head.transform.localPosition = new Vector3(0, 1.5f * scaleMult, 0);
            head.transform.localScale = Vector3.one * 0.9f * scaleMult;
            head.GetComponent<Renderer>().sharedMaterial = bodyMat;
            Object.DestroyImmediate(head.GetComponent<Collider>());

            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Eye"; eye.transform.SetParent(head.transform);
            eye.transform.localPosition = new Vector3(0, 0.1f, 0.42f);
            eye.transform.localScale = Vector3.one * 0.35f;
            eye.GetComponent<Renderer>().sharedMaterial = m.EnemyGlow;
            Object.DestroyImmediate(eye.GetComponent<Collider>());

            if (isGolem)
            {
                // Tanduk batu Golem
                foreach (float side in new[] { -1f, 1f })
                {
                    var horn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    horn.transform.SetParent(head.transform);
                    horn.transform.localPosition = new Vector3(0.6f * side, 0.8f, 0);
                    horn.transform.localScale = new Vector3(0.18f, 0.8f, 0.18f);
                    horn.transform.localRotation = Quaternion.Euler(0, 0, -30f * side);
                    horn.GetComponent<Renderer>().sharedMaterial = m.AncientStone;
                    Object.DestroyImmediate(horn.GetComponent<Collider>());
                }
                // Kristal di dada Golem (sesuai spec)
                var chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chest.name = "ChestCrystal"; chest.transform.SetParent(enemy.transform);
                chest.transform.localPosition = new Vector3(0, 0.9f * scaleMult, 0.65f * scaleMult);
                chest.transform.localScale = new Vector3(0.5f, 0.8f, 0.3f) * scaleMult;
                chest.GetComponent<Renderer>().sharedMaterial = m.Crystal;
                chest.AddComponent<CrystalPulse>();
                Object.DestroyImmediate(chest.GetComponent<Collider>());
            }

            var ep = enemy.AddComponent<EnemyPlaceholder>();
            ep.enemyType = type;
            ep.canPatrol = patrol;
            ep.patrolDistance = dist;
            ep.patrolAxis = axis == default ? Vector3.right : axis;
        }

        private static void MkTree(Vector3 pos, float scale, LumoraMats m, GameObject parent)
        {
            var tree = new GameObject("Tree");
            tree.transform.SetParent(parent.transform);
            tree.transform.position = pos;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk"; trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = new Vector3(0, 1.5f * scale, 0);
            trunk.transform.localScale = new Vector3(0.5f * scale, 1.5f * scale, 0.5f * scale);
            trunk.GetComponent<Renderer>().sharedMaterial = m.TreeBark;
            Object.DestroyImmediate(trunk.GetComponent<Collider>());

            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Crown"; crown.transform.SetParent(tree.transform);
            crown.transform.localPosition = new Vector3(0, 3.5f * scale, 0);
            crown.transform.localScale = new Vector3(2.8f * scale, 2.2f * scale, 2.8f * scale);
            crown.GetComponent<Renderer>().sharedMaterial = m.Foliage;
            Object.DestroyImmediate(crown.GetComponent<Collider>());
        }

        private static void MkMushroom(Vector3 pos, LumoraMats m, GameObject parent)
        {
            var shroom = new GameObject("GlowMushroom");
            shroom.transform.SetParent(parent.transform);
            shroom.transform.position = pos;

            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.transform.SetParent(shroom.transform);
            stem.transform.localPosition = new Vector3(0, 0.3f, 0);
            stem.transform.localScale = new Vector3(0.2f, 0.3f, 0.2f);
            stem.GetComponent<Renderer>().sharedMaterial = m.Stone;
            Object.DestroyImmediate(stem.GetComponent<Collider>());

            var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.transform.SetParent(shroom.transform);
            cap.transform.localPosition = new Vector3(0, 0.65f, 0);
            cap.transform.localScale = new Vector3(0.75f, 0.38f, 0.75f);
            cap.GetComponent<Renderer>().sharedMaterial = m.Crystal;
            cap.AddComponent<CrystalPulse>().pulseSpeed = 2.0f;
            Object.DestroyImmediate(cap.GetComponent<Collider>());
        }

        private static void MkPillar(string name, Vector3 pos, float height, Material mat, GameObject parent, bool tipped)
        {
            var col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            col.name = name;
            col.transform.SetParent(parent.transform);
            col.transform.position = pos + Vector3.up * height;
            col.transform.localScale = new Vector3(0.8f, height, 0.8f);
            if (tipped) col.transform.rotation = Quaternion.Euler(0, 0, 85f);
            col.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(col.GetComponent<Collider>());
        }

        private static void MkTorch(string name, Vector3 pos, LumoraMats m, GameObject parent)
        {
            var torch = new GameObject(name);
            torch.transform.SetParent(parent.transform);
            torch.transform.position = pos;

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(torch.transform);
            pole.transform.localPosition = new Vector3(0, 0.5f, 0);
            pole.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);
            pole.GetComponent<Renderer>().sharedMaterial = m.DarkWood;
            Object.DestroyImmediate(pole.GetComponent<Collider>());

            var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.transform.SetParent(torch.transform);
            flame.transform.localPosition = new Vector3(0, 1.1f, 0);
            flame.transform.localScale = new Vector3(0.3f, 0.4f, 0.3f);
            flame.GetComponent<Renderer>().sharedMaterial = m.Portal; // kuning-oranye
            Object.DestroyImmediate(flame.GetComponent<Collider>());

            var lg = new GameObject("TorchLight"); lg.transform.SetParent(torch.transform);
            lg.transform.localPosition = new Vector3(0, 1.2f, 0);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.6f, 0.15f);
            l.range = 5f; l.intensity = 2.5f; l.lightmapBakeType = LightmapBakeType.Baked;
        }

        private static void MkMoss(string name, Vector3 pos, GameObject parent, Material mat)
        {
            var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plane.name = name;
            plane.transform.SetParent(parent.transform);
            plane.transform.position = pos;
            plane.transform.localScale = new Vector3(2f, 3f, 1f);
            plane.transform.rotation = Quaternion.Euler(0, 90f, 0);
            plane.GetComponent<Renderer>().sharedMaterial = mat;
            plane.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.DestroyImmediate(plane.GetComponent<Collider>());
        }

        private static void SetupLighting(
            Color sunCol, float sunInt, Vector3 sunRot,
            Color fogCol, float fogDens,
            Color ambSky, Color ambEq, Color ambGround)
        {
            var sunObj = new GameObject("Directional Light (Sun)");
            var sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = sunCol;
            sun.intensity = sunInt;
            sunObj.transform.rotation = Quaternion.Euler(sunRot);
            RenderSettings.sun = sun;

            RenderSettings.fog = true;
            RenderSettings.fogColor = fogCol;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDens;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambSky;
            RenderSettings.ambientEquatorColor = ambEq;
            RenderSettings.ambientGroundColor = ambGround;
            SetupLumoraSkybox(ambSky, fogCol);
        }

        private static void SetupLumoraSkybox(Color skyTint, Color groundColor)
        {
            const string path = "Assets/Materials/EnchantedForest/Mat_ForestSkybox.mat";
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sky == null)
            {
                Shader shader = Shader.Find("Skybox/Procedural");
                if (shader == null)
                {
                    Debug.LogError("[Lumora] Shader Skybox/Procedural tidak ditemukan.");
                    return;
                }
                sky = new Material(shader) { name = "Mat_ForestSkybox" };
                sky.SetFloat("_SunSize", 0.035f);
                sky.SetFloat("_AtmosphereThickness", 1.2f);
                sky.SetFloat("_Exposure", 1.15f);
                AssetDatabase.CreateAsset(sky, path);
            }
            sky.SetColor("_SkyTint", Color.Lerp(Color.white, skyTint, 0.35f));
            sky.SetColor("_GroundColor", groundColor);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
        }

        private static void MkCharacterPart(Transform parent, string partName, PrimitiveType shape, Vector3 position, Vector3 scale, Material material, float xRotation = 0f, float yRotation = 0f, float zRotation = 0f)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
            part.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(part.GetComponent<Collider>());
        }

        private static GameObject SetupCamera(List<Vector3> waypoints, GameObject parent, float speed)
        {
            var camObj = new GameObject("ShowcaseCamera_Lumora");
            camObj.tag = "MainCamera";
            camObj.transform.SetParent(parent.transform);
            Camera camera = camObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camObj.AddComponent<AudioListener>();

            var sc = camObj.AddComponent<ShowcaseCamera>();
            sc.moveSpeed = speed;
            sc.rotationSmoothness = 2.5f;
            sc.loop = false; // jalan sekali, berhenti di akhir (cocok untuk rekaman)
            sc.playOnStart = true;

            var wpRoot = new GameObject("CameraWaypoints");
            wpRoot.transform.SetParent(parent.transform);

            for (int i = 0; i < waypoints.Count; i++)
            {
                var wp = new GameObject($"WP_{i + 1}");
                wp.transform.SetParent(wpRoot.transform);
                wp.transform.position = waypoints[i];
                sc.waypoints.Add(wp.transform);
            }
            return camObj;
        }

        private static void MkAudioManager(GameObject parent)
        {
            var ao = new GameObject("GameAudioManager");
            ao.transform.SetParent(parent.transform);
            var mgr = ao.AddComponent<GameAudioManager>();
            mgr.coinClip      = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/CoinPickup.wav");
            mgr.bounceClip    = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BouncePad.wav");
            mgr.victoryClip   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VictoryFanfare.wav");
            mgr.ambientClip       = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ForestAmbience.wav");
        }

        private static void MkPlayer(Vector3 spawn, LumoraMats m, GameObject parent, GameObject scCam)
        {
            var player = new GameObject("Riko_Player");
            player.tag = "Player";
            player.transform.SetParent(parent.transform);
            player.transform.position = spawn + Vector3.up * 0.1f;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.4f; cc.center = new Vector3(0, 0.9f, 0);

            var vis = new GameObject("Visuals");
            vis.transform.SetParent(player.transform);

            // Body (tunik biru Riko)
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body"; body.transform.SetParent(vis.transform);
            body.transform.localPosition = new Vector3(0, 0.78f, 0);
            body.transform.localScale = new Vector3(0.55f, 0.72f, 0.4f);
            body.GetComponent<Renderer>().sharedMaterial = m.PlayerTunic;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            MkCharacterPart(vis.transform, "LeftArm", PrimitiveType.Capsule, new Vector3(-0.37f, 0.78f, 0.015f), new Vector3(0.18f, 0.54f, 0.18f), m.PlayerTunic, 0f, 0f, -12f);
            MkCharacterPart(vis.transform, "RightArm", PrimitiveType.Capsule, new Vector3(0.37f, 0.78f, 0.015f), new Vector3(0.18f, 0.54f, 0.18f), m.PlayerTunic, 0f, 0f, 12f);
            MkCharacterPart(vis.transform, "LeftGlove", PrimitiveType.Sphere, new Vector3(-0.44f, 0.49f, 0.02f), Vector3.one * 0.16f, m.PlayerLeather);
            MkCharacterPart(vis.transform, "RightGlove", PrimitiveType.Sphere, new Vector3(0.44f, 0.49f, 0.02f), Vector3.one * 0.16f, m.PlayerLeather);
            MkCharacterPart(vis.transform, "LeftLeg", PrimitiveType.Capsule, new Vector3(-0.15f, 0.25f, 0f), new Vector3(0.21f, 0.42f, 0.22f), m.PlayerLeather);
            MkCharacterPart(vis.transform, "RightLeg", PrimitiveType.Capsule, new Vector3(0.15f, 0.25f, 0f), new Vector3(0.21f, 0.42f, 0.22f), m.PlayerLeather);
            MkCharacterPart(vis.transform, "LeftBoot", PrimitiveType.Cube, new Vector3(-0.15f, 0.08f, 0.07f), new Vector3(0.24f, 0.15f, 0.36f), m.DarkWood);
            MkCharacterPart(vis.transform, "RightBoot", PrimitiveType.Cube, new Vector3(0.15f, 0.08f, 0.07f), new Vector3(0.24f, 0.15f, 0.36f), m.DarkWood);

            // Kepala
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head"; head.transform.SetParent(vis.transform);
            head.transform.localPosition = new Vector3(0, 1.4f, 0);
            head.transform.localScale = Vector3.one * 0.44f;
            head.GetComponent<Renderer>().sharedMaterial = m.PlayerSkin;
            Object.DestroyImmediate(head.GetComponent<Collider>());

            // Topi penjaga hutan
            var hatRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hatRim.name = "Hat"; hatRim.transform.SetParent(vis.transform);
            hatRim.transform.localPosition = new Vector3(0, 1.56f, 0);
            hatRim.transform.localScale = new Vector3(0.75f, 0.05f, 0.75f);
            hatRim.GetComponent<Renderer>().sharedMaterial = m.PlayerLeather;
            Object.DestroyImmediate(hatRim.GetComponent<Collider>());

            // Kristal pegangan (torch Lumora)
            var stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stick.name = "CrystalStaff"; stick.transform.SetParent(vis.transform);
            stick.transform.localPosition = new Vector3(0.38f, 0.6f, 0.22f);
            stick.transform.localScale = new Vector3(0.07f, 0.35f, 0.07f);
            stick.GetComponent<Renderer>().sharedMaterial = m.DarkWood;
            Object.DestroyImmediate(stick.GetComponent<Collider>());

            var gem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gem.name = "LumoraGem"; gem.transform.SetParent(stick.transform);
            gem.transform.localPosition = new Vector3(0, 1.1f, 0);
            gem.transform.localScale = Vector3.one * 2.2f;
            gem.GetComponent<Renderer>().sharedMaterial = m.Crystal;
            gem.AddComponent<CrystalPulse>().pulseSpeed = 2.5f;
            Object.DestroyImmediate(gem.GetComponent<Collider>());

            // Player Camera (Third Person)
            var playerCam = new GameObject("PlayerCamera_ThirdPerson");
            playerCam.transform.SetParent(player.transform);
            playerCam.transform.localPosition = new Vector3(0, 2.8f, -5.5f);
            var cam = playerCam.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            playerCam.AddComponent<AudioListener>();
            var follow = playerCam.AddComponent<ThirdPersonCameraFollow>();
            follow.target = player.transform;

            var ctrl = player.AddComponent<PlayerController3D>();
            ctrl.cameraTransform = playerCam.transform;
            ctrl.spawnPoint = spawn + Vector3.up * 0.5f;
            ctrl.fallThresholdY = spawn.y - 12f;

            // GameModeManager
            var gmmObj = new GameObject("GameModeManager");
            gmmObj.transform.SetParent(parent.transform);
            var gmm = gmmObj.AddComponent<GameModeManager>();
            gmm.showcaseCameraObj = scCam;
            gmm.playerCameraObj = playerCam;
            gmm.playerCharacterObj = player;
            gmm.currentMode = GameModeManager.CameraMode.ShowcaseTour;
        }

        private static void MkTitleUI(GameObject parent, string levelName, string subtitle, int levelNum)
        {
            var titleObj = new GameObject("LevelTitleUI");
            titleObj.transform.SetParent(parent.transform);
            var ui = titleObj.AddComponent<LevelTitleUI>();
            ui.levelName = levelName;
            ui.levelSubtitle = subtitle;
            ui.levelNumber = levelNum;
            ui.displayDuration = 4.0f;
            ui.fadeDuration = 1.2f;
            ui.showOnStart = true;
            ui.showControlsHint = true;
        }

        // ── Register scenes ke Build Settings ────────────────────────────────
        private static void RegisterScenes()
        {
            string[] paths = {
                $"{ScenePath}/Level_1_HutanFajar.unity",
                $"{ScenePath}/Level_2_GuaLumut.unity",
                $"{ScenePath}/Level_3_KuilReruntuhan.unity",
            };
            var list = new List<EditorBuildSettingsScene>();
            // Pertahankan scene lama (EnchantedForest) jika masih ada
            foreach (var existing in EditorBuildSettings.scenes)
            {
                if (existing != null && File.Exists(existing.path)) list.Add(existing);
            }
            foreach (var p in paths)
            {
                if (File.Exists(p) && !list.Exists(s => s.path == p))
                {
                    list.Add(new EditorBuildSettingsScene(p, true));
                }
            }
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static void OpenScene(string fileName)
        {
            string full = $"{ScenePath}/{fileName}";
            if (File.Exists(full))
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(full);
            }
            else
            {
                EditorUtility.DisplayDialog("Scene Belum Dibuat",
                    $"{fileName} belum ada.\nKlik ⚡ Generate Semua Level Lumora terlebih dahulu.", "OK");
            }
        }
    }
}
