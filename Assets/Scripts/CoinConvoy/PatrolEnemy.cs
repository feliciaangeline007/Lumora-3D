using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CoinConvoy
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PatrolEnemy : MonoBehaviour
    {
        public static readonly List<PatrolEnemy> ActivePatrols = new List<PatrolEnemy>();

        [Header("Waypoint (seret 2-4 empty GameObject)")]
        [SerializeField] private Transform[] waypoints;

        [Header("Patroli")]
        [SerializeField] private float patrolSpeed = 2.5f;

        [Header("Deteksi Pemain")]
        [SerializeField] private float detectRange = 10f;   // Jarak mulai mengejar
        [SerializeField] private float loseRange = 16f;     // Jarak berhenti mengejar
        [SerializeField] private float chaseSpeed = 5.5f;
        [SerializeField] private float telegraphRangeMultiplier = 1.35f;
        [SerializeField] private float destinationRefreshInterval = 0.2f;
        [SerializeField] private float navMeshSnapDistance = 2f;

        private NavMeshAgent agent;
        private Transform target;
        private int waypointIndex;
        private bool chasing;
        private bool telegraphing;
        private bool convoyEventAlert;
        private float destinationRefreshTimer;
        private Vector3 lastDestination;
        private bool hasDestination;

        private Renderer alertBeacon;
        private MaterialPropertyBlock alertPropertyBlock;
        private GameObject coneVisual;
        private static Material alertMaterial;

        public bool IsChasing => chasing;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            Rigidbody enemyBody = GetComponent<Rigidbody>();
            if (enemyBody != null) enemyBody.isKinematic = true;
            CreateSearchlight();
        }

        private void OnEnable()
        {
            if (!ActivePatrols.Contains(this))
                ActivePatrols.Add(this);
        }

        private void OnDisable()
        {
            ActivePatrols.Remove(this);
        }

        private void Start()
        {
            if (!agent.isOnNavMesh &&
                NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSnapDistance, agent.areaMask))
            {
                agent.baseOffset = Mathf.Max(agent.baseOffset, transform.position.y - hit.position.y);
                if (!agent.Warp(hit.position))
                    Debug.LogWarning("[PatrolEnemy] Could not place enemy on the NavMesh.", this);
            }
            if (!agent.isOnNavMesh)
                Debug.LogWarning("[PatrolEnemy] Enemy is not on a baked NavMesh.", this);

            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }
            if (waypoints != null && waypoints.Length > 0 && agent.isOnNavMesh)
                agent.SetDestination(waypoints[0].position);
        }

        private void Update()
        {
            if (convoyEventAlert && !chasing)
                SetBeaconColor(new Color(1f, 0.65f + Mathf.Sin(Time.time * 9f) * 0.12f, 0.12f));

            if (!agent.isOnNavMesh || target == null) return;
            float distance = Vector3.Distance(transform.position, target.position);

            if (!chasing)
            {
                bool shouldTelegraph = distance <= detectRange * telegraphRangeMultiplier;
                if (telegraphing != shouldTelegraph)
                {
                    telegraphing = shouldTelegraph;
                    SetSearchlightMode(telegraphing, false);
                }
            }

            // Masuk mode kejar
            if (!chasing && distance <= detectRange)
            {
                chasing = true;
                agent.speed = chaseSpeed;
                SetSearchlightMode(false, true);
            }
            // Keluar mode kejar kalau pemain menjauh
            else if (chasing && distance > loseRange)
            {
                chasing = false;
                agent.speed = patrolSpeed;
                telegraphing = false;
                SetSearchlightMode(false, false);
                if (waypoints != null && waypoints.Length > 0)
                    agent.SetDestination(waypoints[waypointIndex].position);
            }

            if (chasing)
            {
                destinationRefreshTimer -= Time.deltaTime;
                if (destinationRefreshTimer <= 0f &&
                    (!hasDestination || (target.position - lastDestination).sqrMagnitude > 0.25f))
                {
                    destinationRefreshTimer = Mathf.Max(0.05f, destinationRefreshInterval);
                    lastDestination = target.position;
                    hasDestination = true;
                    agent.SetDestination(lastDestination);
                }
                return;
            }

            // Patroli: lanjut ke waypoint berikutnya bila sudah sampai
            if (waypoints == null || waypoints.Length == 0) return;
            if (!agent.pathPending && agent.remainingDistance < 0.6f)
            {
                waypointIndex = (waypointIndex + 1) % waypoints.Length;
                agent.SetDestination(waypoints[waypointIndex].position);
            }
        }

        private void CreateSearchlight()
        {
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "PatrolAlertBeacon";
            beacon.transform.SetParent(transform, false);
            beacon.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            beacon.transform.localScale = new Vector3(0.28f, 0.18f, 0.28f);

            Collider collider = beacon.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            alertBeacon = beacon.GetComponent<Renderer>();
            if (alertBeacon != null)
            {
                if (alertMaterial == null)
                    alertMaterial = CreateAlertMaterial();
                alertBeacon.sharedMaterial = alertMaterial;
                alertPropertyBlock = new MaterialPropertyBlock();
            }
            SetSearchlightMode(false, false);
        }

        private void SetSearchlightMode(bool telegraph, bool chasingPlayer)
        {
            if (chasingPlayer)
            {
                SetBeaconColor(new Color(1f, 0.12f, 0.08f));
            }
            else if (telegraph || convoyEventAlert)
            {
                SetBeaconColor(new Color(1f, 0.58f, 0.08f));
            }
            else
            {
                SetBeaconColor(new Color(1f, 0.82f, 0.32f));
            }
        }

        private void SetBeaconColor(Color color)
        {
            if (alertBeacon == null) return;
            alertPropertyBlock.SetColor("_BaseColor", color);
            alertPropertyBlock.SetColor("_Color", color);
            alertPropertyBlock.SetColor("_EmissionColor", color * 1.8f);
            alertBeacon.SetPropertyBlock(alertPropertyBlock);
        }

        private static Material CreateAlertMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader);
            material.EnableKeyword("_EMISSION");
            material.enableInstancing = true;
            return material;
        }

        public void SetConvoyEventAlert(bool active)
        {
            convoyEventAlert = active;
            SetSearchlightMode(telegraphing, chasing);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, loseRange);
        }
    }
}
