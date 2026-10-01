using UnityEngine;

namespace CoinConvoy
{
    public class FollowCamera : MonoBehaviour
    {
        public static FollowCamera Instance { get; private set; }

        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 7f, -10f);
        [SerializeField] private float smooth = 8f;
        [SerializeField] private float lookAheadDistance = 2.5f;
        [SerializeField] private float nitroFovKick = 5f;
        [SerializeField] private float fovSmooth = 6f;
        [SerializeField] private float shakeReturnSpeed = 18f;

        private Camera attachedCamera;
        private CarController car;
        private float baseFov;
        private float shakeDuration;
        private float shakeRemaining;
        private float shakeMagnitude;
        private Vector3 shakeOffset;

        private void Awake()
        {
            Instance = this;
            attachedCamera = GetComponent<Camera>();
            if (attachedCamera != null) baseFov = attachedCamera.fieldOfView;
            if (target != null) car = target.GetComponent<CarController>();
        }

        private void LateUpdate()
        {
            if (target == null) return;
            if (car == null) car = target.GetComponent<CarController>();

            float speedRatio = car != null ? car.SpeedRatio : 0f;
            Vector3 lookAhead = target.forward * (lookAheadDistance * speedRatio);
            Vector3 desiredPosition = target.TransformPoint(offset) + lookAhead;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smooth * Time.deltaTime);

            if (shakeRemaining > 0f)
            {
                shakeRemaining -= Time.unscaledDeltaTime;
                float strength = shakeDuration > 0f ? shakeMagnitude * (shakeRemaining / shakeDuration) : 0f;
                float time = Time.unscaledTime * 38f;
                shakeOffset = new Vector3(Mathf.Sin(time) * strength, Mathf.Sin(time * 1.37f) * strength * 0.55f, 0f);
            }
            else
            {
                shakeOffset = Vector3.Lerp(shakeOffset, Vector3.zero, shakeReturnSpeed * Time.unscaledDeltaTime);
            }

            transform.position += shakeOffset;
            transform.LookAt(target.position + lookAhead + Vector3.up * 1.2f);

            if (attachedCamera != null)
            {
                float targetFov = baseFov + (car != null && car.IsNitroActive ? nitroFovKick : 0f);
                attachedCamera.fieldOfView = Mathf.Lerp(attachedCamera.fieldOfView, targetFov, fovSmooth * Time.deltaTime);
            }
        }

        public void Shake(float magnitude = 0.28f, float duration = 0.22f)
        {
            shakeMagnitude = Mathf.Max(shakeMagnitude, magnitude);
            shakeDuration = Mathf.Max(shakeDuration, duration);
            shakeRemaining = shakeDuration;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
