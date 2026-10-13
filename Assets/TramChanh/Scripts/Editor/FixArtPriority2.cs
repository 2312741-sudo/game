using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TramChanh.EditorTools
{
    public static class FixArtPriority2
    {
        public const string MainScenePath = "Assets/TramChanh/Scenes/Gameplay/SCN_TramChanh_Main.unity";
        public const string ScreenshotsDir = "QA/Screenshots/ART_FIX_007";

        [MenuItem("Tram Chanh/Art/Capture Item 2 Before")]
        public static void CaptureBefore()
        {
            CaptureViewInternal("Item2_BuildingFoundation_Before.png");
        }

        [MenuItem("Tram Chanh/Art/Capture Item 2 After")]
        public static void CaptureAfter()
        {
            CaptureViewInternal("Item2_BuildingFoundation_After.png");
        }

        private static void CaptureViewInternal(string filename)
        {
            if (!Directory.Exists(ScreenshotsDir)) Directory.CreateDirectory(ScreenshotsDir);

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
                // Street-level horizontal view looking at building foundation / sidewalk interface
                // Player eye height near ground (0.4m), looking directly at the base of South shophouse
                Vector3 camPos = new Vector3(2.5f, 0.35f, -2.5f);
                Vector3 lookTarget = new Vector3(2.5f, 0.05f, -5.0f);

                CaptureView(camPos, lookTarget, 55f, Path.Combine(ScreenshotsDir, filename), 1920, 1080);
                Debug.Log($"[Item 2] Captured {filename}");
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
