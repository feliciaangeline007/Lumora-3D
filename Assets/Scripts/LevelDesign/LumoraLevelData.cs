using UnityEngine;

namespace EnchantedForest
{
    /// <summary>
    /// ScriptableObject yang menyimpan konfigurasi visual dan metadata tiap level Lumora.
    /// Buat instance-nya lewat: Assets → Create → Lumora → Level Data
    ///
    /// CARA PAKAI:
    ///   1. Klik kanan di Project → Create → Lumora → Level Data
    ///   2. Isi semua field sesuai level (nama, warna, pencahayaan, fog)
    ///   3. Assign ke LumoraLevelBuilder atau baca via Resources.Load di runtime
    /// </summary>
    [CreateAssetMenu(
        fileName  = "LumoraLevel_Data",
        menuName  = "Lumora/Level Data",
        order     = 1)]
    public class LumoraLevelData : ScriptableObject
    {
        // ── Identitas Level ──────────────────────────────────────────────────
        [Header("Identitas Level")]
        [Tooltip("Nama level yang ditampilkan di UI dan title card. Contoh: Hutan Fajar")]
        public string levelName = "Hutan Fajar";

        [Tooltip("Subtitle untuk title card rekaman. Contoh: Level 1  •  Mudah")]
        public string levelSubtitle = "Level 1  •  Mudah";

        [Tooltip("Nomor urut level (1-3)")]
        [Range(1, 3)]
        public int levelNumber = 1;

        [Tooltip("Nama file scene Unity (.unity tanpa path). Contoh: Level_1_HutanFajar")]
        public string sceneFileName = "Level_1_HutanFajar";

        [Tooltip("Deskripsi singkat cerita yang terjadi di level ini (untuk dokumentasi)")]
        [TextArea(2, 4)]
        public string storyDescription =
            "Riko memulai perjalanan di tepi Hutan Fajar. " +
            "Cahaya pagi menerobos kanopi pohon saat ia mencari serpihan Kristal Lumora pertama.";

        // ── Kesulitan ────────────────────────────────────────────────────────
        public enum DifficultyLevel { Mudah, Sedang, Sulit }

        [Header("Kesulitan")]
        public DifficultyLevel difficulty = DifficultyLevel.Mudah;

        [Tooltip("Perkiraan durasi jalan dari start ke finish (detik)")]
        [Range(30, 180)]
        public int estimatedDurationSeconds = 65;

        // ── Pencahayaan ──────────────────────────────────────────────────────
        [Header("Pencahayaan (Directional Light)")]
        [Tooltip("Warna sinar matahari/utama level ini")]
        public Color sunColor = new Color(1f, 0.93f, 0.82f);

        [Tooltip("Intensitas Directional Light")]
        [Range(0f, 3f)]
        public float sunIntensity = 1.2f;

        [Tooltip("Rotasi Directional Light (X=sudut matahari, Y=arah)")]
        public Vector3 sunRotation = new Vector3(45f, -30f, 0f);

        // ── Fog ──────────────────────────────────────────────────────────────
        [Header("Fog Atmosfer")]
        [Tooltip("Aktifkan fog")]
        public bool fogEnabled = true;

        [Tooltip("Warna fog")]
        public Color fogColor = new Color(0.72f, 0.85f, 0.78f);

        [Tooltip("Kepadatan fog (Exponential Squared). Kecil = tipis, besar = pekat)")]
        [Range(0f, 0.15f)]
        public float fogDensity = 0.012f;

        // ── Ambient Light ────────────────────────────────────────────────────
        [Header("Ambient Light")]
        [Tooltip("Warna langit (ambient sky) — berpengaruh pada bayangan objek dari atas")]
        public Color ambientSkyColor = new Color(0.5f, 0.6f, 0.75f);

        [Tooltip("Warna ekuator (ambient equator) — warna cahaya dari samping")]
        public Color ambientEquatorColor = new Color(0.35f, 0.38f, 0.42f);

        [Tooltip("Warna tanah (ambient ground) — bayangan dari bawah)")]
        public Color ambientGroundColor = new Color(0.08f, 0.07f, 0.05f);

        // ── Palet Warna Level ────────────────────────────────────────────────
        [Header("Palet Warna Level")]
        [Tooltip("Warna utama platform (misal: hijau tua untuk L1, abu gelap untuk L2)")]
        public Color platformPrimaryColor = new Color(0.18f, 0.48f, 0.25f);

