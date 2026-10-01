using System.Collections;
using UnityEngine;

namespace EnchantedForest
{
    /// <summary>
    /// Overlay judul level di layar untuk keperluan rekaman video tugas.
    /// Menampilkan nama level, subjudul, dan instruksi kontrol di pojok layar.
    /// Tidak memerlukan Canvas atau TextMeshPro — murni OnGUI Legacy (paling mudah untuk pemula).
    ///
    /// CARA PAKAI:
    ///   1. Attach script ini ke GameObject mana saja di scene (misal ke "GameModeManager").
    ///   2. Isi field levelName, levelSubtitle, dan levelNumber sesuai level.
    ///   3. Script akan otomatis menampilkan judul saat Play.
    /// </summary>
    public class LevelTitleUI : MonoBehaviour
    {
        // ── Teks Judul ───────────────────────────────────────────────────────
        [Header("Teks Judul Level")]
        [Tooltip("Nama level yang ditampilkan besar di layar. Contoh: Hutan Fajar")]
        public string levelName = "Hutan Fajar";

        [Tooltip("Subjudul kecil di bawah nama level. Contoh: Level 1 — Mudah")]
        public string levelSubtitle = "Level 1  •  Mudah";

        [Tooltip("Nomor level untuk ikon (1, 2, atau 3)")]
        [Range(1, 3)]
        public int levelNumber = 1;

        // ── Durasi Tampil ────────────────────────────────────────────────────
        [Header("Durasi Tampil")]
        [Tooltip("Durasi judul terlihat penuh sebelum mulai fade (detik)")]
        public float displayDuration = 3.5f;

        [Tooltip("Durasi fade out (detik)")]
        public float fadeDuration = 1.5f;

        [Tooltip("Langsung tampil saat Play, atau tunggu dipanggil manual")]
        public bool showOnStart = true;

        // ── Warna & Style ────────────────────────────────────────────────────
        [Header("Tampilan")]
        [Tooltip("Warna judul utama. Default: kuning Biji Cahaya (#FFD84A)")]
        public Color titleColor = new Color(1f, 0.847f, 0.290f);        // #FFD84A

        [Tooltip("Warna subjudul. Default: cyan kristal (#4FF5D8)")]
        public Color subtitleColor = new Color(0.310f, 0.961f, 0.847f); // #4FF5D8

        [Tooltip("Tampilkan panel instruksi kontrol di pojok kanan bawah")]
        public bool showControlsHint = true;

        // ── HUD Koin (dari GameModeManager) ─────────────────────────────────
        [Header("HUD")]
        [Tooltip("Sembunyikan HUD Lumora bawaan (GameModeManager) selama judul tampil")]
        public bool hideDefaultHUDDuringTitle = false;

        // ── Private ──────────────────────────────────────────────────────────
        private float _alpha = 0f;
        private bool _isVisible = false;
        private bool _isFading = false;
        private Coroutine _displayCoroutine;

        // Cache style agar tidak re-alloc tiap frame
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _boxStyle;
        private bool _stylesInitialized = false;

        // Ikon level berdasarkan nomor
        private static readonly string[] LevelIcons = { "🌅", "🕳️", "⚔️" };

        private void Start()
        {
            if (showOnStart)
            {
                ShowTitle();
            }
        }

        /// <summary>Tampilkan judul level (bisa dipanggil dari script lain).</summary>
        public void ShowTitle()
        {
            if (_displayCoroutine != null)
            {
                StopCoroutine(_displayCoroutine);
            }
            _displayCoroutine = StartCoroutine(DisplayRoutine());
        }

        /// <summary>Sembunyikan judul segera.</summary>
        public void HideTitle()
        {
            if (_displayCoroutine != null)
            {
                StopCoroutine(_displayCoroutine);
            }
            _isVisible = false;
            _alpha = 0f;
        }

        private IEnumerator DisplayRoutine()
        {
            // Fade in
            _isVisible = true;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.6f; // fade in 0.6 detik
                _alpha = Mathf.Clamp01(t);
                yield return null;
            }
            _alpha = 1f;

            // Tampil penuh
            yield return new WaitForSeconds(displayDuration);

