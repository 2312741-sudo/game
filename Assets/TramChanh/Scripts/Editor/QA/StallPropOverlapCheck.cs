using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TramChanh.Core.GroundTruth;
using TramChanh.Stall.Anchors;
using TramChanh.Stall.Layout;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TramChanh.EditorTools
{
    /// <summary>
    /// Read-only QA check: do the counter props of the stall overlap each other on the XZ plane?
    /// Props are the enabled colliders on the Interactable layer under the stall root; their world-space
    /// bounds are fed to the pure rule in <see cref="StallCounterLayoutRule"/>. The check never moves
    /// anything: it only reports. The prefab is opened with <see cref="PrefabUtility.LoadPrefabContents"/>
    /// and discarded without saving.
    /// </summary>
    public static class StallPropOverlapCheck
    {
        public const string StallPrefabPath = "Assets/TramChanh/Prefabs/Stall/PF_Stall_TramChanh.prefab";
        public const string CounterRendererName = "SM_Stall_Counter";
        public const string RedTeaRackName = "RedTeaRack";

        /// <summary>Menu entry under Tram Chanh/QA/ (one attribute, same root as the other QA commands).</summary>
        [MenuItem(TramChanhValidationMenu.MenuRoot + "Check Stall Prop Overlaps", false, 25)]
        public static void CheckStallPrefabMenu()
        {
            string text = CheckPrefab(StallPrefabPath, StallCounterLayoutRule.DefaultToleranceMetres, out int problems);
            if (problems == 0) { Debug.Log(text); }
            else { Debug.LogWarning(text); }
        }

        public const string DrinkStationPrefabPath = "Assets/TramChanh/Prefabs/Workstations/DrinkWave/PF_DrinkStation.prefab";
        public const string GrillPrefabPath = "Assets/TramChanh/Prefabs/Workstations/PF_Grill_Elmich.prefab";
        public const string ScreenshotsDirectory = "QA/Screenshots/Stall_Assembled";

        /// <summary>
        /// Menu entry for the fully assembled stall: stall prefab plus its station props in a temporary scene.
        /// Does not save changes to prefabs or scenes.
        /// </summary>
        [MenuItem(TramChanhValidationMenu.MenuRoot + "Check Assembled Stall Overlaps", false, 26)]
        public static void CheckAssembledStallMenu()
        {
            string text = CheckAssembled(StallPrefabPath, StallCounterLayoutRule.DefaultToleranceMetres, out int problems, true);
            if (problems == 0) { Debug.Log(text); }
            else { Debug.LogWarning(text); }
        }

        /// <summary>
        /// Creates a temporary in-memory scene, instantiates the stall prefab and attaches the runtime drink station
        /// (PF_DrinkStation containing PF_RedTeaRack, PF_ToppingStation, PF_IceBin, PF_ReadyCounterPoint, and WipeArea)
        /// to the canonical anchors matching runtime behavior. Runs Analyze and captures screenshots without saving.
        /// </summary>
        public static string CheckAssembled(string assetPath, float tolerance, out int problems, bool captureScreenshots = true)
        {
            Scene tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            try
            {
                GameObject stallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (stallPrefab == null)
                {
                    problems = 1;
                    return "[StallPropOverlap] ERROR: could not load stall prefab at " + assetPath;
                }

                GameObject stall = (GameObject)PrefabUtility.InstantiatePrefab(stallPrefab, tempScene);
                stall.transform.position = Vector3.zero;
                stall.transform.rotation = Quaternion.identity;

                var anchors = stall.GetComponentInChildren<StallAnchorSet>(true);
                if (anchors == null)
                {
                    problems = 1;
                    return "[StallPropOverlap] ERROR: no StallAnchorSet on " + assetPath;
                }

                var missing = new List<string>();

                // 1. Attach PF_DrinkStation (compound station holding PF_RedTeaRack, PF_ToppingStation, PF_IceBin, PF_ReadyCounterPoint, WipeArea)
                GameObject drinkStationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DrinkStationPrefabPath);
                if (drinkStationPrefab != null)
                {
                    GameObject drinkStation = (GameObject)PrefabUtility.InstantiatePrefab(drinkStationPrefab, tempScene);
                    drinkStation.transform.SetParent(stall.transform, false);
                    drinkStation.transform.SetPositionAndRotation(anchors.transform.position, anchors.transform.rotation);
                }
                else
                {
                    missing.Add("DrinkStation prefab at " + DrinkStationPrefabPath);
                }

                string text = Analyze(stall, assetPath + " (assembled)", tolerance, out problems);

                if (missing.Count > 0)
                {
                    text += "\nMISSING:\n" + string.Join("\n", missing);
                    problems += missing.Count;
                }

                if (captureScreenshots)
                {
                    CaptureStallScreenshots(tempScene, stall);
                }

                return text;
            }
            finally
            {
                EditorSceneManager.CloseScene(tempScene, true);
            }
        }

        private static void CaptureStallScreenshots(Scene scene, GameObject stall)
        {
            // Set up lighting
            var lightGo = new GameObject("QA_FillLight");
            SceneManager.MoveGameObjectToScene(lightGo, scene);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = Color.white;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var fillLightGo = new GameObject("QA_AmbientLight");
            SceneManager.MoveGameObjectToScene(fillLightGo, scene);
            var fillLight = fillLightGo.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.6f;
            fillLight.color = new Color(0.9f, 0.95f, 1f);
            fillLightGo.transform.rotation = Quaternion.Euler(-30f, 150f, 0f);

            Shader replacementShader = Shader.Find("Standard");
            if (replacementShader == null) { replacementShader = Shader.Find("Diffuse"); }

            // Hide roof temporarily so top-down camera can see counter surface and props
            var hiddenRoofs = new List<GameObject>();
            foreach (Transform child in stall.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.IndexOf("Roof", System.StringComparison.OrdinalIgnoreCase) >= 0 && child.gameObject.activeSelf)
                {
                    hiddenRoofs.Add(child.gameObject);
                    child.gameObject.SetActive(false);
                }
            }

            // 1. Top-Down Camera (directly above counter surface, below frame beams)
            var topDownGo = new GameObject("QA_Cam_TopDown");
            SceneManager.MoveGameObjectToScene(topDownGo, scene);
            var topDownCam = topDownGo.AddComponent<Camera>();
            topDownCam.clearFlags = CameraClearFlags.SolidColor;
            topDownCam.backgroundColor = new Color(0.12f, 0.13f, 0.15f);
            topDownCam.transform.position = stall.transform.position + new Vector3(0f, 1.90f, 0f);
            topDownCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            topDownCam.fieldOfView = 62f;
            topDownCam.nearClipPlane = 0.05f;
            if (replacementShader != null) { topDownCam.SetReplacementShader(replacementShader, ""); }
            RenderCameraToFile(topDownCam, Path.Combine(ScreenshotsDirectory, "Stall_Assembled_TopDown.png"), 1920, 1080);

            // Restore roof for customer view
            foreach (GameObject roof in hiddenRoofs)
            {
                roof.SetActive(true);
            }

            // 2. Customer-Side Camera (facing stall from +Z customer side)
            var customerGo = new GameObject("QA_Cam_CustomerView");
            SceneManager.MoveGameObjectToScene(customerGo, scene);
            var custCam = customerGo.AddComponent<Camera>();
            custCam.clearFlags = CameraClearFlags.SolidColor;
            custCam.backgroundColor = new Color(0.12f, 0.13f, 0.15f);
            custCam.transform.position = stall.transform.position + new Vector3(0f, 1.45f, 1.8f);
            custCam.transform.LookAt(stall.transform.position + new Vector3(0f, 0.95f, 0f));
            custCam.fieldOfView = 50f;
            if (replacementShader != null) { custCam.SetReplacementShader(replacementShader, ""); }
            RenderCameraToFile(custCam, Path.Combine(ScreenshotsDirectory, "Stall_Assembled_CustomerView.png"), 1920, 1080);
        }

        private static void RenderCameraToFile(Camera cam, string outputPath, int width, int height)
        {
            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture prevRT = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = prevRT;
            RenderTexture.ReleaseTemporary(rt);

            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            File.WriteAllBytes(outputPath, bytes);
        }

        /// <summary>
        /// Opens the prefab asset read-only, runs <see cref="Analyze"/> and returns the report text.
        /// <paramref name="problems"/> counts overlaps, props outside the counter and a misplaced red tea rack.
        /// </summary>
        public static string CheckPrefab(string assetPath, float tolerance, out int problems)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);
            if (contents == null)
            {
                problems = 1;
                return "[StallPropOverlap] ERROR: could not load prefab at " + assetPath;
            }

            try
            {
                return Analyze(contents, assetPath, tolerance, out problems);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>Analyses a live or prefab-contents root. Does not modify it.</summary>
        public static string Analyze(GameObject root, string label, float tolerance, out int problems)
        {
            var sb = new StringBuilder();
            problems = 0;
            sb.AppendLine("Stall prop overlap check: " + label);
            sb.AppendLine("XZ footprints, tolerance " + tolerance.ToString("0.00", CultureInfo.InvariantCulture) + " m. Read-only; nothing was moved.");

            FootprintRect? counter = ReadCounterFootprint(root, sb, ref problems);

            var props = new List<FootprintRect>();
            int skipped = 0;
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (!collider.enabled) { continue; }
                if (collider.gameObject.layer != TramChanhLayers.InteractableIndex)
                {
                    skipped++;
                    continue;
                }

                Bounds b = collider.bounds;
                props.Add(new FootprintRect(RelativePath(root.transform, collider.transform),
                    b.min.x, b.min.z, b.max.x, b.max.z));
            }

            sb.AppendLine();
            sb.AppendLine("Props (Interactable-layer colliders): " + props.Count
                + ". Other enabled colliders, not checked as props: " + skipped + ".");
            foreach (FootprintRect prop in props)
            {
                sb.AppendLine("  - " + prop.Name + "  X[" + Fmt(prop.MinX) + ", " + Fmt(prop.MaxX)
                    + "]  Z[" + Fmt(prop.MinZ) + ", " + Fmt(prop.MaxZ) + "]  " + Fmt(prop.Width) + " x " + Fmt(prop.Depth) + " m");
            }

            if (props.Count == 0)
            {
                sb.AppendLine("  No interactable props under this root, so there is nothing to overlap. Counter props are placed at runtime (e.g. PF_DrinkStation), not in this prefab.");
            }

            IReadOnlyList<PropOverlap> overlaps = StallCounterLayoutRule.FindCrossStationOverlaps(props, tolerance);
            sb.AppendLine();
            sb.AppendLine("Overlapping pairs between different stations: " + overlaps.Count);
            foreach (PropOverlap overlap in overlaps)
            {
                sb.AppendLine("  OVERLAP: " + overlap.First + " <-> " + overlap.Second
                    + "  overlap " + Fmt(overlap.OverlapX) + " x " + Fmt(overlap.OverlapZ) + " m, area "
                    + overlap.AreaSquareMetres.ToString("0.000", CultureInfo.InvariantCulture) + " m2");
            }
            problems += overlaps.Count;

            if (counter.HasValue)
            {
                IReadOnlyList<string> outside = StallCounterLayoutRule.FindPropsOutsideCounter(counter.Value, props, tolerance);
                sb.AppendLine("Props outside the counter footprint: " + outside.Count);
                foreach (string name in outside) { sb.AppendLine("  OUTSIDE: " + name); }
                problems += outside.Count;
            }

            if (counter.HasValue)
            {
                bool anyRack = false;
                foreach (FootprintRect prop in props)
                {
                    if (prop.Name.IndexOf(RedTeaRackName, System.StringComparison.OrdinalIgnoreCase) < 0) { continue; }
                    anyRack = true;
                    bool behind = StallCounterLayoutRule.IsBehindCounter(counter.Value, prop, tolerance);
                    sb.AppendLine("Red tea rack behind counter (" + prop.Name + "): " + (behind ? "yes" : "NO"));
                    if (!behind) { problems++; }
                }

                if (!anyRack)
                {
                    sb.AppendLine("Red tea rack: no interactable collider named *" + RedTeaRackName + "* under this root; behind-counter rule not evaluated.");
                }
            }

            sb.AppendLine();
            sb.AppendLine("SUMMARY: " + (problems == 0 ? "PASS" : "FAIL") + " - problems " + problems);
            return sb.ToString();
        }

        private static FootprintRect? ReadCounterFootprint(GameObject root, StringBuilder sb, ref int problems)
        {
            Renderer counter = null;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.gameObject.name == CounterRendererName) { counter = renderer; break; }
            }

            if (counter == null)
            {
                sb.AppendLine("ERROR: counter renderer '" + CounterRendererName + "' not found; counter footprint checks skipped.");
                problems++;
                return null;
            }

            Bounds b = counter.bounds;
            float widthError = Mathf.Abs(b.size.x - StallDimensions.Width);
            float depthError = Mathf.Abs(b.size.z - StallDimensions.Depth);
            bool ok = widthError <= StallDimensions.FootprintTolerance && depthError <= StallDimensions.FootprintTolerance;
            sb.AppendLine("Counter footprint " + Fmt(b.size.x) + " x " + Fmt(b.size.z) + " m (GT-001 "
                + Fmt(StallDimensions.Width) + " x " + Fmt(StallDimensions.Depth) + " m): " + (ok ? "OK" : "MISMATCH"));
            if (!ok) { problems++; }

            return new FootprintRect("Counter", b.min.x, b.min.z, b.max.x, b.max.z);
        }

        private static string RelativePath(Transform root, Transform target)
        {
            var parts = new List<string>();
            for (Transform t = target; t != null && t != root; t = t.parent)
            {
                parts.Add(t.name);
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string Fmt(float value) => value.ToString("0.000", CultureInfo.InvariantCulture);
    }
}
