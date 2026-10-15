using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TramChanh.EditorTools.QA
{
    /// <summary>
    /// The one shared screenshot tool. Run it in Play Mode on SCN_TramChanh_Main (menu Tram Chanh > QA > Capture Standard Views).
    /// It renders a fixed set of views from the player's eye height and from above, writes PNGs under QA/Screenshots/Capture_*,
    /// and refuses to save an image that is blank or a single colour. Do not write a new capture script per task: add a view here.
    /// </summary>
    public static class CaptureStandardViews
    {
        public const int Width = 1920;
        public const int Height = 1080;
        private const float EyeHeight = 1.6f;
        private const string StallRootName = "StallRoot";

        private readonly struct View
        {
            public View(string name, Vector3 offsetFromStall, Vector3 euler)
            {
                Name = name;
                Offset = offsetFromStall;
                Euler = euler;
            }
            public string Name { get; }
            public Vector3 Offset { get; }
            public Vector3 Euler { get; }
        }

        /// <summary>Offsets are in metres from the stall root, in the stall's frame (+x right of the seller, +z toward the customers).</summary>
        private static readonly View[] Views =
        {
            new View("01_Stall_SellerView", new Vector3(0f, EyeHeight, -1.2f), new Vector3(20f, 0f, 0f)),
            new View("02_Stall_CustomerView", new Vector3(0f, EyeHeight, 1.8f), new Vector3(20f, 180f, 0f)),
            new View("03_Stall_TopDown", new Vector3(0f, 3.2f, -0.05f), new Vector3(90f, 0f, 0f)),
            new View("04_Counter_CloseUp", new Vector3(0f, 1.9f, -0.9f), new Vector3(55f, 0f, 0f)),
            new View("05_Behind_Counter_Row", new Vector3(0.9f, 1.4f, -2.6f), new Vector3(25f, 0f, 0f)),
            new View("06_Street_West120", new Vector3(-120f, EyeHeight, -1.2f), new Vector3(0f, 90f, 0f)),
            new View("07_Street_East120", new Vector3(120f, EyeHeight, -1.2f), new Vector3(0f, -90f, 0f)),
            new View("08_Street_West240", new Vector3(-240f, EyeHeight, 2f), new Vector3(0f, -90f, 0f)),
            new View("09_Street_East240", new Vector3(240f, EyeHeight, 2f), new Vector3(0f, 90f, 0f)),
            new View("10_Street_Center0", new Vector3(0f, EyeHeight, 2.0f), new Vector3(5f, 75f, 0f)),
            new View("11_People_Lineup_Outfits", new Vector3(0f, 1.35f, 1.8f), new Vector3(6f, 0f, 0f)),
            new View("12_People_InGame_WalkingSitting", new Vector3(-2.2f, 1.4f, -0.2f), new Vector3(8f, -135f, 0f)),
        };

        [MenuItem("Tram Chanh/QA/Capture Standard Views")]
        public static void CaptureFromMenu()
        {
            string folder = "QA/Screenshots/Capture_" + DateTime.Now.ToString("yyyyMMdd_HHmm");
            int saved = Capture(folder);
            if (saved > 0) { EditorUtility.RevealInFinder(Path.GetFullPath(folder)); }
            Debug.Log("Capture Standard Views: saved " + saved + " of " + Views.Length + " views to " + folder);
        }

        [MenuItem("Tram Chanh/QA/Capture Standard Views", true)]
        private static bool CanCapture() => Application.isPlaying;

        /// <summary>Renders every standard view into <paramref name="folder"/> (relative to the project root). Returns how many were saved.</summary>
        public static int Capture(string folder)
        {
            if (!Application.isPlaying) { Debug.LogError("Capture Standard Views needs Play Mode on SCN_TramChanh_Main, otherwise the pictures do not show the real game."); return 0; }
            // Unity pauses the game and stops serving mesh data to editor code while its window is in the background, so the
            // pictures and the numbers would be wrong. Refuse instead of writing a misleading report.
            if (!Application.isBatchMode && !Application.isFocused)
            {
                Debug.LogError("Capture Standard Views refused to run: the Unity window is not focused. Click the Unity window, keep it in front, and run the command again.");
                return 0;
            }
            GameObject tempEnv = null;
            GameObject stall = GameObject.Find(StallRootName);
            if (stall == null)
            {
                Debug.LogError("No object named '" + StallRootName + "' in the scene; load SCN_TramChanh_Main or ensure PF_TramChanh_StreetExtended is available.");
                return 0;
            }

            Camera template = Camera.main != null ? Camera.main : UnityEngine.Object.FindAnyObjectByType<Camera>();
            GameObject tempCamGo = null;
            if (template == null)
            {
                tempCamGo = new GameObject("QA_TempTemplateCamera");
                template = tempCamGo.AddComponent<Camera>();
                template.fieldOfView = 60f;
                template.nearClipPlane = 0.1f;
                template.farClipPlane = 600f;
            }

            Directory.CreateDirectory(folder);
            int saved = 0;
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var camObject = new GameObject("QA_CaptureCamera");
            try
            {
                Camera cam = camObject.AddComponent<Camera>();
                cam.CopyFrom(template);
                cam.targetTexture = rt;
                cam.enabled = false;
                foreach (View view in Views)
                {
                    List<GameObject> tempSpawns = new List<GameObject>();
                    try
                    {
                        if (view.Name == "11_People_Lineup_Outfits")
                        {
                            string[] personPaths = new[]
                            {
                                "Assets/TramChanh/Prefabs/People/PF_Person_01.prefab",
                                "Assets/TramChanh/Prefabs/People/PF_Person_02.prefab",
                                "Assets/TramChanh/Prefabs/People/PF_Person_03.prefab",
                                "Assets/TramChanh/Prefabs/People/PF_Person_04.prefab",
                                "Assets/TramChanh/Prefabs/People/PF_Person_05.prefab",
                                "Assets/TramChanh/Prefabs/People/PF_Person_06.prefab"
                            };
                            float[] xOffsets = new[] { -1.9f, -1.15f, -0.4f, 0.4f, 1.15f, 1.9f };
                            for (int i = 0; i < personPaths.Length; i++)
                            {
                                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(personPaths[i]);
                                if (pf != null)
                                {
                                    var inst = UnityEngine.Object.Instantiate(pf);
                                    inst.transform.position = stall.transform.TransformPoint(new Vector3(xOffsets[i], 0f, 4.5f));
                                    inst.transform.rotation = stall.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
                                    tempSpawns.Add(inst);
                                }
                            }
                        }
                        else if (view.Name == "12_People_InGame_WalkingSitting")
                        {
                            var pfWalk = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/People/PF_Person_01.prefab");
                            if (pfWalk != null)
                            {
                                var instWalk = UnityEngine.Object.Instantiate(pfWalk);
                                instWalk.transform.position = stall.transform.TransformPoint(new Vector3(-4.2f, 0f, 1.4f));
                                instWalk.transform.rotation = stall.transform.rotation * Quaternion.Euler(0f, 85f, 0f);
                                tempSpawns.Add(instWalk);
                            }
                            var pfSit = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/People/PF_Person_02.prefab");
                            if (pfSit != null)
                            {
                                var instSit = UnityEngine.Object.Instantiate(pfSit);
                                instSit.transform.position = stall.transform.TransformPoint(new Vector3(-3.35f, 0f, 0.2f));
                                instSit.transform.rotation = stall.transform.rotation * Quaternion.Euler(0f, 0f, 0f);
                                tempSpawns.Add(instSit);
                            }
                        }

                        camObject.transform.SetPositionAndRotation(stall.transform.TransformPoint(view.Offset), stall.transform.rotation * Quaternion.Euler(view.Euler));
                        cam.Render();
                        Texture2D image = ReadBack(rt);
                        if (IsBlank(image))
                        {
                            Debug.LogError("Blank or single-colour image for view " + view.Name + "; not saved. Check lighting, culling and the camera position.");
                        }
                        else
                        {
                            File.WriteAllBytes(Path.Combine(folder, view.Name + ".png"), image.EncodeToPNG());
                            saved++;
                        }
                        UnityEngine.Object.Destroy(image);
                    }
                    finally
                    {
                        // Immediate, so the temporary people are gone before the street is measured in the same call.
                        foreach (var g in tempSpawns)
                        {
                            if (g != null) UnityEngine.Object.DestroyImmediate(g);
                        }
                    }
                }

                MeasureStreetPerformance(cam, stall, folder);
            }
            finally
            {
                RenderTexture.active = null;
                UnityEngine.Object.Destroy(camObject);
                if (tempCamGo != null) UnityEngine.Object.Destroy(tempCamGo);
                if (tempEnv != null) UnityEngine.Object.Destroy(tempEnv);
                rt.Release();
                UnityEngine.Object.Destroy(rt);
            }
            return saved;
        }

        public static void MeasureStreetPerformance(Camera cam, GameObject stall, string folder)
        {
            float[] sampleX = new[] { -240f, -120f, 0f, 120f, 240f };
            var perfLines = new List<string>();
            perfLines.Add("=== 500M Street Performance Audit (ART-FIX-016 Task E) ===");
            perfLines.Add(string.Format("{0,-10} | {1,-18} | {2,-12} | {3,-19} | {4}", "Position", "Triangles in View", "Draw Calls", "Lights Near (<=30m)", "Budget Status"));
            perfLines.Add(new string('-', 82));

            var meshRenderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>();
            var skinnedRenderers = UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>();
            var lights = UnityEngine.Object.FindObjectsByType<Light>();

            foreach (float x in sampleX)
            {
                Vector3 samplePos = stall.transform.TransformPoint(new Vector3(x, EyeHeight, 2.0f));
                Quaternion sampleRot = stall.transform.rotation * Quaternion.Euler(0f, 90f, 0f);
                cam.transform.SetPositionAndRotation(samplePos, sampleRot);

                Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
                int triCount = 0;
                int visibleRenderers = 0;
                HashSet<Renderer> inactiveLod = InactiveLodRenderers(cam);

                foreach (var mr in meshRenderers)
                {
                    if (mr != null && mr.enabled && mr.gameObject.activeInHierarchy && !IsInactiveLod(mr, inactiveLod) && GeometryUtility.TestPlanesAABB(planes, mr.bounds))
                    {
                        visibleRenderers++;
                        var mf = mr.GetComponent<MeshFilter>();
                        if (mf != null) { triCount += CountTriangles(mf.sharedMesh); }
                    }
                }
                foreach (var smr in skinnedRenderers)
                {
                    if (smr != null && smr.enabled && smr.gameObject.activeInHierarchy && !IsInactiveLod(smr, inactiveLod) && GeometryUtility.TestPlanesAABB(planes, smr.bounds))
                    {
                        visibleRenderers++;
                        triCount += CountTriangles(smr.sharedMesh);
                    }
                }

                int lightsNear = 0;
                foreach (var l in lights)
                {
                    if (l != null && l.enabled && l.gameObject.activeInHierarchy && l.type != LightType.Directional)
                    {
                        float dist = Vector3.Distance(l.transform.position, samplePos);
                        if (dist <= Mathf.Max(l.range, 30f))
                        {
                            lightsNear++;
                        }
                    }
                }

                string status = (triCount <= 400000 && lightsNear <= 8) ? "PASS" : "FAIL";
                string row = string.Format("{0,-10} | {1,-18:N0} | {2,-12} | {3,-19} | {4}", $"x = {x:+0;-0;0}m", triCount, visibleRenderers, lightsNear, status);
                perfLines.Add(row);
                Debug.Log($"[PERF] {row}");
            }

            File.WriteAllLines(Path.Combine(folder, "street_performance_report.txt"), perfLines);
        }

        /// <summary>
        /// Triangles of a mesh from its index counts. Unlike Mesh.triangles this works on meshes without Read/Write enabled and does
        /// not copy the index buffer. Only triangle and quad sub-meshes contribute.
        /// </summary>
        public static int CountTriangles(Mesh mesh)
        {
            if (mesh == null) { return 0; }
            long total = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                MeshTopology topology = mesh.GetTopology(i);
                if (topology == MeshTopology.Triangles) { total += mesh.GetIndexCount(i) / 3; }
                else if (topology == MeshTopology.Quads) { total += mesh.GetIndexCount(i) / 2; }
            }
            return (int)total;
        }

        /// <summary>
        /// Renderers that belong to a LOD level other than the one <paramref name="cam"/> would draw. The active level follows
        /// Unity's rule: the first level whose transition height does not exceed the group's relative screen height times the quality
        /// LOD bias. When even the last level is below its threshold the whole group is culled and every renderer is listed.
        /// </summary>
        public static HashSet<Renderer> InactiveLodRenderers(Camera cam)
        {
            var inactive = new HashSet<Renderer>();
            float tanHalfFov = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            foreach (LODGroup group in UnityEngine.Object.FindObjectsByType<LODGroup>())
            {
                if (group == null || !group.enabled || !group.gameObject.activeInHierarchy) { continue; }
                LOD[] levels = group.GetLODs();
                if (levels.Length == 0) { continue; }
                Vector3 scale = group.transform.lossyScale;
                float worldSize = group.size * Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
                float distance = Vector3.Distance(cam.transform.position, group.transform.TransformPoint(group.localReferencePoint));
                float relativeHeight = distance < 0.0001f ? 1f : worldSize / (2f * distance * tanHalfFov);
                relativeHeight *= QualitySettings.lodBias;
                int active = -1;
                for (int i = 0; i < levels.Length; i++)
                {
                    if (relativeHeight >= levels[i].screenRelativeTransitionHeight) { active = i; break; }
                }
                for (int i = 0; i < levels.Length; i++)
                {
                    if (i == active) { continue; }
                    foreach (Renderer renderer in levels[i].renderers) { if (renderer != null) { inactive.Add(renderer); } }
                }
                if (active >= 0)
                {
                    // A renderer shared by the active level and another level is still drawn.
                    foreach (Renderer renderer in levels[active].renderers) { if (renderer != null) { inactive.Remove(renderer); } }
                }
            }
            return inactive;
        }

        private static readonly System.Text.RegularExpressions.Regex LodSuffix =
            new System.Text.RegularExpressions.Regex(@"(?:^|[_\s.\-])LOD(\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>
        /// True when the renderer should not be counted as drawn: it is a lower level of a LODGroup that is not active, or, with no
        /// LODGroup above it, it is a LOD1 or higher renderer named like "..._LOD1" (only the first level is counted).
        /// </summary>
        private static bool IsInactiveLod(Renderer renderer, HashSet<Renderer> inactiveInGroups)
        {
            if (inactiveInGroups.Contains(renderer)) { return true; }
            if (renderer.GetComponentInParent<LODGroup>() != null) { return false; }
            System.Text.RegularExpressions.Match match = LodSuffix.Match(renderer.gameObject.name);
            return match.Success && int.Parse(match.Groups[1].Value) > 0;
        }

        public static void CaptureFromBatchMode()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TramChanh/Scenes/Gameplay/SCN_TramChanh_Main.unity");
            string folder = "QA/Screenshots/Capture_" + DateTime.Now.ToString("yyyyMMdd_HHmm");
            int saved = Capture(folder);
            Debug.Log("[BatchModeCapture] Saved " + saved + " views to " + folder);
        }

        private static Texture2D ReadBack(RenderTexture rt)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            return image;
        }

        /// <summary>True when the picture has fewer than a handful of distinct colours, which means nothing real was rendered.</summary>
        public static bool IsBlank(Texture2D image)
        {
            Color32[] pixels = image.GetPixels32();
            var distinct = new HashSet<int>();
            for (int i = 0; i < pixels.Length; i += 97)
            {
                Color32 p = pixels[i];
                distinct.Add((p.r >> 3) | ((p.g >> 3) << 5) | ((p.b >> 3) << 10));
                if (distinct.Count >= 16) { return false; }
            }
            return true;
        }

        public static void InspectFbxMaterials()
        {
            string[] fbxFiles = new[]
            {
                "Assets/TramChanh/Art/Models/People/SM_Person_MaleCasual.fbx",
                "Assets/TramChanh/Art/Models/People/SM_Person_FemaleTankTop.fbx",
                "Assets/TramChanh/Art/Models/People/SM_Person_MaleHoodie.fbx",
                "Assets/TramChanh/Art/Models/People/SM_Person_FemaleCasual.fbx",
                "Assets/TramChanh/Art/Models/People/SM_Person_MaleWorker.fbx",
                "Assets/TramChanh/Art/Models/People/SM_Person_FemaleDress.fbx"
            };

            foreach (string path in fbxFiles)
            {
                Debug.Log("==========================================");
                Debug.Log($"[InspectFBX] Path: {path}");
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var obj in allAssets)
                {
                    if (obj is Material mat)
                    {
                        Debug.Log($"  [Material] {mat.name} (Shader: {mat.shader?.name})");
                    }
                    else if (obj is Mesh mesh)
                    {
                        Debug.Log($"  [Mesh] {mesh.name}: subMeshCount={mesh.subMeshCount}, verts={mesh.vertexCount}, triangles={mesh.triangles.Length / 3}");
                    }
                }
            }
        }

        public static void SetupPeopleMaterials()
        {
            string matFolder = "Assets/TramChanh/Art/Materials/People";
            if (!Directory.Exists(matFolder)) Directory.CreateDirectory(matFolder);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Material CreateMat(string name, Color color, float smoothness)
            {
                string path = $"{matFolder}/{name}.mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(litShader);
                    AssetDatabase.CreateAsset(mat, path);
                }
                mat.shader = litShader;
                mat.color = color;
                mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Smoothness", smoothness);
                mat.SetFloat("_Metallic", 0f);
                EditorUtility.SetDirty(mat);
                return mat;
            }

            var skinLight = CreateMat("MAT_Person_Skin_Light", new Color(0.93f, 0.84f, 0.76f), 0.15f);
            var skinGolden = CreateMat("MAT_Person_Skin_Golden", new Color(0.86f, 0.72f, 0.57f), 0.15f);
            var skinFair = CreateMat("MAT_Person_Skin_Fair", new Color(0.96f, 0.88f, 0.82f), 0.15f);
            var skinTan = CreateMat("MAT_Person_Skin_Tan", new Color(0.80f, 0.62f, 0.47f), 0.15f);

            var hairDarkBrown = CreateMat("MAT_Person_Hair_DarkBrown", new Color(0.16f, 0.11f, 0.08f), 0.2f);
            var hairBlack = CreateMat("MAT_Person_Hair_Black", new Color(0.08f, 0.08f, 0.09f), 0.2f);
            var hairChestnut = CreateMat("MAT_Person_Hair_Chestnut", new Color(0.26f, 0.14f, 0.10f), 0.2f);

            var shirtCoral = CreateMat("MAT_Person_Shirt_Coral", new Color(0.86f, 0.35f, 0.20f), 0.1f);
            var topYellow = CreateMat("MAT_Person_Top_Yellow", new Color(0.91f, 0.70f, 0.18f), 0.1f);
            var shirtWhite = CreateMat("MAT_Person_Shirt_White", new Color(0.94f, 0.93f, 0.91f), 0.1f);

            var pantsNavy = CreateMat("MAT_Person_Pants_Navy", new Color(0.13f, 0.21f, 0.33f), 0.1f);
            var pantsOlive = CreateMat("MAT_Person_Pants_Olive", new Color(0.22f, 0.34f, 0.25f), 0.1f);

            var shoesWhite = CreateMat("MAT_Person_Shoes_White", new Color(0.90f, 0.90f, 0.90f), 0.25f);
            var shoesTan = CreateMat("MAT_Person_Shoes_Tan", new Color(0.58f, 0.42f, 0.29f), 0.2f);
            var shoesBrown = CreateMat("MAT_Person_Shoes_Brown", new Color(0.30f, 0.20f, 0.14f), 0.2f);
            var shoesBlack = CreateMat("MAT_Person_Shoes_Black", new Color(0.09f, 0.09f, 0.10f), 0.25f);
            var shoesSoles = CreateMat("MAT_Person_Shoes_Soles", new Color(0.50f, 0.50f, 0.50f), 0.15f);
            var eyesDark = CreateMat("MAT_Person_Eyes_Dark", new Color(0.06f, 0.05f, 0.05f), 0.5f);

            var clothingA = AssetDatabase.LoadAssetAtPath<Material>($"{matFolder}/MAT_Person_Clothing_A.mat");
            var clothingB = AssetDatabase.LoadAssetAtPath<Material>($"{matFolder}/MAT_Person_Clothing_B.mat");

            AssetDatabase.SaveAssets();

            void ApplyMaterials(string prefabPath, Material[] materials)
            {
                var contents = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    var smrs = contents.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    foreach (var smr in smrs)
                    {
                        smr.sharedMaterials = materials;
                    }
                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                    Debug.Log($"[PeopleArt] Successfully applied {materials.Length} materials to {prefabPath}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            ApplyMaterials("Assets/TramChanh/Prefabs/People/PF_Person_01.prefab", new Material[]
            {
                shirtCoral, skinLight, pantsNavy, shoesWhite, shoesSoles, hairDarkBrown
            });

            ApplyMaterials("Assets/TramChanh/Prefabs/People/PF_Person_02.prefab", new Material[]
            {
                topYellow, skinGolden, pantsOlive, shoesTan, hairBlack, hairBlack, eyesDark
            });

            ApplyMaterials("Assets/TramChanh/Prefabs/People/PF_Person_03.prefab", new Material[]
            {
                clothingA
            });

            ApplyMaterials("Assets/TramChanh/Prefabs/People/PF_Person_04.prefab", new Material[]
            {
                shirtWhite, skinFair, clothingB, shoesBrown, shoesSoles, hairChestnut, hairChestnut, eyesDark
            });

            ApplyMaterials("Assets/TramChanh/Prefabs/People/PF_Person_05.prefab", new Material[]
            {
                clothingB
            });

            ApplyMaterials("Assets/TramChanh/Prefabs/People/PF_Person_06.prefab", new Material[]
            {
                clothingB, skinTan, shoesBlack, hairBlack, eyesDark
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
