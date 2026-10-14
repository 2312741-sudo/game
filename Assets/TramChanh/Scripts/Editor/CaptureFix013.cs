using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TramChanh.EditorTools
{
    public static class CaptureFix013
    {
        public const string MainScenePath = "Assets/TramChanh/Scenes/Gameplay/SCN_TramChanh_Main.unity";
        public const string ScreenshotsDir = "QA/Screenshots/ART_FIX_013";

        [MenuItem("Tram Chanh/Art/Capture All ART-FIX-013 Deliverables")]
        public static void CaptureAll()
        {
            if (!Directory.Exists(ScreenshotsDir)) Directory.CreateDirectory(ScreenshotsDir);

            // 1. Rebuild Extended Street Prefab to ensure crates, upright tea bags, and all props are serialized
            ExtendedStreetBuilder.BuildExtendedStreetPrefab();
            AssetDatabase.SaveAssets();

            // 2. Open SCN_TramChanh_Main
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            // 3. Ensure environment instance exists in scene
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
                // Ensure proper lighting in the scene for rendering
                EnsureSceneLighting();

                // Shot 1: Street Animals (thú) - Framing urban street dog, cat on porch, pigeons, chickens
                // Player eye level ~1.6m looking along the sidewalk porch
                Vector3 camPos1 = new Vector3(-8.5f, 1.55f, -1.0f);
                Vector3 lookTarget1 = new Vector3(-13.5f, 0.35f, -3.2f);
                CaptureView(camPos1, lookTarget1, 52f, Path.Combine(ScreenshotsDir, "01_Street_Animals.png"), 1920, 1080);

                // Shot 2: Traffic & Utility Vehicles (xe cộ) - Framing small delivery truck, SUV, sedan on north curb, cub, bicycle
                // Player eye level ~1.6m looking across the street
                Vector3 camPos2 = new Vector3(-5.0f, 1.60f, 2.5f);
                Vector3 lookTarget2 = new Vector3(6.0f, 1.10f, 8.5f);
                CaptureView(camPos2, lookTarget2, 58f, Path.Combine(ScreenshotsDir, "02_Vehicles.png"), 1920, 1080);

                // Shot 3: Ambient Pedestrians (người đi bộ) - Framing PF_Person_07 / PF_Person_08 in natural upright pose
                // Player eye level ~1.6m
                Vector3 camPos3 = new Vector3(20.5f, 1.60f, 1.2f);
                Vector3 lookTarget3 = new Vector3(23.5f, 1.35f, 1.2f);
                CaptureView(camPos3, lookTarget3, 46f, Path.Combine(ScreenshotsDir, "03_Pedestrians.png"), 1920, 1080);

                // Shot 4: Street Props & Crates (đồ phố) - Framing CC0 fruit/produce crate with upright tea bags and VN plastic tables/chairs
                // Eye level close-up of crate next to stall with upright tea bags visible inside
                Vector3 camPos4 = new Vector3(1.1f, 0.85f, 0.4f);
                Vector3 lookTarget4 = new Vector3(1.7f, 0.15f, -0.6f);
                CaptureView(camPos4, lookTarget4, 45f, Path.Combine(ScreenshotsDir, "04_Street_Props_And_Crates.png"), 1920, 1080);

                // Shot 5: Vietnamese Sidewalk Plastic Table & Chairs
                Vector3 camPos5 = new Vector3(-8.0f, 1.15f, 2.2f);
                Vector3 lookTarget5 = new Vector3(-10.2f, 0.45f, 1.6f);
                CaptureView(camPos5, lookTarget5, 46f, Path.Combine(ScreenshotsDir, "05_VN_PlasticTableChairs.png"), 1920, 1080);

                Debug.Log("[ART-FIX-013] Successfully captured all validation screenshots!");
            }
            finally
            {
                if (envInst != null) Object.DestroyImmediate(envInst);
            }
        }

        private static void EnsureSceneLighting()
        {
            // Verify directional light exists
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            bool hasDirLight = false;
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional) { hasDirLight = true; break; }
            }

            if (!hasDirLight)
            {
                var lightGo = new GameObject("QA_DirectionalLight");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(1.0f, 0.95f, 0.88f);
                light.intensity = 1.2f;
                lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
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
                string dir = Path.GetDirectoryName(outPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(outPath, bytes);
                Debug.Log($"[ART-FIX-013] Wrote screenshot to {outPath} ({bytes.Length} bytes)");
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
