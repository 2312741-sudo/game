using UnityEditor;
using UnityEngine;

namespace TramChanh.Editor
{
    public static class VehiclesPrefabBuilder
    {
        [MenuItem("TramChanh/Build Vehicles Prefabs")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            // 1. Bicycle Material
            var matBike = EnsureMaterial("Assets/TramChanh/Art/Materials/MAT_Bicycle.mat", urpLit);
            var texBike = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TramChanh/Art/Textures/T_Bicycle_BaseColor.png");
            if (texBike != null) matBike.SetTexture("_BaseMap", texBike);
            EditorUtility.SetDirty(matBike);

            // 2. Motorcycle Cub Material
            var matCub = EnsureMaterial("Assets/TramChanh/Art/Materials/MAT_Motorcycle_Cub.mat", urpLit);
            var texCub = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TramChanh/Art/Textures/T_Motorcycle_Cub_BaseColor.png");
            if (texCub != null) matCub.SetTexture("_BaseMap", texCub);
            EditorUtility.SetDirty(matCub);

            // 3. Truck Small Materials
            var matTruckAtlas = EnsureMaterial("Assets/TramChanh/Art/Materials/MAT_Truck_Small_Atlas.mat", urpLit);
            var texTruck = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TramChanh/Art/Textures/T_Truck_Small_Atlas.png");
            if (texTruck != null) matTruckAtlas.SetTexture("_BaseMap", texTruck);
            EditorUtility.SetDirty(matTruckAtlas);

            var matTruckHead = EnsureMaterial("Assets/TramChanh/Art/Materials/MAT_Truck_Small_Headlights.mat", urpLit);
            matTruckHead.SetColor("_BaseColor", new Color(0.9f, 0.8f, 0.4f, 1f));
            matTruckHead.SetColor("_EmissionColor", new Color(1.5f, 1.2f, 0.4f, 1f));
            matTruckHead.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(matTruckHead);

            var matTruckBrake = EnsureMaterial("Assets/TramChanh/Art/Materials/MAT_Truck_Small_BrakeLight.mat", urpLit);
            matTruckBrake.SetColor("_BaseColor", new Color(0.8f, 0.15f, 0.15f, 1f));
            matTruckBrake.SetColor("_EmissionColor", new Color(1.0f, 0.2f, 0.2f, 1f));
            matTruckBrake.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(matTruckBrake);

            // 4. SUV Materials
            var matSuvBody = EnsureMaterial("Assets/TramChanh/Art/Materials/MAT_Car_SUV_Body.mat", urpLit);
            matSuvBody.SetColor("_BaseColor", new Color(0.18f, 0.28f, 0.42f, 1f));
            matSuvBody.SetFloat("_Smoothness", 0.75f);
            matSuvBody.SetFloat("_Metallic", 0.2f);
            EditorUtility.SetDirty(matSuvBody);

            var matSuvWin = EnsureMaterial("Assets/TramChanh/Art/Materials/MAT_Car_SUV_Windows.mat", urpLit);
            matSuvWin.SetColor("_BaseColor", new Color(0.06f, 0.08f, 0.10f, 1f));
            matSuvWin.SetFloat("_Smoothness", 0.9f);
            EditorUtility.SetDirty(matSuvWin);

            var matSuvBlack = EnsureMaterial("Assets/TramChanh/Art/Materials/MAT_Car_SUV_Black.mat", urpLit);
            matSuvBlack.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.05f, 1f));
            matSuvBlack.SetFloat("_Smoothness", 0.2f);
            EditorUtility.SetDirty(matSuvBlack);

