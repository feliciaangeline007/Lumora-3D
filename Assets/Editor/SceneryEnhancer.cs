using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace CoinConvoy.Editor
{
    /// <summary>
    /// Tool satu klik untuk memperbaiki scenery latar Coin Convoy:
    /// skybox senja, ambient hangat, fog, cahaya sore, gunung jauh,
    /// dinding tak terlihat, ornamen batu, Wind Zone, dan audio ambience.
    /// Aman dijalankan berulang (idempoten) karena objek lama dibuang lalu dibuat ulang.
    /// </summary>
    public static class SceneryEnhancer
    {
        private const string MenuRoot = "Tools/Coin Convoy/";
        private const string MaterialFolder = "Assets/Materials/CoinConvoy";
        private const string SkyMaterialPath = MaterialFolder + "/CoinConvoy_Sky.mat";
        private const string MountainMaterialPath = MaterialFolder + "/CoinConvoy_Mountain.mat";
        private const string RockMaterialPath = MaterialFolder + "/CoinConvoy_Rock.mat";

        [MenuItem(MenuRoot + "Perbaiki Scenery Latar (Scene Aktif)")]
        public static void EnhanceActiveSceneMenu()
        {
            Enhance(SceneManager.GetActiveScene());
            Debug.Log("[Scenery] Scenery latar selesai diperbaiki pada scene aktif.");
        }

        [MenuItem(MenuRoot + "Perbaiki Scenery Semua Scene Build")]
        public static void EnhanceAllScenesMenu()
        {
            if (!EditorUtility.DisplayDialog("Coin Convoy",
                    "Terapkan scenery ke semua scene di Build Settings?", "Ya", "Batal")) return;
            EnhanceAllScenes();
        }

        // Dipanggil dari command line bila diperlukan build otomatis
        public static void EnhanceAllScenesBatch()
        {
            EnhanceAllScenes();
        }

        private static void EnhanceAllScenes()
        {
            Scene original = SceneManager.GetActiveScene();
            List<string> paths = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                if (scene.enabled) paths.Add(scene.path);
            if (paths.Count == 0)
            {
                Debug.LogWarning("[Scenery] Build Settings masih kosong, scene tidak diproses.");
                return;
            }

            foreach (string path in paths)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Enhance(scene);
                EditorSceneManager.SaveScene(scene);
            }
            EditorSceneManager.OpenScene(original.path, OpenSceneMode.Single);
            Debug.Log("[Scenery] Semua scene di Build Settings sudah diperbaiki.");
        }

        private static void Enhance(Scene scene)
        {
            if (!scene.IsValid()) return;

            Material sky = EnsureSkybox();
            SetupLighting(sky);
            SetupFog();
            SetupCamera();

            Bounds arena = ComputeArenaBounds();
            BuildMountains(arena);
            BuildInvisibleWalls(arena);
            BuildRocks(arena);
            BuildWindZone();
            BuildAmbience();
            RebakeNavMesh();

            EditorSceneManager.MarkSceneDirty(scene);
        }

        // --- Langit senja ---
        private static Material EnsureSkybox()
        {
            EnsureFolder(MaterialFolder);
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
            if (sky != null) return sky;

            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null) return RenderSettings.skybox;

            sky = new Material(shader);
            sky.SetFloat("_SunSize", 0.06f);
            sky.SetFloat("_SunSizeConvergence", 4f);
            sky.SetFloat("_AtmosphereThickness", 1.35f);
            sky.SetColor("_SkyTint", new Color(0.92f, 0.78f, 0.68f));
            sky.SetColor("_GroundColor", new Color(0.34f, 0.30f, 0.26f));
            sky.SetFloat("_Exposure", 1.1f);
            AssetDatabase.CreateAsset(sky, SkyMaterialPath);
            AssetDatabase.SaveAssets();
            return sky;
        }

        private static void SetupLighting(Material sky)
        {
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.55f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.66f, 0.52f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.26f, 0.23f, 0.20f);
            RenderSettings.ambientIntensity = 1f;

            Light sun = null;
            Light[] sceneLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Light light in sceneLights)
            {
                if (light.type == LightType.Directional && light.isActiveAndEnabled)
                {
                    sun = light;
                    break;
                }
            }
            if (sun == null)
            {
                foreach (Light light in sceneLights)
                {
                    if (light.type != LightType.Directional) continue;
                    sun = light;
                    sun.enabled = true;
                    break;
                }
            }
            if (sun == null)
            {
                GameObject sunObject = new GameObject("Directional Light");
                sun = sunObject.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            foreach (Light light in sceneLights)
            {
                if (light.type == LightType.Directional && light != sun)
                {
                    light.enabled = false;
                    light.shadows = LightShadows.None;
                }
            }
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            sun.color = new Color(1f, 0.87f, 0.74f); // oranye lembut, kesan sore
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            QualitySettings.shadowDistance = 40f;
            RenderSettings.sun = sun;
        }

        private static void SetupFog()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.74f, 0.70f, 0.64f);
        }

        private static void SetupCamera()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = RenderSettings.fogColor;
        }

        // --- Batas arena dari collider yang ada ---
        private static Bounds ComputeArenaBounds()
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
            bool found = false;
            foreach (Collider collider in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (collider.isTrigger) continue;
                if (collider.gameObject.name.StartsWith("Scenery_")) continue;
                if (collider is MeshCollider meshCollider && meshCollider.sharedMesh == null) continue;

                if (!found) { bounds = collider.bounds; found = true; }
                else bounds.Encapsulate(collider.bounds);
            }
            if (!found || bounds.size.sqrMagnitude < 4f)
                return new Bounds(Vector3.zero, new Vector3(80f, 2f, 80f));

            bounds.size = new Vector3(
                Mathf.Max(bounds.size.x, 20f),
                Mathf.Max(bounds.size.y, 1f),
                Mathf.Max(bounds.size.z, 20f));
            return bounds;
        }

        // --- Pegunungan jauh di tepi map ---
        private static void BuildMountains(Bounds arena)
        {
            Transform old = FindOrCreate("Scenery_Mountains", out bool created);
            if (!created)
            {
                for (int i = old.childCount - 1; i >= 0; i--) Object.DestroyImmediate(old.GetChild(i).gameObject);
            }

            Material material = EnsureMaterial(MountainMaterialPath, new Color(0.30f, 0.35f, 0.44f));
            Random rng = new Random(20261008); // seed tetap: hasil selalu sama
            float radius = Mathf.Max(arena.extents.x, arena.extents.z) * 1.7f + 70f;
            const int count = 14;
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                float distance = radius + NextRange(rng, -18f, 26f);
                float height = NextRange(rng, 24f, 48f);
                float width = NextRange(rng, 55f, 95f);

                GameObject mountain = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mountain.name = "Mountain_" + i.ToString("00");
                mountain.transform.SetParent(old, false);
                mountain.transform.position = new Vector3(
                    arena.center.x + Mathf.Cos(angle) * distance,
                    -height * 0.30f,
                    arena.center.z + Mathf.Sin(angle) * distance);
                mountain.transform.localScale = new Vector3(width, height, width);
                Collider collider = mountain.GetComponent<Collider>();
                if (collider != null) Object.DestroyImmediate(collider);
                Renderer renderer = mountain.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = material;
                GameObjectUtility.SetStaticEditorFlags(mountain, StaticEditorFlags.BatchingStatic);
            }
        }

        // --- Dinding tak terlihat agar pemain tidak keluar map ---
        private static void BuildInvisibleWalls(Bounds arena)
        {
            Transform old = FindOrCreate("Scenery_Walls", out bool created);
            if (!created)
            {
                for (int i = old.childCount - 1; i >= 0; i--) Object.DestroyImmediate(old.GetChild(i).gameObject);
            }

            const float thickness = 2f;
            float height = 30f;
            float halfX = arena.extents.x + thickness;
            float halfZ = arena.extents.z + thickness;
            Vector3 center = new Vector3(arena.center.x, arena.center.y, arena.center.z);

            CreateWall(old, "Wall_North", center + new Vector3(0f, height * 0.5f, halfZ), new Vector3(halfX * 2f + thickness * 2f, height, thickness));
            CreateWall(old, "Wall_South", center + new Vector3(0f, height * 0.5f, -halfZ), new Vector3(halfX * 2f + thickness * 2f, height, thickness));
            CreateWall(old, "Wall_East", center + new Vector3(halfX, height * 0.5f, 0f), new Vector3(thickness, height, halfZ * 2f + thickness * 2f));
            CreateWall(old, "Wall_West", center + new Vector3(-halfX, height * 0.5f, 0f), new Vector3(thickness, height, halfZ * 2f + thickness * 2f));
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false; // tak terlihat, collider tetap aktif
            GameObjectUtility.SetStaticEditorFlags(wall, StaticEditorFlags.BatchingStatic);
        }

        // --- Batu dan semak ornamen di luar jalur ---
        private static void BuildRocks(Bounds arena)
        {
            Transform old = FindOrCreate("Scenery_Rocks", out bool created);
            if (!created)
            {
                for (int i = old.childCount - 1; i >= 0; i--) Object.DestroyImmediate(old.GetChild(i).gameObject);
            }

            Material material = EnsureMaterial(RockMaterialPath, new Color(0.44f, 0.45f, 0.40f));
            Random rng = new Random(8102026);
            float inner = Mathf.Max(arena.extents.x, arena.extents.z) + 6f;
            float outer = inner + Mathf.Max(arena.extents.x, arena.extents.z) * 0.55f + 24f;
            for (int i = 0; i < 16; i++)
            {
                float angle = NextRange(rng, 0f, Mathf.PI * 2f);
                float distance = NextRange(rng, inner, outer);
                float size = NextRange(rng, 1.2f, 3.4f);

                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = "Rock_" + i.ToString("00");
                rock.transform.SetParent(old, false);
                rock.transform.position = new Vector3(
                    arena.center.x + Mathf.Cos(angle) * distance,
                    size * 0.35f,
                    arena.center.z + Mathf.Sin(angle) * distance);
                rock.transform.localScale = new Vector3(size, size * 0.6f, size * NextRange(rng, 0.8f, 1.3f));
                rock.transform.rotation = Quaternion.Euler(0f, NextRange(rng, 0f, 360f), 0f);
                Renderer renderer = rock.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = material;
                GameObjectUtility.SetStaticEditorFlags(rock, StaticEditorFlags.BatchingStatic);
            }
        }

        private static void BuildWindZone()
        {
            if (Object.FindFirstObjectByType<WindZone>(FindObjectsInactive.Include) != null) return;
            GameObject windObject = new GameObject("Scenery_Wind");
            WindZone wind = windObject.AddComponent<WindZone>();
            wind.mode = WindZoneMode.Directional;
            wind.windMain = 0.7f;
            wind.windTurbulence = 0.5f;
            wind.windPulseMagnitude = 1f;
            wind.windPulseFrequency = 0.3f;
        }

        private static void BuildAmbience()
        {
            if (Object.FindFirstObjectByType<AudioSource>(FindObjectsInactive.Include) != null) return;
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ForestAmbience.wav");
            if (clip == null) return;

            GameObject ambienceObject = new GameObject("Scenery_Ambience");
            AudioSource source = ambienceObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.volume = 0.35f;
            source.spatialBlend = 0f;   // ambience 2D, terdengar merata
            source.priority = 200;
        }

        // --- Bake ulang NavMesh karena dinding baru ditambahkan ---
        private static void RebakeNavMesh()
        {
            foreach (NavMeshSurface surface in Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                surface.BuildNavMesh();
        }

        private static Transform FindOrCreate(string name, out bool created)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null) { created = false; return existing.transform; }
            created = true;
            return new GameObject(name).transform;
        }

        private static Material EnsureMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static float NextRange(Random rng, float min, float max)
            => min + (float)rng.NextDouble() * (max - min);
    }
}
