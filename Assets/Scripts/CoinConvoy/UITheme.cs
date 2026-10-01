using UnityEngine;

namespace CoinConvoy
{
    /// <summary>
    /// Design system tokens and procedural UI sprite generators for Coin Convoy.
    /// Provides consistent colors, spacing, and crisp anti-aliased 9-slice sprites
    /// without relying on external image asset imports.
    /// </summary>
    public static class UITheme
    {
        // -------------------------------------------------------------
        // COLOR TOKENS (Design System)
        // -------------------------------------------------------------
        // Backgrounds & Surfaces
        public static readonly Color ColorBgOverlay      = new Color(0.02f, 0.04f, 0.08f, 0.88f); // Deep frosted backdrop
        public static readonly Color ColorCardSurface    = new Color(0.06f, 0.10f, 0.18f, 0.85f); // Glassmorphism card surface
        public static readonly Color ColorCardBorder     = new Color(0.18f, 0.28f, 0.44f, 0.70f); // Card stroke / highlight
        
        // Brand & Accents
        public static readonly Color ColorPrimaryGreen   = new Color(0.06f, 0.75f, 0.48f, 1.00f); // Emerald: Gas, Win, Active
        public static readonly Color ColorPrimaryGreenDark = new Color(0.03f, 0.35f, 0.22f, 0.90f);
        public static readonly Color ColorAccentGold      = new Color(0.98f, 0.72f, 0.12f, 1.00f); // Amber: Coins, High Score
        public static readonly Color ColorAccentGoldDark  = new Color(0.45f, 0.30f, 0.05f, 0.90f);
        public static readonly Color ColorDangerRed       = new Color(0.93f, 0.25f, 0.25f, 1.00f); // Crimson: Brake, Loss, Hearts
        public static readonly Color ColorDangerRedDark   = new Color(0.45f, 0.08f, 0.08f, 0.90f);
        public static readonly Color ColorTechCyan        = new Color(0.12f, 0.76f, 0.92f, 1.00f); // Cyan: Timer, Gate, Steer
        public static readonly Color ColorSlateButton     = new Color(0.12f, 0.17f, 0.27f, 0.92f); // Secondary action
        public static readonly Color ColorSlateBorder     = new Color(0.25f, 0.34f, 0.50f, 0.85f);
        
        // Typography
        public static readonly Color ColorTextPrimary     = new Color(0.97f, 0.98f, 1.00f, 1.00f); // High-contrast white
        public static readonly Color ColorTextSecondary   = new Color(0.62f, 0.70f, 0.82f, 1.00f); // Muted ice-blue
        public static readonly Color ColorTextAccent      = new Color(0.99f, 0.84f, 0.28f, 1.00f); // Gold highlight
        
        // -------------------------------------------------------------
        // PROCEDURAL SPRITE GENERATION (Anti-Aliased 9-Slice)
        // -------------------------------------------------------------
        private static Sprite cachedCardSprite;
        private static Sprite cachedButtonSprite;
        private static Sprite cachedCircleSprite;
        private static Sprite cachedPillSprite;

        /// <summary>
        /// Sliced rounded rectangle suitable for panels and cards (radius ~18px, border stroke 2px).
        /// </summary>
        public static Sprite GetCardSprite()
        {
            if (cachedCardSprite != null) return cachedCardSprite;
            cachedCardSprite = CreateRoundedRectSprite(64, 64, 18, 2, Color.white, new Color(1f, 1f, 1f, 0.8f));
            return cachedCardSprite;
        }

        /// <summary>
        /// Sliced rounded rectangle for buttons with smooth rounded corners (radius ~14px).
        /// </summary>
        public static Sprite GetButtonSprite()
        {
            if (cachedButtonSprite != null) return cachedButtonSprite;
            cachedButtonSprite = CreateRoundedRectSprite(48, 48, 14, 2, Color.white, new Color(1f, 1f, 1f, 0.85f));
            return cachedButtonSprite;
        }

        /// <summary>
        /// Sliced pill shape for HUD chips and tags.
        /// </summary>
        public static Sprite GetPillSprite()
        {
            if (cachedPillSprite != null) return cachedPillSprite;
            cachedPillSprite = CreateRoundedRectSprite(48, 48, 23, 2, Color.white, new Color(1f, 1f, 1f, 0.9f));
            return cachedPillSprite;
        }

        /// <summary>
        /// Crisp circle sprite for icons, dots, and circular buttons.
        /// </summary>
        public static Sprite GetCircleSprite()
        {
            if (cachedCircleSprite != null) return cachedCircleSprite;
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

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
            cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return cachedCircleSprite;
        }

        private static Sprite CreateRoundedRectSprite(int width, int height, int radius, int border, Color fillColor, Color borderColor)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Compute distance to corner centers
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
                        // Check if within border or fill
                        float innerRadius = radius - border;
                        float innerAlpha = Mathf.Clamp01(innerRadius + 0.5f - dist);

                        Color pixelColor = Color.Lerp(borderColor, fillColor, innerAlpha);
                        pixelColor.a *= outerAlpha;
                        tex.SetPixel(x, y, pixelColor);
                    }
                }
            }
            tex.Apply();

            // 9-slice borders: Vector4(left, bottom, right, top)
            Vector4 borderVector = new Vector4(radius + 2, radius + 2, radius + 2, radius + 2);
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, borderVector);
        }
    }
}
