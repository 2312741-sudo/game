using System;
using System.IO;
using TramChanh.Core.GroundTruth;
using TramChanh.Core.Provisional;
using TramChanh.Stall.Anchors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TramChanh.EditorTools
{
    /// <summary>
    /// High-Fidelity 3D Builder for the real Vietnamese Trạm Chanh stall (ART-STALL-002).
    /// Assembles PBR materials, textured meshes, light fixtures, colliders and anchors into
    /// production prefabs, and generates multi-angle validation screenshots in an authentic night scene.
    /// </summary>
    public static class StallHighFidelityBuilder
    {
        public const string TexturesFolder = "Assets/TramChanh/Art/Textures";
        public const string MaterialsFolder = "Assets/TramChanh/Art/Materials";
        public const string ModelsFolder = "Assets/TramChanh/Art/Models/Stall";
        public const string BrandingModelsFolder = "Assets/TramChanh/Art/Models/Branding";
        public const string LightingModelsFolder = "Assets/TramChanh/Art/Models/Lighting";
        public const string StallPrefabPath = "Assets/TramChanh/Prefabs/Stall/PF_Stall_TramChanh.prefab";
        public const string SignPrefabPath = "Assets/TramChanh/Prefabs/Stall/PF_Sign_TramChanh_New.prefab";
        public const string EdisonBulbPrefabPath = "Assets/TramChanh/Prefabs/Stall/PF_EdisonBulb.prefab";
        public const string LedStripPrefabPath = "Assets/TramChanh/Prefabs/Stall/PF_LEDStrip.prefab";
        public const string PreviewScenePath = "Assets/TramChanh/Scenes/Art/SCN_Art_StallPreview.unity";
        public const string ScreenshotsFolder = "QA/Screenshots/Stall_HighFidelity";

        [MenuItem("Tram Chanh/Art/Build High-Fidelity Stall + Capture Screenshots")]
        public static void BuildAndCaptureScreenshots()
        {
            Debug.Log("[TramChanh] Starting High-Fidelity Stall Reconstruction...");
            ConfigureTextureImporters();
            ConfigureModelImporters();
            CreatePbrMaterials();
            CreateSignPrefab();
            CreateLightingPrefabs();
            CreateStallPrefab();
            CreatePreviewScene();
            CaptureScreenshots();
            Debug.Log("[TramChanh] High-Fidelity Stall Reconstruction & QA Screenshots Complete!");
        }

        public static void ConfigureTextureImporters()
        {
            EnsureFolder(TexturesFolder);

            SetTextureType($"{TexturesFolder}/T_Wood_DarkCounter_Normal.png", TextureImporterType.NormalMap, false);
            SetTextureType($"{TexturesFolder}/T_CorrugatedMetal_Normal.png", TextureImporterType.NormalMap, false);

            SetTextureType($"{TexturesFolder}/T_Wood_DarkCounter_MaskMap.png", TextureImporterType.Default, false);
            SetTextureType($"{TexturesFolder}/T_CorrugatedMetal_MaskMap.png", TextureImporterType.Default, false);
            SetTextureType($"{TexturesFolder}/T_Sign_TramChanh_New_Emission.png", TextureImporterType.Default, false);

            AssetDatabase.SaveAssets();
        }

        private static void SetTextureType(string path, TextureImporterType type, bool sRgb)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool dirty = false;
            if (importer.textureType != type)
            {
                importer.textureType = type;
                dirty = true;
            }
            if (importer.sRGBTexture != sRgb)
            {
                importer.sRGBTexture = sRgb;
                dirty = true;
            }
            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        public static void ConfigureModelImporters()
        {
            string[] modelPaths =
            {
                $"{ModelsFolder}/SM_Stall_Base.obj",
                $"{ModelsFolder}/SM_Stall_Counter.obj",
                $"{ModelsFolder}/SM_Stall_Frame.obj",
                $"{ModelsFolder}/SM_Stall_Roof.obj",
                $"{ModelsFolder}/SM_Stall_CasterWheel.obj",
                $"{BrandingModelsFolder}/SM_Sign_TramChanh_New.obj",
                $"{LightingModelsFolder}/SM_EdisonBulb.obj",
                $"{LightingModelsFolder}/SM_LEDStrip.obj",
            };

            foreach (string path in modelPaths)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;

                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.globalScale = 1.0f;
                importer.importNormals = ModelImporterNormals.Import;
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
        }

        public static void CreatePbrMaterials()
        {
            EnsureFolder(MaterialsFolder);
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // 1. Sign Material
            Material matSign = LoadOrCreateMaterial("MAT_Sign_TramChanh_New", urpLit);
            Texture2D signBase = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Sign_TramChanh_New_BaseColor.png");
            Texture2D signEmission = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Sign_TramChanh_New_Emission.png");
            if (signBase != null) matSign.SetTexture("_BaseMap", signBase);
            if (signEmission != null)
            {
                matSign.SetTexture("_EmissionMap", signEmission);
                matSign.SetColor("_EmissionColor", Color.white * 2.2f);
                matSign.EnableKeyword("_EMISSION");
            }
            matSign.SetFloat("_Smoothness", 0.75f);
            matSign.SetFloat("_Metallic", 0.1f);
            EditorUtility.SetDirty(matSign);

            // 2. Dark Wood Countertop Material
            Material matWood = LoadOrCreateMaterial("MAT_Stall_WoodCounter", urpLit);
            Texture2D woodBase = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Wood_DarkCounter_BaseColor.png");
            Texture2D woodNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Wood_DarkCounter_Normal.png");
            Texture2D woodMask = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Wood_DarkCounter_MaskMap.png");
            if (woodBase != null) matWood.SetTexture("_BaseMap", woodBase);
            if (woodNormal != null)
            {
                matWood.SetTexture("_BumpMap", woodNormal);
                matWood.EnableKeyword("_NORMALMAP");
            }
            if (woodMask != null) matWood.SetTexture("_MetallicGlossMap", woodMask);
            matWood.SetFloat("_Smoothness", 0.45f);
            matWood.SetFloat("_Metallic", 0.05f);
            EditorUtility.SetDirty(matWood);

            // 3. Corrugated Metal Material
            Material matCorrugated = LoadOrCreateMaterial("MAT_Stall_CorrugatedMetal", urpLit);
            Texture2D corrBase = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_CorrugatedMetal_BaseColor.png");
            Texture2D corrNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_CorrugatedMetal_Normal.png");
            Texture2D corrMask = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_CorrugatedMetal_MaskMap.png");
            if (corrBase != null) matCorrugated.SetTexture("_BaseMap", corrBase);
            if (corrNormal != null)
            {
                matCorrugated.SetTexture("_BumpMap", corrNormal);
                matCorrugated.EnableKeyword("_NORMALMAP");
            }
            if (corrMask != null) matCorrugated.SetTexture("_MetallicGlossMap", corrMask);
            matCorrugated.SetFloat("_Smoothness", 0.35f);
            matCorrugated.SetFloat("_Metallic", 0.65f);
            EditorUtility.SetDirty(matCorrugated);

            // 4. Dark Structural Frame Material
            Material matFrame = LoadOrCreateMaterial("MAT_Stall_DarkFrame", urpLit);
            Texture2D frameBase = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_DarkFrame_BaseColor.png");
            if (frameBase != null) matFrame.SetTexture("_BaseMap", frameBase);
            matFrame.SetColor("_BaseColor", new Color(0.14f, 0.14f, 0.15f));
            matFrame.SetFloat("_Smoothness", 0.30f);
            matFrame.SetFloat("_Metallic", 0.40f);
            EditorUtility.SetDirty(matFrame);

            // 5. Caster Wheel Material
            Material matWheel = LoadOrCreateMaterial("MAT_Stall_CasterWheel", urpLit);
            matWheel.SetColor("_BaseColor", new Color(0.08f, 0.08f, 0.08f));
            matWheel.SetFloat("_Smoothness", 0.25f);
            matWheel.SetFloat("_Metallic", 0.30f);
            EditorUtility.SetDirty(matWheel);

            // 6. Edison Bulb Glass Material (Transparent)
            Material matGlass = LoadOrCreateMaterial("MAT_Stall_Bulb_Glass", urpLit);
            matGlass.SetFloat("_Surface", 1); // Transparent
            matGlass.SetFloat("_Blend", 0);   // Alpha
            matGlass.SetColor("_BaseColor", new Color(1.0f, 0.95f, 0.85f, 0.30f));
            matGlass.SetFloat("_Smoothness", 0.95f);
            matGlass.SetFloat("_Metallic", 0.05f);
            matGlass.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(matGlass);

            // 7. Edison Bulb Filament Material (Emissive)
            Material matFilament = LoadOrCreateMaterial("MAT_Stall_Bulb_Filament", urpLit);
            matFilament.SetColor("_BaseColor", new Color(1.0f, 0.75f, 0.3f));
            matFilament.SetColor("_EmissionColor", new Color(3.5f, 2.0f, 0.6f));
            matFilament.EnableKeyword("_EMISSION");
            matFilament.SetFloat("_Smoothness", 0.8f);
            EditorUtility.SetDirty(matFilament);

            // 8. LED Strip Material (Emissive)
            Material matLed = LoadOrCreateMaterial("MAT_Stall_LEDStrip", urpLit);
            matLed.SetColor("_BaseColor", new Color(1.0f, 0.95f, 0.85f));
            matLed.SetColor("_EmissionColor", new Color(2.5f, 2.2f, 1.8f));
            matLed.EnableKeyword("_EMISSION");
            matLed.SetFloat("_Smoothness", 0.7f);
            EditorUtility.SetDirty(matLed);

            // 9. Ground Wet Asphalt Material
            Material matAsphalt = LoadOrCreateMaterial("MAT_Street_WetAsphalt", urpLit);
            matAsphalt.SetColor("_BaseColor", new Color(0.08f, 0.09f, 0.10f));
            matAsphalt.SetFloat("_Smoothness", 0.82f); // Wet reflective surface
            matAsphalt.SetFloat("_Metallic", 0.15f);
            EditorUtility.SetDirty(matAsphalt);

            // 10. Sidewalk Concrete Material
            Material matSidewalk = LoadOrCreateMaterial("MAT_Street_Sidewalk", urpLit);
            matSidewalk.SetColor("_BaseColor", new Color(0.35f, 0.36f, 0.37f));
            matSidewalk.SetFloat("_Smoothness", 0.40f);
            matSidewalk.SetFloat("_Metallic", 0.05f);
            EditorUtility.SetDirty(matSidewalk);

            AssetDatabase.SaveAssets();
        }

        public static void CreateSignPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(SignPrefabPath));
            Mesh signMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{BrandingModelsFolder}/SM_Sign_TramChanh_New.obj");
            Material signMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Sign_TramChanh_New.mat");

            var root = new GameObject("PF_Sign_TramChanh_New");
            root.layer = TramChanhLayers.EnvironmentIndex;

            var visual = new GameObject("SM_Sign_TramChanh_New");
            visual.transform.SetParent(root.transform, false);
            visual.layer = TramChanhLayers.EnvironmentIndex;
            var mf = visual.AddComponent<MeshFilter>();
            mf.sharedMesh = signMesh;
            var mr = visual.AddComponent<MeshRenderer>();
            mr.sharedMaterial = signMat;

            // BoxCollider matching rear-center pivot (extends +Z forward)
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0f, 0.06f);
            col.size = new Vector3(1.66f, 0.30f, 0.12f);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, SignPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[TramChanh] Saved Final Sign Prefab: " + SignPrefabPath);
        }

        public static void CreateLightingPrefabs()
        {
            EnsureFolder(Path.GetDirectoryName(EdisonBulbPrefabPath));

            // Edison Bulb Prefab
            Mesh bulbMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{LightingModelsFolder}/SM_EdisonBulb.obj");
            Material bulbGlassMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Stall_Bulb_Glass.mat");
            Material bulbFilamentMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Stall_Bulb_Filament.mat");

            var bulbRoot = new GameObject("PF_EdisonBulb");
            bulbRoot.layer = TramChanhLayers.EnvironmentIndex;

            var bulbVisual = new GameObject("Visual");
            bulbVisual.transform.SetParent(bulbRoot.transform, false);
            bulbVisual.layer = TramChanhLayers.EnvironmentIndex;
            var bmf = bulbVisual.AddComponent<MeshFilter>();
            bmf.sharedMesh = bulbMesh;
            var bmr = bulbVisual.AddComponent<MeshRenderer>();
            bmr.sharedMaterials = new Material[] { bulbFilamentMat, bulbGlassMat };

            var lightObj = new GameObject("PointLight");
            lightObj.transform.SetParent(bulbRoot.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.82f, 0.55f); // 2700K warm Edison glow
            light.range = 3.5f;
            light.intensity = 2.8f;
            light.shadows = LightShadows.Soft;

            PrefabUtility.SaveAsPrefabAsset(bulbRoot, EdisonBulbPrefabPath);
            Object.DestroyImmediate(bulbRoot);

            // LED Strip Prefab
            Mesh ledMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{LightingModelsFolder}/SM_LEDStrip.obj");
            Material ledMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Stall_LEDStrip.mat");

            var ledRoot = new GameObject("PF_LEDStrip");
            ledRoot.layer = TramChanhLayers.EnvironmentIndex;
            var lmf = ledRoot.AddComponent<MeshFilter>();
            lmf.sharedMesh = ledMesh;
            var lmr = ledRoot.AddComponent<MeshRenderer>();
            lmr.sharedMaterial = ledMat;

            PrefabUtility.SaveAsPrefabAsset(ledRoot, LedStripPrefabPath);
            Object.DestroyImmediate(ledRoot);
        }

        public static void CreateStallPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(StallPrefabPath));

            Mesh baseMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_Stall_Base.obj");
            Mesh counterMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_Stall_Counter.obj");
            Mesh frameMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_Stall_Frame.obj");
            Mesh roofMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_Stall_Roof.obj");
            Mesh wheelMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_Stall_CasterWheel.obj");

            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Stall_WoodCounter.mat");
            Material corrMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Stall_CorrugatedMetal.mat");
            Material frameMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Stall_DarkFrame.mat");
            Material wheelMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Stall_CasterWheel.mat");

            GameObject signPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SignPrefabPath);
            GameObject bulbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EdisonBulbPrefabPath);
            GameObject ledPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LedStripPrefabPath);

            var root = new GameObject("PF_Stall_TramChanh");
            root.layer = TramChanhLayers.EnvironmentIndex;
            root.AddComponent<StallAnchorSet>();

            // 1. Structure
            var structureGroup = CreateGroup("Structure", root.transform);
            CreateMeshChild("SM_Stall_Base", structureGroup, baseMesh, corrMat);

            // 2. Counter (top at y = 1.00m)
            var counterGroup = CreateGroup("Counter", root.transform);
            CreateMeshChild("SM_Stall_Counter", counterGroup, counterMesh, woodMat);

            // 3. Frame (dark supporting beams + diagonal supports)
            var frameGroup = CreateGroup("Frame", root.transform);
            CreateMeshChild("SM_Stall_Frame", frameGroup, frameMesh, frameMat);

            // 4. Roof (dual slope corrugated metal canopy, peak y = 2.20m)
            var roofGroup = CreateGroup("Roof", root.transform);
            CreateMeshChild("SM_Stall_Roof", roofGroup, roofMesh, corrMat);

            // 5. Sign (mounted under front roof eaves proud of the frame)
            var signGroup = CreateGroup("Sign", root.transform);
            if (signPrefab != null)
            {
                var signInstance = (GameObject)PrefabUtility.InstantiatePrefab(signPrefab, signGroup);
                signInstance.name = "PF_Sign_TramChanh_New";
                signInstance.transform.localPosition = new Vector3(0f, 1.85f, 0.38f);
                signInstance.transform.localRotation = Quaternion.identity;
            }

            // 6. Lights (4 Edison bulbs + 1 LED strip)
            var lightsGroup = CreateGroup("Lights", root.transform);
            if (bulbPrefab != null)
            {
                float[] bulbX = new float[] { -0.55f, -0.18f, 0.18f, 0.55f };
                for (int i = 0; i < bulbX.Length; i++)
                {
                    var bulb = (GameObject)PrefabUtility.InstantiatePrefab(bulbPrefab, lightsGroup);
                    bulb.name = $"PF_EdisonBulb_{i}";
                    bulb.transform.localPosition = new Vector3(bulbX[i], 1.92f, 0.15f);
                }
            }
            if (ledPrefab != null)
            {
                var led = (GameObject)PrefabUtility.InstantiatePrefab(ledPrefab, lightsGroup);
                led.name = "PF_LEDStrip";
                led.transform.localPosition = new Vector3(0f, 1.68f, 0.40f);
            }

            // 7. Wheels (4 caster wheels at corner offsets)
            var wheelsGroup = CreateGroup("Wheels", root.transform);
            float wx = 0.80f;
            float wz = 0.30f;
            int wheelIdx = 0;
            foreach (float sx in new[] { -1f, 1f })
            {
                foreach (float sz in new[] { -1f, 1f })
                {
                    var wheelObj = CreateMeshChild($"SM_Stall_CasterWheel_{wheelIdx++}", wheelsGroup, wheelMesh, wheelMat);
                    wheelObj.transform.localPosition = new Vector3(sx * wx, 0.10f, sz * wz);
                }
            }

            // 8. Colliders (Compound BoxColliders conforming to GT-001)
            var collidersGroup = CreateGroup("Colliders", root.transform);
            AddBoxCollider(collidersGroup, "Body", new Vector3(0f, 0.50f, 0f), new Vector3(1.80f, 1.00f, 0.80f));
            AddBoxCollider(collidersGroup, "Roof", new Vector3(0f, 2.13f, 0f), new Vector3(1.80f, 0.14f, 0.80f));
            AddBoxCollider(collidersGroup, "Post_FL", new Vector3(-0.85f, 1.50f, 0.35f), new Vector3(0.06f, 1.00f, 0.06f));
            AddBoxCollider(collidersGroup, "Post_FR", new Vector3(0.85f, 1.50f, 0.35f), new Vector3(0.06f, 1.00f, 0.06f));
            AddBoxCollider(collidersGroup, "Post_BL", new Vector3(-0.85f, 1.50f, -0.35f), new Vector3(0.06f, 1.00f, 0.06f));
            AddBoxCollider(collidersGroup, "Post_BR", new Vector3(0.85f, 1.50f, -0.35f), new Vector3(0.06f, 1.00f, 0.06f));

            // 9. Anchors (All 10 canonical stations)
            var anchorsGroup = CreateGroup("Anchors", root.transform);
            CreateAnchor(anchorsGroup, StallAnchorId.TeaRack, "TeaRackAnchor", new Vector3(-0.72f, 1.00f, -0.13f));
            CreateAnchor(anchorsGroup, StallAnchorId.Topping, "ToppingStationAnchor", new Vector3(-0.41f, 0.91f, -0.13f));
            CreateAnchor(anchorsGroup, StallAnchorId.IceBin, "IceBinAnchor", new Vector3(-0.10f, 0.82f, -0.13f));
            CreateAnchor(anchorsGroup, StallAnchorId.WipeArea, "WipeAreaAnchor", new Vector3(-0.15f, 1.00f, -0.15f));
            CreateAnchor(anchorsGroup, StallAnchorId.ReadyCounter, "ReadyCounterAnchor", new Vector3(0f, 1.00f, 0.25f));
            CreateAnchor(anchorsGroup, StallAnchorId.BatterArea, "BatterAreaAnchor", new Vector3(0.15f, 1.00f, -0.15f));
            CreateAnchor(anchorsGroup, StallAnchorId.Grill, "GrillAnchor", new Vector3(0.25f, 1.00f, -0.13f));
            CreateAnchor(anchorsGroup, StallAnchorId.RollArea, "RollAreaAnchor", new Vector3(0.52f, 1.00f, -0.15f));
            CreateAnchor(anchorsGroup, StallAnchorId.Sauce, "SauceAnchor", new Vector3(0.52f, 1.00f, -0.13f));
            CreateAnchor(anchorsGroup, StallAnchorId.Wrap, "WrappingAnchor", new Vector3(0.73f, 1.00f, -0.13f));

            SetLayerRecursively(root, TramChanhLayers.EnvironmentIndex);

            PrefabUtility.SaveAsPrefabAsset(root, StallPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[TramChanh] Saved High-Fidelity Stall Prefab: " + StallPrefabPath);
        }

        public static void CreatePreviewScene()
        {
            EnsureFolder(Path.GetDirectoryName(PreviewScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting setup: realistic nighttime urban environment
            var lighting = new GameObject("Lighting");
            var moonLightObj = new GameObject("Moonlight_Directional");
            moonLightObj.transform.SetParent(lighting.transform, false);
            moonLightObj.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            var moonLight = moonLightObj.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.color = new Color(0.6f, 0.7f, 0.9f); // Cool night sky fill
            moonLight.intensity = 0.35f;
            moonLight.shadows = LightShadows.Soft;

            var streetLightObj = new GameObject("StreetLight_Warm");
            streetLightObj.transform.SetParent(lighting.transform, false);
            streetLightObj.transform.position = new Vector3(3.5f, 4.5f, 4.0f);
            var streetLight = streetLightObj.AddComponent<Light>();
            streetLight.type = LightType.Point;
            streetLight.color = new Color(1.0f, 0.78f, 0.5f); // High-pressure sodium street lamp
            streetLight.range = 15f;
            streetLight.intensity = 2.0f;
            streetLight.shadows = LightShadows.Soft;

            var frontStreetLightObj = new GameObject("StreetLight_Front");
            frontStreetLightObj.transform.SetParent(lighting.transform, false);
            frontStreetLightObj.transform.position = new Vector3(-1.2f, 2.8f, 3.2f);
            var frontLight = frontStreetLightObj.AddComponent<Light>();
            frontLight.type = LightType.Point;
            frontLight.color = new Color(1.0f, 0.90f, 0.78f);
            frontLight.range = 10f;
            frontLight.intensity = 1.8f;
            frontLight.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.14f, 0.16f, 0.20f);

            // Environment: Road & Sidewalk
            var env = new GameObject("Environment");
            Material asphaltMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Street_WetAsphalt.mat");
            Material sidewalkMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Street_Sidewalk.mat");

            // Road plane (wet asphalt)
            var road = GameObject.CreatePrimitive(PrimitiveType.Plane);
            road.name = "WetAsphaltRoad";
            road.transform.SetParent(env.transform, false);
            road.transform.position = new Vector3(0f, -0.15f, 4.0f);
            road.transform.localScale = new Vector3(2.0f, 1.0f, 1.5f);
            road.GetComponent<Renderer>().sharedMaterial = asphaltMat;
            road.layer = TramChanhLayers.EnvironmentIndex;

            // Sidewalk slab
            var sidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sidewalk.name = "Sidewalk";
            sidewalk.transform.SetParent(env.transform, false);
            sidewalk.transform.position = new Vector3(0f, -0.075f, 0.2f);
            sidewalk.transform.localScale = new Vector3(10f, 0.15f, 3.5f);
            sidewalk.GetComponent<Renderer>().sharedMaterial = sidewalkMat;
            sidewalk.layer = TramChanhLayers.EnvironmentIndex;

            // Stall Instance
            GameObject stallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StallPrefabPath);
            if (stallPrefab != null)
            {
                var stallInstance = (GameObject)PrefabUtility.InstantiatePrefab(stallPrefab);
                stallInstance.transform.position = Vector3.zero;
                stallInstance.transform.rotation = Quaternion.identity;
            }

            // Customer Furniture for street atmosphere
            var customerArea = new GameObject("CustomerArea");
            GameObject stoolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/CustomerArea/PF_PlasticStool.prefab");
            GameObject tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/CustomerArea/PF_YellowCrateTable.prefab");
            if (tablePrefab != null)
            {
                var table = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab, customerArea.transform);
                table.transform.position = new Vector3(2.1f, 0f, 1.1f);
            }
            if (stoolPrefab != null)
            {
                var stool1 = (GameObject)PrefabUtility.InstantiatePrefab(stoolPrefab, customerArea.transform);
                stool1.transform.position = new Vector3(2.1f, 0f, 0.55f);
                var stool2 = (GameObject)PrefabUtility.InstantiatePrefab(stoolPrefab, customerArea.transform);
                stool2.transform.position = new Vector3(2.1f, 0f, 1.65f);
            }

            // Setup 4 Dedicated QA Validation Cameras
            var camerasRoot = new GameObject("QACameras");

            // 1. Front View Camera
            CreateCamera("Cam_Front", camerasRoot.transform,
                new Vector3(0f, 1.30f, 3.2f),
                new Vector3(0f, 1.25f, 0f), 45f);

            // 2. Side Profile Camera (showing A-frame truss & depth)
            CreateCamera("Cam_Side", camerasRoot.transform,
                new Vector3(-3.2f, 1.15f, 0f),
                new Vector3(0f, 1.10f, 0f), 45f);

            // 3. Elevated 45° Perspective View
            CreateCamera("Cam_Elevated", camerasRoot.transform,
                new Vector3(2.4f, 2.5f, 2.5f),
                new Vector3(0f, 1.05f, 0f), 45f);

            // 4. Operator Eye-Level View (first-person worker perspective)
            CreateCamera("Cam_Operator_EyeLevel", camerasRoot.transform,
                new Vector3(0f, 1.62f, -1.15f),
                new Vector3(0f, 0.95f, -0.05f), 65f);

            EditorSceneManager.SaveScene(scene, PreviewScenePath);
            Debug.Log("[TramChanh] Saved Art Preview Scene: " + PreviewScenePath);
        }

        private static Camera CreateCamera(string name, Transform parent, Vector3 position, Vector3 lookTarget, float fov)
        {
            var camObj = new GameObject(name);
            camObj.transform.SetParent(parent, false);
            camObj.transform.position = position;
            camObj.transform.LookAt(lookTarget);

            var cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.Color;
            cam.backgroundColor = new Color(0.03f, 0.03f, 0.05f);
            return cam;
        }

        public static void CaptureScreenshots()
        {
            if (!Directory.Exists(ScreenshotsFolder))
            {
                Directory.CreateDirectory(ScreenshotsFolder);
            }

            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            int width = 1920;
            int height = 1080;

            foreach (var cam in cameras)
            {
                string filename = cam.name switch
                {
                    "Cam_Front" => "Stall_Front_Validation.png",
                    "Cam_Side" => "Stall_Side_Validation.png",
                    "Cam_Elevated" => "Stall_Elevated_Validation.png",
                    "Cam_Operator_EyeLevel" => "Stall_Operator_EyeLevel_Validation.png",
                    _ => null
                };

                if (string.IsNullOrEmpty(filename)) continue;

                var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(rt);

                byte[] bytes = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);

                string outPath = Path.Combine(ScreenshotsFolder, filename);
                File.WriteAllBytes(outPath, bytes);
                Debug.Log($"[TramChanh] Saved Screenshot: {outPath} ({bytes.Length / 1024} KB)");
            }
        }

        private static Transform CreateGroup(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return go.transform;
        }

        private static GameObject CreateMeshChild(string name, Transform parent, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            return go;
        }

        private static void AddBoxCollider(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var holder = CreateGroup(name, parent);
            var col = holder.gameObject.AddComponent<BoxCollider>();
            col.center = center;
            col.size = size;
        }

        private static void CreateAnchor(Transform parent, StallAnchorId id, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.layer = parent.gameObject.layer;
            var anchor = go.AddComponent<StallAnchor>();
            anchor.Configure(id, false, "Real stall dimensions & equipment layout");
        }

        private static Material LoadOrCreateMaterial(string name, Shader shader)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = layer;
            }
        }

        private static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
