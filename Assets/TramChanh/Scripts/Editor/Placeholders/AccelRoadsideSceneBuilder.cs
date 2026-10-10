using System;
using System.Collections.Generic;
using System.IO;
using TramChanh.Core.GroundTruth;
using TramChanh.Core.Provisional;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TramChanh.EditorTools.Placeholders
{
    /// <summary>
    /// Environment-only ACCEL-01 presentation. Canonical prefab roots and anchors survive
    /// mesh replacement. Layout, colours and lights are provisional scene data, not shop measurements.
    /// No intake, ticket or preparation is created here; App composes those independently.
    /// </summary>
    public static class AccelRoadsideSceneBuilder
    {
        public const string EnvironmentPrefabPath = "Assets/TramChanh/Prefabs/ACCEL01/Environment/PF_AccelRoadsideEnvironment.prefab";
        public const string ScenePath = "Assets/TramChanh/Scenes/ACCEL01/SCN_AccelRoadsideEnvironment.unity";
        public const string MaterialFolder = "Assets/TramChanh/Materials/ACCEL01";

        [MenuItem("Tram Chanh/ACCEL-01/Build Roadside Night Environment")]
        public static void BuildAndSave()
        {
            EnsureFolder(Path.GetDirectoryName(EnvironmentPrefabPath));
            EnsureFolder(Path.GetDirectoryName(ScenePath));
            EnsureFolder(MaterialFolder);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var materials = new Palette();
            GameObject environment = BuildEnvironment(materials);
            try { PrefabUtility.SaveAsPrefabAsset(environment, EnvironmentPrefabPath); }
            finally { Object.DestroyImmediate(environment); }
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentPrefabPath);
            PrefabUtility.InstantiatePrefab(saved, scene);
            ConfigureNightLighting();
            var camera = new GameObject("EnvironmentPreviewCamera").AddComponent<Camera>();
            camera.transform.position = new Vector3(4.5f, 3.3f, 6.7f);
            camera.transform.LookAt(new Vector3(-0.2f, 1f, 0.5f));
            camera.fieldOfView = 49f;
            camera.nearClipPlane = 0.05f;
            camera.backgroundColor = new Color(0.016f, 0.029f, 0.063f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Render the saved preview with the pinned URP pipeline. Output stays outside Assets.</summary>
        public static void CapturePreview()
        {
            string output = Environment.GetEnvironmentVariable("TRAM_CHANH_PREVIEW_PATH");
            if (string.IsNullOrWhiteSpace(output)) { throw new InvalidOperationException("Set TRAM_CHANH_PREVIEW_PATH to the review PNG path."); }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera camera = GameObject.Find("EnvironmentPreviewCamera").GetComponent<Camera>();
            var destination = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            bool previousAsyncCompilation = ShaderUtil.allowAsyncCompilation;
            try
            {
                // A fresh Editor has no compiled material variants: wait for real Lit
                // passes instead of exporting its temporary grey compilation shader.
                ShaderUtil.allowAsyncCompilation = false;
                camera.aspect = 1.6f;
                var compiledMaterials = new HashSet<Material>();
                foreach (Renderer renderer in Object.FindObjectsByType<Renderer>())
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material == null || !compiledMaterials.Add(material)) { continue; }
                        int pass = material.FindPass("ForwardLit");
                        if (pass >= 0) { ShaderUtil.CompilePass(material, pass); }
                    }
                }
                destination.Create();
                // Initialize the fresh pipeline/material buffers, then capture a complete frame.
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = destination });
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = destination });
                RenderTexture.active = destination;
                pixels.ReadPixels(new Rect(0f, 0f, 1600f, 1000f), 0, 0); pixels.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
                File.WriteAllBytes(output, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; destination.Release();
                ShaderUtil.allowAsyncCompilation = previousAsyncCompilation;
                Object.DestroyImmediate(destination); Object.DestroyImmediate(pixels);
            }
        }

        /// <summary>Call after creating the final gameplay scene to keep its render settings consistent.</summary>
        public static void ConfigureNightLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.30f, 0.43f);
            RenderSettings.ambientIntensity = 0.8f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.018f, 0.031f, 0.067f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.024f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        }

        private static GameObject BuildEnvironment(Palette palette)
        {
            var root = new GameObject("PF_AccelRoadsideEnvironment");
            root.layer = TramChanhLayers.EnvironmentIndex;
            root.AddComponent<PlaceholderAsset>().Configure("ACCEL-01 visual slice", "DEC-010; DEC-011; DEC-017",
                "All scene layout, lighting, vehicle form, facades and colour styling are provisional development presentation. Canonical stall dimensions remain ground truth. Replace visual children without changing composition anchors.");
            BuildRoadside(root.transform, palette);
            Transform stallRoot = Anchor(root.transform, "StallRoot", Vector3.zero);
            GameObject stall = Nested(StallPlaceholderBuilder.StallPrefabPath, stallRoot, Vector3.zero);
            RecolourStall(stall, palette);
            BuildStallDetails(stall.transform, palette);
            BuildMenuAndSnacks(root.transform, palette);
            BuildFurnitureAndCustomers(root.transform, palette);
            BuildBackdrop(root.transform, palette);
            Transform spawn = Anchor(root.transform, "PlayerSpawn", new Vector3(-1.5f, 0f, 2.7f));
            spawn.localRotation = Quaternion.LookRotation(new Vector3(1.5f, 0f, -2.7f));
            Anchor(root.transform, "LobbyPosition", new Vector3(-1.5f, 0f, 1.5f));
            Anchor(root.transform, "ReadyHandoff", new Vector3(0f, 1f, 0.65f));
            BuildLighting(root.transform, palette);
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) { child.gameObject.layer = TramChanhLayers.EnvironmentIndex; }
            return root;
        }

        private static void BuildRoadside(Transform parent, Palette palette)
        {
            Transform roadside = Anchor(parent, "Roadside", Vector3.zero);
            Box(roadside, "Sidewalk", new Vector3(0f, -0.08f, -0.45f), new Vector3(20f, 0.16f, 8.1f), palette.Pavement, true);
            Box(roadside, "Street", new Vector3(0f, -0.11f, 8.4f), new Vector3(26f, 0.14f, 9.6f), palette.Asphalt, true);
            // Shallow concrete curb and tiled joints supply scale without a shader/texture dependency.
            Box(roadside, "Curb", new Vector3(0f, -0.015f, 3.62f), new Vector3(20f, 0.13f, 0.14f), palette.Concrete);
            for (int x = -9; x <= 9; x++)
            {
                Box(roadside, "PavingJoint", new Vector3(x, 0.001f, -0.5f), new Vector3(0.012f, 0.002f, 8f), palette.Joint);
            }
            for (int z = -4; z <= 3; z++)
            {
                Box(roadside, "PavingJoint", new Vector3(0f, 0.001f, z), new Vector3(20f, 0.002f, 0.012f), palette.Joint);
            }
            for (int x = -11; x <= 11; x += 3)
            {
                Box(roadside, "StreetLaneDash", new Vector3(x, -0.037f, 8.5f), new Vector3(1.3f, 0.004f, 0.12f), palette.Lane);
            }
            for (int x = 5; x <= 8; x++)
            {
                Box(roadside, "CrosswalkStripe", new Vector3(x, -0.037f, 5.55f), new Vector3(0.45f, 0.004f, 2.8f), palette.Lane);
            }
        }

        private static void RecolourStall(GameObject stall, Palette palette)
        {
            foreach (Renderer renderer in stall.GetComponentsInChildren<Renderer>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(renderer.transform, stall.transform);
                Material material = path.StartsWith("Structure", StringComparison.Ordinal) ? palette.StallGreen :
                    path.StartsWith("Counter", StringComparison.Ordinal) ? palette.Steel :
                    path.StartsWith("Roof", StringComparison.Ordinal) ? palette.Roof :
                    path.StartsWith("Frame", StringComparison.Ordinal) ? palette.Frame :
                    path.Contains("TopCap") ? palette.Mustard :
                    path.StartsWith("Sign", StringComparison.Ordinal) ? palette.Sign : palette.Frame;
                renderer.sharedMaterial = material;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        private static void BuildStallDetails(Transform stall, Palette palette)
        {
            // Placeholder text is a single replaceable child on the approved NEW lightbox.
            Transform sign = stall.Find("Sign/PF_Sign_TramChanh_New");
            Transform branding = Anchor(sign, "BrandingPlaceholder", new Vector3(0f, -0.016f, 0.123f));
            Text(branding, "BrandName", "Trạm Chanh", Vector3.zero, 0.16f, palette.Ink, true);
            Text(branding, "BrandSubline", "ĐỒ UỐNG  •  BÁNH LĂN", new Vector3(0f, -0.085f, 0.001f), 0.029f, palette.Ink, true);
            Transform counterFront = Anchor(stall, "CounterBranding", new Vector3(0f, 0.52f, 0.402f));
            Text(counterFront, "CounterName", "Trạm Chanh", Vector3.zero, 0.16f, palette.Sign, true);
            Text(counterFront, "CounterSubline", "GÓC VỈA HÈ", new Vector3(0f, -0.115f, 0.001f), 0.04f, palette.Mustard, true);
            Transform details = Anchor(stall, "PresentationDetails", Vector3.zero);
            Box(details, "CounterFrontTrim", new Vector3(0f, 0.875f, 0.403f), new Vector3(1.76f, 0.025f, 0.013f), palette.Mustard);
            Box(details, "WarmLEDStrip", new Vector3(0f, 2.10f, -0.33f), new Vector3(1.65f, 0.017f, 0.024f), palette.Glow);
            Box(details, "RoofMustardTrim", new Vector3(0f, 2.175f, 0.405f), new Vector3(1.8f, 0.045f, 0.012f), palette.Mustard);
            for (int i = 0; i < 4; i++)
            {
                float x = -0.65f + i * 0.43f;
                Box(details, "BulbCord", new Vector3(x, 2.035f, -0.22f), new Vector3(0.008f, 0.19f, 0.008f), palette.Frame);
                Sphere(details, "WarmHangingBulb", new Vector3(x, 1.92f, -0.22f), Vector3.one * 0.06f, palette.Glow);
            }
        }

        private static void BuildMenuAndSnacks(Transform parent, Palette palette)
        {
            Transform menu = Anchor(parent, "MenuBoard", new Vector3(-1.9f, 0f, -0.25f));
            Box(menu, "Board", new Vector3(0f, 1.35f, 0f), new Vector3(0.65f, 0.83f, 0.05f), palette.Menu, true);
            Box(menu, "FrameTop", new Vector3(0f, 1.79f, 0f), new Vector3(0.7f, 0.04f, 0.06f), palette.Mustard);
            Box(menu, "Stand", new Vector3(0f, 0.45f, 0f), new Vector3(0.05f, 0.9f, 0.05f), palette.Frame);
            Box(menu, "StandFoot", new Vector3(0f, 0.04f, 0f), new Vector3(0.5f, 0.08f, 0.32f), palette.Frame);
            Text(menu, "MenuTitle", "TRẠM CHANH", new Vector3(0f, 1.67f, 0.026f), 0.055f, palette.Mustard, true);
            // Names supplied by the PO; no price, recipe, quantity or flavour mapping is invented.
            Text(menu, "MenuText", "Bánh Lăn Truyền Thống\nBánh Lăn Phô Mai Chảy\nBánh Lăn Choco Chip\nBánh Lăn Cốm Dẻo", new Vector3(0f, 1.30f, 0.026f), 0.037f, palette.Sign, true);
            Transform snacks = Anchor(parent, "SnackDisplayPlaceholder", new Vector3(1.65f, 0f, -0.2f));
            snacks.gameObject.AddComponent<PlaceholderAsset>().Configure("ACCEL-01 decoration", "Snack assortment pending", "Decorative placeholder packets; no sale, price or invented menu data.");
            for (int i = 0; i < 3; i++)
            {
                float y = 0.48f + i * 0.28f;
                Box(snacks, "SteelShelf", new Vector3(0f, y, 0f), new Vector3(0.5f, 0.025f, 0.3f), palette.Steel);
                for (int j = 0; j < 3; j++)
                {
                    Box(snacks, "SnackPacketPlaceholder", new Vector3(-0.15f + j * 0.15f, y + 0.09f, 0f), new Vector3(0.10f, 0.16f, 0.09f), j % 2 == 0 ? palette.Red : palette.Mustard);
                }
            }
            foreach (float x in new[] { -0.22f, 0.22f })
            {
                Box(snacks, "ShelfPost", new Vector3(x, 0.65f, -0.12f), new Vector3(0.025f, 1.3f, 0.025f), palette.Frame);
            }
        }

        private static void BuildFurnitureAndCustomers(Transform parent, Palette palette)
        {
            Transform table = Anchor(parent, "TablePoint", new Vector3(-2.1f, 0f, 1.7f));
            DecorateTable(Nested("Assets/TramChanh/Prefabs/CustomerArea/PF_YellowCrateTable.prefab", table, Vector3.zero), palette);
            Transform furniture = Anchor(parent, "CustomerFurniture", Vector3.zero);
            foreach (Vector3 position in new[] { new Vector3(-2.1f, 0f, 2.35f), new Vector3(-2.8f, 0f, 1.7f), new Vector3(-3.35f, 0f, 0.85f), new Vector3(-4.05f, 0f, 0.2f) })
            {
                GameObject stool = Nested("Assets/TramChanh/Prefabs/CustomerArea/PF_PlasticStool.prefab", furniture, position);
                foreach (Renderer renderer in stool.GetComponentsInChildren<Renderer>()) { renderer.sharedMaterial = palette.Red; PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); }
            }
            Transform cakeTable = Anchor(parent, "CakeTablePoint", new Vector3(-3.35f, 0f, 0.2f));
            DecorateTable(Nested("Assets/TramChanh/Prefabs/CustomerArea/PF_YellowCrateTable.prefab", cakeTable, Vector3.zero), palette);
            Transform cakeCustomer = Anchor(parent, "CakeDineInCustomer", new Vector3(-4.05f, 0f, 0.2f));
            cakeCustomer.gameObject.AddComponent<PlaceholderAsset>().Configure("DEC-010 seated customer art", "Customer art pending", "Seated primitive placeholder. No AI or order logic; CakeTablePoint provides the independent intake seam.");
            Primitive(cakeCustomer, "SeatedBody", PrimitiveType.Capsule, new Vector3(0f, 0.67f, 0f), new Vector3(0.35f, 0.37f, 0.35f), palette.CustomerTwo, false);
            Sphere(cakeCustomer, "Head", new Vector3(0f, 1.08f, 0f), Vector3.one * 0.24f, palette.CustomerTwo);
            Box(cakeCustomer, "Legs", new Vector3(0.12f, 0.23f, 0f), new Vector3(0.24f, 0.40f, 0.29f), palette.Frame);
            Customer(parent, "DineInCustomer", new Vector3(-2.8f, 0f, 2.35f), palette.Customer);
            Transform vehicle = Anchor(parent, "VehiclePoint", new Vector3(2.6f, 0f, 2.5f));
            Transform visual = Anchor(vehicle, "GenericVehicleVisual", new Vector3(0.3f, 0f, 0.5f));
            visual.gameObject.AddComponent<PlaceholderAsset>().Configure("DEC-010 vehicle art", "Vehicle type unconfirmed", "Generic vehicle silhouette for intake location only. Its meshes have no gameplay identity.");
            Box(visual, "VehicleBody", new Vector3(0f, 0.48f, 0f), new Vector3(0.45f, 0.35f, 1.2f), palette.Vehicle, true);
            Box(visual, "Seat", new Vector3(0f, 0.71f, 0.05f), new Vector3(0.4f, 0.07f, 0.57f), palette.Frame);
            foreach (float z in new[] { -0.48f, 0.48f })
            {
                GameObject wheel = Cylinder(visual, "VehicleWheel", new Vector3(0f, 0.22f, z), new Vector3(0.44f, 0.07f, 0.44f), palette.Frame);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            Box(visual, "Handlebar", new Vector3(0f, 0.93f, -0.46f), new Vector3(0.66f, 0.035f, 0.035f), palette.Steel);
            Customer(vehicle, "TakeawayCustomer", new Vector3(-0.45f, 0f, 0.55f), palette.CustomerTwo);
            Text(vehicle, "TakeawayLabel", "MANG ĐI", new Vector3(0.1f, 1.55f, 0.6f), 0.065f, palette.Mustard, true);
        }

        private static void DecorateTable(GameObject table, Palette palette)
        {
            Renderer tray = table.transform.Find("TrayTop").GetComponent<Renderer>();
            tray.sharedMaterial = palette.Steel; PrefabUtility.RecordPrefabInstancePropertyModifications(tray);
            Transform details = Anchor(table.transform, "CratePresentation", Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                float x = -0.245f + i * 0.07f;
                foreach (float z in new[] { -0.201f, 0.201f })
                {
                    Box(details, "CrateVentPlaceholder", new Vector3(x, 0.20f, z), new Vector3(0.033f, 0.18f, 0.003f), palette.Frame);
                }
            }
        }

        private static void BuildBackdrop(Transform parent, Palette palette)
        {
            Transform backdrop = Anchor(parent, "NeighbourhoodBackdrop", Vector3.zero);
            for (int i = 0; i < 5; i++)
            {
                float x = -8f + i * 4f;
                Box(backdrop, "Facade", new Vector3(x, 2.2f, -5.05f), new Vector3(3.8f, 4.4f, 0.4f), i % 2 == 0 ? palette.Facade : palette.FacadeTwo, true);
                Box(backdrop, "Shutter", new Vector3(x, 1.25f, -4.835f), new Vector3(2.5f, 2.5f, 0.03f), palette.Shutter);
                for (int j = 0; j < 10; j++) { Box(backdrop, "ShutterSlat", new Vector3(x, 0.2f + j * 0.24f, -4.81f), new Vector3(2.48f, 0.025f, 0.01f), palette.Frame); }
                Box(backdrop, "Awning", new Vector3(x, 2.7f, -4.4f), new Vector3(3.3f, 0.05f, 1.15f), i % 2 == 0 ? palette.Roof : palette.Mustard);
                Box(backdrop, "UpperWindow", new Vector3(x + 0.8f, 3.45f, -4.82f), new Vector3(0.7f, 0.85f, 0.035f), palette.Window);
            }
            Transform plant = Anchor(backdrop, "SidewalkPlant", new Vector3(-4.8f, 0f, -0.9f));
            Cylinder(plant, "PlantPot", new Vector3(0f, 0.23f, 0f), new Vector3(0.46f, 0.23f, 0.46f), palette.Terracotta);
            Cylinder(plant, "Trunk", new Vector3(0f, 1.2f, 0f), new Vector3(0.08f, 1.0f, 0.08f), palette.Frame);
            Sphere(plant, "Canopy", new Vector3(0f, 2.5f, 0f), new Vector3(1.1f, 1.0f, 1.05f), palette.Foliage);
        }

        private static void BuildLighting(Transform parent, Palette palette)
        {
            Transform lights = Anchor(parent, "NightLighting", Vector3.zero);
            Light moon = NewLight(lights, "CoolStreetAmbient", new Vector3(0f, 5f, 0f), LightType.Directional, new Color(0.48f, 0.65f, 1f), 0.5f, 0f);
            moon.transform.localRotation = Quaternion.Euler(48f, -35f, 0f);
            moon.shadows = LightShadows.Soft;
            NewLight(lights, "WarmStallLight", new Vector3(0f, 1.85f, -0.1f), LightType.Point, new Color(1f, 0.69f, 0.35f), 3.0f, 4.8f);
            NewLight(lights, "WarmLobbyFill", new Vector3(-2.0f, 2.3f, 1.1f), LightType.Point, new Color(1f, 0.77f, 0.47f), 1.5f, 4.5f);
            Transform pole = Anchor(lights, "StreetLamp", new Vector3(5.8f, 0f, 3.2f));
            Cylinder(pole, "Pole", new Vector3(0f, 2.25f, 0f), new Vector3(0.085f, 2.25f, 0.085f), palette.Frame);
            Box(pole, "LampArm", new Vector3(-0.4f, 4.4f, 0f), new Vector3(0.8f, 0.05f, 0.05f), palette.Frame);
            Box(pole, "LampFixture", new Vector3(-0.8f, 4.4f, 0f), new Vector3(0.3f, 0.06f, 0.17f), palette.Window);
            NewLight(pole, "CoolStreetPool", new Vector3(-0.8f, 4.2f, 0f), LightType.Point, new Color(0.45f, 0.68f, 1f), 3f, 7f);
        }

        private static Light NewLight(Transform parent, string name, Vector3 position, LightType type, Color color, float intensity, float range)
        {
            Light light = Anchor(parent, name, position).gameObject.AddComponent<Light>();
            light.type = type; light.color = color; light.intensity = intensity; light.range = range; light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Realtime; return light;
        }

        private static void Customer(Transform parent, string name, Vector3 position, Material material)
        {
            Transform customer = Anchor(parent, name, position);
            customer.gameObject.AddComponent<PlaceholderAsset>().Configure("DEC-010 customer art", "Customer art pending", "1.7m capsule placeholder. No AI or workflow logic.");
            Primitive(customer, "CustomerBody", PrimitiveType.Capsule, new Vector3(0f, 0.85f, 0f), new Vector3(0.4f, 0.85f, 0.4f), material, false);
            Sphere(customer, "Head", new Vector3(0f, 1.52f, 0f), Vector3.one * 0.24f, material);
        }

        private static Transform Anchor(Transform parent, string name, Vector3 position)
        {
            var child = new GameObject(name).transform; child.SetParent(parent, false); child.localPosition = position; return child;
        }

        private static GameObject Nested(string path, Transform parent, Vector3 position)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) { throw new InvalidOperationException("Missing canonical prefab: " + path); }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            instance.transform.localPosition = position; return instance;
        }

        private static void Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool solid = false)
        {
            Primitive(parent, name, PrimitiveType.Cube, position, scale, material, solid);
        }

        private static void Sphere(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            Primitive(parent, name, PrimitiveType.Sphere, position, scale, material, false);
        }

        private static GameObject Cylinder(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            return Primitive(parent, name, PrimitiveType.Cylinder, position, scale, material, false);
        }

        private static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, bool solid)
        {
            GameObject primitive = GameObject.CreatePrimitive(type); primitive.name = name; primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = position; primitive.transform.localScale = scale;
            primitive.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) { Object.DestroyImmediate(primitive.GetComponent<Collider>()); }
            return primitive;
        }

        private static void Text(Transform parent, string name, string content, Vector3 position, float characterSize, Material material, bool faceFront)
        {
            Transform root = Anchor(parent, name, position);
            if (faceFront) { root.localRotation = Quaternion.Euler(0f, 180f, 0f); }
            var text = root.gameObject.AddComponent<TextMesh>();
            text.text = content; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 48; text.characterSize = characterSize * 0.15f; text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center; text.color = material.GetColor("_BaseColor");
            root.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        }

        private static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/'); if (AssetDatabase.IsValidFolder(folder)) { return; }
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/'); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private sealed class Palette
        {
            public readonly Material Pavement = Material("Pavement", new Color(0.33f, 0.35f, 0.35f));
            public readonly Material Concrete = Material("Concrete", new Color(0.49f, 0.50f, 0.48f));
            public readonly Material Joint = Material("PavingJoint", new Color(0.24f, 0.26f, 0.26f));
            public readonly Material Asphalt = Material("Asphalt", new Color(0.085f, 0.11f, 0.14f), 0f, 0.38f);
            public readonly Material Lane = Material("LanePaint", new Color(0.73f, 0.74f, 0.64f));
            public readonly Material StallGreen = Material("StallGreen", new Color(0.10f, 0.25f, 0.19f));
            public readonly Material Roof = Material("Roof", new Color(0.14f, 0.31f, 0.24f));
            public readonly Material Frame = Material("Frame", new Color(0.08f, 0.10f, 0.11f), 0.35f, 0.22f);
            public readonly Material Steel = Material("Steel", new Color(0.64f, 0.67f, 0.66f), 0.72f, 0.48f);
            public readonly Material Sign = Material("NewSignFace", new Color(0.95f, 0.92f, 0.79f), 0f, 0.12f, 0.3f);
            public readonly Material Mustard = Material("Mustard", new Color(0.95f, 0.63f, 0.12f));
            public readonly Material Ink = Material("Ink", new Color(0.08f, 0.09f, 0.075f));
            public readonly Material Menu = Material("MenuBoard", new Color(0.055f, 0.10f, 0.08f));
            public readonly Material Red = Material("PlasticRed", new Color(0.65f, 0.12f, 0.09f));
            public readonly Material Customer = Material("CustomerTeal", new Color(0.14f, 0.51f, 0.46f));
            public readonly Material CustomerTwo = Material("CustomerAmber", new Color(0.61f, 0.39f, 0.16f));
            public readonly Material Vehicle = Material("Vehicle", new Color(0.17f, 0.24f, 0.34f), 0.25f, 0.35f);
            public readonly Material Facade = Material("Facade", new Color(0.26f, 0.28f, 0.31f));
            public readonly Material FacadeTwo = Material("FacadeTwo", new Color(0.33f, 0.28f, 0.23f));
            public readonly Material Shutter = Material("Shutter", new Color(0.21f, 0.26f, 0.28f), 0.3f, 0.22f);
            public readonly Material Terracotta = Material("PlantPot", new Color(0.45f, 0.24f, 0.15f));
            public readonly Material Foliage = Material("Foliage", new Color(0.065f, 0.19f, 0.12f));
            public readonly Material Glow = Material("WarmBulb", new Color(1f, 0.75f, 0.37f), 0f, 0.1f, 2.2f);
            public readonly Material Window = Material("WindowGlow", new Color(0.67f, 0.74f, 0.8f), 0f, 0.2f, 0.6f);

            private static Material Material(string name, Color color, float metallic = 0f, float smoothness = 0.12f, float emission = 0f)
            {
                string path = MaterialFolder + "/MAT_Accel_" + name + ".mat";
                Material existing = AssetDatabase.LoadAssetAtPath<Material>(path); if (existing != null) { return existing; }
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) { throw new InvalidOperationException("Pinned URP Lit shader is unavailable."); }
                var material = new Material(shader) { name = "MAT_Accel_" + name };
                material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", smoothness);
                if (emission > 0f)
                {
                    material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * emission);
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                AssetDatabase.CreateAsset(material, path); return material;
            }
        }
    }
}
