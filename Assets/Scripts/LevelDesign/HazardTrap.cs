using UnityEngine;

namespace EnchantedForest
{
    /// <summary>
    /// Animasi rintangan/obstacle (pendulum berayun, duri berputar, atau log berduri).
    /// </summary>
    [SelectionBase]
    public class HazardTrap : MonoBehaviour
    {
        public enum TrapType
        {
            ContinuousRotation,
            PendulumSwing,
            PulsingSpike,
            SporePuff,
            RollingHazard
        }

        [Header("Tipe Perangkap")]
        public TrapType trapType = TrapType.ContinuousRotation;

        [Header("Pengaturan Rotasi Berkelanjutan")]
        public Vector3 rotationAxis = Vector3.up;
        public float rotationSpeed = 90f;

        [Header("Pengaturan Pendulum Berayun")]
        public Vector3 swingAxis = Vector3.forward;
        public float swingAngle = 45f;
        public float swingSpeed = 2f;

        [Header("Pengaturan Spike Naik Turun")]
        public float spikeOffset = 1.5f;
        public float spikeSpeed = 3f;

        [Header("Pengaturan Jamur Spora Berdenyut")]
        public float sporeScaleMultiplier = 1.6f;
        public float sporePulseSpeed = 2.5f;

        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private Vector3 _startScale;
        private float _timeOffset;

        private void Start()
        {
            _startPosition = transform.position;
            _startRotation = transform.rotation;
            _startScale = transform.localScale;
            _timeOffset = (transform.position.x + transform.position.z) * 0.7f;
        }

        private void Update()
        {
            switch (trapType)
            {
                case TrapType.ContinuousRotation:
                    transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
                    break;

                case TrapType.PendulumSwing:
                    float angle = Mathf.Sin((Time.time + _timeOffset) * swingSpeed) * swingAngle;
                    transform.rotation = _startRotation * Quaternion.AngleAxis(angle, swingAxis);
                    break;

                case TrapType.PulsingSpike:
                    float pingPong = Mathf.PingPong((Time.time + _timeOffset) * spikeSpeed, 1f);
                    transform.position = _startPosition + transform.up * (pingPong * spikeOffset);
                    break;

                case TrapType.SporePuff:
                    float pulse = 1f + Mathf.PingPong((Time.time + _timeOffset) * sporePulseSpeed, sporeScaleMultiplier - 1f);
                    transform.localScale = new Vector3(_startScale.x * pulse, _startScale.y * (2f - pulse), _startScale.z * pulse);
                    break;

                case TrapType.RollingHazard:
                    transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime, Space.World);
                    break;
            }
        }
    }
}
