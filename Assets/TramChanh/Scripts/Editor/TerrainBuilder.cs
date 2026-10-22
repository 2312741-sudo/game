using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using TramChanh.Core.GroundTruth;

namespace TramChanh.EditorTools
{
    /// <summary>
    /// Builds two real Unity Terrains beyond the boulevard gates (x &lt; -256 and x &gt; +256) in the
    /// OPEN scene, under one root "StreetEndTerrains". See Docs/Design/COURSE_BAI3_TERRAIN.md.
    ///
    /// Why the scene and not PF_TramChanh_StreetExtended: ExtendedStreetBuilder regenerates that prefab
    /// from scratch (a Terrain inside it would be wiped on the next rebuild) and the prefab-structure
    /// tests (WaypointGraphBuilderTests, BuildingFoundationGapTests) read it. A scene root leaves both alone.
    ///
    /// Idempotent: running Build twice yields exactly one root and the same assets (TerrainData and
    /// TerrainLayer assets are updated in place). Remove deletes the root, TerrainData and TerrainLayers.
    /// </summary>
    public static class TerrainBuilder
    {
        public const string MenuBuild = "Tram Chanh/Terrain/Build Street End Terrains";
        public const string MenuRemove = "Tram Chanh/Terrain/Remove Street End Terrains";

        public const string RootName = "StreetEndTerrains";
        public const string WestName = "Terrain_StreetEnd_West";
        public const string EastName = "Terrain_StreetEnd_East";

        public const string TerrainFolder = "Assets/TramChanh/Art/Terrain";
        public const string TextureFolder = "Assets/TramChanh/Art/Textures/Terrain";
        public const string TreePrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_StreetTree.prefab";
        public const string WestDataPath = TerrainFolder + "/TD_StreetEnd_West.asset";
        public const string EastDataPath = TerrainFolder + "/TD_StreetEnd_East.asset";

        private const int WestSeed = 1703;
        private const int EastSeed = 4219;
        private const float TreeDensity = 0.45f;
        private const int MaxTreeTriangles = 2000;   // per tree instance, otherwise skip trees

        // Terrain Settings (cheap: the night fog is thick, see COURSE_BAI3_TERRAIN.md)
        private const float PixelError = 12f;
        private const float BaseMapDistance = 80f;
        private const float DetailDistance = 40f;
        private const float DetailDensityScale = 0.5f;
        private const float TreeDistance = 120f;
        private const float TreeBillboardDistance = 80f;

        private static readonly string[] LayerNames = { "Dirt", "Grass", "Gravel" };

        // ------------------------------------------------------------------------------------
        // Menu
        // ------------------------------------------------------------------------------------

