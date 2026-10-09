using System;
using System.IO;
using TramChanh.Core.GroundTruth;
using TramChanh.Drinks.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TramChanh.EditorTools
{
    public static class DrinkEquipmentBuilder
    {
        public const string TexturesFolder = "Assets/TramChanh/Art/Textures";
        public const string MaterialsFolder = "Assets/TramChanh/Art/Materials";
        public const string ModelsFolder = "Assets/TramChanh/Art/Models/DrinkStation";

        public const string TeaBagPrefabPath = "Assets/TramChanh/Prefabs/Items/PF_TeaBag_PrePortioned.prefab";
        public const string TeaRackPrefabPath = "Assets/TramChanh/Prefabs/Workstations/PF_RedTeaRack.prefab";
        public const string ToppingStationPrefabPath = "Assets/TramChanh/Prefabs/Workstations/PF_ToppingStation.prefab";
        public const string IceBinPrefabPath = "Assets/TramChanh/Prefabs/Workstations/PF_IceBin.prefab";
        public const string IceScoopPrefabPath = "Assets/TramChanh/Prefabs/Tools/PF_IceScoop.prefab";

        public const string PreviewScenePath = "Assets/TramChanh/Scenes/Art/SCN_Art_DrinkPreview.unity";
        public const string ScreenshotsFolder = "QA/Screenshots/Drink_HighFidelity";

        [MenuItem("Tram Chanh/Art/Build High-Fidelity Drink Equipment")]
        public static void BuildAndCapture()
        {
            Debug.Log("[TramChanh] Starting Drink Equipment High-Fidelity Reconstruction...");
            ConfigureTextureImporters();
            CreatePbrMaterials();
            UpgradeTeaBagPrefab();
            UpgradeTeaRackPrefab();
            UpgradeToppingStationPrefab();
            UpgradeIceBinPrefab();
            UpgradeIceScoopPrefab();
            CreatePreviewScene();
            CaptureScreenshots();
            Debug.Log("[TramChanh] Drink Equipment Reconstruction Complete!");
        }

        public static void ConfigureTextureImporters()
        {
            SetTextureType($"{TexturesFolder}/T_Steel_Brushed_MaskMap.png", TextureImporterType.Default, false);
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

        public static void CreatePbrMaterials()
        {
            EnsureFolder(MaterialsFolder);
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // 1. Transparent Stand-Up Pouch (Plastic Film)
            Material matPouch = LoadOrCreateMaterial("MAT_TeaBag_Pouch", urpLit);
            Texture2D pouchTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_TeaBag_Pouch_BaseColor.png");
            if (pouchTex != null) matPouch.SetTexture("_BaseColorMap", pouchTex);
            matPouch.SetColor("_BaseColor", new Color(0.95f, 0.97f, 1.0f, 0.65f));
            matPouch.SetFloat("_Surface", 1f); // Transparent
            matPouch.SetFloat("_Blend", 0f);   // Alpha
            matPouch.SetFloat("_Smoothness", 0.92f);
            matPouch.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            matPouch.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            matPouch.SetInt("_ZWrite", 0);
            matPouch.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(matPouch);

            // 2. Amber Tea Liquid Material
            Material matLiquid = LoadOrCreateMaterial("MAT_TeaLiquid_Amber", urpLit);
            matLiquid.SetColor("_BaseColor", new Color(0.85f, 0.42f, 0.08f, 0.90f));
            matLiquid.SetFloat("_Smoothness", 0.88f);
            matLiquid.SetFloat("_Metallic", 0.05f);
            EditorUtility.SetDirty(matLiquid);

            // 3. Coconut Jelly (Nata de Coco) Cubes Material
            Material matJelly = LoadOrCreateMaterial("MAT_CoconutJelly", urpLit);
            matJelly.SetColor("_BaseColor", new Color(0.96f, 0.97f, 0.98f, 0.82f));
            matJelly.SetFloat("_Smoothness", 0.65f);
            EditorUtility.SetDirty(matJelly);

            // 4. Ice Cubes Material
            Material matIce = LoadOrCreateMaterial("MAT_Ice_Cubes", urpLit);
            matIce.SetColor("_BaseColor", new Color(0.90f, 0.95f, 1.0f, 0.45f));
            matIce.SetFloat("_Surface", 1f);
            matIce.SetFloat("_Smoothness", 0.95f);
            matIce.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(matIce);

            // 5. Commercial Red Plastic Rack Material
            Material matRack = LoadOrCreateMaterial("MAT_TeaRack_RedPlastic", urpLit);
            Texture2D rackTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_TeaRack_RedPlastic_BaseColor.png");
            if (rackTex != null) matRack.SetTexture("_BaseColorMap", rackTex);
            matRack.SetColor("_BaseColor", new Color(0.83f, 0.14f, 0.10f));
            matRack.SetFloat("_Smoothness", 0.58f);
            EditorUtility.SetDirty(matRack);

            // 6. Brushed Stainless Steel Material
            Material matSteel = LoadOrCreateMaterial("MAT_Steel_Brushed", urpLit);
            Texture2D steelTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Steel_Brushed_BaseColor.png");
            Texture2D steelMask = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Steel_Brushed_MaskMap.png");
            if (steelTex != null) matSteel.SetTexture("_BaseColorMap", steelTex);
            if (steelMask != null) matSteel.SetTexture("_MetallicGlossMap", steelMask);
            matSteel.SetColor("_BaseColor", new Color(0.85f, 0.87f, 0.88f));
            matSteel.SetFloat("_Metallic", 0.92f);
            matSteel.SetFloat("_Smoothness", 0.72f);
            EditorUtility.SetDirty(matSteel);

            // 7. Clear Acrylic Roll-Top Dome Cover
            Material matCover = LoadOrCreateMaterial("MAT_ToppingStation_Cover", urpLit);
            matCover.SetColor("_BaseColor", new Color(0.92f, 0.96f, 1.0f, 0.28f));
            matCover.SetFloat("_Surface", 1f);
            matCover.SetFloat("_Smoothness", 0.96f);
            matCover.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(matCover);

            AssetDatabase.SaveAssets();
        }

        public static void UpgradeTeaBagPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TeaBagPrefabPath);
            if (prefab == null) return;

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

            Mesh pouchClosedMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_TeaBag_Pouch_Closed.obj");
            Mesh pouchOpenMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_TeaBag_Pouch_Open.obj");
            Mesh liquidMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_TeaBag_Liquid.obj");
            Mesh jellyMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_Topping_CoconutJellyCubes.obj");
            Mesh iceMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_Ice_Cubes.obj");

            Material matPouch = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_TeaBag_Pouch.mat");
            Material matLiquid = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_TeaLiquid_Amber.mat");
            Material matJelly = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_CoconutJelly.mat");
            Material matIce = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Ice_Cubes.mat");

            // Replace Bag_Closed mesh
            Transform bagClosed = root.transform.Find("Visual/Bag_Closed");
            if (bagClosed != null) ReplaceVisualMesh(bagClosed, pouchClosedMesh, matPouch);

            // Replace Bag_Open mesh
            Transform bagOpen = root.transform.Find("Visual/Bag_Open");
            if (bagOpen != null) ReplaceVisualMesh(bagOpen, pouchOpenMesh, matPouch);

            // Replace Contents
            Transform teaLiquid = root.transform.Find("Visual/Contents/TeaLiquid");
            if (teaLiquid != null) ReplaceVisualMesh(teaLiquid, liquidMesh, matLiquid);

            Transform coconutJelly = root.transform.Find("Visual/Contents/CoconutJelly");
            if (coconutJelly != null) ReplaceVisualMesh(coconutJelly, jellyMesh, matJelly);

            Transform lemonJelly = root.transform.Find("Visual/Contents/LemonJelly");
            if (lemonJelly != null) ReplaceVisualMesh(lemonJelly, jellyMesh, matJelly);

            Transform ice = root.transform.Find("Visual/Contents/Ice");
            if (ice != null) ReplaceVisualMesh(ice, iceMesh, matIce);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[TramChanh] Upgraded TeaBag Prefab: " + TeaBagPrefabPath);
        }

        public static void UpgradeTeaRackPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TeaRackPrefabPath);
            if (prefab == null) return;

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

            Mesh rackMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_RedTeaRack.obj");
            Material rackMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_TeaRack_RedPlastic.mat");

            Transform rack = root.transform.Find("Rack");
            if (rack != null) ReplaceVisualMesh(rack, rackMesh, rackMat);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[TramChanh] Upgraded TeaRack Prefab: " + TeaRackPrefabPath);
        }

        public static void UpgradeToppingStationPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ToppingStationPrefabPath);
            if (prefab == null) return;

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

            Mesh frameMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_ToppingStation_Frame.obj");
            Mesh coverMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_ToppingStation_Cover.obj");
            Material steelMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Steel_Brushed.mat");
            Material coverMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_ToppingStation_Cover.mat");

            Transform stationFrame = root.transform.Find("StationFrame");
            if (stationFrame != null) ReplaceVisualMesh(stationFrame, frameMesh, steelMat);

            Transform cover = root.transform.Find("CoverPivot/TransparentCover");
            if (cover != null) ReplaceVisualMesh(cover, coverMesh, coverMat);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[TramChanh] Upgraded ToppingStation Prefab: " + ToppingStationPrefabPath);
        }

        public static void UpgradeIceBinPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(IceBinPrefabPath);
            if (prefab == null) return;

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

            Mesh wellMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_IceBin_Well.obj");
            Mesh lidsMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_IceBin_Lids.obj");
            Material steelMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Steel_Brushed.mat");

            Transform bin = root.transform.Find("Bin");
            if (bin != null) ReplaceVisualMesh(bin, wellMesh, steelMat);

            // Add sliding lids child under bin if not present
            Transform lids = root.transform.Find("Lids");
            if (lids == null)
            {
                var lidsObj = new GameObject("Lids");
                lidsObj.transform.SetParent(root.transform, false);
                var mf = lidsObj.AddComponent<MeshFilter>();
                mf.sharedMesh = lidsMesh;
                var mr = lidsObj.AddComponent<MeshRenderer>();
                mr.sharedMaterial = steelMat;
            }
            else
            {
                ReplaceVisualMesh(lids, lidsMesh, steelMat);
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[TramChanh] Upgraded IceBin Prefab: " + IceBinPrefabPath);
        }

        public static void UpgradeIceScoopPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(IceScoopPrefabPath);
            if (prefab == null) return;

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

            Mesh scoopMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelsFolder}/SM_IceScoop.obj");
            Material steelMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Steel_Brushed.mat");

            var mf = root.GetComponent<MeshFilter>();
            if (mf == null) mf = root.AddComponent<MeshFilter>();
            mf.sharedMesh = scoopMesh;
            var mr = root.GetComponent<MeshRenderer>();
            if (mr == null) mr = root.AddComponent<MeshRenderer>();
            mr.sharedMaterial = steelMat;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[TramChanh] Upgraded IceScoop Prefab: " + IceScoopPrefabPath);
        }

        private static void ReplaceVisualMesh(Transform target, Mesh newMesh, Material newMat)
        {
            // Remove primitive children like PlaceholderPortion, Cube, etc.
            for (int i = target.childCount - 1; i >= 0; i--)
            {
                Transform child = target.GetChild(i);
                if (child.name.Contains("Placeholder") || child.name.Contains("Cube") || child.name.Contains("Side") || child.name.Contains("Bottom") || child.name.Contains("End"))
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
            keyLight.transform.rotation = Quaternion.Euler(45f, -40f, 0f);
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1.0f, 0.95f, 0.85f);
            keyLight.intensity = 1.6f;

            var fillLight = new GameObject("FillLight").AddComponent<Light>();
            fillLight.transform.SetParent(lighting.transform);
            fillLight.transform.rotation = Quaternion.Euler(30f, 130f, 0f);
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.6f, 0.75f, 0.95f);
            fillLight.intensity = 0.8f;

            // Countertop stage
            var stage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stage.name = "Countertop_Stage";
            stage.transform.position = new Vector3(0f, -0.02f, 0f);
            stage.transform.localScale = new Vector3(2.0f, 0.04f, 1.0f);
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/TramChanh/Art/Materials/MAT_Stall_WoodCounter.mat");
            if (woodMat != null) stage.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // Instantiate equipment
            GameObject teaRackPf = AssetDatabase.LoadAssetAtPath<GameObject>(TeaRackPrefabPath);
            if (teaRackPf != null)
            {
                var rack = (GameObject)PrefabUtility.InstantiatePrefab(teaRackPf);
                rack.transform.position = new Vector3(-0.45f, 0f, 0.05f);
            }

            GameObject teaBagPf = AssetDatabase.LoadAssetAtPath<GameObject>(TeaBagPrefabPath);
            if (teaBagPf != null)
            {
                var teabag = (GameObject)PrefabUtility.InstantiatePrefab(teaBagPf);
                teabag.transform.position = new Vector3(-0.15f, 0f, 0.15f);

                var teabag2 = (GameObject)PrefabUtility.InstantiatePrefab(teaBagPf);
                teabag2.transform.position = new Vector3(0.02f, 0f, 0.15f);
                // Set open state for teabag2
                var openObj = teabag2.transform.Find("Visual/Bag_Open");
                var closedObj = teabag2.transform.Find("Visual/Bag_Closed");
                if (openObj != null) openObj.gameObject.SetActive(true);
                if (closedObj != null) closedObj.gameObject.SetActive(false);
            }

            GameObject toppingPf = AssetDatabase.LoadAssetAtPath<GameObject>(ToppingStationPrefabPath);
            if (toppingPf != null)
            {
                var topping = (GameObject)PrefabUtility.InstantiatePrefab(toppingPf);
                topping.transform.position = new Vector3(0.40f, 0f, 0f);
            }

            EditorSceneManager.SaveScene(scene, PreviewScenePath);
            Debug.Log("[TramChanh] Saved Drink Preview Scene: " + PreviewScenePath);
        }

        public static void CaptureScreenshots()
        {
            EnsureFolder(ScreenshotsFolder);
            var scene = EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Single);

            // Render camera
            var camObj = new GameObject("QACamera");
            var cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 50f;

            // Views:
            // 1. Close-up on Pre-Portioned Tea Bags
            camObj.transform.position = new Vector3(-0.06f, 0.18f, -0.28f);
            camObj.transform.rotation = Quaternion.Euler(22f, -15f, 0f);
            CaptureCameraToFile(cam, $"{ScreenshotsFolder}/Drink_TeaBag_Detail.png", 1920, 1080);

            // 2. Full Drink Station Overview
            camObj.transform.position = new Vector3(0.0f, 0.55f, -0.85f);
            camObj.transform.rotation = Quaternion.Euler(32f, 0f, 0f);
            CaptureCameraToFile(cam, $"{ScreenshotsFolder}/Drink_Station_Overview.png", 1920, 1080);

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
