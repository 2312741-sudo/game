using System.IO;
using TramChanh.EditorTools.ProjectSetup;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TramChanh.EditorTools
{
    public static class SetupPedestrianIdleAndCapture013c
    {
        public const string MaleFbxPath = "Assets/TramChanh/Art/Models/People/SM_Person_MaleCasual.fbx";
        public const string FemaleFbxPath = "Assets/TramChanh/Art/Models/People/SM_Person_FemaleTankTop.fbx";
        public const string Person07PrefabPath = "Assets/TramChanh/Prefabs/People/PF_Person_07.prefab";
        public const string Person08PrefabPath = "Assets/TramChanh/Prefabs/People/PF_Person_08.prefab";
        public const string Controller07Path = "Assets/TramChanh/Art/Animations/People/AC_Person_07.controller";
        public const string Controller08Path = "Assets/TramChanh/Art/Animations/People/AC_Person_08.controller";
        public const string MainScenePath = "Assets/TramChanh/Scenes/Gameplay/SCN_TramChanh_Main.unity";
        public const string ScreenshotsDir = "QA/Screenshots/ART_FIX_013c";

        [MenuItem("Tram Chanh/Art/Setup Pedestrian Animations and Capture ART-FIX-013c")]
        public static void RunAll()
        {
            // 0. Ensure project settings & URP
            TramChanhProjectSetup.Apply(true);

            // 1. Setup Idle animations for PF_Person_07 / 08
            SetupPedestrianAnimations();

            // 2. Rebuild Extended Street Prefab
            ExtendedStreetBuilder.BuildExtendedStreetPrefab();
            AssetDatabase.SaveAssets();

            // 3. Capture all validation screenshots
            CaptureScreenshots();
        }

        public static void SetupPedestrianAnimations()
        {
            Debug.Log("[ART-FIX-013c] Setting up pedestrian animations...");

            // 1. Ensure looping Idle clip in ModelImporter
            EnsureLoopingClipsInModelImporter(MaleFbxPath);
            EnsureLoopingClipsInModelImporter(FemaleFbxPath);

            // 2. Male Person 07
            AnimationClip maleIdleClip = FindClip(MaleFbxPath, "Idle");
            if (maleIdleClip != null)
            {
                Debug.Log($"[ART-FIX-013c] Found male idle clip: {maleIdleClip.name}");
                var controller07 = AnimatorController.CreateAnimatorControllerAtPath(Controller07Path);
                var rootStateMachine = controller07.layers[0].stateMachine;
                var state = rootStateMachine.AddState("Idle");
                state.motion = maleIdleClip;
                rootStateMachine.defaultState = state;
                EditorUtility.SetDirty(controller07);

                var p07 = AssetDatabase.LoadAssetAtPath<GameObject>(Person07PrefabPath);
                if (p07 != null)
                {
                    var anim = p07.GetComponent<Animator>();
                    if (anim == null) anim = p07.AddComponent<Animator>();
                    anim.runtimeAnimatorController = controller07;
                    anim.applyRootMotion = false;
                    EditorUtility.SetDirty(p07);
                    Debug.Log("[ART-FIX-013c] Assigned Animator & Controller to PF_Person_07");
                }
            }

            // 3. Female Person 08
            AnimationClip femaleIdleClip = FindClip(FemaleFbxPath, "Idle");
            if (femaleIdleClip != null)
            {
                Debug.Log($"[ART-FIX-013c] Found female idle clip: {femaleIdleClip.name}");
                var controller08 = AnimatorController.CreateAnimatorControllerAtPath(Controller08Path);
                var rootStateMachine = controller08.layers[0].stateMachine;
                var state = rootStateMachine.AddState("Idle");
                state.motion = femaleIdleClip;
                rootStateMachine.defaultState = state;
                EditorUtility.SetDirty(controller08);

                var p08 = AssetDatabase.LoadAssetAtPath<GameObject>(Person08PrefabPath);
                if (p08 != null)
                {
                    var anim = p08.GetComponent<Animator>();
                    if (anim == null) anim = p08.AddComponent<Animator>();
                    anim.runtimeAnimatorController = controller08;
                    anim.applyRootMotion = false;
                    EditorUtility.SetDirty(p08);
                    Debug.Log("[ART-FIX-013c] Assigned Animator & Controller to PF_Person_08");
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static void EnsureLoopingClipsInModelImporter(string fbxPath)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) return;

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            if (clips == null || clips.Length == 0) return;

            bool modified = false;
            foreach (var c in clips)
            {
                if (c.name.IndexOf("Idle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (!c.loopTime)
                    {
                        c.loopTime = true;
                        modified = true;
                    }
                }
            }

            if (modified || importer.clipAnimations.Length == 0)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
                Debug.Log($"[ART-FIX-013c] Configured looping Idle clip on {fbxPath}");
            }
        }

        private static AnimationClip FindClip(string fbxPath, string match)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    if (clip.name.IndexOf(match, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return clip;
                    }
                }
            }
            foreach (var a in assets)
            {
                if (a is AnimationClip clip)
                {
                    if (clip.name.IndexOf(match, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return clip;
                    }
                }
            }
            return null;
        }

        public static void CaptureScreenshots()
        {
            if (!Directory.Exists(ScreenshotsDir)) Directory.CreateDirectory(ScreenshotsDir);

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
                EnsureSceneLighting();

                // Shot 1: Small Truck (PF_Truck_Small) with proper atlas texture and lights
                // Truck parked at (20.0f, 0.0f, 12.0f) facing -X
                Vector3 camPos1 = new Vector3(14.5f, 1.5f, 8.5f);
                Vector3 lookTarget1 = new Vector3(19.5f, 1.3f, 12.0f);
                CaptureView(camPos1, lookTarget1, 46f, Path.Combine(ScreenshotsDir, "01_Truck_Small.png"), 1920, 1080);

                // Shot 2: Street Vendor Cart (PF_StreetVendor_Cart) with proper wood/metal texture
                // Cart placed at (25.0f, 0.0f, 1.8f) facing -100 deg
                Vector3 camPos2 = new Vector3(27.8f, 1.25f, 1.8f);
                Vector3 lookTarget2 = new Vector3(25.0f, 0.90f, 1.8f);
                CaptureView(camPos2, lookTarget2, 46f, Path.Combine(ScreenshotsDir, "02_StreetVendor_Cart.png"), 1920, 1080);

                // Shot 3: Street Animals (Cat, Chicken, Dog) with increased scale visible at player eye level
                // Animals positioned side-by-side on West porch/sidewalk
                Vector3 camPos3 = new Vector3(-12.2f, 1.15f, -0.2f);
                Vector3 lookTarget3 = new Vector3(-12.2f, 0.45f, -2.8f);
                CaptureView(camPos3, lookTarget3, 52f, Path.Combine(ScreenshotsDir, "03_Street_Animals.png"), 1920, 1080);

                // Shot 4: Pedestrian 07 with Idle animation
                // PF_Person_07 is at (23.5f, 0.0f, 1.2f)
                Vector3 camPos4 = new Vector3(24.8f, 1.25f, 3.8f);
                Vector3 lookTarget4 = new Vector3(23.5f, 0.90f, 1.2f);
                CaptureView(camPos4, lookTarget4, 40f, Path.Combine(ScreenshotsDir, "04_Pedestrian_07_Idle.png"), 1920, 1080);

                // Shot 5: Pedestrian 08 with Idle animation
                // PF_Person_08 is at (-13.2f, 0.0f, 1.2f)
                Vector3 camPos5 = new Vector3(-13.2f, 1.25f, -1.2f);
                Vector3 lookTarget5 = new Vector3(-13.2f, 0.90f, 1.2f);
                CaptureView(camPos5, lookTarget5, 40f, Path.Combine(ScreenshotsDir, "05_Pedestrian_08_Idle.png"), 1920, 1080);

                Debug.Log("[ART-FIX-013c] Successfully captured all ART-FIX-013c screenshots!");
            }
            finally
            {
                if (envInst != null) Object.DestroyImmediate(envInst);
            }
        }

        private static void EnsureSceneLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.50f);

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
                light.color = new Color(1.0f, 0.98f, 0.92f);
                light.intensity = 1.3f;
                lightGo.transform.rotation = Quaternion.Euler(45f, -50f, 0f);
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
                Debug.Log($"[ART-FIX-013c] Wrote screenshot to {outPath} ({bytes.Length} bytes)");
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
