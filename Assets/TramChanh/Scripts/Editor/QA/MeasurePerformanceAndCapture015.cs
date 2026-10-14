using System;
using System.Collections.Generic;
using System.IO;
using TramChanh.App;
using TramChanh.Core;
using TramChanh.EditorTools;
using TramChanh.Stall.Anchors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TramChanh.EditorTools.QA
{
    public static class MeasurePerformanceAndCapture015
    {
        public const string MainScenePath = "Assets/TramChanh/Scenes/Gameplay/SCN_TramChanh_Main.unity";
        public const string ScreenshotsDir = "QA/Screenshots/ART_FIX_015";

        [MenuItem("Tram Chanh/QA/Measure Performance and Capture ART-FIX-015")]
        public static void Execute()
        {
            Debug.Log("[ART-FIX-015] ========== Starting ART-FIX-015 Performance Measurement & Capture ==========");
            
            if (!Directory.Exists(ScreenshotsDir))
            {
                Directory.CreateDirectory(ScreenshotsDir);
            }

            // 1. Open SCN_TramChanh_Main
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            Debug.Log($"[ART-FIX-015] Opened scene: {scene.path}");

            // 2. Instantiate Environment & DrinkStation if needed
            var bootstrap = Object.FindAnyObjectByType<TramChanhMainBootstrap>();
            GameObject envInst = null;
            GameObject drinkInst = null;
            if (bootstrap != null)
            {
                var so = new SerializedObject(bootstrap);
                var envPf = so.FindProperty("_environmentPrefab").objectReferenceValue as GameObject;
                if (envPf != null)
                {
                    var existingEnv = GameObject.Find("PF_TramChanh_StreetExtended");
                    if (existingEnv == null)
                    {
                        envInst = (GameObject)PrefabUtility.InstantiatePrefab(envPf);
                        envInst.name = "PF_TramChanh_StreetExtended";
                        envInst.transform.position = Vector3.zero;
                        envInst.transform.rotation = Quaternion.identity;
                        Debug.Log("[ART-FIX-015] Instantiated PF_TramChanh_StreetExtended into SCN_TramChanh_Main");
                    }
                    else
                    {
                        Debug.Log("[ART-FIX-015] Found existing PF_TramChanh_StreetExtended in scene");
                        envInst = existingEnv;
                    }

                    // Instantiate DrinkStation with TeaRack and workstations
                    var drinkPf = so.FindProperty("_drinkStationPrefab").objectReferenceValue as GameObject;
                    var stallRoot = envInst.transform.Find("StallRoot");
                    if (stallRoot != null && drinkPf != null)
                    {
                        var anchors = stallRoot.GetComponentInChildren<StallAnchorSet>(true);
                        if (anchors != null)
                        {
                            drinkInst = (GameObject)PrefabUtility.InstantiatePrefab(drinkPf);
                            drinkInst.name = "DrinkStation";
                            drinkInst.transform.SetPositionAndRotation(anchors.transform.position, anchors.transform.rotation);

                            foreach (StallAnchorId id in new[] { StallAnchorId.TeaRack, StallAnchorId.Topping, StallAnchorId.IceBin, StallAnchorId.WipeArea, StallAnchorId.ReadyCounter })
                            {
                                if (anchors.TryGet(id, out var anchor))
                                {
                                    foreach (Transform child in anchor.transform) child.gameObject.SetActive(false);
                                }
                            }
                            Debug.Log("[ART-FIX-015] Instantiated DrinkStation at StallAnchorSet position");
                        }
                    }
                }
            }

            try
            {
                // Ensure Daylight lighting
                SetupDaylight();

                // Measure Performance at Player View
                Vector3 playerPos = new Vector3(0.0f, 1.6f, -1.2f); // Player eye position at service counter
                Vector3 playerLook = new Vector3(0.0f, 1.2f, 2.5f); // Looking out across stall to street
                
                MeasurePerformance(playerPos, playerLook);

                // Capture required daylight in-game screenshots
                CaptureAllShots();

                Debug.Log("[ART-FIX-015] ========== ART-FIX-015 Execution Completed Successfully ==========");
            }
            finally
            {
                if (drinkInst != null)
                {
                    Object.DestroyImmediate(drinkInst);
                }
                if (envInst != null && envInst.name == "PF_TramChanh_StreetExtended" && !PrefabUtility.IsPartOfPrefabAsset(envInst))
                {
                    Object.DestroyImmediate(envInst);
                }
            }
        }

        private static void SetupDaylight()
        {
            // Clear sky & warm sun
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.65f, 0.65f, 0.70f);
            RenderSettings.ambientIntensity = 1.0f;

            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            Light dirLight = null;
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    dirLight = l;
                    break;
                }
            }

            if (dirLight == null)
            {
                var lightGo = new GameObject("QA_DirectionalLight_Day");
                dirLight = lightGo.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }

            dirLight.color = new Color(1.0f, 0.96f, 0.90f);
            dirLight.intensity = 1.25f;
            dirLight.transform.rotation = Quaternion.Euler(50f, -40f, 0f);
        }

        private static void MeasurePerformance(Vector3 camPos, Vector3 lookTarget)
        {
            int w = 1920;
            int h = 1080;

            var camGo = new GameObject("PerfMeasurementCam");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 300f;
            cam.transform.position = camPos;
            cam.transform.LookAt(lookTarget);

            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            try
            {
                cam.Render();

                // 1. Frustum Culling calculation of visible triangles and draw calls
                Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
                int visibleRenderersCount = 0;
                long totalVisibleTriangles = 0;
                int totalDrawCallsEstimated = 0;

                var meshRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
                foreach (var mr in meshRenderers)
                {
                    if (!mr.enabled || !mr.gameObject.activeInHierarchy) continue;
                    if (GeometryUtility.TestPlanesAABB(planes, mr.bounds))
                    {
                        visibleRenderersCount++;
                        totalDrawCallsEstimated += mr.sharedMaterials.Length;
                        var mf = mr.GetComponent<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            totalVisibleTriangles += mf.sharedMesh.triangles.Length / 3;
                        }
                    }
                }

                var skinnedRenderers = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
                foreach (var smr in skinnedRenderers)
                {
                    if (!smr.enabled || !smr.gameObject.activeInHierarchy) continue;
                    if (GeometryUtility.TestPlanesAABB(planes, smr.bounds))
                    {
                        visibleRenderersCount++;
                        totalDrawCallsEstimated += smr.sharedMaterials.Length;
                        if (smr.sharedMesh != null)
                        {
                            totalVisibleTriangles += smr.sharedMesh.triangles.Length / 3;
                        }
                    }
                }

                // 2. UnityStats if available
                int statsTris = UnityStats.triangles;
                int statsDrawCalls = UnityStats.drawCalls;

                // 3. Real-time lights near player (at 10m and 30m radius)
                var allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
                var immediateLights = new List<Light>(); // <= 10m + directional
                var nearRealtimeLights = new List<Light>(); // <= 30m + directional
                foreach (var l in allLights)
                {
                    if (!l.enabled || !l.gameObject.activeInHierarchy) continue;
                    if (l.lightmapBakeType == LightmapBakeType.Baked) continue;

                    if (l.type == LightType.Directional)
                    {
                        immediateLights.Add(l);
                        nearRealtimeLights.Add(l);
                    }
                    else
                    {
                        float dist = Vector3.Distance(camPos, l.transform.position);
                        if (dist <= 10.0f)
                        {
                            immediateLights.Add(l);
                        }
                        if (dist <= 30.0f)
                        {
                            nearRealtimeLights.Add(l);
                        }
                    }
                }

                Debug.Log($"[ART-FIX-015 PERF REPORT] ========================================");
                Debug.Log($"[ART-FIX-015 PERF REPORT] Camera Position: {camPos}, LookTarget: {lookTarget}, FOV: {cam.fieldOfView}");
                Debug.Log($"[ART-FIX-015 PERF REPORT] Visible Renderers in Frustum: {visibleRenderersCount}");
                Debug.Log($"[ART-FIX-015 PERF REPORT] Visible Triangles (Frustum Culled): {totalVisibleTriangles:N0} (Budget: <= 400,000) -> {(totalVisibleTriangles <= 400000 ? "PASS" : "FAIL")}");
                Debug.Log($"[ART-FIX-015 PERF REPORT] Estimated Draw Calls: {totalDrawCallsEstimated} (UnityStats: {statsDrawCalls})");
                Debug.Log($"[ART-FIX-015 PERF REPORT] Active Real-time Lights immediate (<=10m): {immediateLights.Count} (Budget: <= 8) -> {(immediateLights.Count <= 8 ? "PASS" : "FAIL")}");
                Debug.Log($"[ART-FIX-015 PERF REPORT] Active Real-time Lights street (<=30m): {nearRealtimeLights.Count}");
                
                foreach (var l in nearRealtimeLights)
                {
                    float dist = l.type == LightType.Directional ? 0f : Vector3.Distance(camPos, l.transform.position);
                    Debug.Log($"[ART-FIX-015 PERF REPORT]   - Light: '{l.gameObject.name}', Type: {l.type}, Pos: {l.transform.position}, Dist: {dist:F1}m, Intensity: {l.intensity:F2}, Color: {l.color}");
                }
                Debug.Log($"[ART-FIX-015 PERF REPORT] ========================================");

                // Save perf report to text file
                string reportPath = Path.Combine(ScreenshotsDir, "PERFORMANCE_REPORT.txt");
                using (var sw = new StreamWriter(reportPath))
                {
                    sw.WriteLine("ART-FIX-015 500M STREET PERFORMANCE AUDIT REPORT");
                    sw.WriteLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                    sw.WriteLine($"Scene: {MainScenePath}");
                    sw.WriteLine($"Camera: Pos={camPos}, LookTarget={lookTarget}, FOV={cam.fieldOfView}");
                    sw.WriteLine($"Visible Frustum Renderers: {visibleRenderersCount}");
                    sw.WriteLine($"Visible Triangles in Frustum: {totalVisibleTriangles:N0} (Budget: <= 400,000) [{(totalVisibleTriangles <= 400000 ? "PASS" : "FAIL")}]");
                    sw.WriteLine($"Estimated Draw Calls (Submesh Batches): {totalDrawCallsEstimated}");
                    sw.WriteLine($"UnityStats Triangles: {statsTris:N0}, DrawCalls: {statsDrawCalls}");
                    sw.WriteLine($"Real-time Lights near Player (<=10m immediate): {immediateLights.Count} (Budget: <= 8) [{(immediateLights.Count <= 8 ? "PASS" : "FAIL")}]");
                    sw.WriteLine($"Real-time Lights in vicinity (<=30m extended): {nearRealtimeLights.Count}");
                    sw.WriteLine();
                    sw.WriteLine("Active Lights Detail (within 30m):");
                    foreach (var l in nearRealtimeLights)
                    {
                        float dist = l.type == LightType.Directional ? 0f : Vector3.Distance(camPos, l.transform.position);
                        sw.WriteLine($"  - Name: '{l.gameObject.name}', Type: {l.type}, Pos: {l.transform.position}, Dist: {dist:F1}m, Intensity: {l.intensity:F2}, Color: #{ColorUtility.ToHtmlStringRGB(l.color)}");
                    }
                }
                Debug.Log($"[ART-FIX-015] Written performance report to: {reportPath}");
            }
            finally
            {
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(camGo);
            }
        }

        private static void CaptureAllShots()
        {
            // 1. Player View (looking from stall service counter out to the vibrant daytime street)
            CaptureView(
                new Vector3(0.0f, 1.6f, -1.2f),
                new Vector3(0.0f, 1.2f, 4.0f),
                65f,
                Path.Combine(ScreenshotsDir, "01_PlayerView_Midday_Street.png")
            );

            // 2. West Porch Animals (Cat, Dog, Chicken aligned on west porch Z = -2.8)
            CaptureView(
                new Vector3(-12.0f, 1.15f, -0.6f),
                new Vector3(-12.0f, 0.40f, -2.8f),
                52f,
                Path.Combine(ScreenshotsDir, "02_WestPorch_Animals_Daylight.png")
            );

            // 3. Stall Counter, Plastic Crate, Upright Tea Bags & Red Tea Rack
            // Camera stands behind the service counter angled toward the equipment and crate
            CaptureView(
                new Vector3(-0.95f, 1.45f, -1.35f),
                new Vector3(0.20f, 0.90f, -0.35f),
                58f,
                Path.Combine(ScreenshotsDir, "03_Stall_Crate_And_TeaRack.png")
            );

            // 4. Curbside Chicken at X = 27, Z = 3.5
            CaptureView(
                new Vector3(25.5f, 1.10f, 3.5f),
                new Vector3(27.0f, 0.35f, 3.5f),
                48f,
                Path.Combine(ScreenshotsDir, "04_Curbside_Chicken.png")
            );

            // 5. Pedestrian Lineup on Sidewalk (PF_Person_01..06)
            // Spawn sample pedestrians in scene sidewalk to show realistic upgraded models facing camera
            var samplePedestriansRoot = new GameObject("SamplePedestrians_QA");
            try
            {
                string[] personPfPaths = new[]
                {
                    "Assets/TramChanh/Prefabs/People/PF_Person_01.prefab",
                    "Assets/TramChanh/Prefabs/People/PF_Person_02.prefab",
                    "Assets/TramChanh/Prefabs/People/PF_Person_03.prefab",
                    "Assets/TramChanh/Prefabs/People/PF_Person_04.prefab",
                    "Assets/TramChanh/Prefabs/People/PF_Person_05.prefab",
                    "Assets/TramChanh/Prefabs/People/PF_Person_06.prefab",
                };

                for (int i = 0; i < personPfPaths.Length; i++)
                {
                    var pf = AssetDatabase.LoadAssetAtPath<GameObject>(personPfPaths[i]);
                    if (pf != null)
                    {
                        var inst = (GameObject)PrefabUtility.InstantiatePrefab(pf, samplePedestriansRoot.transform);
                        inst.transform.position = new Vector3(8.5f + i * 0.95f, 0f, 2.2f);
                        inst.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Facing camera (-Z)
                    }
                }

                // Frame all 6 pedestrians from sidewalk looking north (zero obstacles between camera and pedestrians)
                CaptureView(
                    new Vector3(10.85f, 1.35f, -0.6f),
                    new Vector3(10.85f, 0.95f, 2.2f),
                    60f,
                    Path.Combine(ScreenshotsDir, "05_Pedestrians_In_Scene.png")
                );
            }
            finally
            {
                Object.DestroyImmediate(samplePedestriansRoot);
            }
        }

        private static void CaptureView(Vector3 pos, Vector3 target, float fov, string outPath)
        {
            int w = 1920;
            int h = 1080;

            var camGo = new GameObject("QA_ShotCam");
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

                // Validate image: compute mean and standard deviation of color channels
                Color32[] pixels = tex.GetPixels32();
                double sumR = 0, sumG = 0, sumB = 0;
                int step = 16; // sample every 16th pixel for speed
                int sampled = 0;
                for (int i = 0; i < pixels.Length; i += step)
                {
                    sumR += pixels[i].r;
                    sumG += pixels[i].g;
                    sumB += pixels[i].b;
                    sampled++;
                }
                double meanR = sumR / sampled;
                double meanG = sumG / sampled;
                double meanB = sumB / sampled;

                double varR = 0, varG = 0, varB = 0;
                for (int i = 0; i < pixels.Length; i += step)
                {
                    varR += (pixels[i].r - meanR) * (pixels[i].r - meanR);
                    varG += (pixels[i].g - meanG) * (pixels[i].g - meanG);
                    varB += (pixels[i].b - meanB) * (pixels[i].b - meanB);
                }
                double stdR = Math.Sqrt(varR / sampled);
                double stdG = Math.Sqrt(varG / sampled);
                double stdB = Math.Sqrt(varB / sampled);
                double totalStd = (stdR + stdG + stdB) / 3.0;

                byte[] bytes = tex.EncodeToPNG();
                string dir = Path.GetDirectoryName(outPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(outPath, bytes);

                if (totalStd < 8.0)
                {
                    Debug.LogWarning($"[ART-FIX-015] WARNING: Screenshot {Path.GetFileName(outPath)} might be low-contrast or monochrome! TotalStd={totalStd:F2}, Size={bytes.Length} bytes");
                }
                else
                {
                    Debug.Log($"[ART-FIX-015] PASS: Screenshot {Path.GetFileName(outPath)} verified (Color StdDev={totalStd:F2}, Size={bytes.Length / 1024} KB)");
                }
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
