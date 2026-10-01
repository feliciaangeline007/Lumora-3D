using UnityEngine;

namespace CoinConvoy
{
    // Ditempel ke: obstacle bergerak (palang, batu bergulir)
    // Komponen: Rigidbody (Is Kinematic ON) + Collider biasa, tag "Enemy" bila ingin menyakiti pemain
    public class MovingObstacle : MonoBehaviour
    {
        [Header("Titik A dan B (seret 2 empty GameObject)")]
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;

        [Header("Gerakan")]
        [SerializeField] private float speed = 3f;
        [SerializeField] private float pauseAtEnd = 0.5f;

        private Vector3 target;
        private float pauseTimer;

        private void Start()
        {
            if (pointA == null || pointB == null) return;
            target = pointB.position;
            transform.position = pointA.position;
        }

        private void Update()
        {
            if (pointA == null || pointB == null) return;
            if (pauseTimer > 0f)
            {
                pauseTimer -= Time.deltaTime;
                return;
            }
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, target) < 0.05f)
            {
                target = target == pointA.position ? pointB.position : pointA.position;
                pauseTimer = pauseAtEnd;
            }
        }
    }
}
