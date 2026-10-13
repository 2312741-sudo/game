using UnityEditor;
using UnityEngine;

namespace TramChanh.Editor
{
    public static class PedestriansPrefabBuilder
    {
        [MenuItem("TramChanh/Build Pedestrian Prefabs")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            BuildPersonPrefab(
                "Assets/TramChanh/Prefabs/People/PF_Person_07.prefab",
                "Assets/TramChanh/Art/Models/People/SM_Person_MaleCasual.fbx",
                "PF_Person_07"
            );

            BuildPersonPrefab(
                "Assets/TramChanh/Prefabs/People/PF_Person_08.prefab",
                "Assets/TramChanh/Art/Models/People/SM_Person_FemaleTankTop.fbx",
                "PF_Person_08"
            );

            AssetDatabase.SaveAssets();
            Debug.Log("[TramChanh] All pedestrian prefabs created successfully.");
        }

        private static void BuildPersonPrefab(string prefabPath, string modelPath, string name)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                Debug.LogError($"[TramChanh] Failed to load person model at {modelPath}");
                return;
            }

            var root = new GameObject(name);
            root.layer = LayerMask.NameToLayer("Characters") >= 0 ? LayerMask.NameToLayer("Characters") : 0;

            var visual = Object.Instantiate(modelAsset, root.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // Ensure Character layer on all children
            SetLayerRecursively(visual, root.layer);

            // Calculate total triangles
            int totalTris = 0;
            foreach (var smr in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh != null) totalTris += smr.sharedMesh.triangles.Length / 3;
            }
            foreach (var mf in visual.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh != null) totalTris += mf.sharedMesh.triangles.Length / 3;
            }

            Debug.Log($"[TramChanh] {name} total triangles: {totalTris}");

            // Add standard CapsuleCollider for pedestrian body if not present
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.28f;
            col.height = 1.8f;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[TramChanh] Built pedestrian prefab: {prefabPath}");
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
