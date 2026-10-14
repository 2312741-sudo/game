using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using TramChanh.Core.GroundTruth;
using TramChanh.Core.Provisional;

namespace TramChanh.EditorTools
{
    public static class StreetPropsPrefabBuilder
    {
        private const string MaterialsFolder = "Assets/TramChanh/Art/Materials";
        private const string TexturesFolder = "Assets/TramChanh/Art/Textures";
        private const string PrefabsFolder = "Assets/TramChanh/Prefabs/Environment";
        private const string FurniturePrefabsFolder = "Assets/TramChanh/Prefabs/Furniture";
        private const string ModelsFolder = "Assets/TramChanh/Art/Models/Environment";
        private const string FurnitureModelsFolder = "Assets/TramChanh/Art/Models/Furniture";

        public static void BuildAllStreetProps()
        {
            EnsureFolder(MaterialsFolder);
            EnsureFolder(PrefabsFolder);
            EnsureFolder(FurniturePrefabsFolder);

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            // 1. Food Cart Material with texture
            Texture2D cartTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_StreetVendor_Cart_BaseColor.png");
            var matCart = GetOrCreateMaterialWithTexture("MAT_StreetVendor_Cart", urpLit, cartTex, Color.white);

            // 2. Umbrella Material
            var matUmbrella = GetOrCreateMaterial("MAT_Sidewalk_Umbrella", urpLit, new Color(0.20f, 0.55f, 0.85f));

            // 3. Canopy Material (red/white or orange/cream shopfront awning)
            var matCanopy = GetOrCreateMaterial("MAT_Shopfront_Canopy", urpLit, new Color(0.85f, 0.40f, 0.20f));

            // 4. Lantern Material
            var matLantern = GetOrCreateMaterial("MAT_Street_PostLantern", urpLit, new Color(0.18f, 0.18f, 0.20f));

            // 5. Build Food Cart Prefab (Poly by Google: extents 20x6.3x9.6, scale 0.10 -> 2.0m x 0.63m x 0.96m)
            BuildPropPrefab(
                "PF_StreetVendor_Cart",
                PrefabsFolder,
                $"{ModelsFolder}/SM_StreetVendor_Cart.obj",
                scale: 0.10f,
                rotOffset: Quaternion.identity,
                posOffset: Vector3.zero,
                materials: new[] { matCart },
                colCenter: new Vector3(0f, 0.50f, 0.25f),
                colSize: new Vector3(2.0f, 1.0f, 1.0f)
            );

            // 6. Build Sidewalk Umbrella Prefab (extents 0.57x0.67x0.51, scale 3.5 -> 2.0m x 2.35m x 1.8m)
            BuildPropPrefab(
                "PF_Sidewalk_Umbrella",
                PrefabsFolder,
                $"{ModelsFolder}/SM_Sidewalk_Umbrella.obj",
                scale: 3.5f,
                rotOffset: Quaternion.identity,
                posOffset: Vector3.zero,
                materials: new[] { matUmbrella },
                colCenter: new Vector3(0f, 1.15f, 0f),
                colSize: new Vector3(0.3f, 2.3f, 0.3f)
            );

            // 7. Build Canopy Prefab (extents 9.7x4.77x5.03, scale 0.35 -> 3.4m x 1.67m x 1.76m)
            BuildPropPrefab(
                "PF_Shopfront_Canopy",
                PrefabsFolder,
                $"{ModelsFolder}/SM_Shopfront_Canopy.obj",
                scale: 0.35f,
                rotOffset: Quaternion.identity,
                posOffset: Vector3.zero,
                materials: new[] { matCanopy },
                colCenter: new Vector3(0f, 1.0f, 0f),
                colSize: new Vector3(3.4f, 1.5f, 1.8f)
            );

            // 8. Build Post Lantern Prefab (scale 1.0)
            BuildPropPrefab(
                "PF_Street_PostLantern",
                PrefabsFolder,
                $"{ModelsFolder}/SM_Street_PostLantern.fbx",
                scale: 0.8f,
                rotOffset: Quaternion.identity,
                posOffset: Vector3.zero,
                materials: new[] { matLantern },
                colCenter: new Vector3(0f, 1.0f, 0f),
                colSize: new Vector3(0.35f, 2.0f, 0.35f)
            );

            // 9. Build Plastic Table & Chairs Prefab (SM_PlasticTableChairs_VN.obj)
            Material matRed = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Plastic_Red.mat");
            Material matBlue = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Plastic_Blue.mat");
            BuildPropPrefab(
                "PF_PlasticTableChairs_VN",
                FurniturePrefabsFolder,
                $"{FurnitureModelsFolder}/SM_PlasticTableChairs_VN.obj",
                scale: 1.0f,
                rotOffset: Quaternion.identity,
                posOffset: Vector3.zero,
                materials: new[] { matRed, matBlue },
                colCenter: new Vector3(0f, 0.28f, 0f),
                colSize: new Vector3(0.95f, 0.55f, 0.70f)
            );

            // 10. Build Street Plastic Crate Prefab (SM_PlasticCrate_Street.obj)
            Material matCrate = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_PlasticCrate_Street.mat");
            BuildPropPrefab(
                "PF_PlasticCrate_Street",
                PrefabsFolder,
                $"{ModelsFolder}/SM_PlasticCrate_Street.obj",
                scale: 1.0f,
                rotOffset: Quaternion.identity,
                posOffset: Vector3.zero,
                materials: new[] { matCrate },
                colCenter: new Vector3(0f, 0.16f, 0f),
                colSize: new Vector3(0.48f, 0.32f, 0.32f)
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TramChanh] Successfully built all street props prefabs!");
        }