            // Fade out
            _isFading = true;
            t = 1f;
            while (t > 0f)
            {
                t -= Time.deltaTime / fadeDuration;
                _alpha = Mathf.Clamp01(t);
                yield return null;
            }

            _alpha = 0f;
            _isVisible = false;
            _isFading = false;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize   = 42,
                fontStyle  = FontStyle.Bold,
                alignment  = TextAnchor.MiddleCenter,
                wordWrap   = false
            };
            _titleStyle.normal.textColor = titleColor;

            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 20,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                wordWrap  = false
            };
            _subtitleStyle.normal.textColor = subtitleColor;

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 13,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.UpperRight,
                wordWrap  = true
            };
            _hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.75f);

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize  = 13,
                alignment = TextAnchor.UpperLeft
            };
            _boxStyle.normal.textColor = Color.white;

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!_isVisible || _alpha <= 0f) return;

            InitStyles();

            float sw = Screen.width;
            float sh = Screen.height;

            // ── Panel background semi-transparan di tengah ────────────────────
            float panelW = 600f;
            float panelH = 120f;
            float panelX = (sw - panelW) * 0.5f;
            float panelY = sh * 0.12f;

            // Background gelap semi-transparan
            Color prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f * _alpha);
            GUI.DrawTexture(new Rect(panelX - 20f, panelY - 14f, panelW + 40f, panelH + 28f),
                Texture2D.whiteTexture);
            GUI.color = prevColor;

            // Garis hias atas dan bawah (warna cyan)
            GUI.color = new Color(subtitleColor.r, subtitleColor.g, subtitleColor.b, 0.7f * _alpha);
            GUI.DrawTexture(new Rect(panelX - 20f, panelY - 14f, panelW + 40f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(panelX - 20f, panelY + panelH + 14f, panelW + 40f, 2f), Texture2D.whiteTexture);
            GUI.color = prevColor;

            // ── Teks judul besar ──────────────────────────────────────────────
            string icon = (levelNumber >= 1 && levelNumber <= 3) ? LevelIcons[levelNumber - 1] : "✦";
            _titleStyle.normal.textColor = new Color(titleColor.r, titleColor.g, titleColor.b, _alpha);
            GUI.Label(new Rect(panelX, panelY + 6f, panelW, 54f),
                $"{icon}  {levelName}  {icon}", _titleStyle);

            // ── Subjudul ──────────────────────────────────────────────────────
            _subtitleStyle.normal.textColor = new Color(subtitleColor.r, subtitleColor.g, subtitleColor.b, _alpha * 0.9f);
            GUI.Label(new Rect(panelX, panelY + 62f, panelW, 34f),
                levelSubtitle, _subtitleStyle);

            // ── Hint kontrol di pojok kanan bawah ────────────────────────────
            if (showControlsHint)
            {
                _hintStyle.normal.textColor = new Color(1f, 1f, 1f, Mathf.Min(_alpha, 0.8f));
                string hint =
                    "▶  [SPACE] Pause Kamera\n" +
                    "🎮  [C / Tab] Ganti Mode Player\n" +
                    "🏃  WASD + SPACE (Player Mode)";
                GUI.Label(new Rect(sw - 250f, sh - 90f, 235f, 85f), hint, _hintStyle);
            }

            // ── Watermark nama game di pojok kiri bawah ──────────────────────
            GUIStyle watermark = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.LowerLeft
            };
            watermark.normal.textColor = new Color(1f, 1f, 1f, 0.35f * _alpha);
            GUI.Label(new Rect(12f, sh - 30f, 280f, 24f),
                "Lumora: Hutan yang Terlupakan  •  3D Level Design", watermark);
        }

#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(LevelTitleUI))]
        public class LevelTitleUIEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                GUILayout.Space(8);
                if (GUILayout.Button("▶  Preview Judul (Play Mode Only)"))
                {
                    if (Application.isPlaying)
                    {
                        ((LevelTitleUI)target).ShowTitle();
                    }
                    else
                    {
                        UnityEditor.EditorUtility.DisplayDialog("Lumora",
                            "Preview hanya bisa saat Play Mode aktif.\nTekan ▶ Play dulu, lalu klik tombol ini.", "OK");
                    }
                }
            }
        }
#endif
    }
}
