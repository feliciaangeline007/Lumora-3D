using UnityEngine;
using UnityEngine.Rendering;

namespace EnchantedForest
{
    public static class LevelVisualPolish
    {
        private static Material _barkMaterial;
        private static Material _foliageMaterial;
        private static Material _foliageHighlightMaterial;
        private static Material _mossMaterial;
        private static Material _sporeMaterial;
        private static Material _crystalMaterial;
        private static Material _forestDeckMaterial;
        private static Material _caveDeckMaterial;
        private static Material _canopyDeckMaterial;
        private static Material _forestAccentMaterial;
        private static Material _caveAccentMaterial;
        private static Material _canopyAccentMaterial;

        public static Material CrystalMaterial
        {
            get
            {
                EnsureMaterials();
                return _crystalMaterial;
            }
        }

        public static void Apply(PlayerController3D player)
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (!IsPlayableLevel(sceneName) || player == null) return;

            EnsureMaterials();
            ApplyAtmosphere(sceneName);
            ApplyCameraFraming(player, sceneName);

            Transform artRoot = FindArtRoot(player);
            bool shouldBuildLandmarks = artRoot == null;
            if (artRoot == null)
            {
                GameObject artObject = new GameObject("Lumora_VisualPolish");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(artObject, player.gameObject.scene);
                artRoot = artObject.transform;
            }

            ApplySceneStyle(player, sceneName);
            if (!shouldBuildLandmarks) return;

            if (sceneName == "Level_1_PinggirHutan")
            {
                CreateForestFrame(artRoot);
            }
            else if (sceneName == "Level_2_JurangAkar")
            {
                CreateCaveWaypoints(artRoot);
            }
            else
            {
                CreateCanopyAccents(artRoot);
            }

            CreateAmbientSparks(player.transform, sceneName);
        }

        private static Transform FindArtRoot(PlayerController3D player)
        {
            Transform sceneRoot = player.transform.root;
            if (sceneRoot.name == "Lumora_VisualPolish") return sceneRoot;

            Transform existing = sceneRoot.Find("Lumora_VisualPolish");
            if (existing != null) return existing;

            GameObject[] sceneRoots = player.gameObject.scene.GetRootGameObjects();
            for (int i = 0; i < sceneRoots.Length; i++)
            {
                if (sceneRoots[i].name == "Lumora_VisualPolish") return sceneRoots[i].transform;
            }

            return null;
        }

        private static bool IsPlayableLevel(string sceneName)
        {
            return sceneName == "Level_1_PinggirHutan" ||
                   sceneName == "Level_2_JurangAkar" ||
                   sceneName == "Level_3_PuncakPohon";
        }