            var matSuvGrey = EnsureMaterial("Assets/TramChanh/Art/Materials/MAT_Car_SUV_Grey.mat", urpLit);
            matSuvGrey.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.52f, 1f));
            matSuvGrey.SetFloat("_Smoothness", 0.6f);
            matSuvGrey.SetFloat("_Metallic", 0.5f);
            EditorUtility.SetDirty(matSuvGrey);

            var matSuvHead = EnsureMaterial("Assets/TramChanh/Art/Materials/MAT_Car_SUV_Headlights.mat", urpLit);
            matSuvHead.SetColor("_BaseColor", new Color(0.95f, 0.85f, 0.6f, 1f));
            matSuvHead.SetColor("_EmissionColor", new Color(1.5f, 1.3f, 0.8f, 1f));
            matSuvHead.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(matSuvHead);

            var matSuvTail = EnsureMaterial("Assets/TramChanh/Art/Materials/MAT_Car_SUV_TailLights.mat", urpLit);
            matSuvTail.SetColor("_BaseColor", new Color(0.85f, 0.1f, 0.1f, 1f));
            matSuvTail.SetColor("_EmissionColor", new Color(1.2f, 0.15f, 0.15f, 1f));
            matSuvTail.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(matSuvTail);

            AssetDatabase.SaveAssets();

            // Build prefabs
            BuildSimpleVehiclePrefab(
                "Assets/TramChanh/Prefabs/Environment/PF_Bicycle.prefab",
                "Assets/TramChanh/Art/Models/Environment/SM_Bicycle.obj",
                new Material[] { matBike },
                new Vector3(0.46f, 0.98f, 1.64f),
                new Vector3(0f, 0.49f, 0f)
            );

            BuildSimpleVehiclePrefab(
                "Assets/TramChanh/Prefabs/Environment/PF_Motorcycle_Cub.prefab",
                "Assets/TramChanh/Art/Models/Environment/SM_Motorcycle_Cub.obj",
                new Material[] { matCub },
                new Vector3(0.82f, 1.13f, 1.74f),
                new Vector3(0f, 0.565f, 0f)
            );

            BuildModelBasedPrefab(
                "Assets/TramChanh/Prefabs/Environment/PF_Truck_Small.prefab",
                "Assets/TramChanh/Art/Models/Environment/SM_Truck_Small.obj",
                new Material[] { matTruckAtlas, matTruckHead, matTruckBrake },
                new Vector3(2.71f, 2.88f, 5.26f),
                new Vector3(0f, 1.44f, -0.43f)
            );

            BuildModelBasedPrefab(
                "Assets/TramChanh/Prefabs/Environment/PF_Car_SUV.prefab",
                "Assets/TramChanh/Art/Models/Environment/SM_Car_SUV.obj",
                new Material[] { matSuvBody, matSuvWin, matSuvBlack, matSuvGrey, matSuvHead, matSuvTail },
                new Vector3(2.11f, 1.53f, 4.21f),
                new Vector3(0f, 0.765f, 0f)
            );

            AssetDatabase.SaveAssets();
            Debug.Log("[TramChanh] All vehicle prefabs created successfully.");
        }

        private static Material EnsureMaterial(string path, Shader shader)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void BuildSimpleVehiclePrefab(string prefabPath, string modelPath, Material[] mats, Vector3 colSize, Vector3 colCenter)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                Debug.LogError($"[TramChanh] Failed to load model at {modelPath}");
                return;
            }

            Mesh mesh = null;
            MeshFilter mf = modelAsset.GetComponentInChildren<MeshFilter>();
            if (mf != null) mesh = mf.sharedMesh;
            if (mesh == null)
            {
                // try sub-assets
                Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
                foreach (var sa in subAssets)
                {
                    if (sa is Mesh m) { mesh = m; break; }
                }
            }

            var root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefabPath));
            root.layer = LayerMask.NameToLayer("Environment");

            var visual = new GameObject("Visual");
            visual.layer = root.layer;
            visual.transform.SetParent(root.transform, false);

            var visualMf = visual.AddComponent<MeshFilter>();
            visualMf.sharedMesh = mesh;

            var visualMr = visual.AddComponent<MeshRenderer>();
            visualMr.sharedMaterials = mats;

            var col = root.AddComponent<BoxCollider>();
            col.size = colSize;
            col.center = colCenter;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[TramChanh] Built prefab: {prefabPath}");
        }

        private static void BuildModelBasedPrefab(string prefabPath, string modelPath, Material[] fallbackMats, Vector3 colSize, Vector3 colCenter)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                Debug.LogError($"[TramChanh] Failed to load model at {modelPath}");
                return;
            }

            var root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefabPath));
            root.layer = LayerMask.NameToLayer("Environment");

            var visual = Object.Instantiate(modelAsset, root.transform);
            visual.name = "Visual";
            visual.layer = root.layer;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // set layers recursively and assign materials
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.gameObject.layer = root.layer;
                if (r is MeshRenderer mr && fallbackMats != null && fallbackMats.Length > 0)
                {
                    mr.sharedMaterials = fallbackMats;
                }
            }

            var col = root.AddComponent<BoxCollider>();
            col.size = colSize;
            col.center = colCenter;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[TramChanh] Built model-based prefab: {prefabPath}");
        }
    }
}
