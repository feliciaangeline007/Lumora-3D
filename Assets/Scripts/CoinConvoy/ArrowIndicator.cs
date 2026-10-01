using UnityEngine;

namespace CoinConvoy
{
    // Ditempel ke: anak Main Camera (Empty "Arrow")
    // Menunjuk ke arah gerbang escape setelah misi coin selesai.
    public class ArrowIndicator : MonoBehaviour
    {
        [SerializeField] private Transform target;        // seret EscapeGate
        [SerializeField] private float distance = 4f;     // jarak panah dari kamera
        [SerializeField] private float rotateSpeed = 8f;

        private Renderer arrowRenderer;

        private void Start() => arrowRenderer = GetComponentInChildren<Renderer>();

        private void Update()
        {
            bool show = GameManager.Instance != null && GameManager.Instance.GateOpen;
            if (arrowRenderer != null) arrowRenderer.enabled = show;
            if (!show || target == null) return;

            // Posisikan panah di depan kamera, hadap ke gerbang
            Vector3 direction = (target.position - transform.position).normalized;
            Quaternion look = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, rotateSpeed * Time.deltaTime);
        }
    }
}
