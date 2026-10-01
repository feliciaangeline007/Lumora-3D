using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CoinConvoy
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyChaser : MonoBehaviour
    {
        public static readonly List<EnemyChaser> ActiveChasers = new List<EnemyChaser>();

        [Header("Target (opsional, kosongkan untuk cari tag Player)")]
        [SerializeField] private Transform target;

        [Header("Kecepatan")]
        [SerializeField] private float baseSpeed = 3.5f;
        [SerializeField] private float maxSpeed = 7f;
        [SerializeField] private float speedPerCoin = 0.25f;
        [SerializeField] private float destinationRefreshInterval = 0.2f;
        [SerializeField] private float navMeshSnapDistance = 2f;

        private NavMeshAgent agent;
        private Renderer sirenBarRenderer;
        private MaterialPropertyBlock sirenPropertyBlock;
        private float destinationRefreshTimer;
        private Vector3 lastDestination;
        private bool convoyEventAlert;
        private bool hasDestination;
        private bool lastSirenToggle;

        private static Material sirenMaterial;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            Rigidbody enemyBody = GetComponent<Rigidbody>();
            if (enemyBody != null) enemyBody.isKinematic = true;
            CreateSirenLightbar();
        }

        private void OnEnable()
        {
            if (!ActiveChasers.Contains(this))
                ActiveChasers.Add(this);
        }

        private void OnDisable()
        {
            ActiveChasers.Remove(this);
        }

        private void Start()
        {
            if (!agent.isOnNavMesh &&
                NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSnapDistance, agent.areaMask))
            {
                agent.baseOffset = Mathf.Max(agent.baseOffset, transform.position.y - hit.position.y);
                if (!agent.Warp(hit.position))
                    Debug.LogWarning("[EnemyChaser] Could not place enemy on the NavMesh.", this);
            }
            if (!agent.isOnNavMesh)
                Debug.LogWarning("[EnemyChaser] Enemy is not on a baked NavMesh.", this);

            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }
        }

        private void Update()
        {
            AnimateSirens();

            if (target == null || !agent.isOnNavMesh) return;
            destinationRefreshTimer -= Time.deltaTime;
            if (destinationRefreshTimer <= 0f &&
                (!hasDestination || (target.position - lastDestination).sqrMagnitude > 0.25f))
            {
                destinationRefreshTimer = Mathf.Max(0.05f, destinationRefreshInterval);
                lastDestination = target.position;
                hasDestination = true;
                agent.SetDestination(lastDestination);
            }

            if (GameManager.Instance != null)
            {
                float bonus = GameManager.Instance.CollectedCoins * speedPerCoin;
                agent.speed = Mathf.Min(baseSpeed + bonus, maxSpeed);
            }
        }

        public void SetConvoyEventAlert(bool active)
        {
            convoyEventAlert = active;
        }

        private void CreateSirenLightbar()
        {
            GameObject sirenBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sirenBar.name = "SirenLightbar";
            sirenBar.transform.SetParent(transform, false);
            sirenBar.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            sirenBar.transform.localScale = new Vector3(0.7f, 0.15f, 0.25f);

            Collider col = sirenBar.GetComponent<Collider>();
            if (col != null) Destroy(col);

            sirenBarRenderer = sirenBar.GetComponent<Renderer>();
            if (sirenBarRenderer != null)
            {
                if (sirenMaterial == null)
                    sirenMaterial = CreateEmissiveMaterial(Color.white);
                sirenBarRenderer.sharedMaterial = sirenMaterial;
                sirenPropertyBlock = new MaterialPropertyBlock();
            }

        }

        private void AnimateSirens()
        {
            bool toggle = (Mathf.FloorToInt(Time.time * (convoyEventAlert ? 12f : 7f)) % 2) == 0;
            if (sirenBarRenderer == null || toggle == lastSirenToggle) return;
            lastSirenToggle = toggle;
            Color color = toggle ? new Color(1f, 0.12f, 0.08f) : new Color(0.05f, 0.55f, 1f);
            sirenPropertyBlock.SetColor("_BaseColor", color);
            sirenPropertyBlock.SetColor("_Color", color);
            sirenPropertyBlock.SetColor("_EmissionColor", color * 2f);
            sirenBarRenderer.SetPropertyBlock(sirenPropertyBlock);
        }

        private static Material CreateEmissiveMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader);
            material.color = color;
            material.enableInstancing = true;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.4f);
            }
            return material;
        }
    }
}