        [MenuItem(MenuBuild)]
        public static void BuildFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[TerrainBuilder] Exit Play Mode first.");
                return;
            }
            Build();
        }

        [MenuItem(MenuRemove)]
        public static void RemoveFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[TerrainBuilder] Exit Play Mode first.");
                return;
            }
            Remove();
        }

        // ------------------------------------------------------------------------------------
        // Build
        // ------------------------------------------------------------------------------------

        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            var log = new StringBuilder();
            log.AppendLine($"[TerrainBuilder] Build in scene '{scene.name}'");
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[TerrainBuilder] No open scene.");
                return;
            }
            if (scene.name != "SCN_TramChanh_Main")
                Debug.LogWarning($"[TerrainBuilder] Open scene is '{scene.name}', not SCN_TramChanh_Main. Continuing anyway.");

            EnsureFolder(TerrainFolder);
            ConfigureTextureImporters(log);

            TerrainLayer[] layers = EnsureLayers(log);
            if (layers == null) return;

            DetailPrototype[] details = BuildDetailPrototypes(log);
            TreePrototype[] trees = BuildTreePrototypes(log);

            TerrainData west = BuildTerrainData(WestDataPath, WestSeed, true, layers, details, trees, log);
            TerrainData east = BuildTerrainData(EastDataPath, EastSeed, false, layers, details, trees, log);

            RemoveSceneRoot(scene, log);   // rebuild the scene objects from scratch -> idempotent

            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Build Street End Terrains");
            log.AppendLine($"  created root '{RootName}'");

            PlaceTerrain(root.transform, WestName, west, -(TerrainBuilderMath.StreetEdgeX + TerrainBuilderMath.TerrainLength), log);
            PlaceTerrain(root.transform, EastName, east, TerrainBuilderMath.StreetEdgeX, log);

            int others = 0;
            foreach (Terrain t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.transform.parent == null || t.transform.parent.name != RootName) others++;
            if (others > 0)
                log.AppendLine($"  WARNING: scene has {others} other Terrain object(s) outside '{RootName}'.");

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            log.AppendLine("Done. Save the scene (Ctrl+S) to keep the terrains. NOT compiled/run by the author.");
            Debug.Log(log.ToString());
        }

        // ------------------------------------------------------------------------------------
        // Remove
        // ------------------------------------------------------------------------------------

        public static void Remove()
        {
            var log = new StringBuilder();
            log.AppendLine("[TerrainBuilder] Remove");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                if (RemoveSceneRoot(scene, log)) EditorSceneManager.MarkSceneDirty(scene);
            }

            foreach (string path in new[] { WestDataPath, EastDataPath, LayerPath(0), LayerPath(1), LayerPath(2) })
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(path) == null) continue;
                bool ok = AssetDatabase.DeleteAsset(path);
                log.AppendLine($"  {(ok ? "deleted" : "FAILED to delete")} {path}");
            }
            if (AssetDatabase.IsValidFolder(TerrainFolder))
            {
                int left = 0;
                foreach (string f in System.IO.Directory.GetFileSystemEntries(TerrainFolder))
                    if (!f.EndsWith(".meta")) left++;
                if (left == 0 && AssetDatabase.DeleteAsset(TerrainFolder))
                    log.AppendLine($"  deleted empty folder {TerrainFolder}");
            }
            AssetDatabase.SaveAssets();
            log.AppendLine($"Done. Source textures in {TextureFolder} are kept (delete by hand if unwanted).");
            Debug.Log(log.ToString());
        }

        private static bool RemoveSceneRoot(Scene scene, StringBuilder log)
        {
            bool removed = false;
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name != RootName) continue;
                Undo.DestroyObjectImmediate(go);
                log.AppendLine($"  removed scene root '{RootName}'");
                removed = true;
            }
            return removed;
        }

        // ------------------------------------------------------------------------------------
        // Textures and layers
        // ------------------------------------------------------------------------------------

        private static string LayerPath(int i) => $"{TerrainFolder}/TL_Terrain_{LayerNames[i]}.terrainlayer";

        private static void ConfigureTextureImporters(StringBuilder log)
        {
            bool changed = false;
            foreach (string n in LayerNames)
            {
                changed |= SetImporter($"{TextureFolder}/T_Terrain_{n}_Albedo.png", false, true, false, log);
                changed |= SetImporter($"{TextureFolder}/T_Terrain_{n}_Normal.png", true, false, false, log);
            }
            changed |= SetImporter($"{TextureFolder}/T_Terrain_GrassBlades.png", false, true, true, log);
            changed |= SetImporter($"{TextureFolder}/T_Terrain_WildFlowers.png", false, true, true, log);
            if (changed) AssetDatabase.Refresh();
        }

        private static bool SetImporter(string path, bool normalMap, bool srgb, bool alphaCutout, StringBuilder log)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null)
            {
                log.AppendLine($"  WARNING: texture missing {path} (run ArtSource/Scripts/generate_terrain_textures.py)");
                return false;
            }
            bool dirty = false;
            TextureImporterType type = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (imp.textureType != type) { imp.textureType = type; dirty = true; }
            if (!normalMap && imp.sRGBTexture != srgb) { imp.sRGBTexture = srgb; dirty = true; }
            if (!imp.mipmapEnabled) { imp.mipmapEnabled = true; dirty = true; }
            if (alphaCutout)
            {
                if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; dirty = true; }
                if (imp.wrapMode != TextureWrapMode.Clamp) { imp.wrapMode = TextureWrapMode.Clamp; dirty = true; }
            }
            else if (imp.wrapMode != TextureWrapMode.Repeat) { imp.wrapMode = TextureWrapMode.Repeat; dirty = true; }
            if (imp.maxTextureSize < 512) { imp.maxTextureSize = 512; dirty = true; }
            if (dirty)
            {
                imp.SaveAndReimport();
                log.AppendLine($"  import settings applied: {path}");
            }
            return dirty;
        }

        private static TerrainLayer[] EnsureLayers(StringBuilder log)
        {
            var layers = new TerrainLayer[TerrainBuilderMath.LayerCount];
            for (int i = 0; i < layers.Length; i++)
            {
                var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/T_Terrain_{LayerNames[i]}_Albedo.png");
                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/T_Terrain_{LayerNames[i]}_Normal.png");
                if (albedo == null || normal == null)
                {
                    Debug.LogError($"[TerrainBuilder] Missing terrain textures for '{LayerNames[i]}' in {TextureFolder}. " +
                                   "Run: python3 ArtSource/Scripts/generate_terrain_textures.py then let Unity import.");
                    return null;
                }
                string path = LayerPath(i);
                var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                bool created = layer == null;
                if (created) layer = new TerrainLayer();
                layer.diffuseTexture = albedo;
                layer.normalMapTexture = normal;
                layer.tileSize = new Vector2(i == TerrainBuilderMath.LayerGravel ? 5f : 8f, i == TerrainBuilderMath.LayerGravel ? 5f : 8f);
                layer.tileOffset = Vector2.zero;
                layer.normalScale = 1f;
                layer.metallic = 0f;
                layer.smoothness = 0f;
                if (created) AssetDatabase.CreateAsset(layer, path); else EditorUtility.SetDirty(layer);
                layers[i] = layer;
                log.AppendLine($"  {(created ? "created" : "updated")} {path}");
            }
            return layers;
        }

        // ------------------------------------------------------------------------------------
        // Prototypes
        // ------------------------------------------------------------------------------------

        private static DetailPrototype[] BuildDetailPrototypes(StringBuilder log)
        {
            var list = new List<DetailPrototype>();
            AddDetail(list, "T_Terrain_GrassBlades", 0.9f, 1.6f, 0.5f, 0.9f,
                new Color(0.55f, 0.75f, 0.35f), new Color(0.62f, 0.58f, 0.30f), log);
            AddDetail(list, "T_Terrain_WildFlowers", 0.7f, 1.1f, 0.4f, 0.7f,
                Color.white, new Color(0.9f, 0.9f, 0.8f), log);
            return list.ToArray();
        }

        private static void AddDetail(List<DetailPrototype> list, string texName, float minW, float maxW, float minH, float maxH,
            Color healthy, Color dry, StringBuilder log)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{texName}.png");
            if (tex == null)
            {
                log.AppendLine($"  WARNING: detail texture {texName} missing, skipped");
                return;
            }
            list.Add(new DetailPrototype
            {
                prototypeTexture = tex,
                usePrototypeMesh = false,
                renderMode = DetailRenderMode.GrassBillboard,
                minWidth = minW, maxWidth = maxW, minHeight = minH, maxHeight = maxH,
                noiseSpread = 0.4f,
                healthyColor = healthy,
                dryColor = dry,
                holeEdgePadding = 0.1f
            });
            log.AppendLine($"  detail prototype: {texName}");
        }

        public const string TerrainTreePrefabPath = "Assets/TramChanh/Art/Terrain/PF_StreetTree_Terrain.prefab";

        /// <summary>
        /// Unity draws a Terrain tree only when the prototype prefab has a renderer or an LODGroup on its ROOT. The street tree keeps its
        /// meshes on children, so this variant adds a single-level LODGroup on the root that lists all of them. The street tree itself is untouched.
        /// </summary>
        private static GameObject EnsureTerrainTreePrefab(GameObject source, StringBuilder log)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(TerrainTreePrefabPath);
            if (existing != null) { return existing; }
            string folder = System.IO.Path.GetDirectoryName(TerrainTreePrefabPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder)) { return source; }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                var group = instance.GetComponent<LODGroup>();
                if (group == null) { group = instance.AddComponent<LODGroup>(); }
                group.SetLODs(new[] { new LOD(0.01f, renderers) });
                group.RecalculateBounds();
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, TerrainTreePrefabPath);
                log.AppendLine($"  created terrain tree variant {TerrainTreePrefabPath} (LODGroup on the root, {renderers.Length} renderers)");
                return saved;
            }
            finally { Object.DestroyImmediate(instance); }
        }

        private static TreePrototype[] BuildTreePrototypes(StringBuilder log)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreePrefabPath);
            if (prefab == null)
            {
                log.AppendLine($"  WARNING: tree prefab missing ({TreePrefabPath}), terrains get no trees");
                return new TreePrototype[0];
            }
            int tris = 0;
            foreach (MeshFilter mf in prefab.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;
            bool hasLod = prefab.GetComponentInChildren<LODGroup>(true) != null;
            log.AppendLine($"  tree prefab PF_StreetTree: {tris} triangles per tree, LODGroup={hasLod}");
            if (tris > MaxTreeTriangles)
            {
                log.AppendLine($"  WARNING: {tris} triangles > {MaxTreeTriangles}, trees skipped (budget)");
                return new TreePrototype[0];
            }
            GameObject terrainTree = EnsureTerrainTreePrefab(prefab, log);
            return new[] { new TreePrototype { prefab = terrainTree != null ? terrainTree : prefab, bendFactor = 0.6f } };
        }

        // ------------------------------------------------------------------------------------
        // TerrainData
        // ------------------------------------------------------------------------------------

        private static TerrainData BuildTerrainData(string path, int seed, bool west, TerrainLayer[] layers,
            DetailPrototype[] details, TreePrototype[] trees, StringBuilder log)
        {
            var td = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            bool created = td == null;
            if (created)
            {
                td = new TerrainData();
                AssetDatabase.CreateAsset(td, path);
            }

            // Raise/Lower + Smooth: heights from a controlled noise function (joint edge exactly y = 0)
            int hr = TerrainBuilderMath.HeightmapResolution;
            td.heightmapResolution = hr;
            td.size = new Vector3(TerrainBuilderMath.TerrainLength, TerrainBuilderMath.TerrainSizeY, TerrainBuilderMath.TerrainWidth);
            float[,] heights = TerrainBuilderMath.BuildHeights(hr, seed, west);
            td.SetHeights(0, 0, heights);

            // Paint Texture: splat map from slope and height
            td.terrainLayers = layers;
            td.alphamapResolution = TerrainBuilderMath.AlphamapResolution;
            td.baseMapResolution = 256;
            float[,,] splat = TerrainBuilderMath.BuildSplat(heights, TerrainBuilderMath.AlphamapResolution, seed, west);
            td.SetAlphamaps(0, 0, splat);

            // Paint Trees
            td.treePrototypes = trees;
            var instances = new List<TreeInstance>();
            if (trees.Length > 0)
            {
                foreach (TerrainBuilderMath.TreeSpot s in TerrainBuilderMath.PlaceTrees(heights, seed, west, TreeDensity))
                {
                    instances.Add(new TreeInstance
                    {
                        prototypeIndex = 0,
                        position = new Vector3(s.XNorm, s.YNorm, s.ZNorm),
                        widthScale = s.Scale,
                        heightScale = s.Scale,
                        rotation = s.RotationRad,
                        color = Color.white,
                        lightmapColor = Color.white
                    });
                }
            }
            td.SetTreeInstances(instances.ToArray(), true);

            // Paint Details
            td.SetDetailResolution(TerrainBuilderMath.DetailResolution, TerrainBuilderMath.DetailPerPatch);
            td.detailPrototypes = details;
            long grassTotal = 0;
            for (int i = 0; i < details.Length; i++)
            {
                int[,] map = TerrainBuilderMath.BuildDetailLayer(heights, splat, TerrainBuilderMath.DetailResolution, seed + i, west, i == 1);
                td.SetDetailLayer(0, 0, i, map);
                grassTotal += TerrainBuilderMath.SumDetail(map);
            }

            // Wind (Terrain Settings > Wind Settings for Grass)
            td.wavingGrassStrength = 0.25f;
            td.wavingGrassAmount = 0.2f;
            td.wavingGrassSpeed = 0.3f;
            td.wavingGrassTint = new Color(0.7f, 0.7f, 0.7f, 1f);

            EditorUtility.SetDirty(td);
            log.AppendLine($"  {(created ? "created" : "updated")} {path}: heights {hr}x{hr}, alphamap {TerrainBuilderMath.AlphamapResolution}, " +
                           $"trees {instances.Count}, detail density sum {grassTotal}");
            return td;
        }

        // ------------------------------------------------------------------------------------
        // Scene objects
        // ------------------------------------------------------------------------------------

        private static void PlaceTerrain(Transform parent, string name, TerrainData data, float x, StringBuilder log)
        {
            GameObject go = Terrain.CreateTerrainGameObject(data);
            go.name = name;
            go.layer = TramChanhLayers.EnvironmentIndex;
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, TerrainBuilderMath.TerrainOriginY, TerrainBuilderMath.TerrainOriginZ);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);

            Terrain t = go.GetComponent<Terrain>();
            t.drawInstanced = true;
            t.heightmapPixelError = PixelError;
            t.basemapDistance = BaseMapDistance;
            t.detailObjectDistance = DetailDistance;
            t.detailObjectDensity = DetailDensityScale;
            t.treeDistance = TreeDistance;
            t.treeBillboardDistance = TreeBillboardDistance;
            t.treeCrossFadeLength = 20f;
            t.treeMaximumFullLODCount = 30;
            t.drawHeightmap = true;
            t.drawTreesAndFoliage = true;
            t.shadowCastingMode = ShadowCastingMode.Off;
            t.reflectionProbeUsage = ReflectionProbeUsage.Off;
            t.allowAutoConnect = false;

            TerrainCollider col = go.GetComponent<TerrainCollider>();
            if (col != null)
            {
                col.terrainData = data;
                // Far outside the playable limit: tree colliders only cost physics work. The setting has no public property
                // in this Unity version, so it is written through the serialized field when it exists.
                var colliderObject = new SerializedObject(col);
                SerializedProperty treeColliders = colliderObject.FindProperty("m_EnableTreeColliders");
                if (treeColliders != null)
                {
                    treeColliders.boolValue = false;
                    colliderObject.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Undo.RegisterCreatedObjectUndo(go, "Create Terrain");
            log.AppendLine($"  placed {name} at ({x}, {TerrainBuilderMath.TerrainOriginY}, {TerrainBuilderMath.TerrainOriginZ}), " +
                           $"drawInstanced, pixelError {PixelError}, baseMapDist {BaseMapDistance}, detailDist {DetailDistance}, " +
                           $"treeDist {TreeDistance}, layer Environment, collider on");
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }
    }
}
