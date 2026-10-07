using System;
using System.IO;
using TramChanh.Core.GroundTruth;
using TramChanh.Core.Provisional;
using TramChanh.Stall.Anchors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TramChanh.EditorTools.Placeholders
{
    /// <summary>
    /// Creates the primitive ART-STALL-001 inspection scene. All equipment geometry,
    /// furniture placement and station poses are provisional editor seeds (DEC-011).
    /// Existing assets are kept so subsequent artist edits are never discarded.
    /// No production controllers, interaction behaviour or player logic are added.
    /// </summary>
    public static class GameplayBlockoutBuilder
    {
        public const string ScenePath = "Assets/TramChanh/Scenes/Gameplay/SCN_Gameplay_Blockout.unity";
        public const string WorkstationFolder = "Assets/TramChanh/Prefabs/Workstations";
        public const string FurnitureFolder = "Assets/TramChanh/Prefabs/CustomerArea";
        public const float ReferenceHeight = 1.7f;

        public static readonly string[] EquipmentNames =
        {
            "PF_RedTeaRack", "PF_ToppingStation", "PF_IceBin", "PF_Grill_Elmich",
            "PF_ReadyCounterPoint", "PF_PlasticStool", "PF_YellowCrateTable",
        };

        [MenuItem("Tram Chanh/Placeholders/Create Gameplay Blockout Scene")]
        public static void CreateFromMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                CreateAndSave();
            }
        }

        public static string PrefabPath(string name)
        {
            string folder = name == "PF_ReadyCounterPoint" ? "Assets/TramChanh/Prefabs/Stall"
                : name == "PF_PlasticStool" || name == "PF_YellowCrateTable" ? FurnitureFolder
                : WorkstationFolder;
            return folder + "/" + name + ".prefab";
        }

        public static void CreateAndSave()
        {
            // Idempotent: reopen a completed scene rather than reset edited placements.
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                return;
            }

            GameObject stallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StallPlaceholderBuilder.StallPrefabPath)
                ?? StallPlaceholderBuilder.BuildAndSavePrefabs();
            foreach (string name in EquipmentNames)
            {
                string path = PrefabPath(name);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                {
                    continue;
                }

                GameObject equipment = CreateEquipment(name);
                PrefabUtility.SaveAsPrefabAsset(equipment, path);
                Object.DestroyImmediate(equipment);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var environment = new GameObject("Environment");
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundPlane";
            ground.transform.SetParent(environment.transform, false);
            ground.GetComponent<Renderer>().sharedMaterial = MaterialAsset("MAT_Placeholder_Ground", new Color(0.32f, 0.36f, 0.34f));
            Object.DestroyImmediate(ground.GetComponent<MeshCollider>());
            var groundCollider = ground.AddComponent<BoxCollider>();
            groundCollider.center = new Vector3(0f, -0.05f, 0f);
            groundCollider.size = new Vector3(10f, 0.1f, 10f);
            ground.layer = TramChanhLayers.EnvironmentIndex;

            GameObject stall = (GameObject)PrefabUtility.InstantiatePrefab(stallPrefab);
            StallAnchorSet anchors = stall.GetComponent<StallAnchorSet>();
            PlaceStation(anchors, StallAnchorId.TeaRack, "TeaRackAnchor", new Vector3(-0.72f, 1f, -0.13f), "PF_RedTeaRack");
            PlaceStation(anchors, StallAnchorId.Topping, "ToppingStationAnchor", new Vector3(-0.41f, 0.91f, -0.13f), "PF_ToppingStation");
            PlaceStation(anchors, StallAnchorId.IceBin, "IceBinAnchor", new Vector3(-0.10f, 0.82f, -0.13f), "PF_IceBin");
            PlaceStation(anchors, StallAnchorId.Grill, "GrillAnchor", new Vector3(0.25f, 1f, -0.13f), "PF_Grill_Elmich");
            PlaceStation(anchors, StallAnchorId.Sauce, "SauceAnchor", new Vector3(0.52f, 1f, -0.13f));
            PlaceStation(anchors, StallAnchorId.Wrap, "WrappingAnchor", new Vector3(0.73f, 1f, -0.13f));
            PlaceStation(anchors, StallAnchorId.ReadyCounter, "ReadyCounterAnchor", new Vector3(0f, 1f, 0.25f), "PF_ReadyCounterPoint");
            CreateCounterRecesses(stall);

            var customerArea = new GameObject("CustomerArea");
            InstantiateAt("PF_YellowCrateTable", customerArea.transform, new Vector3(2f, 0f, 1.1f));
            InstantiateAt("PF_PlasticStool", customerArea.transform, new Vector3(2f, 0f, 0.45f));
            InstantiateAt("PF_PlasticStool", customerArea.transform, new Vector3(2f, 0f, 1.75f));

            var references = new GameObject("ScaleReferences");
            GameObject reference = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            reference.name = "PlayerHeightReference_1.7m";
            reference.transform.SetParent(references.transform, false);
            reference.transform.localPosition = new Vector3(-1.4f, ReferenceHeight * 0.5f, -0.9f);
            // Unity capsule is two units tall. This is a visual reference, not a player.
            reference.transform.localScale = new Vector3(0.4f, ReferenceHeight * 0.5f, 0.4f);
            Object.DestroyImmediate(reference.GetComponent<Collider>());
            reference.GetComponent<Renderer>().sharedMaterial = MaterialAsset("MAT_Placeholder_Reference", new Color(0.2f, 0.6f, 0.8f));
            Mark(reference, "Temporary 1.7 m height reference; no player controller.");

            var lighting = new GameObject("Lighting");
            var lightObject = new GameObject("DirectionalLight");
            lightObject.transform.SetParent(lighting.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.65f, 0.65f, 0.65f);

            var cameraObject = new GameObject("BlockoutOverviewCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(3.8f, 3.1f, -5.2f);
            cameraObject.transform.LookAt(new Vector3(0.45f, 0.9f, 0.2f));
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 48f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 50f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.2f, 0.24f, 0.28f);
            cameraObject.AddComponent<AudioListener>();

            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException("Could not save " + ScenePath);
            }

            Debug.Log("[TramChanh] Saved primitive gameplay blockout: " + ScenePath);
        }

        private static GameObject CreateEquipment(string name)
        {
            var root = new GameObject(name);
            root.layer = name == "PF_PlasticStool" || name == "PF_YellowCrateTable"
                ? TramChanhLayers.EnvironmentIndex : TramChanhLayers.InteractableIndex;
            Mark(root, "Primitive blockout only. Equipment dimensions, anchor poses and furniture placement are provisional; no production gameplay.");
            Material neutral = MaterialAsset("MAT_Placeholder", new Color(0.55f, 0.57f, 0.58f));
            Material red = MaterialAsset("MAT_Placeholder_Red", new Color(0.72f, 0.12f, 0.08f));
            Material yellow = MaterialAsset("MAT_Placeholder_Yellow", new Color(0.9f, 0.65f, 0.05f));

            switch (name)
            {
                case "PF_RedTeaRack":
                    OpenBox("Rack", root.transform, new Vector3(0.24f, 0.22f, 0.22f), red);
                    Anchor(root.transform, "InteractionPoint", new Vector3(0f, 0.22f, -0.11f));
                    Anchor(root.transform, "OutputPoint", new Vector3(0f, 0.24f, 0f));
                    Transform slots = Child("BagSlots", root.transform);
                    Anchor(slots, "Slot01", new Vector3(-0.05f, 0.03f, 0f));
                    Anchor(slots, "Slot02", new Vector3(0.05f, 0.03f, 0f));
                    break;
                case "PF_ToppingStation":
                    Box("StationFrame", root.transform, new Vector3(0f, 0.01f, 0f), new Vector3(0.32f, 0.02f, 0.28f), neutral);
                    foreach (float side in new[] { -1f, 1f })
                    {
                        Transform bin = Child(side < 0 ? "CoconutJellyBin" : "LemonJellyBin", root.transform);
                        bin.localPosition = new Vector3(side * 0.08f, 0.02f, 0f);
                        OpenBox("Bin", bin, new Vector3(0.14f, 0.07f, 0.26f), neutral);
                        Anchor(bin, "InteractionPoint", new Vector3(0f, 0.07f, 0f));
                    }
                    Transform cover = Child("CoverPivot", root.transform);
                    cover.localPosition = new Vector3(0f, 0.09f, 0.14f);
                    Box("TransparentCover", cover, new Vector3(0f, 0f, -0.14f), new Vector3(0.32f, 0.01f, 0.28f), neutral);
                    // Keep the cover raised for inspection; no animation or lid behaviour.
                    cover.localRotation = Quaternion.Euler(70f, 0f, 0f);
                    break;
                case "PF_IceBin":
                    OpenBox("Bin", root.transform, new Vector3(0.24f, 0.18f, 0.28f), neutral);
                    Box("IceVolume", root.transform, new Vector3(0f, 0.10f, 0f), new Vector3(0.20f, 0.03f, 0.24f), neutral);
                    Anchor(root.transform, "InteractionPoint", new Vector3(0f, 0.18f, 0f));
                    Anchor(root.transform, "ScoopRestPoint", new Vector3(0.09f, 0.18f, 0f));
                    break;
                case "PF_Grill_Elmich":
                    Box("Base", root.transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.32f, 0.10f, 0.28f), neutral);
                    Box("LowerPlate", root.transform, new Vector3(0f, 0.11f, 0f), new Vector3(0.28f, 0.02f, 0.24f), neutral);
                    Transform hinge = Child("LidPivot", root.transform);
                    hinge.localPosition = new Vector3(0f, 0.12f, 0.14f);
                    Box("Lid", hinge, new Vector3(0f, 0.04f, -0.14f), new Vector3(0.32f, 0.06f, 0.28f), neutral);
                    Box("UpperPlate", hinge, new Vector3(0f, 0f, -0.14f), new Vector3(0.28f, 0.02f, 0.24f), neutral);
                    Box("Handle", hinge, new Vector3(0f, 0.04f, -0.30f), new Vector3(0.20f, 0.025f, 0.03f), neutral);
                    Box("Display", root.transform, new Vector3(0f, 0.065f, -0.145f), new Vector3(0.08f, 0.025f, 0.01f), red, false);
                    Transform grillAnchors = Child("Anchors", root.transform);
                    Anchor(grillAnchors, "InteractionPoint", new Vector3(0f, 0.12f, -0.14f));
                    Anchor(grillAnchors, "CakePlacementPoint", new Vector3(0f, 0.12f, 0f));
                    Anchor(grillAnchors, "SpatulaPoint", new Vector3(0.16f, 0.12f, 0f));
                    Anchor(grillAnchors, "AudioPoint", new Vector3(0f, 0.05f, 0f));
                    break;
                case "PF_ReadyCounterPoint":
                    Transform trigger = Child("InteractionTrigger", root.transform);
                    trigger.gameObject.layer = TramChanhLayers.InteractableIndex;
                    var collider = trigger.gameObject.AddComponent<BoxCollider>();
                    collider.isTrigger = true;
                    collider.center = new Vector3(0f, 0.025f, 0f);
                    collider.size = new Vector3(0.32f, 0.05f, 0.20f);
                    Anchor(root.transform, "DrinkPlacement", new Vector3(-0.08f, 0f, 0f));
                    Anchor(root.transform, "CakePlacement", new Vector3(0.08f, 0f, 0f));
                    Anchor(root.transform, "OrderIndicator", new Vector3(0f, 0.1f, 0f));
                    break;
                case "PF_PlasticStool":
                    Box("Seat", root.transform, new Vector3(0f, 0.28f, 0f), new Vector3(0.30f, 0.04f, 0.30f), red);
                    foreach (float x in new[] { -0.11f, 0.11f })
                    {
                        foreach (float z in new[] { -0.11f, 0.11f })
                        {
                            Box("Leg", root.transform, new Vector3(x, 0.13f, z), new Vector3(0.04f, 0.26f, 0.04f), red);
                        }
                    }
                    Anchor(root.transform, "SeatPoint", new Vector3(0f, 0.30f, 0f));
                    break;
                case "PF_YellowCrateTable":
                    Box("Crate", root.transform, new Vector3(0f, 0.20f, 0f), new Vector3(0.60f, 0.40f, 0.40f), yellow);
                    Box("TrayTop", root.transform, new Vector3(0f, 0.415f, 0f), new Vector3(0.60f, 0.03f, 0.40f), neutral);
                    Anchor(root.transform, "InteractionPoint", new Vector3(0f, 0.43f, -0.2f));
                    Anchor(root.transform, "DeliveryPoint", new Vector3(0f, 0.43f, 0f));
                    Transform seats = Child("Seats", root.transform);
                    Anchor(seats, "Seat01", new Vector3(0f, 0f, -0.65f));
                    Anchor(seats, "Seat02", new Vector3(0f, 0f, 0.65f));
                    break;
                default:
                    Object.DestroyImmediate(root);
                    throw new ArgumentException("Unknown blockout prefab: " + name, nameof(name));
            }

            return root;
        }

        private static void CreateCounterRecesses(GameObject stall)
        {
            // Scene-only primitive openings for flush, recessed bins. Keep the prefab
            // hierarchy and GT-001 top height; no change to the real workflow/layout.
            Transform slab = stall.transform.Find("Counter/SM_Stall_Counter");
            slab.GetComponent<Renderer>().enabled = false;
            Transform counter = stall.transform.Find("Counter");
            Material material = slab.GetComponent<Renderer>().sharedMaterial;
            float y = StallDimensions.CounterHeight - 0.02f;
            Box("SM_Counter_Front", counter, new Vector3(0f, y, 0.205f), new Vector3(1.8f, 0.04f, 0.39f), material);
            Box("SM_Counter_Back", counter, new Vector3(0f, y, -0.335f), new Vector3(1.8f, 0.04f, 0.13f), material);
            Box("SM_Counter_Left", counter, new Vector3(-0.735f, y, -0.13f), new Vector3(0.33f, 0.04f, 0.28f), material);
            Box("SM_Counter_Right", counter, new Vector3(0.455f, y, -0.13f), new Vector3(0.89f, 0.04f, 0.28f), material);
            Transform cabinet = stall.transform.Find("Structure/SM_Stall_Base");
            cabinet.localPosition = new Vector3(0f, 0.46f, 0f);
            cabinet.localScale = new Vector3(StallDimensions.Width, 0.72f, StallDimensions.Depth);
            PrefabUtility.RecordPrefabInstancePropertyModifications(slab.GetComponent<Renderer>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(cabinet);
            BoxCollider body = stall.transform.Find("Colliders/Body").GetComponent<BoxCollider>();
            body.center = cabinet.localPosition;
            body.size = cabinet.localScale;
            PrefabUtility.RecordPrefabInstancePropertyModifications(body);
        }

        private static void PlaceStation(StallAnchorSet set, StallAnchorId id, string name, Vector3 position, string prefabName = null)
        {
            if (!set.TryGet(id, out StallAnchor anchor))
            {
                throw new InvalidOperationException("Missing stall anchor: " + id);
            }

            anchor.name = name;
            anchor.transform.localPosition = position;
            anchor.Configure(id, false, "provisional blockout scene seed (DEC-011); not a measured station location");
            PrefabUtility.RecordPrefabInstancePropertyModifications(anchor.gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(anchor.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(anchor);
            if (prefabName != null)
            {
                InstantiateAt(prefabName, anchor.transform, Vector3.zero);
            }
        }

        private static void InstantiateAt(string name, Transform parent, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.localPosition = position;
        }

        private static void Mark(GameObject root, string notes)
        {
            root.AddComponent<PlaceholderAsset>().Configure("ART-STALL-001 blockout / final art tasks", "DEC-011; unmeasured equipment geometry", notes);
        }

        private static Transform Child(string name, Transform parent)
        {
            var child = new GameObject(name);
            child.layer = parent.gameObject.layer;
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void Anchor(Transform parent, string name, Vector3 position)
        {
            Child(name, parent).localPosition = position;
        }

        private static void Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool collider = true)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.layer = parent.gameObject.layer;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider)
            {
                Object.DestroyImmediate(box.GetComponent<Collider>());
            }
        }

        private static void OpenBox(string name, Transform parent, Vector3 size, Material material, bool collider = true)
        {
            Transform group = Child(name, parent);
            const float wall = 0.01f; // Provisional primitive wall thickness.
            Box("Bottom", group, new Vector3(0f, wall * 0.5f, 0f), new Vector3(size.x, wall, size.z), material, collider);
            foreach (float side in new[] { -1f, 1f })
            {
                Box("Side", group, new Vector3(side * (size.x - wall) * 0.5f, size.y * 0.5f, 0f), new Vector3(wall, size.y, size.z), material, collider);
                Box("End", group, new Vector3(0f, size.y * 0.5f, side * (size.z - wall) * 0.5f), new Vector3(size.x - wall * 2f, size.y, wall), material, collider);
            }
        }

        private static Material MaterialAsset(string name, Color color)
        {
            string path = StallPlaceholderBuilder.MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                material.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(material, path);
            }

            return material;
        }
    }
}