        private static void BuildPropPrefab(string prefabName, string targetFolder, string modelPath, float scale, Quaternion rotOffset, Vector3 posOffset, Material[] materials, Vector3 colCenter, Vector3 colSize)
        {
            string prefabPath = $"{targetFolder}/{prefabName}.prefab";
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                Debug.LogError($"[StreetPropsPrefabBuilder] Could not load model at {modelPath}");
                return;
            }

            GameObject root = new GameObject(prefabName);
            root.layer = TramChanhLayers.EnvironmentIndex;

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, root.transform);
            visual.name = "Visual";
            visual.layer = TramChanhLayers.EnvironmentIndex;
            visual.transform.localPosition = posOffset;
            visual.transform.localRotation = rotOffset;
            visual.transform.localScale = Vector3.one * scale;

            var renderers = visual.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.gameObject.layer = TramChanhLayers.EnvironmentIndex;
                if (materials != null && materials.Length > 0)
                {
                    if (materials.Length == 1)
                    {
                        var mats = new Material[r.sharedMaterials.Length];
                        for (int i = 0; i < mats.Length; i++) mats[i] = materials[0];
                        r.sharedMaterials = mats;
                    }
                    else
                    {
                        var mats = new Material[Math.Min(r.sharedMaterials.Length, materials.Length)];
                        for (int i = 0; i < mats.Length; i++) mats[i] = materials[i];
                        r.sharedMaterials = mats;
                    }
                }
            }

            var col = root.AddComponent<BoxCollider>();
            col.center = colCenter;
            col.size = colSize;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[StreetPropsPrefabBuilder] Saved prefab: {prefabPath}");
        }

        private static Material GetOrCreateMaterial(string name, Shader shader, Color color)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.name = name;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static Material GetOrCreateMaterialWithTexture(string name, Shader shader, Texture2D texture, Color tint)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.name = name;
                if (texture != null)
                {
                    if (mat.HasProperty("_BaseColorMap")) mat.SetTexture("_BaseColorMap", texture);
                    else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
                }
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                if (texture != null)
                {
                    if (mat.HasProperty("_BaseColorMap")) mat.SetTexture("_BaseColorMap", texture);
                    else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
                }
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
    }
}
