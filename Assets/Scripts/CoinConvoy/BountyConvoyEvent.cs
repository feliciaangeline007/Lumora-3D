using System.Collections;
using UnityEngine;

namespace CoinConvoy
{
    public sealed class BountyConvoyEvent : MonoBehaviour
    {
        [SerializeField] private float alertDuration = 8f;

        private bool triggered;
        private Coroutine alertRoutine;

        private void Update()
        {
            GameManager manager = GameManager.Instance;
            if (triggered || manager == null || manager.IsEnded || !manager.GateOpen) return;

            triggered = true;
            alertRoutine = StartCoroutine(PlayAlert());
        }

        private IEnumerator PlayAlert()
        {
            HudManager.Instance?.ShowPowerUpNotification(
                "BOUNTY CONVOY INBOUND!", UITheme.ColorDangerRed);
            SetEnemyAlert(true);
            yield return new WaitForSeconds(Mathf.Max(0.1f, alertDuration));
            SetEnemyAlert(false);
            alertRoutine = null;
        }

        private void SetEnemyAlert(bool active)
        {
            for (int i = 0; i < EnemyChaser.ActiveChasers.Count; i++)
            {
                EnemyChaser chaser = EnemyChaser.ActiveChasers[i];
                if (chaser != null) chaser.SetConvoyEventAlert(active);
            }
            for (int i = 0; i < PatrolEnemy.ActivePatrols.Count; i++)
            {
                PatrolEnemy patrol = PatrolEnemy.ActivePatrols[i];
                if (patrol != null) patrol.SetConvoyEventAlert(active);
            }
        }

        private void OnDisable()
        {
            if (alertRoutine != null)
            {
                StopCoroutine(alertRoutine);
                alertRoutine = null;
            }
            SetEnemyAlert(false);
        }
    }
}
