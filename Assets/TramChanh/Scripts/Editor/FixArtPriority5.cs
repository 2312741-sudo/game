using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TramChanh.EditorTools
{
    public static class FixArtPriority5
    {
        public const string DrinkPreviewScenePath = "Assets/TramChanh/Scenes/Art/SCN_Art_DrinkPreview.unity";
        public const string ScreenshotsDir = "QA/Screenshots/ART_FIX_007";

        [MenuItem("Tram Chanh/Art/Capture Item 5 Before")]
        public static void CaptureBefore()
        {
            CaptureTeaBagViewInternal("Item5_TeaBag_Before.png");
        }

        [MenuItem("Tram Chanh/Art/Capture Item 5 After")]
        public static void CaptureAfter()
        {
            CaptureTeaBagViewInternal("Item5_TeaBag_After.png");
        }

        private static void CaptureTeaBagViewInternal(string filename)
        {
            if (!Directory.Exists(ScreenshotsDir)) Directory.CreateDirectory(ScreenshotsDir);

            // If preview scene doesn't exist, create it via DrinkEquipmentBuilder
            if (!File.Exists(DrinkPreviewScenePath))
            {
                DrinkEquipmentBuilder.CreatePreviewScene();
            }

            var scene = EditorSceneManager.OpenScene(DrinkPreviewScenePath, OpenSceneMode.Single);

            try
            {
                // Close-up player/operator perspective on pre-portioned tea bags on the counter
                Vector3 camPos = new Vector3(-0.05f, 0.14f, -0.22f);
                Quaternion camRot = Quaternion.Euler(15f, -8f, 0f);

                CaptureView(camPos, camRot, 45f, Path.Combine(ScreenshotsDir, filename), 1920, 1080);
                Debug.Log($"[Item 5] Captured {filename}");
            }
            finally
            {
            }
        }

        private static void CaptureView(Vector3 pos, Quaternion rot, float fov, string outPath, int w, int h)
        {
            var camGo = new GameObject("CaptureCam");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 50f;
            cam.clearFlags = CameraClearFlags.Skybox;

            cam.transform.position = pos;
            cam.transform.rotation = rot;

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