        private static void EnsureMaterials()
        {
            if (_barkMaterial != null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogError("[Lumora art] Shader Lit tidak ditemukan; dekorasi visual dilewati.");
                return;
            }

            _barkMaterial = CreateMaterial(shader, "Lumora_Runtime_Bark", new Color(0.22f, 0.12f, 0.075f), 0f, 0.18f);
            _foliageMaterial = CreateMaterial(shader, "Lumora_Runtime_Foliage", new Color(0.055f, 0.24f, 0.13f), 0f, 0.12f);
            _foliageHighlightMaterial = CreateMaterial(shader, "Lumora_Runtime_FoliageHighlight", new Color(0.18f, 0.43f, 0.22f), 0f, 0.1f);
            _mossMaterial = CreateMaterial(shader, "Lumora_Runtime_GlowMoss", new Color(0.06f, 0.48f, 0.38f), 0f, 0.3f, new Color(0.06f, 0.8f, 0.57f) * 1.2f);
            _crystalMaterial = CreateMaterial(shader, "Lumora_Runtime_Crystal", new Color(0.2f, 0.86f, 0.78f), 0.15f, 0.55f, new Color(0.18f, 0.9f, 0.78f) * 1.5f);
            _forestDeckMaterial = CreateMaterial(shader, "Lumora_Runtime_ForestDeck", new Color(0.18f, 0.43f, 0.27f), 0f, 0.12f);
            _caveDeckMaterial = CreateMaterial(shader, "Lumora_Runtime_CaveDeck", new Color(0.25f, 0.36f, 0.42f), 0.08f, 0.24f);
            _canopyDeckMaterial = CreateMaterial(shader, "Lumora_Runtime_CanopyDeck", new Color(0.35f, 0.31f, 0.21f), 0.02f, 0.2f);
            _forestAccentMaterial = CreateMaterial(shader, "Lumora_Runtime_ForestAccent", new Color(0.92f, 0.62f, 0.29f), 0f, 0.16f);
            _caveAccentMaterial = CreateMaterial(shader, "Lumora_Runtime_CaveAccent", new Color(0.1f, 0.62f, 0.57f), 0.08f, 0.3f, new Color(0.04f, 0.34f, 0.27f) * 0.5f);
            _canopyAccentMaterial = CreateMaterial(shader, "Lumora_Runtime_CanopyAccent", new Color(0.9f, 0.56f, 0.24f), 0.08f, 0.28f, new Color(0.3f, 0.1f, 0.025f) * 0.35f);

            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                                    Shader.Find("Particles/Standard Unlit");
            if (particleShader != null)
            {
                _sporeMaterial = new Material(particleShader)
                {
                    name = "Lumora_Runtime_Spore",
                    enableInstancing = true
                };
                if (_sporeMaterial.HasProperty("_BaseColor"))
                    _sporeMaterial.SetColor("_BaseColor", Color.white);
                if (_sporeMaterial.HasProperty("_Color"))
                    _sporeMaterial.SetColor("_Color", Color.white);
            }
        }

        private static Material CreateMaterial(Shader shader, string materialName, Color color, float metallic, float smoothness, Color? emission = null)
        {
            Material material = new Material(shader)
            {
                name = materialName,
                enableInstancing = true
            };

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }

