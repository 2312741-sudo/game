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
    public static class EnvironmentFurnitureBuilder
    {
        public const string TexturesFolder = "Assets/TramChanh/Art/Textures";
        public const string MaterialsFolder = "Assets/TramChanh/Art/Materials";
        public const string FurnitureModelsFolder = "Assets/TramChanh/Art/Models/Furniture";
        public const string EnvironmentModelsFolder = "Assets/TramChanh/Art/Models/Environment";

        public const string StoolPrefabPath = "Assets/TramChanh/Prefabs/CustomerArea/PF_PlasticStool.prefab";
        public const string CrateTablePrefabPath = "Assets/TramChanh/Prefabs/CustomerArea/PF_YellowCrateTable.prefab";
        public const string CrateTableDrinkWavePath = "Assets/TramChanh/Prefabs/Workstations/DrinkWave/PF_YellowCrateTable.prefab";
        public const string TrayPrefabPath = "Assets/TramChanh/Prefabs/CustomerArea/PF_StainlessTray.prefab";

        public const string PreviewScenePath = "Assets/TramChanh/Scenes/Art/SCN_Art_EnvironmentPreview.unity";
        public const string ScreenshotsFolder = "QA/Screenshots/Environment_HighFidelity";

        [MenuItem("Tram Chanh/Art/Build High-Fidelity Street Environment & Furniture")]
        public static void BuildAndCapture()
        {
            Debug.Log("[TramChanh] Starting Environment & Furniture High-Fidelity Reconstruction...");
            ConfigureTextureImporters();
            CreatePbrMaterials();
            UpgradeStoolPrefab();
            UpgradeCrateTablePrefab();
            CreateTrayPrefab();
            CreatePreviewScene();
            CaptureScreenshots();
            Debug.Log("[TramChanh] Environment & Furniture Reconstruction Complete!");
        }

        public static void ConfigureTextureImporters()
        {
            SetTextureType($"{TexturesFolder}/T_Asphalt_Night_MaskMap.png", TextureImporterType.Default, false);
            AssetDatabase.SaveAssets();
        }

        private static void SetTextureType(string path, TextureImporterType type, bool isSrgb)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = type;
                importer.sRGBTexture = isSrgb;
                importer.SaveAndReimport();
            }
        }

        public static Mesh LoadMesh(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Mesh direct = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (direct != null) return direct;

            var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var sub in subAssets)
            {
                if (sub is Mesh m) return m;
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model != null)
            {
                MeshFilter mf = model.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) return mf.sharedMesh;
            }

            Debug.LogError("[TramChanh] Failed to load mesh from path: " + path);
            return null;
        }

        public static void CreatePbrMaterials()
        {
            EnsureFolder(MaterialsFolder);
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // 1. Beige Plastic Stool Material (IMG_5228.JPG)
            Material matStool = LoadOrCreateMaterial("MAT_Plastic_BeigeStool", urpLit);
            Texture2D stoolTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Plastic_Beige_BaseColor.png");
            if (stoolTex != null) matStool.SetTexture("_BaseMap", stoolTex);
            matStool.SetColor("_BaseColor", new Color(0.84f, 0.78f, 0.67f));
            matStool.SetFloat("_Smoothness", 0.42f);
            matStool.SetFloat("_Metallic", 0.02f);
            EditorUtility.SetDirty(matStool);

            // 2. Yellow HDPE Crate Plastic Material (IMG_5231.JPG)
            Material matCrate = LoadOrCreateMaterial("MAT_Crate_YellowHDPE", urpLit);
            Texture2D crateTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Crate_Yellow_BaseColor.png");
            if (crateTex != null) matCrate.SetTexture("_BaseMap", crateTex);
            matCrate.SetColor("_BaseColor", new Color(0.90f, 0.69f, 0.09f));
            matCrate.SetFloat("_Smoothness", 0.52f);
            matCrate.SetFloat("_Metallic", 0.04f);
            EditorUtility.SetDirty(matCrate);

            // 3. Stainless Steel Serving Tray
            Material matTray = LoadOrCreateMaterial("MAT_Stainless_ServingTray", urpLit);
            matTray.SetColor("_BaseColor", new Color(0.85f, 0.88f, 0.90f));
            matTray.SetFloat("_Metallic", 0.95f);
            matTray.SetFloat("_Smoothness", 0.85f);
            EditorUtility.SetDirty(matTray);

            // 4. Sidewalk Paver Tiles
            Material matSidewalk = LoadOrCreateMaterial("MAT_Street_SidewalkTiles", urpLit);
            Texture2D swTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Sidewalk_Tiles_BaseColor.png");
            if (swTex != null) matSidewalk.SetTexture("_BaseMap", swTex);
            matSidewalk.SetColor("_BaseColor", new Color(0.70f, 0.71f, 0.72f));
            matSidewalk.SetFloat("_Smoothness", 0.35f);
            EditorUtility.SetDirty(matSidewalk);

            // 5. Wet Night Asphalt
            Material matAsphalt = LoadOrCreateMaterial("MAT_Street_WetAsphaltNight", urpLit);
            Texture2D aspTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Asphalt_Night_BaseColor.png");
            Texture2D aspMask = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Asphalt_Night_MaskMap.png");
            if (aspTex != null) matAsphalt.SetTexture("_BaseMap", aspTex);
            if (aspMask != null) matAsphalt.SetTexture("_MetallicGlossMap", aspMask);
            matAsphalt.SetColor("_BaseColor", new Color(0.10f, 0.11f, 0.12f));
            matAsphalt.SetFloat("_Metallic", 0.10f);
            matAsphalt.SetFloat("_Smoothness", 0.85f); // Wet reflection
            EditorUtility.SetDirty(matAsphalt);

            AssetDatabase.SaveAssets();
        }

        public static void UpgradeStoolPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StoolPrefabPath);
            if (prefab == null) return;

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

            Mesh stoolMesh = LoadMesh($"{FurnitureModelsFolder}/SM_PlasticStool.obj");
            Material stoolMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Plastic_BeigeStool.mat");

            // Remove primitive children under Seat and Legs
            Transform seat = root.transform.Find("Seat");
            if (seat != null)
            {
                seat.localPosition = Vector3.zero;
                for (int i = seat.childCount - 1; i >= 0; i--) Object.DestroyImmediate(seat.GetChild(i).gameObject);
                var mf = seat.GetComponent<MeshFilter>();
                if (mf == null) mf = seat.gameObject.AddComponent<MeshFilter>();
                mf.sharedMesh = stoolMesh;
                var mr = seat.GetComponent<MeshRenderer>();
                if (mr == null) mr = seat.gameObject.AddComponent<MeshRenderer>();
                mr.sharedMaterial = stoolMat;

                var col = seat.GetComponent<BoxCollider>();
                if (col == null) col = seat.gameObject.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.15f, 0f);
                col.size = new Vector3(0.30f, 0.30f, 0.30f);
            }

            // Remove legacy individual Leg primitives to avoid z-fighting
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = root.transform.GetChild(i);
                if (child.name.StartsWith("Leg"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            // Preserve SeatPoint
            Transform seatPoint = root.transform.Find("SeatPoint");
            if (seatPoint != null)
            {
                seatPoint.localPosition = new Vector3(0f, 0.30f, 0f);
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[TramChanh] Upgraded Stool Prefab: " + StoolPrefabPath);
        }

        public static void UpgradeCrateTablePrefab()
        {
            UpgradeCrateFile(CrateTablePrefabPath);
            if (File.Exists(CrateTableDrinkWavePath)) UpgradeCrateFile(CrateTableDrinkWavePath);
        }

        private static void UpgradeCrateFile(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            GameObject root = PrefabUtility.LoadPrefabContents(path);

            Mesh crateMesh = LoadMesh($"{FurnitureModelsFolder}/SM_YellowCrateTable.obj");
            Material crateMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Crate_YellowHDPE.mat");

            Transform crate = root.transform.Find("Crate");
            if (crate != null)
            {
                crate.localPosition = Vector3.zero;
                for (int i = crate.childCount - 1; i >= 0; i--) Object.DestroyImmediate(crate.GetChild(i).gameObject);
                var mf = crate.GetComponent<MeshFilter>();
                if (mf == null) mf = crate.gameObject.AddComponent<MeshFilter>();
                mf.sharedMesh = crateMesh;
                var mr = crate.GetComponent<MeshRenderer>();
                if (mr == null) mr = crate.gameObject.AddComponent<MeshRenderer>();
                mr.sharedMaterial = crateMat;

                var col = crate.GetComponent<BoxCollider>();
                if (col == null) col = crate.gameObject.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.16f, 0f);
                col.size = new Vector3(0.45f, 0.32f, 0.35f);
            }

            // TrayTop holds stainless tray mesh
            Transform trayTop = root.transform.Find("TrayTop");
            if (trayTop != null)
            {
                trayTop.localPosition = new Vector3(0f, 0.32f, 0f);
                Mesh trayMesh = LoadMesh($"{FurnitureModelsFolder}/SM_StainlessTray.obj");
                Material trayMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Stainless_ServingTray.mat");
                if (trayMesh != null && trayMat != null)
                {
                    var mf = trayTop.GetComponent<MeshFilter>();
                    if (mf == null) mf = trayTop.gameObject.AddComponent<MeshFilter>();
                    mf.sharedMesh = trayMesh;
                    var mr = trayTop.GetComponent<MeshRenderer>();
                    if (mr == null) mr = trayTop.gameObject.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = trayMat;

                    var col = trayTop.GetComponent<BoxCollider>();
                    if (col == null) col = trayTop.gameObject.AddComponent<BoxCollider>();
                    col.center = new Vector3(0f, 0.011f, 0f);
                    col.size = new Vector3(0.40f, 0.022f, 0.30f);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[TramChanh] Upgraded Crate Table Prefab: " + path);
        }

        public static void CreateTrayPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(TrayPrefabPath));

            Mesh trayMesh = LoadMesh($"{FurnitureModelsFolder}/SM_StainlessTray.obj");
            Material trayMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Stainless_ServingTray.mat");

            var root = new GameObject("PF_StainlessTray");
            root.layer = TramChanhLayers.EnvironmentIndex;

            var mf = root.AddComponent<MeshFilter>();
            mf.sharedMesh = trayMesh;
            var mr = root.AddComponent<MeshRenderer>();
            mr.sharedMaterial = trayMat;

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.011f, 0f);
            col.size = new Vector3(0.40f, 0.022f, 0.30f);

            PrefabUtility.SaveAsPrefabAsset(root, TrayPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[TramChanh] Created Stainless Tray Prefab: " + TrayPrefabPath);
        }

        public static void CreatePreviewScene()
        {
            EnsureFolder(Path.GetDirectoryName(PreviewScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting setup: realistic night ambient with warm sodium street light
            var lighting = new GameObject("Lighting");
            var keyLight = new GameObject("Streetlight").AddComponent<Light>();
            keyLight.transform.SetParent(lighting.transform);
            keyLight.transform.position = new Vector3(-0.6f, 2.8f, -0.3f);
            keyLight.type = LightType.Point;
            keyLight.range = 10f;
            keyLight.color = new Color(1.0f, 0.86f, 0.65f); // Warm sodium street lamp
            keyLight.intensity = 2.8f;
            keyLight.shadows = LightShadows.Soft;

            var fillLight = new GameObject("SkyFill").AddComponent<Light>();
            fillLight.transform.SetParent(lighting.transform);
            fillLight.transform.rotation = Quaternion.Euler(55f, -140f, 0f);
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.40f, 0.50f, 0.75f); // Deep blue night sky
            fillLight.intensity = 0.4f;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.18f, 0.20f, 0.25f);

            // Street Environment Ground (Sidewalk & Asphalt)
            Mesh swMesh = LoadMesh($"{EnvironmentModelsFolder}/SM_Sidewalk_Section.obj");
            Mesh rdMesh = LoadMesh($"{EnvironmentModelsFolder}/SM_Asphalt_Road.obj");
            Material swMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Street_SidewalkTiles.mat");
            Material rdMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Street_WetAsphaltNight.mat");

            var envRoot = new GameObject("Environment");
            if (swMesh != null)
            {
                var swObj = new GameObject("Sidewalk");
                swObj.transform.SetParent(envRoot.transform, false);
                swObj.transform.position = new Vector3(0f, 0f, 0f);
                swObj.AddComponent<MeshFilter>().sharedMesh = swMesh;
                swObj.AddComponent<MeshRenderer>().sharedMaterial = swMat;
            }
            if (rdMesh != null)
            {
                var rdObj = new GameObject("Roadway");
                rdObj.transform.SetParent(envRoot.transform, false);
                rdObj.transform.position = new Vector3(2.5f, 0f, 0f);
                rdObj.AddComponent<MeshFilter>().sharedMesh = rdMesh;
                rdObj.AddComponent<MeshRenderer>().sharedMaterial = rdMat;
            }

            // Customer Seating Setup (1 Yellow Crate Table + 4 Beige Stools)
            GameObject cratePf = AssetDatabase.LoadAssetAtPath<GameObject>(CrateTablePrefabPath);
            GameObject stoolPf = AssetDatabase.LoadAssetAtPath<GameObject>(StoolPrefabPath);

            var seatingRoot = new GameObject("CustomerSeating_Cluster");
            seatingRoot.transform.position = new Vector3(-0.6f, 0.15f, 0.0f); // on sidewalk surface

            if (cratePf != null)
            {
                var crate = (GameObject)PrefabUtility.InstantiatePrefab(cratePf, seatingRoot.transform);
                crate.transform.localPosition = Vector3.zero;
            }
            if (stoolPf != null)
            {
                Vector3[] stoolOffsets = new Vector3[]
                {
                    new Vector3(-0.42f, 0f, 0f),
                    new Vector3( 0.42f, 0f, 0f),
                    new Vector3(0f, 0f, -0.35f),
                    new Vector3(0f, 0f,  0.35f)
                };
                for (int i = 0; i < stoolOffsets.Length; i++)
                {
                    var stool = (GameObject)PrefabUtility.InstantiatePrefab(stoolPf, seatingRoot.transform);
                    stool.transform.localPosition = stoolOffsets[i];
                }
            }

            // Also place a second seating table cluster nearby for atmosphere
            var seating2 = new GameObject("CustomerSeating_Cluster_02");
            seating2.transform.position = new Vector3(-0.6f, 0.15f, 1.4f);
            if (cratePf != null)
            {
                var crate2 = (GameObject)PrefabUtility.InstantiatePrefab(cratePf, seating2.transform);
                crate2.transform.localPosition = Vector3.zero;
            }
            if (stoolPf != null)
            {
                var s1 = (GameObject)PrefabUtility.InstantiatePrefab(stoolPf, seating2.transform);
                s1.transform.localPosition = new Vector3(-0.42f, 0f, 0f);
                var s2 = (GameObject)PrefabUtility.InstantiatePrefab(stoolPf, seating2.transform);
                s2.transform.localPosition = new Vector3(0.42f, 0f, 0f);
            }

            EditorSceneManager.SaveScene(scene, PreviewScenePath);
            Debug.Log("[TramChanh] Saved Environment Preview Scene: " + PreviewScenePath);
        }

        public static void CaptureScreenshots()
        {
            EnsureFolder(ScreenshotsFolder);
            var scene = EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Single);

            // 1. Eye-level close-up on Yellow Crate Table and Stools
            var camObj1 = new GameObject("QACamera_Detail");
            var cam1 = camObj1.AddComponent<Camera>();
            cam1.fieldOfView = 50f;
            cam1.nearClipPlane = 0.05f;
            cam1.farClipPlane = 50f;
            cam1.clearFlags = CameraClearFlags.Color;
            cam1.backgroundColor = new Color(0.04f, 0.05f, 0.07f);
            camObj1.transform.position = new Vector3(-0.6f, 0.60f, -0.62f);
            camObj1.transform.LookAt(new Vector3(-0.6f, 0.28f, 0.0f));
            CaptureCameraToFile(cam1, $"{ScreenshotsFolder}/Furniture_CrateTable_Detail.png", 1920, 1080);
            Object.DestroyImmediate(camObj1);

            // 2. Wide Street Environment Overview
            var camObj2 = new GameObject("QACamera_Overview");
            var cam2 = camObj2.AddComponent<Camera>();
            cam2.fieldOfView = 50f;
            cam2.nearClipPlane = 0.05f;
            cam2.farClipPlane = 50f;
            cam2.clearFlags = CameraClearFlags.Color;
            cam2.backgroundColor = new Color(0.04f, 0.05f, 0.07f);
            camObj2.transform.position = new Vector3(1.6f, 1.8f, -2.2f);
            camObj2.transform.LookAt(new Vector3(-0.5f, 0.35f, 0.5f));
            CaptureCameraToFile(cam2, $"{ScreenshotsFolder}/Environment_Street_Overview.png", 1920, 1080);
            Object.DestroyImmediate(camObj2);
        }

        private static void CaptureCameraToFile(Camera cam, string filePath, int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
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
            else
            {
                if (mat.shader != shader)
                {
                    mat.shader = shader;
                }
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
