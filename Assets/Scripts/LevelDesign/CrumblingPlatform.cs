using UnityEngine;

namespace EnchantedForest
{
    /// <summary>
    /// Platform rapuh (Crumbling Leaf / Brittle Bark) yang bergetar (shake) saat diinjak atau terus-menerus.
    /// </summary>
    [SelectionBase]
    public class CrumblingPlatform : MonoBehaviour
    {
        [Header("Animasi Goyang Rapuh")]
        public float shakeIntensity = 0.04f;
        public float shakeSpeed = 25f;

        private Vector3 _initialPosition;

        private void Start()
        {
            _initialPosition = transform.position;
        }

        private void Update()
        {
            // Getaran kecil memberikan petunjuk visual (visual affordance) bahwa platform ini rapuh
            float offsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
            float offsetZ = Mathf.Cos(Time.time * shakeSpeed * 1.2f) * shakeIntensity;
            transform.position = _initialPosition + new Vector3(offsetX, 0, offsetZ);
        }
    }
}