            return material;
        }

        private static void ApplyAtmosphere(string sceneName)
        {
            if (sceneName == "Level_2_JurangAkar")
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = new Color(0.18f, 0.29f, 0.31f);
                RenderSettings.fogDensity = 0.008f;
                RenderSettings.ambientSkyColor = new Color(0.2f, 0.39f, 0.43f);
                RenderSettings.ambientEquatorColor = new Color(0.13f, 0.24f, 0.24f);
            }
            else if (sceneName == "Level_3_PuncakPohon")
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = new Color(0.68f, 0.43f, 0.3f);
                RenderSettings.fogDensity = 0.004f;
                RenderSettings.ambientSkyColor = new Color(0.8f, 0.62f, 0.43f);
                RenderSettings.ambientEquatorColor = new Color(0.42f, 0.3f, 0.2f);
            }
        }

        private static void ApplyCameraFraming(PlayerController3D player, string sceneName)
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera == null || camera.gameObject.scene != player.gameObject.scene) continue;
                camera.fieldOfView = sceneName == "Level_3_PuncakPohon" ? 58f : 54f;
            }
        }

        private static void ApplySceneStyle(PlayerController3D player, string sceneName)
        {
            Material deckMaterial = sceneName == "Level_2_JurangAkar"
                ? _caveDeckMaterial
                : sceneName == "Level_3_PuncakPohon"
                    ? _canopyDeckMaterial
                    : _forestDeckMaterial;

            Material accentMaterial = sceneName == "Level_2_JurangAkar"
                ? _caveAccentMaterial
                : sceneName == "Level_3_PuncakPohon"
                    ? _canopyAccentMaterial
                    : _forestAccentMaterial;

            Renderer[] renderers = player.transform.root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.gameObject.name.StartsWith("Lumora_")) continue;

                GameObject target = renderer.gameObject;
                if (IsPlatformSurface(target.name) && target.TryGetComponent(out BoxCollider box) && !box.isTrigger)
                {
                    CreatePlatformTop(target.transform, box, deckMaterial, accentMaterial);
                    continue;
                }

                if (target.name.Contains("GreatTreeCanopy") || target.name.Contains("Canopy_Base"))
                {
                    Tint(renderer, new Color(0.12f, 0.42f, 0.2f));
                }
                else if (target.name.Contains("ColossalTreeTrunk") || target.name == "Trunk")
                {
                    Tint(renderer, new Color(0.37f, 0.21f, 0.12f));
                }
            }

            Collider[] colliders = player.transform.root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider != null && collider.name.Contains("ColossalTreeTrunk"))
                {
                    CreateRootButtresses(FindArtRoot(player), collider.transform.position, collider.bounds.extents.x);
                    break;
                }
            }
        }

        private static bool IsPlatformSurface(string name)
        {
            return name.Contains("Platform") ||
                   name.Contains("Cliff") ||
                   name.Contains("StepStone") ||
                   name.Contains("SpiralStep") ||
                   name.Contains("Pillar_") ||
                   name.Contains("RootBridge") ||
                   name.Contains("Sanctuary") ||
                   name.Contains("MidIsle") ||
                   name.StartsWith("P0_") ||
                   name.StartsWith("P1_") ||
                   name.StartsWith("P2_") ||
                   name.StartsWith("P3_") ||
                   name.StartsWith("P4_") ||
                   name.StartsWith("P5_");
        }

        private static void CreatePlatformTop(Transform platform, BoxCollider box, Material deck, Material accent)
        {
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "Lumora_DeckInlay";
            cap.transform.SetParent(platform, false);
            cap.transform.localPosition = box.center + Vector3.up * (box.size.y * 0.5f + 0.018f);
            cap.transform.localRotation = Quaternion.identity;
            cap.transform.localScale = new Vector3(box.size.x * 0.92f, 0.035f, box.size.z * 0.92f);
            ConfigureDecorationRenderer(cap, deck);
            Object.Destroy(cap.GetComponent<Collider>());

            if (box.size.x < 3f || box.size.z < 3f) return;

            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "Lumora_DeckAccent";
            stripe.transform.SetParent(platform, false);
            stripe.transform.localPosition = box.center + Vector3.up * (box.size.y * 0.5f + 0.038f);
            stripe.transform.localRotation = Quaternion.identity;
            stripe.transform.localScale = new Vector3(box.size.x * 0.08f, 0.012f, box.size.z * 0.68f);
            ConfigureDecorationRenderer(stripe, accent);
            Object.Destroy(stripe.GetComponent<Collider>());
        }

        private static void Tint(Renderer renderer, Color tint)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", tint);
            block.SetColor("_Color", tint);
            renderer.SetPropertyBlock(block);
        }

        private static void ConfigureDecorationRenderer(GameObject decoration, Material material)
        {
            Renderer renderer = decoration.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void CreateForestFrame(Transform parent)
        {
            Vector3[] treePositions =
            {
                new Vector3(-8f, 0f, 8f),
                new Vector3(8f, 0f, 17f),
                new Vector3(-8f, 0.5f, 28f),
                new Vector3(8f, 1.5f, 38f),
                new Vector3(-8f, 4.5f, 49f),
                new Vector3(8f, 5.5f, 59f)
            };

            for (int i = 0; i < treePositions.Length; i++)
            {
                CreateTree(parent, treePositions[i], i % 2 == 0 ? 1f : 0.88f);
            }

            Vector3[] mossPositions =
            {
                new Vector3(-4.8f, 0.55f, 8f),
                new Vector3(4.8f, 0.55f, 19f),
                new Vector3(-4.8f, 2.1f, 30f),
                new Vector3(4.8f, 2.1f, 41f),
                new Vector3(-4.2f, 5.15f, 53f),
                new Vector3(4.2f, 6.15f, 60f)
            };

            for (int i = 0; i < mossPositions.Length; i++)
            {
                CreateGlowPlant(parent, mossPositions[i], 0.8f + (i % 3) * 0.12f);
            }

            CreatePortalFrame(parent, new Vector3(0f, 5.9f, 58.5f), 4.6f, 5f, _forestAccentMaterial);
        }

        private static void CreateTree(Transform parent, Vector3 position, float scale)
        {
            GameObject tree = new GameObject("CanopyTree_Deco");
            tree.transform.SetParent(parent, false);
            tree.transform.localPosition = position;
            tree.transform.localScale = Vector3.one * scale;

            CreatePart("Trunk", PrimitiveType.Cylinder, tree.transform,
                new Vector3(0f, 1.65f, 0f), new Vector3(0.48f, 1.65f, 0.48f), _barkMaterial,
                Quaternion.Euler(0f, 0f, 2f));
            CreatePart("LowerCrown", PrimitiveType.Sphere, tree.transform,
                new Vector3(0f, 3.35f, 0f), new Vector3(2.3f, 1.65f, 2.05f), _foliageMaterial);
            CreatePart("LeftCrown", PrimitiveType.Sphere, tree.transform,
                new Vector3(-0.82f, 3.85f, 0.18f), new Vector3(1.45f, 1.3f, 1.4f), _foliageHighlightMaterial);
            CreatePart("RightCrown", PrimitiveType.Sphere, tree.transform,
                new Vector3(0.85f, 4.15f, -0.12f), new Vector3(1.55f, 1.35f, 1.45f), _foliageMaterial);
            CreatePart("TopCrown", PrimitiveType.Sphere, tree.transform,
                new Vector3(0.08f, 4.75f, 0f), new Vector3(1.35f, 1.35f, 1.3f), _foliageHighlightMaterial);
        }

        private static void CreateGlowPlant(Transform parent, Vector3 position, float scale)
        {
            GameObject plant = new GameObject("LumoraMoss_Deco");
            plant.transform.SetParent(parent, false);
            plant.transform.localPosition = position;
            plant.transform.localScale = Vector3.one * scale;

            CreatePart("Stem", PrimitiveType.Cylinder, plant.transform,
                new Vector3(0f, 0.28f, 0f), new Vector3(0.09f, 0.28f, 0.09f), _barkMaterial);
            GameObject cap = CreatePart("GlowCap", PrimitiveType.Sphere, plant.transform,
                new Vector3(0f, 0.62f, 0f), new Vector3(0.58f, 0.26f, 0.58f), _mossMaterial);
            if (cap != null) cap.AddComponent<CrystalPulse>().pulseSpeed = 1.1f;

            CreatePart("SideBud", PrimitiveType.Sphere, plant.transform,
                new Vector3(0.26f, 0.4f, 0.16f), new Vector3(0.17f, 0.18f, 0.17f), _crystalMaterial);
        }

        private static void CreateCaveWaypoints(Transform parent)
        {
            Vector3[] cliffPositions =
            {
                new Vector3(-13f, -1f, 2f),
                new Vector3(13f, -0.2f, 11f),
                new Vector3(-13f, 1.4f, 22f),
                new Vector3(13f, 2.5f, 33f),
                new Vector3(-13f, 3.5f, 45f),
                new Vector3(13f, 5f, 57f),
                new Vector3(-13f, 6f, 69f),
                new Vector3(13f, 7f, 77f)
            };

            for (int i = 0; i < cliffPositions.Length; i++)
            {
                float height = 15f + (i % 3) * 2f;
                GameObject pillar = CreatePart("CavePillar_Deco", PrimitiveType.Cylinder, parent,
                    cliffPositions[i] + Vector3.up * (height * 0.5f),
                    new Vector3(1.8f, height * 0.5f, 1.8f), _barkMaterial,
                    Quaternion.Euler(0f, 0f, (i % 2 == 0 ? -1f : 1f) * 3f));
                if (pillar != null)
                {
                    GameObject crown = CreatePart("CaveRockCap_Deco", PrimitiveType.Sphere, parent,
                        cliffPositions[i] + Vector3.up * height,
                        new Vector3(3.4f, 2.2f, 3f), _foliageMaterial);
                    if (crown != null) Tint(crown.GetComponent<Renderer>(), new Color(0.13f, 0.2f, 0.19f));
                }
            }

            Vector3[] positions =
            {
                new Vector3(-8.2f, 8f, 8f),
                new Vector3(8.2f, 8.4f, 25f),
                new Vector3(-8.2f, 10f, 41f),
                new Vector3(8.2f, 14f, 67f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                CreateGlowCrystal(parent, positions[i], i % 2 == 0 ? 1f : 0.78f, (i % 2 == 0 ? -1f : 1f) * 14f);
            }

            CreateGlowCrystal(parent, new Vector3(0f, -7f, 40f), 3.4f, 0f);
            CreatePortalFrame(parent, new Vector3(0f, 14.8f, 69f), 5.2f, 4.6f, _caveAccentMaterial);
        }

        private static void CreateCanopyAccents(Transform parent)
        {
            Vector3[] positions =
            {
                new Vector3(-5.8f, 10f, 22f),
                new Vector3(5.8f, 18f, 22f),
                new Vector3(-5.8f, 27f, 27f),
                new Vector3(5.8f, 35f, 29f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                CreateGlowCrystal(parent, positions[i], 0.76f, i % 2 == 0 ? 18f : -18f);
            }

            CreateCanopyBranches(parent);
            CreatePortalFrame(parent, new Vector3(-3.36f, 35.5f, 55.5f), 6.5f, 6f, _canopyAccentMaterial);
        }

        private static void CreateCanopyBranches(Transform parent)
        {
            for (int i = 0; i < 7; i++)
            {
                float angle = i * Mathf.PI * 2f / 7f;
                float radius = 8.4f;
                Vector3 branchPosition = new Vector3(
                    Mathf.Cos(angle) * radius,
                    9f + i * 3.8f,
                    25f + Mathf.Sin(angle) * radius);
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0.1f, Mathf.Sin(angle)).normalized;
                CreateBranch(parent, branchPosition, direction, 4.5f - i * 0.2f, 0.45f);

                Vector3 leafPosition = branchPosition + direction * 3.7f + Vector3.up * 1.2f;
                CreatePart("CanopyLeafCluster", PrimitiveType.Sphere, parent,
                    leafPosition, new Vector3(2.5f, 0.8f, 1.8f), i % 2 == 0 ? _foliageMaterial : _foliageHighlightMaterial,
                    Quaternion.LookRotation(direction, Vector3.up));
            }
        }

        private static void CreateRootButtresses(Transform artRoot, Vector3 trunkPosition, float trunkRadius)
        {
            if (artRoot == null) return;
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI * 2f / 6f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                if (Vector3.Dot(direction, Vector3.back) > 0.75f) continue;
                Vector3 start = trunkPosition + direction * (trunkRadius * 0.65f) + Vector3.up * -25f;
                Vector3 end = trunkPosition + direction * (trunkRadius + 5f) + Vector3.up * -18f;
                CreateBranch(artRoot, (start + end) * 0.5f, (end - start).normalized,
                    Vector3.Distance(start, end), 0.95f);
            }
        }

        private static void CreateBranch(Transform parent, Vector3 position, Vector3 direction, float length, float thickness)
        {
            GameObject branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            branch.name = "CanopyBranch_Deco";
            branch.transform.SetParent(parent, false);
            branch.transform.position = position;
            branch.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            branch.transform.localScale = new Vector3(thickness, length * 0.5f, thickness);
            ConfigureDecorationRenderer(branch, _barkMaterial);
            Object.Destroy(branch.GetComponent<Collider>());
        }

        private static void CreatePortalFrame(Transform parent, Vector3 center, float width, float height, Material accent)
        {
            CreatePart("LandmarkPillar", PrimitiveType.Cube, parent,
                center + new Vector3(-width * 0.5f, height * 0.5f, 0f),
                new Vector3(0.4f, height, 0.55f), _barkMaterial);
            CreatePart("LandmarkPillar", PrimitiveType.Cube, parent,
                center + new Vector3(width * 0.5f, height * 0.5f, 0f),
                new Vector3(0.4f, height, 0.55f), _barkMaterial);
            CreatePart("LandmarkLintel", PrimitiveType.Cube, parent,
                center + Vector3.up * height,
                new Vector3(width + 0.8f, 0.55f, 0.72f), accent);
            CreatePart("LandmarkGem", PrimitiveType.Sphere, parent,
                center + Vector3.up * (height + 0.55f),
                new Vector3(0.72f, 0.9f, 0.48f), _crystalMaterial);
        }

        private static void CreateGlowCrystal(Transform parent, Vector3 position, float scale, float lean)
        {
            GameObject cluster = new GameObject("LumoraCrystal_Deco");
            cluster.transform.SetParent(parent, false);
            cluster.transform.localPosition = position;
            cluster.transform.localScale = Vector3.one * scale;
            cluster.transform.localRotation = Quaternion.Euler(0f, 0f, lean);

            GameObject mainShard = CreatePart("MainShard", PrimitiveType.Capsule, cluster.transform,
                Vector3.zero, new Vector3(0.3f, 0.95f, 0.3f), _crystalMaterial);
            if (mainShard != null) mainShard.AddComponent<CrystalPulse>().pulseSpeed = 0.9f;

            CreatePart("SideShard", PrimitiveType.Capsule, cluster.transform,
                new Vector3(0.29f, -0.16f, 0f), new Vector3(0.2f, 0.58f, 0.2f), _mossMaterial,
                Quaternion.Euler(0f, 0f, -22f));
            CreatePart("BaseRock", PrimitiveType.Sphere, cluster.transform,
                new Vector3(0f, -0.66f, 0f), new Vector3(0.66f, 0.24f, 0.5f), _barkMaterial);
        }

        private static GameObject CreatePart(
            string partName,
            PrimitiveType shape,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Quaternion? rotation = null)
        {
            if (material == null) return null;

            GameObject part = GameObject.CreatePrimitive(shape);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            if (rotation.HasValue) part.transform.localRotation = rotation.Value;

            Collider partCollider = part.GetComponent<Collider>();
            if (partCollider != null) Object.Destroy(partCollider);

            Renderer partRenderer = part.GetComponent<Renderer>();
            partRenderer.sharedMaterial = material;
            partRenderer.shadowCastingMode = ShadowCastingMode.Off;
            partRenderer.receiveShadows = false;
            return part;
        }

        private static void CreateAmbientSparks(Transform player, string sceneName)
        {
            if (_sporeMaterial == null) return;

            GameObject sparks = new GameObject("Lumora_AmbientSparks");
            sparks.transform.SetParent(player, false);
            sparks.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            ParticleSystem particles = sparks.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.duration = 8f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.025f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.09f);
            main.startColor = sceneName == "Level_3_PuncakPohon"
                ? new Color(1f, 0.72f, 0.34f, 0.88f)
                : sceneName == "Level_2_JurangAkar"
                    ? new Color(0.42f, 0.87f, 1f, 0.9f)
                    : new Color(0.63f, 1f, 0.68f, 0.9f);
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 5f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 4f;
            shape.radiusThickness = 0.12f;

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.12f;
            noise.frequency = 0.18f;
            noise.scrollSpeed = 0.1f;

            ParticleSystemRenderer particleRenderer = sparks.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.material = _sporeMaterial;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
        }
    }
}
