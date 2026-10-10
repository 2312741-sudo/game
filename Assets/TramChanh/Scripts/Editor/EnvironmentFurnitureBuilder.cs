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
        public const string CustomerAreaPrefabPath = "Assets/TramChanh/Prefabs/CustomerArea/PF_CustomerArea_10Tables.prefab";

        public const string StallPrefabPath = "Assets/TramChanh/Prefabs/Stall/PF_Stall_TramChanh.prefab";
        public const string SignPrefabPath = "Assets/TramChanh/Prefabs/Stall/PF_Sign_TramChanh_New.prefab";

        public const string ShopfrontAPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_VietnameseShopfront_A.prefab";
        public const string ShopfrontBPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_VietnameseShopfront_B.prefab";
        public const string ShopfrontCPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_VietnameseShopfront_C.prefab";
        public const string StreetTreePrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_StreetTree.prefab";
        public const string StreetLampPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_StreetLamp.prefab";
        public const string UtilityPolePrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_UtilityPole.prefab";
        public const string MotorbikePrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_Motorbike.prefab";
        public const string RoadsidePropsPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_RoadsideProps.prefab";

        public const string PreviewScenePath = "Assets/TramChanh/Scenes/Art/SCN_Art_EnvironmentPreview.unity";
        public const string ScreenshotsFolder = "QA/Screenshots/Environment_HighFidelity";

        private struct TableSpec
        {
            public string id;
            public Vector3 pos;
            public float tableRot;
            public Vector3 seatOffset;
            public int stoolCount;
        }

        [MenuItem("Tram Chanh/Art/Build High-Fidelity Street Environment Wave")]
        public static void BuildAndCapture()
        {
            Debug.Log("[TramChanh] Starting Environment & 10-Table Expansion Wave Reconstruction...");
            ConfigureTextureImporters();
            CreatePbrMaterials();
            UpgradeStoolPrefab();
            UpgradeCrateTablePrefab();
            CreateTrayPrefab();
            CreateEnvironmentPrefabs();
            CreateCustomerAreaPrefab();
            CreatePreviewScene();
            CaptureScreenshots();
            Debug.Log("[TramChanh] Environment & 10-Table Wave Reconstruction Complete!");
        }

        public static void ConfigureTextureImporters()
        {
            SetTextureType($"{TexturesFolder}/T_Asphalt_Night_MaskMap.png", TextureImporterType.Default, false, false);
            SetTextureType($"{TexturesFolder}/T_Foliage_Tree_BaseColor.png", TextureImporterType.Default, true, true);
            SetTextureType($"{TexturesFolder}/T_Plastic_Beige_BaseColor.png", TextureImporterType.Default, true, false);
            SetTextureType($"{TexturesFolder}/T_Crate_Yellow_BaseColor.png", TextureImporterType.Default, true, false);
            SetTextureType($"{TexturesFolder}/T_Sidewalk_Tiles_BaseColor.png", TextureImporterType.Default, true, false);
            SetTextureType($"{TexturesFolder}/T_Asphalt_Night_BaseColor.png", TextureImporterType.Default, true, false);
            SetTextureType($"{TexturesFolder}/T_Shopfront_A_BaseColor.png", TextureImporterType.Default, true, false);
            SetTextureType($"{TexturesFolder}/T_Shopfront_B_BaseColor.png", TextureImporterType.Default, true, false);
            SetTextureType($"{TexturesFolder}/T_Shopfront_C_BaseColor.png", TextureImporterType.Default, true, false);
            SetTextureType($"{TexturesFolder}/T_Bark_Tree_BaseColor.png", TextureImporterType.Default, true, false);
            SetTextureType($"{TexturesFolder}/T_UtilityPole_BaseColor.png", TextureImporterType.Default, true, false);
            SetTextureType($"{TexturesFolder}/T_Motorbike_BaseColor.png", TextureImporterType.Default, true, false);
            AssetDatabase.SaveAssets();
        }

        private static void SetTextureType(string path, TextureImporterType type, bool isSrgb, bool alphaIsTransparency)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = type;
                importer.sRGBTexture = isSrgb;
                importer.alphaIsTransparency = alphaIsTransparency;
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

            // 1. Beige Plastic Stool (IMG_5228.JPG)
            Material matStool = LoadOrCreateMaterial("MAT_Plastic_BeigeStool", urpLit);
            Texture2D stoolTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Plastic_Beige_BaseColor.png");
            if (stoolTex != null) matStool.SetTexture("_BaseMap", stoolTex);
            matStool.SetColor("_BaseColor", new Color(0.84f, 0.78f, 0.67f));
            matStool.SetFloat("_Smoothness", 0.42f);
            matStool.SetFloat("_Metallic", 0.02f);
            EditorUtility.SetDirty(matStool);

            // 2. Yellow Crate Plastic (IMG_5231.JPG)
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
            matSidewalk.SetFloat("_Metallic", 0.02f);
            EditorUtility.SetDirty(matSidewalk);

            // 5. Wet Night Asphalt
            Material matAsphalt = LoadOrCreateMaterial("MAT_Street_WetAsphaltNight", urpLit);
            Texture2D aspTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Asphalt_Night_BaseColor.png");
            Texture2D aspMask = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Asphalt_Night_MaskMap.png");
            if (aspTex != null) matAsphalt.SetTexture("_BaseMap", aspTex);
            if (aspMask != null) matAsphalt.SetTexture("_MetallicGlossMap", aspMask);
            matAsphalt.SetColor("_BaseColor", new Color(0.12f, 0.13f, 0.14f));
            matAsphalt.SetFloat("_Metallic", 0.10f);
            matAsphalt.SetFloat("_Smoothness", 0.85f);
            EditorUtility.SetDirty(matAsphalt);

            // 6. Shophouse A (Tạp Hóa Bình An)
            Material matShopA = LoadOrCreateMaterial("MAT_Shopfront_A", urpLit);
            Texture2D shopATex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Shopfront_A_BaseColor.png");
            if (shopATex != null) matShopA.SetTexture("_BaseMap", shopATex);
            matShopA.SetColor("_BaseColor", Color.white);
            matShopA.SetFloat("_Smoothness", 0.35f);
            matShopA.SetFloat("_Metallic", 0.05f);
            EditorUtility.SetDirty(matShopA);

            // 7. Shophouse B (Nhà Thuốc Đức Nguyên)
            Material matShopB = LoadOrCreateMaterial("MAT_Shopfront_B", urpLit);
            Texture2D shopBTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Shopfront_B_BaseColor.png");
            if (shopBTex != null) matShopB.SetTexture("_BaseMap", shopBTex);
            matShopB.SetColor("_BaseColor", Color.white);
            matShopB.SetFloat("_Smoothness", 0.40f);
            matShopB.SetFloat("_Metallic", 0.15f);
            EditorUtility.SetDirty(matShopB);

            // 8. Shophouse C (Sửa Xe Máy Vĩnh Phát)
            Material matShopC = LoadOrCreateMaterial("MAT_Shopfront_C", urpLit);
            Texture2D shopCTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Shopfront_C_BaseColor.png");
            if (shopCTex != null) matShopC.SetTexture("_BaseMap", shopCTex);
            matShopC.SetColor("_BaseColor", Color.white);
            matShopC.SetFloat("_Smoothness", 0.30f);
            matShopC.SetFloat("_Metallic", 0.10f);
            EditorUtility.SetDirty(matShopC);

            // 9. Tree Foliage Canopy (Alpha Cutout)
            Material matFoliage = LoadOrCreateMaterial("MAT_Foliage_Tree", urpLit);
            Texture2D folTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Foliage_Tree_BaseColor.png");
            if (folTex != null) matFoliage.SetTexture("_BaseMap", folTex);
            matFoliage.SetColor("_BaseColor", Color.white);
            matFoliage.SetFloat("_Smoothness", 0.20f);
            matFoliage.SetFloat("_AlphaClip", 1.0f);
            matFoliage.SetFloat("_Cutoff", 0.33f);
            matFoliage.EnableKeyword("_ALPHATEST_ON");
            matFoliage.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(matFoliage);

            // 10. Tree Trunk Bark
            Material matBark = LoadOrCreateMaterial("MAT_Bark_Tree", urpLit);
            Texture2D barkTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Bark_Tree_BaseColor.png");
            if (barkTex != null) matBark.SetTexture("_BaseMap", barkTex);
            matBark.SetColor("_BaseColor", new Color(0.75f, 0.68f, 0.60f));
            matBark.SetFloat("_Smoothness", 0.15f);
            matBark.SetFloat("_Metallic", 0.01f);
            EditorUtility.SetDirty(matBark);

            // 11. Streetlamp Pole & Mast
            Material matLamp = LoadOrCreateMaterial("MAT_Street_StreetLamp", urpLit);
            matLamp.SetColor("_BaseColor", new Color(0.28f, 0.30f, 0.32f));
            matLamp.SetFloat("_Smoothness", 0.65f);
            matLamp.SetFloat("_Metallic", 0.75f);
            EditorUtility.SetDirty(matLamp);

            // 12. Utility Pole with Cables
            Material matPole = LoadOrCreateMaterial("MAT_Street_UtilityPole", urpLit);
            Texture2D poleTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_UtilityPole_BaseColor.png");
            if (poleTex != null) matPole.SetTexture("_BaseMap", poleTex);
            matPole.SetColor("_BaseColor", Color.white);
            matPole.SetFloat("_Smoothness", 0.25f);
            matPole.SetFloat("_Metallic", 0.20f);
            EditorUtility.SetDirty(matPole);

            // 13. Commuter Motorbike
            Material matMoto = LoadOrCreateMaterial("MAT_Street_Motorbike", urpLit);
            Texture2D motoTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Motorbike_BaseColor.png");
            if (motoTex != null) matMoto.SetTexture("_BaseMap", motoTex);
            matMoto.SetColor("_BaseColor", Color.white);
            matMoto.SetFloat("_Smoothness", 0.65f);
            matMoto.SetFloat("_Metallic", 0.50f);
            EditorUtility.SetDirty(matMoto);

            // 14. Roadside Props (Trash bin, ceramic pots)
            Material matProps = LoadOrCreateMaterial("MAT_Street_RoadsideProps", urpLit);
            matProps.SetColor("_BaseColor", new Color(0.18f, 0.42f, 0.24f));
            matProps.SetFloat("_Smoothness", 0.45f);
            matProps.SetFloat("_Metallic", 0.05f);
            EditorUtility.SetDirty(matProps);

            AssetDatabase.SaveAssets();
        }

        public static void UpgradeStoolPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StoolPrefabPath);
            if (prefab == null) return;

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            root.layer = TramChanhLayers.EnvironmentIndex;

            Mesh stoolMesh = LoadMesh($"{FurnitureModelsFolder}/SM_PlasticStool.obj");
            Material stoolMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Plastic_BeigeStool.mat");

            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = root.transform.GetChild(i);
                if (child.name.StartsWith("Leg")) Object.DestroyImmediate(child.gameObject);
            }

            Transform seat = root.transform.Find("Seat");
            if (seat == null)
            {
                var seatGo = new GameObject("Seat");
                seatGo.transform.SetParent(root.transform, false);
                seat = seatGo.transform;
            }

            seat.gameObject.layer = TramChanhLayers.EnvironmentIndex;
            seat.localPosition = Vector3.zero;
            seat.localScale = Vector3.one;
            seat.localRotation = Quaternion.identity;
            for (int i = seat.childCount - 1; i >= 0; i--) Object.DestroyImmediate(seat.GetChild(i).gameObject);

            var mf = seat.GetComponent<MeshFilter>() ?? seat.gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = stoolMesh;
            var mr = seat.GetComponent<MeshRenderer>() ?? seat.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = stoolMat;

            var col = seat.GetComponent<BoxCollider>() ?? seat.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.15f, 0f);
            col.size = new Vector3(0.30f, 0.30f, 0.30f);

            Transform seatPoint = root.transform.Find("SeatPoint");
            if (seatPoint != null) seatPoint.localPosition = new Vector3(0f, 0.30f, 0f);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[TramChanh] Upgraded Stool Prefab: " + StoolPrefabPath);
        }

        public static void UpgradeCrateTablePrefab()
        {
            UpgradeCrateFile(CrateTablePrefabPath, false);
            if (File.Exists(CrateTableDrinkWavePath)) UpgradeCrateFile(CrateTableDrinkWavePath, true);
        }

        private static void UpgradeCrateFile(string path, bool isGameplayVariant)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (!isGameplayVariant) root.layer = TramChanhLayers.EnvironmentIndex;

            Mesh crateMesh = LoadMesh($"{FurnitureModelsFolder}/SM_YellowCrateTable.obj");
            Material crateMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Crate_YellowHDPE.mat");
            Mesh trayMesh = LoadMesh($"{FurnitureModelsFolder}/SM_StainlessTray.obj");
            Material trayMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Stainless_ServingTray.mat");

            Transform crate = root.transform.Find("Crate");
            if (crate != null)
            {
                crate.localPosition = Vector3.zero;
                crate.localScale = Vector3.one;
                crate.localRotation = Quaternion.identity;
                for (int i = crate.childCount - 1; i >= 0; i--) Object.DestroyImmediate(crate.GetChild(i).gameObject);
                var mf = crate.GetComponent<MeshFilter>() ?? crate.gameObject.AddComponent<MeshFilter>();
                mf.sharedMesh = crateMesh;
                var mr = crate.GetComponent<MeshRenderer>() ?? crate.gameObject.AddComponent<MeshRenderer>();
                mr.sharedMaterial = crateMat;

                var col = crate.GetComponent<BoxCollider>() ?? crate.gameObject.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.16f, 0f);
                col.size = new Vector3(0.45f, 0.32f, 0.35f);
            }

            Transform trayTop = root.transform.Find("TrayTop");
            if (trayTop != null)
            {
                trayTop.localPosition = new Vector3(0f, 0.32f, 0f);
                trayTop.localScale = Vector3.one;
                trayTop.localRotation = Quaternion.identity;
                if (trayMesh != null && trayMat != null)
                {
                    var mf = trayTop.GetComponent<MeshFilter>() ?? trayTop.gameObject.AddComponent<MeshFilter>();
                    mf.sharedMesh = trayMesh;
                    var mr = trayTop.GetComponent<MeshRenderer>() ?? trayTop.gameObject.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = trayMat;

                    var col = trayTop.GetComponent<BoxCollider>() ?? trayTop.gameObject.AddComponent<BoxCollider>();
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

        public static void CreateEnvironmentPrefabs()
        {
            EnsureFolder("Assets/TramChanh/Prefabs/Environment");

            CreateSimpleVisualPrefab(
                ShopfrontAPrefabPath,
                "PF_VietnameseShopfront_A",
                LoadMesh($"{EnvironmentModelsFolder}/SM_Shopfront_A.obj"),
                AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Shopfront_A.mat"),
                new Vector3(4.5f, 11.5f, 4.5f),
                new Vector3(0f, 5.75f, 2.25f)
            );

            CreateSimpleVisualPrefab(
                ShopfrontBPrefabPath,
                "PF_VietnameseShopfront_B",
                LoadMesh($"{EnvironmentModelsFolder}/SM_Shopfront_B.obj"),
                AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Shopfront_B.mat"),
                new Vector3(5.0f, 14.0f, 4.5f),
                new Vector3(0f, 7.0f, 2.25f)
            );

            CreateSimpleVisualPrefab(
                ShopfrontCPrefabPath,
                "PF_VietnameseShopfront_C",
                LoadMesh($"{EnvironmentModelsFolder}/SM_Shopfront_C.obj"),
                AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Shopfront_C.mat"),
                new Vector3(4.2f, 11.0f, 4.5f),
                new Vector3(0f, 5.5f, 2.25f)
            );

            var treeRoot = new GameObject("PF_StreetTree");
            treeRoot.layer = TramChanhLayers.EnvironmentIndex;

            var trunkGo = new GameObject("Trunk");
            trunkGo.transform.SetParent(treeRoot.transform, false);
            trunkGo.layer = TramChanhLayers.EnvironmentIndex;
            trunkGo.AddComponent<MeshFilter>().sharedMesh = LoadMesh($"{EnvironmentModelsFolder}/SM_StreetTree_Trunk.obj");
            trunkGo.AddComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Bark_Tree.mat");

            var canopyGo = new GameObject("Canopy");
            canopyGo.transform.SetParent(treeRoot.transform, false);
            canopyGo.layer = TramChanhLayers.EnvironmentIndex;
            canopyGo.AddComponent<MeshFilter>().sharedMesh = LoadMesh($"{EnvironmentModelsFolder}/SM_StreetTree_Canopy.obj");
            canopyGo.AddComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Foliage_Tree.mat");

            var treeCol = treeRoot.AddComponent<BoxCollider>();
            treeCol.center = new Vector3(0f, 3.0f, 0f);
            treeCol.size = new Vector3(2.5f, 6.0f, 2.5f);

            PrefabUtility.SaveAsPrefabAsset(treeRoot, StreetTreePrefabPath);
            Object.DestroyImmediate(treeRoot);
            Debug.Log("[TramChanh] Created Street Tree Prefab: " + StreetTreePrefabPath);

            var lampRoot = new GameObject("PF_StreetLamp");
            lampRoot.layer = TramChanhLayers.EnvironmentIndex;
            var lmf = lampRoot.AddComponent<MeshFilter>();
            lmf.sharedMesh = LoadMesh($"{EnvironmentModelsFolder}/SM_StreetLamp.obj");
            var lmr = lampRoot.AddComponent<MeshRenderer>();
            lmr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Street_StreetLamp.mat");
            var lcol = lampRoot.AddComponent<BoxCollider>();
            lcol.center = new Vector3(0f, 3.1f, 0.7f);
            lcol.size = new Vector3(0.5f, 6.2f, 2.2f);

            var lightGo = new GameObject("LuminaireLight");
            lightGo.transform.SetParent(lampRoot.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 5.95f, 1.75f);
            lightGo.transform.localRotation = Quaternion.Euler(85f, 0f, 0f);
            var lightComp = lightGo.AddComponent<Light>();
            lightComp.type = LightType.Spot;
            lightComp.spotAngle = 78f;
            lightComp.range = 14f;
            lightComp.color = new Color(1.0f, 0.84f, 0.60f);
            lightComp.intensity = 3.6f;
            lightComp.shadows = LightShadows.Soft;

            PrefabUtility.SaveAsPrefabAsset(lampRoot, StreetLampPrefabPath);
            Object.DestroyImmediate(lampRoot);
            Debug.Log("[TramChanh] Created Streetlamp Prefab: " + StreetLampPrefabPath);

            CreateSimpleVisualPrefab(
                UtilityPolePrefabPath,
                "PF_UtilityPole",
                LoadMesh($"{EnvironmentModelsFolder}/SM_UtilityPole.obj"),
                AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Street_UtilityPole.mat"),
                new Vector3(1.8f, 7.5f, 0.5f),
                new Vector3(0f, 3.75f, 0f)
            );

            CreateSimpleVisualPrefab(
                MotorbikePrefabPath,
                "PF_Motorbike",
                LoadMesh($"{EnvironmentModelsFolder}/SM_Motorbike.obj"),
                AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Street_Motorbike.mat"),
                new Vector3(0.7f, 1.05f, 1.8f),
                new Vector3(0f, 0.52f, 0f)
            );

            var propsRoot = new GameObject("PF_RoadsideProps");
            propsRoot.layer = TramChanhLayers.EnvironmentIndex;

            var binGo = new GameObject("TrashBin");
            binGo.transform.SetParent(propsRoot.transform, false);
            binGo.layer = TramChanhLayers.EnvironmentIndex;
            binGo.AddComponent<MeshFilter>().sharedMesh = LoadMesh($"{EnvironmentModelsFolder}/SM_TrashBin.obj");
            binGo.AddComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Street_RoadsideProps.mat");
            var binCol = binGo.AddComponent<BoxCollider>();
            binCol.center = new Vector3(0f, 0.42f, 0f);
            binCol.size = new Vector3(0.45f, 0.85f, 0.45f);

            var plantGo = new GameObject("PottedPlant");
            plantGo.transform.SetParent(propsRoot.transform, false);
            plantGo.transform.localPosition = new Vector3(0.8f, 0f, 0f);
            plantGo.layer = TramChanhLayers.EnvironmentIndex;
            plantGo.AddComponent<MeshFilter>().sharedMesh = LoadMesh($"{EnvironmentModelsFolder}/SM_PottedPlant.obj");
            plantGo.AddComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Foliage_Tree.mat");

            PrefabUtility.SaveAsPrefabAsset(propsRoot, RoadsidePropsPrefabPath);
            Object.DestroyImmediate(propsRoot);
            Debug.Log("[TramChanh] Created Roadside Props Prefab: " + RoadsidePropsPrefabPath);
        }

        private static void CreateSimpleVisualPrefab(string prefabPath, string goName, Mesh mesh, Material mat, Vector3 colSize, Vector3 colCenter)
        {
            var go = new GameObject(goName);
            go.layer = TramChanhLayers.EnvironmentIndex;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            if (colSize != Vector3.zero)
            {
                var col = go.AddComponent<BoxCollider>();
                col.center = colCenter;
                col.size = colSize;
            }
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log("[TramChanh] Created Prefab: " + prefabPath);
        }

        // =====================================================================
        // 10 Customer Tables Area Prefab (Compliant with TABLE_ANCHOR_CONTRACT.md)
        // =====================================================================
        public static void CreateCustomerAreaPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(CustomerAreaPrefabPath));

            GameObject cratePf = AssetDatabase.LoadAssetAtPath<GameObject>(CrateTablePrefabPath);
            GameObject stoolPf = AssetDatabase.LoadAssetAtPath<GameObject>(StoolPrefabPath);

            var root = new GameObject("CustomerArea");
            root.layer = TramChanhLayers.EnvironmentIndex;

            TableSpec[] specs = new TableSpec[]
            {
                new TableSpec { id = "TABLE_01", pos = new Vector3(-2.10f, 0f,  1.70f), tableRot =   0f, seatOffset = new Vector3(-0.45f, 0f,  0.00f), stoolCount = 4 },
                new TableSpec { id = "TABLE_02", pos = new Vector3(-3.35f, 0f,  0.20f), tableRot =  15f, seatOffset = new Vector3(-0.45f, 0f,  0.00f), stoolCount = 3 },
                new TableSpec { id = "TABLE_03", pos = new Vector3(-4.60f, 0f,  1.70f), tableRot = -10f, seatOffset = new Vector3(-0.45f, 0f,  0.00f), stoolCount = 4 },
                new TableSpec { id = "TABLE_04", pos = new Vector3(-5.85f, 0f,  0.20f), tableRot =  20f, seatOffset = new Vector3(-0.45f, 0f,  0.00f), stoolCount = 2 },
                new TableSpec { id = "TABLE_05", pos = new Vector3(-7.10f, 0f,  1.70f), tableRot =   5f, seatOffset = new Vector3(-0.45f, 0f,  0.00f), stoolCount = 3 },
                new TableSpec { id = "TABLE_06", pos = new Vector3(-3.35f, 0f, -1.80f), tableRot = -15f, seatOffset = new Vector3(-0.45f, 0f,  0.00f), stoolCount = 4 },
                new TableSpec { id = "TABLE_07", pos = new Vector3(-5.85f, 0f, -1.80f), tableRot =   0f, seatOffset = new Vector3(-0.45f, 0f,  0.00f), stoolCount = 3 },
                new TableSpec { id = "TABLE_08", pos = new Vector3( 4.20f, 0f,  0.60f), tableRot = -10f, seatOffset = new Vector3( 0.45f, 0f,  0.00f), stoolCount = 4 },
                new TableSpec { id = "TABLE_09", pos = new Vector3( 5.60f, 0f, -0.90f), tableRot =  25f, seatOffset = new Vector3( 0.45f, 0f,  0.00f), stoolCount = 3 },
                new TableSpec { id = "TABLE_10", pos = new Vector3( 4.20f, 0f, -2.20f), tableRot =  -5f, seatOffset = new Vector3( 0.45f, 0f,  0.00f), stoolCount = 2 },
            };

            for (int t = 0; t < specs.Length; t++)
            {
                var spec = specs[t];
                var tableGo = new GameObject(spec.id);
                tableGo.layer = TramChanhLayers.EnvironmentIndex;
                tableGo.transform.SetParent(root.transform, false);
                tableGo.transform.localPosition = spec.pos;
                tableGo.transform.localRotation = Quaternion.Euler(0f, spec.tableRot, 0f);

                var seatGo = new GameObject("Seat");
                seatGo.layer = TramChanhLayers.EnvironmentIndex;
                seatGo.transform.SetParent(tableGo.transform, false);
                seatGo.transform.localPosition = spec.seatOffset;
                seatGo.transform.localRotation = Quaternion.LookRotation(-spec.seatOffset.normalized, Vector3.up);

                var serviceGo = new GameObject("ServicePoint");
                serviceGo.layer = TramChanhLayers.EnvironmentIndex;
                serviceGo.transform.SetParent(tableGo.transform, false);
                serviceGo.transform.localPosition = new Vector3(0f, 0f, 0.80f);

                if (cratePf != null)
                {
                    var crateInst = (GameObject)PrefabUtility.InstantiatePrefab(cratePf, tableGo.transform);
                    crateInst.transform.localPosition = Vector3.zero;
                    crateInst.transform.localRotation = Quaternion.identity;
                }

                if (stoolPf != null)
                {
                    var s0 = (GameObject)PrefabUtility.InstantiatePrefab(stoolPf, tableGo.transform);
                    s0.transform.localPosition = spec.seatOffset;
                    s0.transform.localRotation = Quaternion.LookRotation(-spec.seatOffset.normalized, Vector3.up);

                    if (spec.stoolCount >= 2)
                    {
                        var s1 = (GameObject)PrefabUtility.InstantiatePrefab(stoolPf, tableGo.transform);
                        s1.transform.localPosition = -spec.seatOffset;
                        s1.transform.localRotation = Quaternion.LookRotation(spec.seatOffset.normalized, Vector3.up);
                    }
                    if (spec.stoolCount >= 3)
                    {
                        var s2 = (GameObject)PrefabUtility.InstantiatePrefab(stoolPf, tableGo.transform);
                        s2.transform.localPosition = new Vector3(0f, 0f, 0.38f);
                        s2.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    }
                    if (spec.stoolCount >= 4)
                    {
                        var s3 = (GameObject)PrefabUtility.InstantiatePrefab(stoolPf, tableGo.transform);
                        s3.transform.localPosition = new Vector3(0f, 0f, -0.38f);
                        s3.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    }
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, CustomerAreaPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[TramChanh] Created 10-Table Customer Area Prefab: " + CustomerAreaPrefabPath);
        }

        public static void CreatePreviewScene()
        {
            EnsureFolder(Path.GetDirectoryName(PreviewScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lighting = new GameObject("Lighting");
            var keyLight = new GameObject("Streetlight").AddComponent<Light>();
            keyLight.transform.SetParent(lighting.transform);
            keyLight.transform.position = new Vector3(-0.6f, 5.8f, 2.5f);
            keyLight.type = LightType.Point;
            keyLight.range = 22f;
            keyLight.color = new Color(1.0f, 0.86f, 0.65f);
            keyLight.intensity = 3.2f;
            keyLight.shadows = LightShadows.Soft;

            var fillLight = new GameObject("SkyFill").AddComponent<Light>();
            fillLight.transform.SetParent(lighting.transform);
            fillLight.transform.rotation = Quaternion.Euler(55f, -140f, 0f);
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.35f, 0.45f, 0.70f);
            fillLight.intensity = 0.45f;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.18f, 0.24f);

            var envRoot = new GameObject("StreetEnvironment");
            Mesh swMesh = LoadMesh($"{EnvironmentModelsFolder}/SM_Sidewalk_Extended.obj");
            Mesh rdMesh = LoadMesh($"{EnvironmentModelsFolder}/SM_Asphalt_Road_Extended.obj");
            Material swMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Street_SidewalkTiles.mat");
            Material rdMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Street_WetAsphaltNight.mat");

            if (swMesh != null)
            {
                var swObj = new GameObject("Sidewalk_Extended");
                swObj.transform.SetParent(envRoot.transform, false);
                swObj.transform.position = new Vector3(0f, 0f, 0f);
                swObj.AddComponent<MeshFilter>().sharedMesh = swMesh;
                swObj.AddComponent<MeshRenderer>().sharedMaterial = swMat;
                var col = swObj.AddComponent<BoxCollider>();
                col.center = new Vector3(0.0f, -0.075f, -0.8f);
                col.size = new Vector3(36.0f, 0.15f, 8.8f);
            }
            if (rdMesh != null)
            {
                var rdObj = new GameObject("Roadway_Extended");
                rdObj.transform.SetParent(envRoot.transform, false);
                rdObj.transform.position = new Vector3(0f, 0f, 0f);
                rdObj.AddComponent<MeshFilter>().sharedMesh = rdMesh;
                rdObj.AddComponent<MeshRenderer>().sharedMaterial = rdMat;
                var col = rdObj.AddComponent<BoxCollider>();
                col.center = new Vector3(0.0f, -0.175f, 8.8f);
                col.size = new Vector3(36.0f, 0.05f, 10.4f);
            }

            // Warm ambient lights for Customer Seating Areas
            var seatingLight = new GameObject("CustomerAreaLight").AddComponent<Light>();
            seatingLight.transform.SetParent(lighting.transform, false);
            seatingLight.transform.position = new Vector3(-4.5f, 3.2f, 0.0f);
            seatingLight.type = LightType.Point;
            seatingLight.range = 14.0f;
            seatingLight.color = new Color(1.0f, 0.88f, 0.70f);
            seatingLight.intensity = 2.4f;
            seatingLight.shadows = LightShadows.Soft;

            var rightSeatingLight = new GameObject("RightSeatingLight").AddComponent<Light>();
            rightSeatingLight.transform.SetParent(lighting.transform, false);
            rightSeatingLight.transform.position = new Vector3(4.5f, 3.2f, -0.5f);
            rightSeatingLight.type = LightType.Point;
            rightSeatingLight.range = 10.0f;
            rightSeatingLight.color = new Color(1.0f, 0.88f, 0.70f);
            rightSeatingLight.intensity = 2.0f;
            rightSeatingLight.shadows = LightShadows.Soft;

            GameObject stallPf = AssetDatabase.LoadAssetAtPath<GameObject>(StallPrefabPath);
            if (stallPf != null)
            {
                var stallInst = (GameObject)PrefabUtility.InstantiatePrefab(stallPf, envRoot.transform);
                stallInst.transform.position = Vector3.zero;
                stallInst.transform.rotation = Quaternion.identity;

                var stallLight = new GameObject("WarmStallBulb").AddComponent<Light>();
                stallLight.transform.SetParent(stallInst.transform, false);
                stallLight.transform.localPosition = new Vector3(0f, 1.9f, 0f);
                stallLight.type = LightType.Point;
                stallLight.range = 6.0f;
                stallLight.color = new Color(1.0f, 0.82f, 0.50f);
                stallLight.intensity = 3.8f;
            }

            GameObject custAreaPf = AssetDatabase.LoadAssetAtPath<GameObject>(CustomerAreaPrefabPath);
            if (custAreaPf != null)
            {
                var custInst = (GameObject)PrefabUtility.InstantiatePrefab(custAreaPf, envRoot.transform);
                custInst.transform.position = Vector3.zero;
                custInst.transform.rotation = Quaternion.identity;
            }

            GameObject shopAPf = AssetDatabase.LoadAssetAtPath<GameObject>(ShopfrontAPrefabPath);
            GameObject shopBPf = AssetDatabase.LoadAssetAtPath<GameObject>(ShopfrontBPrefabPath);
            GameObject shopCPf = AssetDatabase.LoadAssetAtPath<GameObject>(ShopfrontCPrefabPath);

            var backdrop = new GameObject("NeighbourhoodBackdrop");
            backdrop.transform.SetParent(envRoot.transform, false);

            if (shopCPf != null)
            {
                var sc = (GameObject)PrefabUtility.InstantiatePrefab(shopCPf, backdrop.transform);
                sc.transform.position = new Vector3(-7.0f, 0.0f, -5.0f);
                sc.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            if (shopAPf != null)
            {
                var sa = (GameObject)PrefabUtility.InstantiatePrefab(shopAPf, backdrop.transform);
                sa.transform.position = new Vector3(-2.6f, 0.0f, -5.0f);
                sa.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            if (shopBPf != null)
            {
                var sb = (GameObject)PrefabUtility.InstantiatePrefab(shopBPf, backdrop.transform);
                sb.transform.position = new Vector3(2.5f, 0.0f, -5.0f);
                sb.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            if (shopAPf != null)
            {
                var sa2 = (GameObject)PrefabUtility.InstantiatePrefab(shopAPf, backdrop.transform);
                sa2.transform.position = new Vector3(7.2f, 0.0f, -5.0f);
                sa2.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            GameObject treePf = AssetDatabase.LoadAssetAtPath<GameObject>(StreetTreePrefabPath);
            if (treePf != null)
            {
                float[] treeXs = new float[] { -7.5f, -4.5f, 4.5f, 7.5f };
                for (int i = 0; i < treeXs.Length; i++)
                {
                    var tree = (GameObject)PrefabUtility.InstantiatePrefab(treePf, envRoot.transform);
                    tree.transform.position = new Vector3(treeXs[i], 0.0f, 3.2f);
                    tree.transform.rotation = Quaternion.Euler(0f, i * 65f, 0f);
                }
            }

            GameObject lampPf = AssetDatabase.LoadAssetAtPath<GameObject>(StreetLampPrefabPath);
            if (lampPf != null)
            {
                float[] lampXs = new float[] { -5.5f, 5.5f };
                for (int i = 0; i < lampXs.Length; i++)
                {
                    var lamp = (GameObject)PrefabUtility.InstantiatePrefab(lampPf, envRoot.transform);
                    lamp.transform.position = new Vector3(lampXs[i], 0.0f, 3.4f);
                    lamp.transform.rotation = Quaternion.identity;
                }
            }

            GameObject polePf = AssetDatabase.LoadAssetAtPath<GameObject>(UtilityPolePrefabPath);
            if (polePf != null)
            {
                var pole = (GameObject)PrefabUtility.InstantiatePrefab(polePf, envRoot.transform);
                pole.transform.position = new Vector3(-8.2f, 0.0f, 3.2f);
            }

            GameObject motoPf = AssetDatabase.LoadAssetAtPath<GameObject>(MotorbikePrefabPath);
            if (motoPf != null)
            {
                var m1 = (GameObject)PrefabUtility.InstantiatePrefab(motoPf, envRoot.transform);
                m1.transform.position = new Vector3(-6.5f, 0.0f, 2.7f);
                m1.transform.rotation = Quaternion.Euler(0f, 35f, 0f);

                var m2 = (GameObject)PrefabUtility.InstantiatePrefab(motoPf, envRoot.transform);
                m2.transform.position = new Vector3(-7.2f, 0.0f, -3.8f);
                m2.transform.rotation = Quaternion.Euler(0f, -60f, 0f);

                var m3 = (GameObject)PrefabUtility.InstantiatePrefab(motoPf, envRoot.transform);
                m3.transform.position = new Vector3(5.2f, 0.0f, 2.6f);
                m3.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            }

            GameObject propsPf = AssetDatabase.LoadAssetAtPath<GameObject>(RoadsidePropsPrefabPath);
            if (propsPf != null)
            {
                var p1 = (GameObject)PrefabUtility.InstantiatePrefab(propsPf, envRoot.transform);
                p1.transform.position = new Vector3(-1.8f, 0.0f, 3.1f);

                var p2 = (GameObject)PrefabUtility.InstantiatePrefab(propsPf, envRoot.transform);
                p2.transform.position = new Vector3(2.8f, 0.0f, 3.1f);
            }

            EditorSceneManager.SaveScene(scene, PreviewScenePath);
            Debug.Log("[TramChanh] Saved Environment Preview Scene: " + PreviewScenePath);
        }

        public static void CaptureScreenshots()
        {
            EnsureFolder(ScreenshotsFolder);
            var scene = EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Single);

            // A. Env_A_Overview_TopDown.png
            CaptureView("Env_A_Overview_TopDown", new Vector3(-0.5f, 9.5f, 8.5f), new Vector3(-0.5f, 0.5f, -0.5f), 55f);

            // B. Env_B_Stall_AcrossRoad.png
            CaptureView("Env_B_Stall_AcrossRoad", new Vector3(0.0f, 1.45f, 5.8f), new Vector3(0.0f, 1.15f, 0.0f), 52f);

            // C. Env_C_Customer_SeatingArea.png
            CaptureView("Env_C_Customer_SeatingArea", new Vector3(-6.8f, 1.4f, 3.2f), new Vector3(-4.2f, 0.30f, 0.2f), 55f);

            // D. Env_D_Shopfronts_Architecture.png
            CaptureView("Env_D_Shopfronts_Architecture", new Vector3(-1.0f, 2.4f, 2.2f), new Vector3(-1.0f, 4.5f, -5.0f), 65f);

            // E. Env_E_Streetlamps_Trees.png
            CaptureView("Env_E_Streetlamps_Trees", new Vector3(-9.5f, 2.2f, 5.5f), new Vector3(-1.0f, 2.5f, 3.2f), 55f);

            // F. Env_F_Player_EyeLevel_Night.png
            CaptureView("Env_F_Player_EyeLevel_Night", new Vector3(-1.5f, 1.6f, 2.7f), new Vector3(-0.3f, 1.05f, 0.2f), 60f);

            // G. Env_G_All_10_Tables_Labeled.png
            CaptureView("Env_G_All_10_Tables_Labeled", new Vector3(-0.5f, 7.8f, 6.8f), new Vector3(-0.5f, 0.2f, -0.5f), 65f);

            CaptureView("Furniture_CrateTable_Detail", new Vector3(-2.1f, 0.65f, 0.95f), new Vector3(-2.1f, 0.28f, 1.70f), 45f);
            CaptureView("Environment_Street_Overview", new Vector3(3.5f, 3.0f, 5.0f), new Vector3(-1.5f, 0.8f, 0.0f), 55f);
        }

        private static void CaptureView(string filename, Vector3 pos, Vector3 lookAt, float fov)
        {
            var camObj = new GameObject("QACamera_" + filename);
            var cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 60f;
            cam.clearFlags = CameraClearFlags.Color;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.07f);
            camObj.transform.position = pos;
            camObj.transform.LookAt(lookAt);

            string filePath = $"{ScreenshotsFolder}/{filename}.png";
            CaptureCameraToFile(cam, filePath, 1920, 1080);
            Object.DestroyImmediate(camObj);
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