        [Tooltip("Warna sekunder platform (misal: batu kuno)")]
        public Color platformSecondaryColor = new Color(0.38f, 0.42f, 0.44f);

        [Tooltip("Warna kristal Lumora di level ini. Default: cyan #4FF5D8")]
        public Color crystalColor = new Color(0.31f, 0.96f, 0.85f);

        [Tooltip("Warna coin Biji Cahaya. Default: kuning #FFD84A")]
        public Color coinColor = new Color(1f, 0.847f, 0.29f);

        [Tooltip("Warna hazard/bahaya. Default: merah-ungu #8B1A4A")]
        public Color hazardColor = new Color(0.42f, 0.06f, 0.15f);

        // ── Kamera Showcase ──────────────────────────────────────────────────
        [Header("Kamera Showcase")]
        [Tooltip("Kecepatan terbang kamera showcase (meter per detik)")]
        [Range(1f, 10f)]
        public float cameraSpeed = 3.5f;

        [Tooltip("Posisi spawn player di scene ini")]
        public Vector3 playerSpawnPosition = new Vector3(0f, 1f, 0f);

        [Tooltip("Threshold Y jatuh sebelum respawn (relatif dari spawn)")]
        public float fallRespawnThreshold = -12f;

        // ── Audio ────────────────────────────────────────────────────────────
        [Header("Audio")]
        [Tooltip("Path audio clip BGM level ini (misal: Assets/Audio/ForestAmbience.wav)")]
        public string bgmAudioPath = "Assets/Audio/ForestAmbience.wav";

        // ── Properti Helper ──────────────────────────────────────────────────

        /// <summary>Nama file scene beserta ekstensi.</summary>
        public string SceneFileWithExtension =>
            sceneFileName.EndsWith(".unity") ? sceneFileName : sceneFileName + ".unity";

        /// <summary>Path lengkap scene relatif dari Assets/.</summary>
        public string FullScenePath =>
            $"Assets/Scenes/{SceneFileWithExtension}";

        /// <summary>Ikon emoji sesuai nomor level.</summary>
        public string LevelIcon => levelNumber switch
        {
            1 => "🌅",
            2 => "🕳️",
            3 => "⚔️",
            _ => "✦"
        };

        /// <summary>Teks durasi yang diformat ke menit:detik.</summary>
        public string FormattedDuration
        {
            get
            {
                int min = estimatedDurationSeconds / 60;
                int sec = estimatedDurationSeconds % 60;
                return min > 0 ? $"{min}m {sec}s" : $"{sec}s";
            }
        }

        // ── Preset Factory ────────────────────────────────────────────────────
#if UNITY_EDITOR
        /// <summary>
        /// Isi field dengan preset Level 1 — Hutan Fajar.
        /// Panggil dari Inspector via tombol context menu.
        /// </summary>
        [ContextMenu("Isi Preset: Level 1 — Hutan Fajar")]
        public void ApplyPresetLevel1()
        {
            levelName               = "Hutan Fajar";
            levelSubtitle           = "Level 1  •  Mudah";
            levelNumber             = 1;
            sceneFileName           = "Level_1_HutanFajar";
            storyDescription        = "Riko memulai perjalanan di tepi Hutan Fajar. Cahaya pagi menerobos kanopi pohon saat ia mencari serpihan Kristal Lumora pertama.";
            difficulty              = DifficultyLevel.Mudah;
            estimatedDurationSeconds = 65;

            sunColor                = new Color(1f, 0.93f, 0.82f);
            sunIntensity            = 1.2f;
            sunRotation             = new Vector3(30f, -30f, 0f);

            fogEnabled              = true;
            fogColor                = new Color(0.80f, 0.90f, 0.82f);
            fogDensity              = 0.012f;

            ambientSkyColor         = new Color(0.55f, 0.70f, 0.85f);
            ambientEquatorColor     = new Color(0.42f, 0.52f, 0.48f);
            ambientGroundColor      = new Color(0.08f, 0.10f, 0.05f);

            platformPrimaryColor    = new Color(0.18f, 0.48f, 0.25f); // hijau tua
            platformSecondaryColor  = new Color(0.38f, 0.42f, 0.44f); // batu
            crystalColor            = new Color(0.31f, 0.96f, 0.85f); // cyan
            coinColor               = new Color(1f, 0.847f, 0.29f);   // kuning
            hazardColor             = new Color(0.42f, 0.06f, 0.15f); // merah-ungu

            cameraSpeed             = 3.2f;
            playerSpawnPosition     = new Vector3(0f, 0.6f, 0f);
            fallRespawnThreshold    = -12f;
            bgmAudioPath            = "Assets/Audio/ForestAmbience.wav";

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("[Lumora] Preset Level 1 — Hutan Fajar diterapkan.");
        }

