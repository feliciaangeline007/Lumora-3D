using UnityEngine;

namespace CoinConvoy
{
    // Ditempel ke: gerbang keluar (Cube/Portal). Collider Is Trigger = ON, tag "Untagged"
    public class EscapeGate : MonoBehaviour
    {
        [Header("Tampilan")]
        [SerializeField] private Color closedColor = new Color(0.6f, 0.1f, 0.1f, 0.7f);
        [SerializeField] private Color openColor = new Color(0.1f, 0.9f, 0.4f, 0.7f);
        [SerializeField] private Renderer gateRenderer; // kolom gerbang
        [SerializeField] private float colorTransitionSpeed = 2.5f;

        private bool isOpen;
        private MaterialPropertyBlock propertyBlock;
        private Color displayedColor;
        private Color targetColor;

        private void Reset() => gateRenderer = GetComponent<Renderer>();

        private void Awake()
        {
            if (gateRenderer == null) gateRenderer = GetComponent<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
            if (gateRenderer != null && gateRenderer.sharedMaterial != null &&
                gateRenderer.sharedMaterial.HasProperty("_EmissionColor"))
                gateRenderer.sharedMaterial.EnableKeyword("_EMISSION");
            displayedColor = closedColor;
            targetColor = closedColor;
            ApplyColor(displayedColor);
        }

        private void Update()
        {
            if (displayedColor == targetColor) return;
            displayedColor = Color.MoveTowards(displayedColor, targetColor,
                colorTransitionSpeed * Time.unscaledDeltaTime);
            ApplyColor(displayedColor);
        }

        public void Open()
        {
            isOpen = true;
            targetColor = openColor;
        }

        public void Close()
        {
            isOpen = false;
            displayedColor = closedColor;
            targetColor = closedColor;
            ApplyColor(displayedColor);
        }

        private void ApplyColor(Color color)
        {
            if (gateRenderer != null)
            {
                gateRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", color);
                propertyBlock.SetColor("_Color", color);
                propertyBlock.SetColor("_EmissionColor", color * 0.55f);
                gateRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isOpen || !other.CompareTag("Player")) return;
            if (GameManager.Instance != null) GameManager.Instance.ReachEscape();
        }
    }
}
