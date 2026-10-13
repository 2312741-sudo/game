using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TramChanh.EditorTools
{
    public static class CaptureFix010c
    {
        public const string MainScenePath = "Assets/TramChanh/Scenes/Gameplay/SCN_TramChanh_Main.unity";
        public const string ScreenshotsDir = "QA/Screenshots/ART_FIX_010c";

        [MenuItem("Tram Chanh/Art/Capture All ART-FIX-010c Deliverables")]
        public static void CaptureAll()
        {
            if (!Directory.Exists(ScreenshotsDir)) Directory.CreateDirectory(ScreenshotsDir);

            // 1. Ensure Sign material has calibrated non-blown out emission
            var signMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/TramChanh/Art/Materials/MAT_Sign_TramChanh_New.mat");
            if (signMat != null)
            {
                signMat.EnableKeyword("_EMISSION");
                signMat.SetColor("_EmissionColor", new Color(0.5f, 0.5f, 0.5f, 1.0f));
                EditorUtility.SetDirty(signMat);
            }

            // Ensure MenuBoard material has correct menu textures
            var menuMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/TramChanh/Art/Materials/MAT_Menu_Board.mat");
            var menuBaseTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TramChanh/Art/Textures/T_Menu_Board_BaseColor.png");
            var menuEmTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TramChanh/Art/Textures/T_Menu_Board_Emission.png");
            if (menuMat != null)
            {
                if (menuBaseTex != null) menuMat.SetTexture("_BaseMap", menuBaseTex);
                if (menuEmTex != null) menuMat.SetTexture("_EmissionMap", menuEmTex);
                menuMat.EnableKeyword("_EMISSION");
                menuMat.SetColor("_EmissionColor", new Color(0.85f, 0.82f, 0.75f, 1.0f));
                EditorUtility.SetDirty(menuMat);
            }
            AssetDatabase.SaveAssets();

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
                if (bv != null && menuMat != null)
                {
                    bv.GetComponent<MeshRenderer>().sharedMaterial = menuMat;
                }
                // Shot 1: Signboard & Updated Counter Cutouts from Customer Straight-On View
                // Showing: 
                // - Updated sign: "9 Hải Thượng" / "0941 668 405", no blowout, unclipped logos flanking title
                // - Updated counter: cutouts shifted to the left (customer view) with left hole close to table edge
                Vector3 camPos1 = new Vector3(0.0f, 1.70f, 2.7f);
                Vector3 lookTarget1 = new Vector3(0.0f, 1.45f, 0.38f);
                CaptureView(camPos1, lookTarget1, 44f, Path.Combine(ScreenshotsDir, "01_Signboard_And_Counter_After.png"), 1920, 1080);
                CaptureView(new Vector3(0.0f, 1.75f, 2.7f), new Vector3(0.0f, 1.85f, 0.38f), 42f, Path.Combine("QA/Screenshots/ART_FIX_007", "Item3_Signboard_After.png"), 1920, 1080);

                // Shot 2: Natural Street Tree in Real Game Scene (SCN_TramChanh_Main)
                // Tree on south sidewalk curb (x = 4.5, z = 3.2), height ~5.6m, separate trunk and canopy
                Vector3 camPos2 = new Vector3(2.5f, 1.70f, -0.5f);
                Vector3 lookTarget2 = new Vector3(4.5f, 3.0f, 3.2f);
                CaptureView(camPos2, lookTarget2, 55f, Path.Combine(ScreenshotsDir, "02_StreetTree_InGame.png"), 1920, 1080);

                // Shot 3: PF_Car_Sedan Placed in Real Street (North curb at z = 12.0m)
                // Showing sedan parked along north curb, completely clear of RoadWest/RoadEast motorbike lanes
                Vector3 camPos3 = new Vector3(0.0f, 2.2f, 4.0f);
                Vector3 lookTarget3 = new Vector3(8.0f, 0.8f, 12.0f);
                CaptureView(camPos3, lookTarget3, 50f, Path.Combine(ScreenshotsDir, "03_CarSedan_NorthCurb.png"), 1920, 1080);

                // Shot 4: Motorbike Rider Posture (Unobstructed angle: no poles or trees blocking view)
                // Showing hips firmly on seat, hands on handlebars, feet on footrests
                GameObject riderInst = null;
                var riderPf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/People/PF_Rider_01.prefab");
                if (riderPf != null)
                {
                    riderInst = PrefabUtility.InstantiatePrefab(riderPf) as GameObject;
                    riderInst.transform.position = new Vector3(0.0f, 0.0f, 5.85f);
                    riderInst.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                }
                try
                {
                    Vector3 camPos4 = new Vector3(-1.8f, 1.15f, 4.3f);
                    Vector3 lookTarget4 = new Vector3(0.0f, 0.95f, 5.85f);
                    CaptureView(camPos4, lookTarget4, 45f, Path.Combine(ScreenshotsDir, "04_Motorbike_Rider_Unobstructed.png"), 1920, 1080);
                }
                finally
                {
                    if (riderInst != null) Object.DestroyImmediate(riderInst);
                }

                // Shot 5: Horizontal Eye-Level View of Street and Building Foundations
                // Horizontal eye-level showing building foundations flush touching the ground with zero gaps
                Vector3 camPos5 = new Vector3(2.5f, 0.35f, -2.5f);
                Vector3 lookTarget5 = new Vector3(2.5f, 0.05f, -5.0f);
                CaptureView(camPos5, lookTarget5, 55f, Path.Combine(ScreenshotsDir, "05_BuildingFoundations_TouchingGround.png"), 1920, 1080);

                Debug.Log("[ART-FIX-010c] Successfully captured all 5 validation screenshots!");
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
                string dir = Path.GetDirectoryName(outPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
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
