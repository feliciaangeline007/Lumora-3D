using System.Collections;
using UnityEngine;

namespace CoinConvoy
{
    // TWIST B - ESCAPE BERANI
    // Ditempel ke: GameObject "PathLights" berisi daftar lentera di sepanjang jalur
    // Menyala berurutan setelah semua coin terkumpul dan gerbang terbuka
    public class PathLights : MonoBehaviour
    {
        [Header("Lentera sepanjang jalur (seret berurutan dari spawn ke gerbang)")]
        [SerializeField] private GameObject[] lights;
        [SerializeField] private float interval = 0.25f;     // Jeda antar lentera

        private bool started;

        private void Awake()
        {
            // Semua lentera mati di awal
            if (lights == null) return;
            foreach (GameObject item in lights)
            {
                if (item == null) continue;
                Light[] localLights = item.GetComponentsInChildren<Light>(true);
                foreach (Light localLight in localLights)
                {
                    localLight.enabled = false;
                    localLight.shadows = LightShadows.None;
                }

                Renderer renderer = item.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null &&
                    renderer.sharedMaterial.HasProperty("_EmissionColor"))
                {
                    Material material = renderer.sharedMaterial;
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", material.color * 1.4f);
                }
                item.SetActive(false);
            }
        }

        private void Update()
        {
            if (started) return;
            GameManager gm = GameManager.Instance;
            if (gm == null || !gm.GateOpen) return;
            started = true;
            StartCoroutine(LightUpRoutine());
        }

        private IEnumerator LightUpRoutine()
        {
            if (lights == null) yield break;
            foreach (GameObject item in lights)
            {
                if (item != null) item.SetActive(true);
                yield return new WaitForSecondsRealtime(interval);
            }
        }
    }
}
