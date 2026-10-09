using System;
using System.IO;
using TramChanh.Core.GroundTruth;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TramChanh.EditorTools
{
    public static class CakeEquipmentBuilder
    {
        public const string TexturesFolder = "Assets/TramChanh/Art/Textures";
        public const string MaterialsFolder = "Assets/TramChanh/Art/Materials";
        public const string ModelsFolder = "Assets/TramChanh/Art/Models/CakeStation";
        public const string PropsModelsFolder = "Assets/TramChanh/Art/Models/Props";
        public const string FoodModelsFolder = "Assets/TramChanh/Art/Models/Food";

        public const string GrillPrefabPath = "Assets/TramChanh/Prefabs/Workstations/PF_Grill_Elmich.prefab";
        public const string CupPrefabPath = "Assets/TramChanh/Prefabs/Tools/PF_BatterMeasureCup_500ml.prefab";
        public const string SauceBagPrefabPath = "Assets/TramChanh/Prefabs/Tools/PF_SauceBag.prefab";
        public const string SpatulaPrefabPath = "Assets/TramChanh/Prefabs/Tools/PF_Spatula.prefab";
        public const string ScissorsPrefabPath = "Assets/TramChanh/Prefabs/Tools/PF_Scissors.prefab";
        public const string RollCakePrefabPath = "Assets/TramChanh/Prefabs/Food/PF_RollCake_Baked.prefab";

        public const string PreviewScenePath = "Assets/TramChanh/Scenes/Art/SCN_Art_CakePreview.unity";
        public const string ScreenshotsFolder = "QA/Screenshots/Cake_HighFidelity";

        [MenuItem("Tram Chanh/Art/Build High-Fidelity Cake Equipment")]
        public static void BuildAndCapture()
        {
            Debug.Log("[TramChanh] Starting Cake Equipment High-Fidelity Reconstruction...");
            CreatePbrMaterials();
            UpgradeGrillPrefab();
            CreateToolPrefabs();
            CreateFoodPrefabs();
            CreatePreviewScene();
            CaptureScreenshots();
            Debug.Log("[TramChanh] Cake Equipment Reconstruction Complete!");
        }

        public static void CreatePbrMaterials()
        {
            EnsureFolder(MaterialsFolder);
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // 1. Grill Matte Black Phenolic Body
            Material matBody = LoadOrCreateMaterial("MAT_Grill_Elmich_Black", urpLit);
            matBody.SetColor("_BaseColor", new Color(0.12f, 0.13f, 0.14f));
            matBody.SetFloat("_Smoothness", 0.42f);
            matBody.SetFloat("_Metallic", 0.05f);
            EditorUtility.SetDirty(matBody);

            // 2. Ribbed Cast Non-Stick Plates
            Material matPlate = LoadOrCreateMaterial("MAT_Grill_Plate_Ribbed", urpLit);
            matPlate.SetColor("_BaseColor", new Color(0.15f, 0.16f, 0.17f));
            matPlate.SetFloat("_Smoothness", 0.55f);
            matPlate.SetFloat("_Metallic", 0.85f);
            EditorUtility.SetDirty(matPlate);

            // 3. Digital Display with 160°C LED readout
            Material matDisplay = LoadOrCreateMaterial("MAT_Grill_Display_Digital", urpLit);
            Texture2D dispTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Grill_Display_BaseColor.png");
            Texture2D dispEm = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Grill_Display_Emission.png");
            if (dispTex != null) matDisplay.SetTexture("_BaseColorMap", dispTex);
            if (dispEm != null)
            {
                matDisplay.SetTexture("_EmissionMap", dispEm);
                matDisplay.SetColor("_EmissionColor", Color.white);
                matDisplay.EnableKeyword("_EMISSION");
            }
            matDisplay.SetFloat("_Smoothness", 0.90f);
            EditorUtility.SetDirty(matDisplay);

            // 4. Polished Stainless Steel Accents
            Material matSteel = LoadOrCreateMaterial("MAT_Grill_StainlessAccents", urpLit);
            matSteel.SetColor("_BaseColor", new Color(0.88f, 0.90f, 0.92f));
            matSteel.SetFloat("_Metallic", 0.95f);
            matSteel.SetFloat("_Smoothness", 0.85f);
            EditorUtility.SetDirty(matSteel);

            // 5. Translucent Polypropylene Measuring Cup
            Material matCup = LoadOrCreateMaterial("MAT_MeasuringCup_Translucent", urpLit);
            matCup.SetColor("_BaseColor", new Color(0.92f, 0.95f, 0.98f, 0.50f));
            matCup.SetFloat("_Surface", 1f); // Transparent
            matCup.SetFloat("_Smoothness", 0.80f);
            matCup.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(matCup);

            // 6. Creamy Yellow Cake Batter
            Material matBatter = LoadOrCreateMaterial("MAT_Batter_Yellow", urpLit);
            matBatter.SetColor("_BaseColor", new Color(0.95f, 0.82f, 0.38f));
            matBatter.SetFloat("_Smoothness", 0.72f);
            EditorUtility.SetDirty(matBatter);

            // 7. Rich Sweet Sauce
            Material matSauce = LoadOrCreateMaterial("MAT_Sauce_Brown", urpLit);
            matSauce.SetColor("_BaseColor", new Color(0.24f, 0.12f, 0.06f));
            matSauce.SetFloat("_Smoothness", 0.82f);
            EditorUtility.SetDirty(matSauce);

            // 8. Scissors Red Plastic Handles
            Material matRedHandle = LoadOrCreateMaterial("MAT_Scissors_Red", urpLit);
            matRedHandle.SetColor("_BaseColor", new Color(0.85f, 0.15f, 0.12f));
            matRedHandle.SetFloat("_Smoothness", 0.60f);
            EditorUtility.SetDirty(matRedHandle);

            // 9. Toasted Rolled Cake (Bánh Lăn Nướng)
            Material matCake = LoadOrCreateMaterial("MAT_RollCake_Baked", urpLit);
            Texture2D cakeTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_RollCake_BaseColor.png");
            if (cakeTex != null) matCake.SetTexture("_BaseColorMap", cakeTex);
            matCake.SetColor("_BaseColor", new Color(0.92f, 0.70f, 0.40f));
            matCake.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(matCake);

            // 10. Kraft Wrapping Paper
            Material matKraft = LoadOrCreateMaterial("MAT_KraftPaper_Wrapper", urpLit);
            Texture2D kraftTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_KraftPaper_BaseColor.png");
            if (kraftTex != null) matKraft.SetTexture("_BaseColorMap", kraftTex);
            matKraft.SetColor("_BaseColor", new Color(0.78f, 0.62f, 0.44f));
            matKraft.SetFloat("_Smoothness", 0.18f);
            EditorUtility.SetDirty(matKraft);

            AssetDatabase.SaveAssets();
        }

        public static void UpgradeGrillPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrillPrefabPath);
            if (prefab == null) return;

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

            Mesh baseMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_Grill_Elmich_Base.obj");
            Mesh lidMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_Grill_Elmich_Lid.obj");

            Material matBody = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Grill_Elmich_Black.mat");
            Material matPlate = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Grill_Plate_Ribbed.mat");
            Material matDisplay = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Grill_Display_Digital.mat");
            Material matSteel = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Grill_StainlessAccents.mat");

            // Replace Base visual
            Transform baseTransform = root.transform.Find("Base");
            if (baseTransform != null)
            {
                ReplaceVisualMesh(baseTransform, baseMesh, matBody);

                // Lower plate
                Transform lowerPlate = baseTransform.Find("LowerPlate");
                if (lowerPlate != null) ReplaceVisualMesh(lowerPlate, baseMesh, matPlate);

                // Display
                Transform display = baseTransform.Find("Display");
                if (display != null) ReplaceVisualMesh(display, baseMesh, matDisplay);
            }

            // Replace LidPivot / Lid visual
            Transform lidPivot = root.transform.Find("LidPivot");
            if (lidPivot != null)
            {
                Transform lid = lidPivot.Find("Lid");
                if (lid != null) ReplaceVisualMesh(lid, lidMesh, matBody);

                Transform upperPlate = lidPivot.Find("UpperPlate");
                if (upperPlate != null) ReplaceVisualMesh(upperPlate, lidMesh, matPlate);

                Transform handle = lidPivot.Find("Handle");
                if (handle != null) ReplaceVisualMesh(handle, lidMesh, matSteel);
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[TramChanh] Upgraded Grill Prefab: " + GrillPrefabPath);
        }

        public static void CreateToolPrefabs()
        {
            EnsureFolder(Path.GetDirectoryName(CupPrefabPath));

            // 1. Measuring Cup 500ml
            Mesh cupMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{PropsModelsFolder}/SM_BatterMeasureCup_500ml.obj");
            Material cupMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_MeasuringCup_Translucent.mat");
            CreateOrUpdatePrefab(CupPrefabPath, "PF_BatterMeasureCup_500ml", cupMesh, cupMat, new Vector3(0.10f, 0.14f, 0.10f));

            // 2. Sauce Bag
            Mesh sauceMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{PropsModelsFolder}/SM_SauceBag.obj");
            Material sauceMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Sauce_Brown.mat");
            CreateOrUpdatePrefab(SauceBagPrefabPath, "PF_SauceBag", sauceMesh, sauceMat, new Vector3(0.08f, 0.22f, 0.08f));

            // 3. Spatula
            Mesh spatMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{PropsModelsFolder}/SM_Spatula.obj");
            Material steelMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Grill_StainlessAccents.mat");
            CreateOrUpdatePrefab(SpatulaPrefabPath, "PF_Spatula", spatMesh, steelMat, new Vector3(0.04f, 0.05f, 0.32f));

            // 4. Scissors
            Mesh scisMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{PropsModelsFolder}/SM_Scissors.obj");
            Material redMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Scissors_Red.mat");
            CreateOrUpdatePrefab(ScissorsPrefabPath, "PF_Scissors", scisMesh, redMat, new Vector3(0.08f, 0.02f, 0.21f));
        }

        public static void CreateFoodPrefabs()
        {
            EnsureFolder(Path.GetDirectoryName(RollCakePrefabPath));

            // Baked Roll Cake with Kraft Paper wrapper
            Mesh cakeMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{FoodModelsFolder}/SM_RollCake_Baked.obj");
            Mesh wrapMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{FoodModelsFolder}/SM_RollCake_Wrapper.obj");
            Material cakeMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_RollCake_Baked.mat");
            Material wrapMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_KraftPaper_Wrapper.mat");

            var root = new GameObject("PF_RollCake_Baked");
            root.layer = TramChanhLayers.EnvironmentIndex;

            var cakeObj = new GameObject("Cake");
            cakeObj.transform.SetParent(root.transform, false);
            var mf1 = cakeObj.AddComponent<MeshFilter>();
            mf1.sharedMesh = cakeMesh;
            var mr1 = cakeObj.AddComponent<MeshRenderer>();
            mr1.sharedMaterial = cakeMat;

            var wrapObj = new GameObject("Wrapper");
            wrapObj.transform.SetParent(root.transform, false);
            var mf2 = wrapObj.AddComponent<MeshFilter>();
            mf2.sharedMesh = wrapMesh;
            var mr2 = wrapObj.AddComponent<MeshRenderer>();
            mr2.sharedMaterial = wrapMat;

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.028f, 0f);
            col.size = new Vector3(0.06f, 0.06f, 0.18f);

            PrefabUtility.SaveAsPrefabAsset(root, RollCakePrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[TramChanh] Created Baked Roll Cake Prefab: " + RollCakePrefabPath);
        }

        private static void CreateOrUpdatePrefab(string prefabPath, string name, Mesh mesh, Material mat, Vector3 colSize)
        {
            var root = new GameObject(name);
            root.layer = TramChanhLayers.EnvironmentIndex;
            var mf = root.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = root.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, colSize.y / 2f, 0f);
            col.size = colSize;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[TramChanh] Created/Updated Prefab: {prefabPath}");
        }

        private static void ReplaceVisualMesh(Transform target, Mesh newMesh, Material newMat)
        {
            for (int i = target.childCount - 1; i >= 0; i--)
            {
                Transform child = target.GetChild(i);
                if (child.name.Contains("Placeholder") || child.name.Contains("Cube"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            var mf = target.GetComponent<MeshFilter>();
            if (mf == null) mf = target.gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = newMesh;

            var mr = target.GetComponent<MeshRenderer>();
            if (mr == null) mr = target.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = newMat;
        }

        public static void CreatePreviewScene()
        {
            EnsureFolder(Path.GetDirectoryName(PreviewScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting setup
            var lighting = new GameObject("Lighting");
            var keyLight = new GameObject("KeyLight").AddComponent<Light>();
            keyLight.transform.SetParent(lighting.transform);
            keyLight.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1.0f, 0.95f, 0.85f);
            keyLight.intensity = 1.6f;

            var fillLight = new GameObject("FillLight").AddComponent<Light>();
            fillLight.transform.SetParent(lighting.transform);
            fillLight.transform.rotation = Quaternion.Euler(30f, 140f, 0f);
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.6f, 0.75f, 0.95f);
            fillLight.intensity = 0.8f;

            // Countertop stage
            var stage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stage.name = "Countertop_Stage";
            stage.transform.position = new Vector3(0f, -0.02f, 0f);
            stage.transform.localScale = new Vector3(2.0f, 0.04f, 1.0f);

            // Instantiate Grill
            GameObject grillPf = AssetDatabase.LoadAssetAtPath<GameObject>(GrillPrefabPath);
            if (grillPf != null)
            {
                var grill = (GameObject)PrefabUtility.InstantiatePrefab(grillPf);
                grill.transform.position = new Vector3(-0.25f, 0f, 0.05f);
            }

            // Instantiate Tools & Baked Cake
            GameObject cupPf = AssetDatabase.LoadAssetAtPath<GameObject>(CupPrefabPath);
            if (cupPf != null)
            {
                var cup = (GameObject)PrefabUtility.InstantiatePrefab(cupPf);
                cup.transform.position = new Vector3(0.18f, 0f, 0.15f);
            }

            GameObject cakePf = AssetDatabase.LoadAssetAtPath<GameObject>(RollCakePrefabPath);
            if (cakePf != null)
            {
                var cake = (GameObject)PrefabUtility.InstantiatePrefab(cakePf);
                cake.transform.position = new Vector3(0.32f, 0f, -0.05f);
                cake.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            }

            GameObject spatPf = AssetDatabase.LoadAssetAtPath<GameObject>(SpatulaPrefabPath);
            if (spatPf != null)
            {
                var spat = (GameObject)PrefabUtility.InstantiatePrefab(spatPf);
                spat.transform.position = new Vector3(0.15f, 0f, -0.15f);
            }

            GameObject scisPf = AssetDatabase.LoadAssetAtPath<GameObject>(ScissorsPrefabPath);
            if (scisPf != null)
            {
                var scis = (GameObject)PrefabUtility.InstantiatePrefab(scisPf);
                scis.transform.position = new Vector3(0.35f, 0f, -0.22f);
            }

            EditorSceneManager.SaveScene(scene, PreviewScenePath);
            Debug.Log("[TramChanh] Saved Cake Preview Scene: " + PreviewScenePath);
        }

        public static void CaptureScreenshots()
        {
            EnsureFolder(ScreenshotsFolder);
            var scene = EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Single);

            var camObj = new GameObject("QACamera");
            var cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 50f;

            // 1. Close-up on Elmich Contact Grill
            camObj.transform.position = new Vector3(-0.25f, 0.32f, -0.42f);
            camObj.transform.rotation = Quaternion.Euler(32f, 0f, 0f);
            CaptureCameraToFile(cam, $"{ScreenshotsFolder}/Cake_Grill_Detail.png", 1920, 1080);

            // 2. Overview of Cake Preparation Station
            camObj.transform.position = new Vector3(0.05f, 0.50f, -0.75f);
            camObj.transform.rotation = Quaternion.Euler(32f, 0f, 0f);
            CaptureCameraToFile(cam, $"{ScreenshotsFolder}/Cake_Station_Overview.png", 1920, 1080);

            Object.DestroyImmediate(camObj);
        }

        private static void CaptureCameraToFile(Camera cam, string filePath, int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            File.WriteAllBytes(filePath, bytes);
            Debug.Log($"[TramChanh] Saved Screenshot: {filePath} ({bytes.Length / 1024} KB)");
        }

        private static Material LoadOrCreateMaterial(string matName, Shader shader)
        {
            string path = $"{MaterialsFolder}/{matName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
