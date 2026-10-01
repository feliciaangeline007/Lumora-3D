using UnityEngine;

namespace EnchantedForest
{
    public sealed class FinishPortalTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerController3D>() == null) return;

            if (GameModeManager.Instance == null)
            {
                Debug.LogError("[Lumora] Finish portal entered, but GameModeManager is missing.", this);
                return;
            }

            GameModeManager.Instance.TryFinishLevel();
        }
    }
}
