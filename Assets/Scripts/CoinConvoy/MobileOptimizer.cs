using UnityEngine;

namespace CoinConvoy
{
    // Ditempel ke: GameObject "MobileOptimizer" di scene Game (dan MainMenu)
    // Mengatur performa dasar agar HP tidak cepat panas dan baterai hemat
    public class MobileOptimizer : MonoBehaviour
    {
        [Header("Target FPS")]
        [SerializeField] private int targetFrameRate = 30; // Stabil 30 FPS

        [Header("Kecerahan layar otomatis")]
        [SerializeField] private bool lockBrightness = false; // true = hemat baterai
        [SerializeField] private bool disableLocalRealtimeLights = true;

        private void Awake()
        {
            // Kunci framerate: lebih penting daripada FPS tinggi yang tidak stabil
            Application.targetFrameRate = targetFrameRate;
            // VSync mati agar target FPS dihormati di Android
            QualitySettings.vSyncCount = 0;
            // Hemat daya: jangan render saat aplikasi tidak terlihat
            Application.runInBackground = false;

            QualitySettings.shadowDistance = 40f;
            OptimizeSceneLights();
        }

        private void OptimizeSceneLights()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Light mainDirectional = null;
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light.type == LightType.Directional && light.enabled && mainDirectional == null)
                    mainDirectional = light;
            }
            if (mainDirectional == null)
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type != LightType.Directional) continue;
                    mainDirectional = lights[i];
                    mainDirectional.enabled = true;
                    break;
                }
            }

            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light.type == LightType.Directional)
                {
                    if (light != mainDirectional)
                    {
                        light.enabled = false;
                        light.shadows = LightShadows.None;
                    }
                }
                else if (disableLocalRealtimeLights)
                {
                    light.enabled = false;
                    light.shadows = LightShadows.None;
                }
            }
        }

        private void OnApplicationPause(bool paused)
        {
            // Saat HP masuk background: hentikan musik agar tidak mengganggu
            if (paused) AudioListener.pause = true;
            else AudioListener.pause = false;
        }
    }
}
