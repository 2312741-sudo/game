using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TramChanh.EditorTools
{
    public static class FixArtPriority3
    {
        public const string MainScenePath = "Assets/TramChanh/Scenes/Gameplay/SCN_TramChanh_Main.unity";
        public const string ScreenshotsDir = "QA/Screenshots/ART_FIX_007";

        [MenuItem("Tram Chanh/Art/Capture Item 3 Before")]
        public static void CaptureBefore()
        {
            CaptureSignViewInternal("Item3_Signboard_Before.png");
        }

        [MenuItem("Tram Chanh/Art/Capture Item 3 After")]
        public static void CaptureAfter()
        {
            CaptureSignViewInternal("Item3_Signboard_After.png");
        }

        private static void CaptureSignViewInternal(string filename)
        {
            if (!Directory.Exists(ScreenshotsDir)) Directory.CreateDirectory(ScreenshotsDir);

            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/TramChanh/Art/Materials/MAT_Sign_TramChanh_New.mat");
            if (mat != null)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.5f, 0.5f, 0.5f, 1.0f));
                EditorUtility.SetDirty(mat);
                AssetDatabase.SaveAssets();
            }

            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            // Instantiate environment if needed
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
                // Customer eye-level straight-on view looking directly at the signboard above the stall
                // Stall is at (0, 0, 0), Signboard is at (0, 1.85, 0.38) facing +Z
                // Customer eye height at 1.70m, positioned at (0, 1.75m, 2.8m), looking directly at the sign center (0, 1.85m, 0.38m)
                Vector3 camPos = new Vector3(0.0f, 1.75f, 2.7f);
                Vector3 lookTarget = new Vector3(0.0f, 1.85f, 0.38f);

                CaptureView(camPos, lookTarget, 42f, Path.Combine(ScreenshotsDir, filename), 1920, 1080);
                Debug.Log($"[Item 3] Captured {filename}");
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
