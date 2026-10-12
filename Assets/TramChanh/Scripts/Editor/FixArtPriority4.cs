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

        private static void CaptureMenuViewInternal(string filename)
        {
            if (!Directory.Exists(ScreenshotsDir)) Directory.CreateDirectory(ScreenshotsDir);

            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/TramChanh/Art/Materials/MAT_Menu_Board.mat");
            var emTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TramChanh/Art/Textures/T_Menu_Board_Emission.png");
            if (mat != null)
            {
                if (emTex != null) mat.SetTexture("_EmissionMap", emTex);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.85f, 0.82f, 0.75f, 1.0f));
                EditorUtility.SetDirty(mat);
                AssetDatabase.SaveAssets();
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
                // Customer eye-level perspective looking at MenuBoard on the stall
                // Player stands near stall right-front corner (0.8m, 1.35m, 1.8m), looking at menu board (1.05m, 1.15m, 0.35m)
                Vector3 camPos = new Vector3(0.85f, 1.35f, 1.85f);
                Vector3 lookTarget = new Vector3(1.05f, 1.15f, 0.35f);

                CaptureView(camPos, lookTarget, 40f, Path.Combine(ScreenshotsDir, filename), 1920, 1080);
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
