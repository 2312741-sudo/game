using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TramChanh.EditorTools
{
    public static class FixArtPriority4
    {
        public const string MainScenePath = "Assets/TramChanh/Scenes/Gameplay/SCN_TramChanh_Main.unity";
        public const string ScreenshotsDir = "QA/Screenshots/ART_FIX_007";

        [MenuItem("Tram Chanh/Art/Capture Item 4 Before")]
        public static void CaptureBefore()
        {
            CaptureMenuViewInternal("Item4_MenuBoard_Before.png");
        }

        [MenuItem("Tram Chanh/Art/Capture Item 4 After")]
        public static void CaptureAfter()
        {
            CaptureMenuViewInternal("Item4_MenuBoard_After.png");
        }

        public static void FindBlockingObjects()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var bootstrap = Object.FindAnyObjectByType<TramChanh.App.TramChanhMainBootstrap>();
            GameObject envInst = null;
            if (bootstrap != null)
            {
                var so = new SerializedObject(bootstrap);
                var envPf = so.FindProperty("_environmentPrefab").objectReferenceValue as GameObject;
                if (envPf != null)
                {
                    envInst = PrefabUtility.InstantiatePrefab(envPf) as GameObject;
                    envInst.transform.position = Vector3.zero;
                }
            }

            try
            {
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    var b = r.bounds;
                    // Check if bounds overlap around stall right front corner: X in [0.2, 1.5], Y in [0.5, 2.5], Z in [0.0, 2.0]
                    if (b.min.x <= 1.5f && b.max.x >= 0.2f &&
                        b.min.y <= 2.5f && b.max.y >= 0.5f &&
                        b.min.z <= 2.0f && b.max.z >= 0.0f)
                    {
                        Debug.Log($"[BlockingCandidate] name='{r.gameObject.name}', path='{GetPath(r.transform)}', bounds={b.min} to {b.max}, mat='{r.sharedMaterial?.name}'");
                    }
                }
            }
            finally
            {
                if (envInst != null) Object.DestroyImmediate(envInst);
            }
        }

        private static string GetPath(Transform t)
        {
            if (t.parent == null) return t.name;
            return GetPath(t.parent) + "/" + t.name;
        }

        private static void CaptureMenuViewInternal(string filename)
        {
            if (!Directory.Exists(ScreenshotsDir)) Directory.CreateDirectory(ScreenshotsDir);

            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/TramChanh/Art/Materials/MAT_Menu_Board.mat");
            var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TramChanh/Art/Textures/T_Menu_Board_BaseColor.png");
            var emTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TramChanh/Art/Textures/T_Menu_Board_Emission.png");
            if (mat != null)
            {
                if (baseTex != null) mat.SetTexture("_BaseMap", baseTex);
                if (emTex != null) mat.SetTexture("_EmissionMap", emTex);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.85f, 0.82f, 0.75f, 1.0f));
                EditorUtility.SetDirty(mat);
                AssetDatabase.SaveAssets();

                var bm = mat.GetTexture("_BaseMap");
                var em = mat.GetTexture("_EmissionMap");
                Debug.Log($"[Item 4 TextureCheck] BaseMap: {bm?.name} ({bm?.width}x{bm?.height}), EmissionMap: {em?.name} ({em?.width}x{em?.height})");
            }

            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            // Instantiate environment
            var bootstrap = Object.FindAnyObjectByType<TramChanh.App.TramChanhMainBootstrap>();
            GameObject envInst = null;
            if (bootstrap != null)
            {
                var so = new SerializedObject(bootstrap);
                var envPf = so.FindProperty("_environmentPrefab").objectReferenceValue as GameObject;
                if (envPf != null)
                {
                    envInst = PrefabUtility.InstantiatePrefab(envPf) as GameObject;
                    envInst.transform.position = Vector3.zero;
                }
            }

            try
            {
                var bv = envInst != null ? envInst.transform.Find("MenuBoard/BoardVisual") : null;
                if (bv != null)
                {
                    var mr = bv.GetComponent<MeshRenderer>();
                    var mf = bv.GetComponent<MeshFilter>();
                    var m = mf?.sharedMesh;
                    Debug.Log($"[Item 4] Mesh: {m?.name}, verts={m?.vertexCount}, uvs={m?.uv?.Length}");
                    if (m != null && m.uv != null)
                    {
                        for (int i = 0; i < m.uv.Length; i++)
                        {
                            Debug.Log($"  uv[{i}] = {m.uv[i]}, v[{i}] = {m.vertices[i]}");
                        }
                    }
                    Debug.Log($"[Item 4] BoardVisual worldPos={bv.position}, scale={bv.lossyScale}, rot={bv.rotation.eulerAngles}, mat={mr?.sharedMaterial?.name}, mainTex={mr?.sharedMaterial?.mainTexture?.name}");
                }
                else
                {
                    Debug.LogWarning("[Item 4] BoardVisual NOT found under MenuBoard!");
                }

                Vector3 camPos = new Vector3(1.05f, 1.18f, 1.35f);
                Vector3 lookTarget = new Vector3(1.05f, 1.15f, 0.44f);

                CaptureView(camPos, lookTarget, 45f, Path.Combine(ScreenshotsDir, filename), 1920, 1080);
                Debug.Log($"[Item 4] Captured {filename}");
            }
            finally
            {
                if (envInst != null) Object.DestroyImmediate(envInst);
            }
        }

        private static void CaptureView(Vector3 pos, Vector3 target, float fov, string outPath, int w, int h)
        {
            var camGo = new GameObject("CaptureCam");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 300f;
            cam.clearFlags = CameraClearFlags.Skybox;

            cam.transform.position = pos;
            cam.transform.LookAt(target);

            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);

            try
            {
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();

                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(outPath, bytes);
            }
            finally
            {
                RenderTexture.active = null;
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(camGo);
            }
        }
    }
}
