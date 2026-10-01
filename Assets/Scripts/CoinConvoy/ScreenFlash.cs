using UnityEngine;
using UnityEngine.UI;

namespace CoinConvoy
{
    // Ditempel ke: Image merah transparan yang menutupi seluruh layar (Canvas)
    // Menyala sesaat saat pemain terkena damage
    public class ScreenFlash : MonoBehaviour
    {
        [SerializeField] private float flashDuration = 0.35f;
        [SerializeField] private float maxAlpha = 0.45f;
        private Image flashImage;
        private float timer;

        private void Awake()
        {
            flashImage = GetComponent<Image>();
            if (flashImage != null)
            {
                Color color = flashImage.color;
                color.a = 0f;
                flashImage.color = color;
            }
        }

        private void Update()
        {
            if (timer <= 0f || flashImage == null) return;
            timer -= Time.unscaledDeltaTime; // Tetap jalan walau game pause
            Color color = flashImage.color;
            color.a = Mathf.Lerp(0f, maxAlpha, timer / flashDuration);
            flashImage.color = color;
        }

        // Dipanggil saat damage terjadi
        public void Flash() => timer = flashDuration;
    }
}
