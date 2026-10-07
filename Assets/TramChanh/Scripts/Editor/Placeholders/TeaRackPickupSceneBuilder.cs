using System.IO;
using TramChanh.App;
using TramChanh.Content;
using TramChanh.Core.GroundTruth;
using TramChanh.Core.Provisional;
using TramChanh.Drinks.Runtime;
using TramChanh.DevTools.Drinks;
using TramChanh.Interaction;
using TramChanh.Interaction.Preview;
using TramChanh.UI.Localization;
using TramChanh.UI.Prompt;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TramChanh.EditorTools.Placeholders
{
    /// <summary>Provisional editor seeds for DRINK-001; never rewrites the blockout or station layout.</summary>
    public static class TeaRackPickupSceneBuilder
    {
        public const string ScenePath = "Assets/TramChanh/Scenes/Test/SCN_TeaRackPickupTest.unity";
        public const string BagPath = "Assets/TramChanh/Prefabs/Items/PF_TeaBag_PrePortioned.prefab";
        public const string BalancePath = "Assets/TramChanh/ScriptableObjects/Balance/SO_Balance_TeaPickupTest.asset";
        public const string TextPath = "Assets/TramChanh/ScriptableObjects/UI/SO_PromptText_TeaPickupTest.asset";

        [MenuItem("Tram Chanh/Placeholders/Create Tea Rack Pickup Test Scene")]
        public static void CreateFromMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                CreateAndSave();
            }
        }

        public static void CreateAndSave()
        {
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            CreateBag();
            if (!AssetDatabase.CopyAsset(PlayerInteractionSceneBuilder.ScenePath, ScenePath))
            {
                throw new IOException("The saved player interaction scene is required.");
            }
            var scene = EditorSceneManager.OpenScene(ScenePath);
            GameObject stall = null;
            GameObject player = null;
            PlayerInteractionTestBootstrap bootstrap = null;
            InteractionPromptView prompt = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "PF_Stall_TramChanh")
                {
                    stall = root;
                }
                if (root.name == "PF_Player")
                {
                    player = root;
                }
                if (root.TryGetComponent(out PlayerInteractionTestBootstrap foundBootstrap))
                {
                    bootstrap = foundBootstrap;
                }
                if (root.TryGetComponent(out InteractionPromptView foundPrompt))
                {
                    prompt = foundPrompt;
                }
            }
            Transform tea = stall.transform.Find("Anchors/TeaRackAnchor/PF_RedTeaRack");
            Transform slots = tea.Find("BagSlots");
            if (!File.Exists(BalancePath))
            {
                AssetDatabase.CopyAsset(PlayerInteractionSceneBuilder.BalancePath, BalancePath);
                var data = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalancePath);
                var serialized = new SerializedObject(data);
                serialized.FindProperty("_teaRackCapacity").intValue = slots.childCount;
                serialized.FindProperty("_teaRackInitialStock").intValue = slots.childCount;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalancePath);
            Object.DestroyImmediate(tea.GetComponent<InspectionInteractable>());
            var rack = tea.gameObject.AddComponent<TeaRackController>();
            var rackData = new SerializedObject(rack);
            rackData.FindProperty("_id").intValue = 1;
            rackData.FindProperty("_interactionPoint").objectReferenceValue = tea.Find("InteractionPoint");
            rackData.FindProperty("_balance").objectReferenceValue = balance;
            rackData.FindProperty("_bagPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(BagPath).GetComponent<TeaBagItem>();
            SerializedProperty bagSlots = rackData.FindProperty("_bagSlots");
            bagSlots.arraySize = slots.childCount;
            for (int i = 0; i < slots.childCount; i++)
            {
                bagSlots.GetArrayElementAtIndex(i).objectReferenceValue = slots.GetChild(i);
            }
            rackData.ApplyModifiedPropertiesWithoutUndo();
            SetReference(tea.GetComponent<InteractableRef>(), "_behaviour", rack);
            var handView = player.AddComponent<HeldItemView>();
            Transform holdAnchor = Child(player.transform.Find("PlayerCamera/HandSocket"), "HoldAnchor");
            // Provisional hand pose stored in the scene; keeps this bag inside the view.
            holdAnchor.localPosition = new Vector3(0f, 0.12f, 0.1f);
            SetReference(handView, "_holdAnchor", holdAnchor);
            SetReference(bootstrap, "_heldItemView", handView);
            SetReference(bootstrap, "_balance", balance);
            ConfigurePickupFixture(bootstrap, rack);
            SetReference(prompt, "_table", CreateText());
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[TramChanh] Saved DRINK-001 pickup-only test scene: " + ScenePath);
        }

        /// <summary>Retires the production bypass in the saved pickup-only fixture.</summary>
        public static void MigratePickupFixture()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            PlayerInteractionTestBootstrap bootstrap = null;
            TeaRackController rack = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.TryGetComponent(out PlayerInteractionTestBootstrap found)) { bootstrap = found; }
                var racks = root.GetComponentsInChildren<TeaRackController>(true);
                if (racks.Length > 0) { rack = racks[0]; }
            }
            if (bootstrap == null || rack == null) { throw new System.InvalidOperationException("Pickup fixture wiring is missing."); }
            ConfigurePickupFixture(bootstrap, rack);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }
        private static void ConfigurePickupFixture(PlayerInteractionTestBootstrap bootstrap, TeaRackController rack)
        {
            var setup = bootstrap.GetComponent<TeaRackPickupTestSetup>();
            if (setup == null) { setup = bootstrap.gameObject.AddComponent<TeaRackPickupTestSetup>(); }
            SetReference(setup, "_bootstrap", bootstrap);
            SetReference(setup, "_rack", rack);
        }

        private static void CreateBag()
        {
            if (File.Exists(BagPath))
            {
                return;
            }
            // Geometry is a provisional editor seed, not a real tea quantity or final art.
            var root = new GameObject("PF_TeaBag_PrePortioned");
            root.layer = TramChanhLayers.EnvironmentIndex;
            root.AddComponent<PlaceholderAsset>().Configure("ART-DRINK-002", "Real bag dimensions pending", "Primitive pre-portioned bag; no measuring, toppings or ice.");
            var anchors = Child(root.transform, "Anchors");
            Transform grip = Child(anchors, "HandGrip");
            Transform placement = Child(anchors, "PlacementPoint");
            placement.localPosition = new Vector3(0f, -0.18f, 0f);
            var visual = Child(root.transform, "Visual");
            Material plastic = CreateMaterial("MAT_Plastic_Transparent", new Color(0.9f, 0.95f, 1f, 0.25f), true);
            Material tea = CreateMaterial("MAT_Food_Tea", new Color(0.65f, 0.3f, 0.05f, 1f), false);
            Box(visual, "Bag_Closed", new Vector3(0f, -0.09f, 0f), new Vector3(0.07f, 0.18f, 0.06f), plastic);
            Child(visual, "Bag_Open").gameObject.SetActive(false);
            Box(visual, "TeaLiquid", new Vector3(0f, -0.125f, 0f), new Vector3(0.055f, 0.1f, 0.045f), tea);
            var contents = Child(visual, "Contents");
            foreach (string name in new[] { "CoconutJelly", "LemonJelly", "Ice" })
            {
                Child(contents, name).gameObject.SetActive(false);
            }
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, -0.09f, 0f);
            collider.size = new Vector3(0.07f, 0.18f, 0.06f);
            var item = root.AddComponent<TeaBagItem>();
            SetReference(item, "_handGrip", grip);
            SetReference(item, "_placementPoint", placement);
            PrefabUtility.SaveAsPrefabAsset(root, BagPath);
            Object.DestroyImmediate(root);
        }

        private static Material CreateMaterial(string name, Color color, bool transparent)
        {
            string path = StallPlaceholderBuilder.MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color);
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetShaderPassEnabled("ShadowCaster", false);
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static PromptLocalizationTable CreateText()
        {
            if (!File.Exists(TextPath))
            {
                AssetDatabase.CopyAsset(PlayerInteractionSceneBuilder.LocalizationPath, TextPath);
                var table = AssetDatabase.LoadAssetAtPath<PromptLocalizationTable>(TextPath);
                var data = new SerializedObject(table);
                SerializedProperty entries = data.FindProperty("_entries");
                entries.GetArrayElementAtIndex(0).FindPropertyRelative("_en").stringValue = "WASD: move   Mouse: look   E / click: take bag   Esc: pause   Pickup test only";
                entries.GetArrayElementAtIndex(0).FindPropertyRelative("_vi").stringValue = "WASD: di chuyển   Chuột: nhìn   E / nhấp: lấy túi trà   Esc: tạm dừng   Cảnh thử lấy túi";
                var rows = new[]
                {
                    ("drink.rack.take_bag", "Take pre-portioned tea bag", "Lấy túi trà chia sẵn"),
                    ("hands.full", "Hands full — already holding an item", "Đang cầm vật phẩm"),
                    ("drink.rack.empty", "Tea rack is empty", "Giá trà đã hết túi"),
                    ("stall.no_ticket.drink", "A drink ticket from Lobby is required", "Cần phiếu nước từ Lobby"),
                    ("interaction.paused", "Game paused", "Đang tạm dừng"),
                    ("interaction.stall_role_required", "Stall role required", "Cần vai trò quầy"),
                    ("drink.rack.not_configured", "Tea rack setup is missing", "Thiếu thiết lập giá trà")
                };
                int offset = entries.arraySize;
                entries.arraySize += rows.Length;
                for (int i = 0; i < rows.Length; i++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(offset + i);
                    entry.FindPropertyRelative("_key").stringValue = rows[i].Item1;
                    entry.FindPropertyRelative("_en").stringValue = rows[i].Item2;
                    entry.FindPropertyRelative("_vi").stringValue = rows[i].Item3;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            return AssetDatabase.LoadAssetAtPath<PromptLocalizationTable>(TextPath);
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.gameObject.layer = parent.gameObject.layer;
            return child;
        }

        private static void Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            box.layer = parent.gameObject.layer;
            box.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(box.GetComponent<Collider>());
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
