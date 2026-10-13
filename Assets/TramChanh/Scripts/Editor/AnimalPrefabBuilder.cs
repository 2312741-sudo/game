using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using TramChanh.Core.GroundTruth;
using TramChanh.Core.Provisional;

namespace TramChanh.EditorTools
{
    public static class AnimalPrefabBuilder
    {
        private const string MaterialsFolder = "Assets/TramChanh/Art/Materials";
        private const string PrefabsFolder = "Assets/TramChanh/Prefabs/Environment";
        private const string ModelsFolder = "Assets/TramChanh/Art/Models/Environment";

        public static void BuildAllAnimals()
        {
            EnsureFolder(MaterialsFolder);
            EnsureFolder(PrefabsFolder);

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            // 1. Create Materials
            var matDog = GetOrCreateMaterial("MAT_Animal_Dog", urpLit, new Color(0.82f, 0.58f, 0.35f));
            var matCatMain = GetOrCreateMaterial("MAT_Animal_Cat_Main", urpLit, new Color(0.90f, 0.60f, 0.32f));
            var matCatSec = GetOrCreateMaterial("MAT_Animal_Cat_Secondary", urpLit, new Color(0.95f, 0.93f, 0.88f));
            var matCatEars = GetOrCreateMaterial("MAT_Animal_Cat_Ears", urpLit, new Color(0.95f, 0.75f, 0.75f));
            var matChickMain = GetOrCreateMaterial("MAT_Animal_Chicken_Main", urpLit, new Color(0.95f, 0.88f, 0.72f));
            var matChickRed = GetOrCreateMaterial("MAT_Animal_Chicken_Red", urpLit, new Color(0.85f, 0.15f, 0.15f));
            var matChickBeak = GetOrCreateMaterial("MAT_Animal_Chicken_Beak", urpLit, new Color(0.95f, 0.75f, 0.15f));
            var matPigMain = GetOrCreateMaterial("MAT_Animal_Pigeon_Main", urpLit, new Color(0.48f, 0.52f, 0.58f));
            var matPigSec = GetOrCreateMaterial("MAT_Animal_Pigeon_Secondary", urpLit, new Color(0.32f, 0.35f, 0.40f));
            var matEyeWhite = GetOrCreateMaterial("MAT_Animal_Eye_White", urpLit, new Color(0.95f, 0.95f, 0.95f));
            var matEyeBlack = GetOrCreateMaterial("MAT_Animal_Eye_Black", urpLit, new Color(0.10f, 0.10f, 0.10f));

            // 2. Build Dog Prefab (Target height ~ 0.60m)
            BuildAnimalPrefab(
                "PF_Street_Dog",
                $"{ModelsFolder}/SM_Street_Dog.fbx",
                scale: 0.32f,
                materials: new[] { matDog },
                colCenter: new Vector3(0f, 0.30f, 0f),
                colSize: new Vector3(0.30f, 0.60f, 0.68f)
            );

            // 3. Build Cat Prefab (Target height ~ 0.32m)
            // Materials: Eye_White, Eye_Black, Cat_Main, Cat_Secondary, Ears
            BuildAnimalPrefab(
                "PF_Street_Cat",
                $"{ModelsFolder}/SM_Street_Cat.fbx",
                scale: 0.165f,
                materials: new[] { matEyeWhite, matEyeBlack, matCatMain, matCatSec, matCatEars },
                colCenter: new Vector3(0f, 0.16f, 0f),
                colSize: new Vector3(0.24f, 0.32f, 0.30f)
            );

            // 4. Build Chicken Prefab (Target height ~ 0.38m)
            // Materials: Beak, Eye_Black, Chicken_Red, Eye_White, Chicken_Main
            BuildAnimalPrefab(
                "PF_Street_Chicken",
                $"{ModelsFolder}/SM_Street_Chicken.fbx",
                scale: 0.16f,
                materials: new[] { matChickBeak, matEyeBlack, matChickRed, matEyeWhite, matChickMain },
                colCenter: new Vector3(0f, 0.19f, 0f),
                colSize: new Vector3(0.25f, 0.38f, 0.34f)
            );

            // 5. Build Pigeon Prefab (Target height ~ 0.22m)
            // Materials: Eye_White, Eye_Black, Pigeon_Main, Pigeon_Secondary
            BuildAnimalPrefab(
                "PF_Street_Pigeon",
                $"{ModelsFolder}/SM_Street_Pigeon.fbx",
                scale: 0.12f,
                materials: new[] { matEyeWhite, matEyeBlack, matPigMain, matPigSec },
                colCenter: new Vector3(0f, 0.11f, 0f),
                colSize: new Vector3(0.18f, 0.22f, 0.24f)
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TramChanh] Successfully built all 4 street animal prefabs!");
        }

        private static void BuildAnimalPrefab(string prefabName, string fbxPath, float scale, Material[] materials, Vector3 colCenter, Vector3 colSize)
        {
            string prefabPath = $"{PrefabsFolder}/{prefabName}.prefab";
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null)
            {
                Debug.LogError($"[AnimalPrefabBuilder] Could not load FBX at {fbxPath}");
                return;
            }

            GameObject root = new GameObject(prefabName);
            root.layer = TramChanhLayers.EnvironmentIndex;

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
            visual.name = "Visual";
            visual.layer = TramChanhLayers.EnvironmentIndex;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * scale;

            // Set materials on all renderers
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

            // Add Collider on root
            var col = root.AddComponent<BoxCollider>();
            col.center = colCenter;
            col.size = colSize;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[AnimalPrefabBuilder] Saved prefab: {prefabPath}");
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

        private static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
    }
}
