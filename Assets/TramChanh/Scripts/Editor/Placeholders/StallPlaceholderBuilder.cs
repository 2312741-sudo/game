using System;
using System.Collections.Generic;
using System.IO;
using TramChanh.Core.GroundTruth;
using TramChanh.Core.Provisional;
using TramChanh.Stall.Anchors;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TramChanh.EditorTools.Placeholders
{
    /// <summary>
    /// ART-STALL-001 phase A (blockout) for Unity: builds PF_Stall_TramChanh and the
    /// NEW sign placeholder PF_Sign_TramChanh_New from primitives with the final
    /// hierarchy, colliders and anchors (ASSET_INTEGRATION.md §5.1–5.2).
    ///
    /// - The old illuminated TRAM CHANH letters are never built (GT-002).
    /// - Anchor positions are seeds only; rebuilding keeps positions already edited
    ///   in the prefab, so station placement stays a prefab/scene concern (DEC-011).
    /// - A prefab that no longer carries <see cref="PlaceholderAsset"/> is treated as
    ///   final art and is never overwritten.
    /// </summary>
    public static class StallPlaceholderBuilder
    {
        public const string StallPrefabPath = "Assets/TramChanh/Prefabs/Stall/PF_Stall_TramChanh.prefab";
        public const string SignPrefabPath = "Assets/TramChanh/Prefabs/Stall/PF_Sign_TramChanh_New.prefab";
        public const string MaterialFolder = "Assets/TramChanh/Art/Materials";

        public const string StallRootName = "PF_Stall_TramChanh";
        public const string SignRootName = "PF_Sign_TramChanh_New";

        public static readonly string[] StallGroups =
        {
            "Structure", "Counter", "Frame", "Roof", "Sign", "Lights", "Wheels", "Colliders", "Anchors",
        };

        public readonly struct AnchorPose
        {
            public AnchorPose(Vector3 localPosition, Quaternion localRotation, bool confirmed, string source)
            {
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                Confirmed = confirmed;
                Source = source;
            }

            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public bool Confirmed { get; }
            public string Source { get; }
        }

        public sealed class PlaceholderMaterials
        {
            public Material Stall;
            public Material Wheel;
            public Material SignHousing;
            public Material SignTop;

            /// <summary>In-memory materials (tests). Not saved as assets.</summary>
            public static PlaceholderMaterials CreateTransient()
            {
                return new PlaceholderMaterials
                {
                    Stall = NewMaterial("MAT_Placeholder_Stall", new Color(0.18f, 0.17f, 0.16f)),
                    Wheel = NewMaterial("MAT_Placeholder_Wheel", new Color(0.05f, 0.05f, 0.05f)),
                    SignHousing = NewMaterial("MAT_Sign_TramChanh_New", Color.white),
                    SignTop = NewMaterial("MAT_Placeholder_SignTop", new Color(0.86f, 0.62f, 0.12f)),
                };
            }
        }

        [MenuItem("Tram Chanh/Placeholders/Build Stall + New Sign Placeholder")]
        public static void BuildFromMenu()
        {
            try
            {
                BuildAndSavePrefabs();
            }
            catch (Exception exception)
            {
                Debug.LogError("[TramChanh] Stall placeholder build failed: " + exception.Message);
                throw;
            }
        }

        /// <summary>Builds and saves both prefabs. Returns the stall prefab asset.</summary>
        public static GameObject BuildAndSavePrefabs()
        {
            EnsureFolder(Path.GetDirectoryName(StallPrefabPath));
            EnsureFolder(MaterialFolder);
            PlaceholderMaterials materials = LoadOrCreateMaterialAssets();

            GameObject signPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SignPrefabPath);
            if (signPrefab == null || signPrefab.GetComponent<PlaceholderAsset>() != null)
            {
                GameObject sign = BuildSignHierarchy(materials);
                signPrefab = PrefabUtility.SaveAsPrefabAsset(sign, SignPrefabPath);
                Object.DestroyImmediate(sign);
            }
            else
            {
                Debug.Log("[TramChanh] " + SignPrefabPath + " is final art; left unchanged.");
            }

            GameObject existingStall = AssetDatabase.LoadAssetAtPath<GameObject>(StallPrefabPath);
            if (existingStall != null && existingStall.GetComponent<PlaceholderAsset>() == null)
            {
                Debug.Log("[TramChanh] " + StallPrefabPath + " is final art; left unchanged.");
                return existingStall;
            }

            Dictionary<StallAnchorId, AnchorPose> preserved = ReadAnchorPoses(existingStall);
            GameObject stall = BuildStallHierarchy(materials, preserved, signPrefab);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(stall, StallPrefabPath);
            Object.DestroyImmediate(stall);
            AssetDatabase.SaveAssets();
            Debug.Log($"[TramChanh] Built {StallPrefabPath} (kept {preserved.Count} edited anchor positions).");
            return saved;
        }

        /// <summary>
        /// Builds the stall hierarchy in the open scene. Callers own the result.
        /// <paramref name="signPrefab"/> null = build the sign inline (tests).
        /// </summary>
        public static GameObject BuildStallHierarchy(
            PlaceholderMaterials materials,
            IReadOnlyDictionary<StallAnchorId, AnchorPose> preservedAnchors = null,
            GameObject signPrefab = null)
        {
            var root = new GameObject(StallRootName);
            root.layer = TramChanhLayers.EnvironmentIndex;
            root.AddComponent<PlaceholderAsset>().Configure(
                "ART-STALL-001",
                "DEC-011",
                "Blockout. Footprint, counter height and total height are GT-001. Wheel, post, " +
                "slab and roof sizes and the A-frame silhouette are provisional. Counter openings " +
                "for the recessed topping station and ice bin are added with the final mesh.");
            root.AddComponent<StallAnchorSet>();

            var groups = new Dictionary<string, Transform>();
            foreach (string group in StallGroups)
            {
                groups[group] = CreateChild(group, root.transform).transform;
            }

            float w = StallPlaceholderSpec.Width;
            float d = StallPlaceholderSpec.Depth;

            // Structure: lower cabinet between the wheels and the counter slab.
            float structureBottom = StallPlaceholderSpec.StructureBottom;
            float structureTop = StallPlaceholderSpec.CounterSlabBottom;
            CreateBox("SM_Stall_Base", groups["Structure"],
                new Vector3(0f, (structureBottom + structureTop) * 0.5f, 0f),
                new Vector3(w, structureTop - structureBottom, d), materials.Stall);

            // Counter slab: its top is exactly the counter height (GT-001).
            CreateBox("SM_Stall_Counter", groups["Counter"],
                new Vector3(0f, StallPlaceholderSpec.CounterTop - StallPlaceholderSpec.CounterSlabThickness * 0.5f, 0f),
                new Vector3(w, StallPlaceholderSpec.CounterSlabThickness, d), materials.Stall);

            // Frame: four vertical posts (final A-frame silhouette comes from ART-STALL-001 B).
            float postHeight = StallPlaceholderSpec.RoofBottom - StallPlaceholderSpec.CounterTop;
            float postY = StallPlaceholderSpec.CounterTop + postHeight * 0.5f;
            float postX = w * 0.5f - StallPlaceholderSpec.PostSize * 0.5f;
            float postZ = d * 0.5f - StallPlaceholderSpec.PostSize * 0.5f;
            var postSize = new Vector3(StallPlaceholderSpec.PostSize, postHeight, StallPlaceholderSpec.PostSize);
            int postIndex = 0;
            foreach (float sx in new[] { -1f, 1f })
            {
                foreach (float sz in new[] { -1f, 1f })
                {
                    CreateBox($"SM_Stall_Frame_Post{postIndex++}", groups["Frame"],
                        new Vector3(sx * postX, postY, sz * postZ), postSize, materials.Stall);
                }
            }

            // Roof: top at the approximate total height (GT-001).
            CreateBox("SM_Stall_Roof", groups["Roof"],
                new Vector3(0f, StallPlaceholderSpec.TotalHeight - StallPlaceholderSpec.RoofThickness * 0.5f, 0f),
                new Vector3(w, StallPlaceholderSpec.RoofThickness, d), materials.Stall);

            // Wheels: caster placeholders at the corners, axle along X.
            float wheelX = w * 0.5f - StallPlaceholderSpec.WheelInset;
            float wheelZ = d * 0.5f - StallPlaceholderSpec.WheelInset;
            int wheelIndex = 0;
            foreach (float sx in new[] { -1f, 1f })
            {
                foreach (float sz in new[] { -1f, 1f })
                {
                    GameObject wheel = CreatePrimitive(PrimitiveType.Cylinder, $"SM_Stall_CasterWheel{wheelIndex++}", groups["Wheels"], materials.Wheel);
                    wheel.transform.localPosition = new Vector3(sx * wheelX, StallPlaceholderSpec.WheelDiameter * 0.5f, sz * wheelZ);
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    // Unity cylinder: height 2 along local Y, diameter 1.
                    wheel.transform.localScale = new Vector3(
                        StallPlaceholderSpec.WheelDiameter, StallPlaceholderSpec.WheelWidth * 0.5f, StallPlaceholderSpec.WheelDiameter);
                }
            }

            // Sign: the NEW sign only, rear-centre pivot, inside the footprint under the roof.
            GameObject sign = signPrefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(signPrefab, groups["Sign"])
                : BuildSignHierarchy(materials);
            sign.name = SignRootName;
            sign.transform.SetParent(groups["Sign"], false);
            Vector3 signSize = StallPlaceholderSpec.SignSize;
            sign.transform.localPosition = new Vector3(
                0f,
                StallPlaceholderSpec.RoofBottom - 0.01f - signSize.y * 0.5f,
                d * 0.5f - signSize.z);

            // Compound primitive colliders ([AP] §8): no MeshCollider.
            Transform colliders = groups["Colliders"];
            AddBoxCollider(colliders, "Body",
                new Vector3(0f, (structureBottom + StallPlaceholderSpec.CounterTop) * 0.5f, 0f),
                new Vector3(w, StallPlaceholderSpec.CounterTop - structureBottom, d));
            AddBoxCollider(colliders, "Roof",
                new Vector3(0f, StallPlaceholderSpec.TotalHeight - StallPlaceholderSpec.RoofThickness * 0.5f, 0f),
                new Vector3(w, StallPlaceholderSpec.RoofThickness, d));
            postIndex = 0;
            foreach (float sx in new[] { -1f, 1f })
            {
                foreach (float sz in new[] { -1f, 1f })
                {
                    AddBoxCollider(colliders, $"Post{postIndex++}", new Vector3(sx * postX, postY, sz * postZ), postSize);
                }
            }

            // Anchors: provisional seeds unless an edited pose is preserved.
            foreach (StallAnchorId id in (StallAnchorId[])Enum.GetValues(typeof(StallAnchorId)))
            {
                GameObject anchorObject = CreateChild(AnchorName(id), groups["Anchors"]);
                var anchor = anchorObject.AddComponent<StallAnchor>();
                if (preservedAnchors != null && preservedAnchors.TryGetValue(id, out AnchorPose pose))
                {
                    anchorObject.transform.localPosition = pose.LocalPosition;
                    anchorObject.transform.localRotation = pose.LocalRotation;
                    anchor.Configure(id, pose.Confirmed, pose.Source);
                }
                else
                {
                    anchorObject.transform.localPosition = StallPlaceholderSpec.SeedPosition(id);
                    anchor.Configure(id, false, "provisional seed — awaiting real stall reference (DEC-011)");
                }
            }

            SetLayerRecursively(root, TramChanhLayers.EnvironmentIndex);
            return root;
        }

        /// <summary>Builds the NEW sign placeholder (no text, no old letters).</summary>
        public static GameObject BuildSignHierarchy(PlaceholderMaterials materials)
        {
            Vector3 size = StallPlaceholderSpec.SignSize;
            var root = new GameObject(SignRootName);
            root.AddComponent<PlaceholderAsset>().Configure(
                "ART-BRAND-001 / ART-BRAND-002",
                "Sign size and artwork",
                "Placeholder volume for the NEW Tram Chanh lightbox sign. Size is provisional. " +
                "Branding ('Trạm' black, 'Chanh' mustard-orange, bottom strip) arrives as a texture " +
                "on MAT_Sign_TramChanh_New. Never replace with the old illuminated letters.");

            // Pivot = rear centre ([AP] §7): the box extends forward (+Z) from the pivot.
            float topCapHeight = size.y * 0.15f;
            CreateBox("SM_Sign_TramChanh_New", root.transform,
                new Vector3(0f, -topCapHeight * 0.5f, size.z * 0.5f),
                new Vector3(size.x, size.y - topCapHeight, size.z), materials.SignHousing);
            CreateBox("SM_Sign_TramChanh_New_TopCap", root.transform,
                new Vector3(0f, size.y * 0.5f - topCapHeight * 0.5f, size.z * 0.5f),
                new Vector3(size.x, topCapHeight, size.z), materials.SignTop);

            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0f, size.z * 0.5f);
            collider.size = size;

            SetLayerRecursively(root, TramChanhLayers.EnvironmentIndex);
            return root;
        }

        public static string AnchorName(StallAnchorId id)
        {
            return id + "Anchor";
        }

        private static Dictionary<StallAnchorId, AnchorPose> ReadAnchorPoses(GameObject prefab)
        {
            var poses = new Dictionary<StallAnchorId, AnchorPose>();
            if (prefab == null)
            {
                return poses;
            }

            foreach (StallAnchor anchor in prefab.GetComponentsInChildren<StallAnchor>(true))
            {
                Transform t = anchor.transform;
                poses[anchor.Id] = new AnchorPose(t.localPosition, t.localRotation, anchor.PositionConfirmed, anchor.PositionSource);
            }

            return poses;
        }

        private static PlaceholderMaterials LoadOrCreateMaterialAssets()
        {
            PlaceholderMaterials transient = PlaceholderMaterials.CreateTransient();
            return new PlaceholderMaterials
            {
                Stall = LoadOrCreateMaterial(transient.Stall),
                Wheel = LoadOrCreateMaterial(transient.Wheel),
                SignHousing = LoadOrCreateMaterial(transient.SignHousing),
                SignTop = LoadOrCreateMaterial(transient.SignTop),
            };
        }

        private static Material LoadOrCreateMaterial(Material template)
        {
            string path = $"{MaterialFolder}/{template.name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            AssetDatabase.CreateAsset(template, path);
            return template;
        }

        private static Material NewMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            return material;
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            Object.DestroyImmediate(primitive.GetComponent<Collider>());
            primitive.transform.SetParent(parent, false);
            primitive.GetComponent<MeshRenderer>().sharedMaterial = material;
            return primitive;
        }

        private static void CreateBox(string name, Transform parent, Vector3 center, Vector3 size, Material material)
        {
            GameObject box = CreatePrimitive(PrimitiveType.Cube, name, parent, material);
            box.transform.localPosition = center;
            box.transform.localScale = size;
        }

        private static void AddBoxCollider(Transform parent, string name, Vector3 center, Vector3 size)
        {
            GameObject holder = CreateChild(name, parent);
            var box = holder.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = layer;
            }
        }

        private static void EnsureFolder(string assetFolder)
        {
            assetFolder = assetFolder.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(assetFolder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetFolder));
        }
    }
}
