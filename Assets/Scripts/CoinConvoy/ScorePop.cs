using TMPro;
using UnityEngine;

namespace CoinConvoy
{
    // Ditempel ke: TMP_Text ScoreText. Membuat angka skor membesar saat bertambah
    public class ScorePop : MonoBehaviour
    {
        [SerializeField] private float popScale = 1.35f;
        [SerializeField] private float popSpeed = 10f;
        private TMP_Text label;
        private RectTransform rect;
        private int lastShownScore = -1;
        private float scale = 1f;

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
            rect = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (label == null || GameManager.Instance == null) return;
            int current = GameManager.Instance.Score;
            if (current != lastShownScore)
            {
                lastShownScore = current;
                scale = popScale;   // Skor berubah → langsung membesar
            }
            // Kembali ke ukuran normal secara halus
            scale = Mathf.Lerp(scale, 1f, popSpeed * Time.deltaTime);
            rect.localScale = Vector3.one * scale;
        }
    }
}