        [ContextMenu("Isi Preset: Level 2 — Gua Lumut Bercahaya")]
        public void ApplyPresetLevel2()
        {
            levelName               = "Gua Lumut Bercahaya";
            levelSubtitle           = "Level 2  •  Sedang";
            levelNumber             = 2;
            sceneFileName           = "Level_2_GuaLumut";
            storyDescription        = "Riko turun ke dalam Gua Lumut yang gelap dan lembap. Satu-satunya cahaya adalah kristal cyan yang tumbuh di dinding dan Biji Cahaya yang ia kumpulkan.";
            difficulty              = DifficultyLevel.Sedang;
            estimatedDurationSeconds = 80;

            sunColor                = new Color(0.65f, 0.70f, 0.90f);
            sunIntensity            = 0.3f;
            sunRotation             = new Vector3(35f, 45f, 0f);

            fogEnabled              = true;
            fogColor                = new Color(0.04f, 0.06f, 0.12f);
            fogDensity              = 0.06f;

            ambientSkyColor         = new Color(0.04f, 0.06f, 0.12f);
            ambientEquatorColor     = new Color(0.02f, 0.04f, 0.08f);
            ambientGroundColor      = new Color(0.01f, 0.01f, 0.02f);

            platformPrimaryColor    = new Color(0.25f, 0.28f, 0.30f); // batu gelap
            platformSecondaryColor  = new Color(0.12f, 0.18f, 0.20f); // batu lebih gelap
            crystalColor            = new Color(0.31f, 0.96f, 0.85f); // cyan terang
            coinColor               = new Color(1f, 0.847f, 0.29f);   // kuning
            hazardColor             = new Color(0.55f, 0.08f, 0.20f); // merah-ungu pekat

            cameraSpeed             = 3.0f;
            playerSpawnPosition     = new Vector3(0f, 1f, 0f);
            fallRespawnThreshold    = -15f;
            bgmAudioPath            = "Assets/Audio/ForestAmbience.wav";

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("[Lumora] Preset Level 2 — Gua Lumut Bercahaya diterapkan.");
        }

        [ContextMenu("Isi Preset: Level 3 — Kuil Reruntuhan")]
        public void ApplyPresetLevel3()
        {
            levelName               = "Kuil Reruntuhan";
            levelSubtitle           = "Level 3  •  Sulit  •  Mini-Boss Golem";
            levelNumber             = 3;
            sceneFileName           = "Level_3_KuilReruntuhan";
            storyDescription        = "Riko tiba di reruntuhan kuil kuno. Langit senja mewarnai segalanya oranye-merah. Golem Penjaga menjaga kristal terakhir di ruang takhta.";
            difficulty              = DifficultyLevel.Sulit;
            estimatedDurationSeconds = 100;

            sunColor                = new Color(1f, 0.65f, 0.35f);
            sunIntensity            = 0.8f;
            sunRotation             = new Vector3(15f, -60f, 0f);

            fogEnabled              = true;
            fogColor                = new Color(0.60f, 0.30f, 0.18f);
            fogDensity              = 0.025f;

            ambientSkyColor         = new Color(0.60f, 0.35f, 0.20f);
            ambientEquatorColor     = new Color(0.40f, 0.22f, 0.12f);
            ambientGroundColor      = new Color(0.08f, 0.05f, 0.03f);

            platformPrimaryColor    = new Color(0.38f, 0.34f, 0.28f); // batu kuno
            platformSecondaryColor  = new Color(0.22f, 0.20f, 0.16f); // batu gelap
            crystalColor            = new Color(0.31f, 0.96f, 0.85f); // cyan
            coinColor               = new Color(1f, 0.847f, 0.29f);   // kuning
            hazardColor             = new Color(0.42f, 0.06f, 0.15f); // merah-ungu

            cameraSpeed             = 4.0f;
            playerSpawnPosition     = new Vector3(0f, 1f, 10f);
            fallRespawnThreshold    = -18f;
            bgmAudioPath            = "Assets/Audio/ForestAmbience.wav";

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("[Lumora] Preset Level 3 — Kuil Reruntuhan diterapkan.");
        }
#endif
    }
}
