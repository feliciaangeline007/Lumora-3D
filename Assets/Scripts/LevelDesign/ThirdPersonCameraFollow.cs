using UnityEngine;

namespace EnchantedForest
{
    /// <summary>
    /// Kamera orang ketiga (Third Person) halus yang mengikuti karakter pemain.
    /// </summary>
    public class ThirdPersonCameraFollow : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;

        [Header("Offset & Jarak")]
        public Vector3 offset = new Vector3(0, 3.2f, -6.5f);
        public float followSmoothness = 6.0f;
        public float rotationSmoothness = 8.0f;

        [Header("Rotasi Mouse (Opsional)")]
        public bool enableMouseLook = true;
        public float mouseSensitivity = 2.0f;
        public float touchLookSensitivity = 0.12f;
        public float minPitch = -15f;
        public float maxPitch = 60f;

        private float _yaw;
        private float _pitch = 15f;

        public void ApplyTouchLook(Vector2 screenDelta)
        {
            _yaw += screenDelta.x * touchLookSensitivity;
            _pitch = Mathf.Clamp(_pitch - screenDelta.y * touchLookSensitivity, minPitch, maxPitch);
        }

        private void Start()
        {
            if (target != null)
            {
                _yaw = target.eulerAngles.y;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (enableMouseLook)
            {
                float mouseX = 0f;
                float mouseY = 0f;

#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Mouse.current != null)
                {
                    Vector2 delta = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();
                    mouseX = delta.x * mouseSensitivity * 0.1f;
                    mouseY = delta.y * mouseSensitivity * 0.1f;
                }
#elif ENABLE_LEGACY_INPUT_MANAGER
                mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
#endif

                _yaw += mouseX;
                _pitch -= mouseY;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            }

            Quaternion targetRotation = Quaternion.Euler(_pitch, _yaw, 0);
            Vector3 desiredPosition = target.position + targetRotation * offset;

            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmoothness * Time.deltaTime);
            transform.LookAt(target.position + Vector3.up * 1.5f);
        }
    }
}
