using System;
using System.Collections.Generic;
using System.IO;
using TramChanh.App;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using TramChanh.Core.Provisional;
using TramChanh.Drinks.Runtime;
using TramChanh.Interaction;
using TramChanh.Interaction.Player;
using TramChanh.Lobby;
using TramChanh.Stall.Anchors;
using TramChanh.Stall.Runtime;
using TramChanh.UI.Localization;
using TramChanh.UI.Orders;
using TramChanh.UI.Prompt;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEngine.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace TramChanh.EditorTools.Placeholders
{
    /// <summary>Saved provisional gameplay assets over the unchanged blockout. Recipe seeds are TBD data.</summary>
    public static class DrinkWaveSceneBuilder
    {
        public const string ScenePath = "Assets/TramChanh/Scenes/Gameplay/SCN_DrinkWave.unity";
        public const string BasePath = "Assets/TramChanh/Scenes/Gameplay/SCN_Gameplay_Blockout.unity";
        public const string Folder = "Assets/TramChanh/Prefabs/Workstations/DrinkWave";
        public const string StationPath = Folder + "/PF_DrinkStation.prefab";
        public const string RecipePath = "Assets/TramChanh/ScriptableObjects/Recipes/SO_Recipe_Drink_Slice.asset";
        public const string DefinitionPath = "Assets/TramChanh/ScriptableObjects/Items/SO_Item_Drink_Slice.asset";
        public const string TextPath = "Assets/TramChanh/ScriptableObjects/UI/SO_PromptText_DrinkWave.asset";
        public const string BalancePath = "Assets/TramChanh/ScriptableObjects/Balance/SO_Balance_DrinkWave.asset";
        private const string BagPath = TeaRackPickupSceneBuilder.BagPath;

        [MenuItem("Tram Chanh/Placeholders/Create Drink Wave Gameplay")]
        public static void CreateFromMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { CreateAndSave(); }
        }
        public static void CreateAndSave()
        {
            if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
            EnsureFolder(Folder);
            EnsureFolder("Assets/TramChanh/Art/Animations/DrinkWave");
            EnsureFolder("Assets/TramChanh/Prefabs/Tools");
            var poses = ReadStationPoses();
            var definition = Asset<ItemDefinition>(DefinitionPath);
            Set(definition, "_id", "drink.slice");
            Set(definition, "_displayName", "Drink (menu name pending)");
            Set(definition, "_kind", (int)ItemKind.Drink);
            var recipe = Asset<DrinkRecipe>(RecipePath);
            Set(recipe, "_itemDefinition", definition);
            Set(recipe, "_teaBagType", "Pre-portioned tea bag");
            // DEC-007: editor seeds only, serialized and replaceable. No real tea quantity is assumed.
            Set(recipe, "_shakeHoldSeconds", 1f);
            Set(recipe, "_wipeHoldSeconds", 1f);
            Set(recipe, "_coconutJellyPortion", 0f);
            Set(recipe, "_lemonJellyPortion", 0f);
            Set(recipe, "_icePortion", 0f);
            var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalancePath);
            if (balance == null)
            {
                if (!AssetDatabase.CopyAsset(TeaRackPickupSceneBuilder.BalancePath, BalancePath)) { throw new IOException("Pickup balance is required."); }
                balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalancePath);
            }
            ConfigureBag();
            Set(definition, "_preparedPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(BagPath));
            GameObject station = CreateStation(poses, balance, recipe);
            GameObject table = CreateTable();
            GameObject vehicle = CreateVehicle();
            PromptLocalizationTable text = CreateText();
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Scene transitions can unload unused ScriptableObjects. Reload after that boundary.
            definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(DefinitionPath);
            balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalancePath);
            text = AssetDatabase.LoadAssetAtPath<PromptLocalizationTable>(TextPath);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerInteractionSceneBuilder.PlayerPath));
            player.transform.position = new Vector3(-0.72f, 0f, -1.2f); // Provisional editor spawn, does not move stall anchors.
            var view = player.AddComponent<HeldItemView>();
            Transform hold = Child(player.transform.Find("PlayerCamera/HandSocket"), "HoldAnchor");
            hold.localPosition = new Vector3(0f, 0.12f, 0.1f);
            Set(view, "_holdAnchor", hold);
            var lobby = player.AddComponent<LobbyOrderController>();
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PlayerInteractionSceneBuilder.PanelPath);
            var hud = new GameObject("InteractionHUD");
            hud.AddComponent<UIDocument>().panelSettings = panel;
            var prompt = hud.AddComponent<InteractionPromptView>();
            Set(prompt, "_table", text); Set(prompt, "_language", "vi");
            var entry = new GameObject("OrderEntryHUD");
            var document = entry.AddComponent<UIDocument>(); document.panelSettings = panel; document.sortingOrder = 10;
            var entryUI = entry.AddComponent<OrderEntryUI>();
            Set(entryUI, "_table", text); Set(entryUI, "_language", "vi");
            var bootstrapObject = new GameObject("DrinkWaveBootstrap");
            var loader = bootstrapObject.AddComponent<DrinkWaveSceneLoader>(); Set(loader, "_baseScenePath", BasePath);
            var bootstrap = bootstrapObject.AddComponent<DrinkWaveBootstrap>();
            Set(bootstrap, "_loader", loader); Set(bootstrap, "_balance", balance); Set(bootstrap, "_drinkDefinition", definition);
            Set(bootstrap, "_stationPrefab", station); Set(bootstrap, "_tablePointPrefab", table); Set(bootstrap, "_vehiclePointPrefab", vehicle);
            Set(bootstrap, "_input", player.GetComponent<PlayerInputReader>()); Set(bootstrap, "_player", player.GetComponent<FirstPersonController>());
            Set(bootstrap, "_interactor", player.GetComponent<PlayerInteractor>()); Set(bootstrap, "_heldView", view);
            Set(bootstrap, "_prompt", prompt); Set(bootstrap, "_entryUI", entryUI); Set(bootstrap, "_lobby", lobby);
            EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
            Debug.Log("[TramChanh] Saved additive drink gameplay: " + ScenePath);
        }
        public static void RepairShakeVisual()
        {
            ConfigureBag(); AssetDatabase.SaveAssets();
        }
        public static void RepairSavedSceneReferences()
        {
            CreateText(); AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.OpenScene(ScenePath);
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.TryGetComponent(out DrinkWaveBootstrap bootstrap))
                {
                    Set(bootstrap, "_balance", AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalancePath));
                    Set(bootstrap, "_drinkDefinition", AssetDatabase.LoadAssetAtPath<ItemDefinition>(DefinitionPath));
                }
                if (root.TryGetComponent(out InteractionPromptView prompt)) { Set(prompt, "_table", AssetDatabase.LoadAssetAtPath<PromptLocalizationTable>(TextPath)); }
                if (root.TryGetComponent(out OrderEntryUI entry)) { Set(entry, "_table", AssetDatabase.LoadAssetAtPath<PromptLocalizationTable>(TextPath)); }
            }
            EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
        }

        private static Dictionary<StallAnchorId, (Vector3 position, Quaternion rotation)> ReadStationPoses()
        {
            var scene = EditorSceneManager.OpenScene(BasePath);
            StallAnchorSet set = null;
            foreach (var root in scene.GetRootGameObjects()) { if (root.TryGetComponent(out StallAnchorSet found)) { set = found; } }
            if (set == null) { throw new InvalidOperationException("Saved blockout has no stall anchors."); }
            var result = new Dictionary<StallAnchorId, (Vector3, Quaternion)>();
            foreach (StallAnchorId id in new[] { StallAnchorId.TeaRack, StallAnchorId.Topping, StallAnchorId.IceBin, StallAnchorId.WipeArea, StallAnchorId.ReadyCounter })
            {
                if (!set.TryGet(id, out StallAnchor anchor)) { throw new InvalidOperationException("Missing blockout station anchor."); }
                Transform pose = anchor.transform.childCount > 0 ? anchor.transform.GetChild(0) : anchor.transform;
                result.Add(id, (set.transform.InverseTransformPoint(pose.position), Quaternion.Inverse(set.transform.rotation) * pose.rotation));
            }
            return result;
        }
        private static void ConfigureBag()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BagPath);
            try
            {
                Transform visual = root.transform.Find("Visual");
                Transform open = visual.Find("Bag_Open");
                if (open.childCount == 0) { Box(open, "OpenPlastic", new Vector3(0f, -0.09f, 0f), new Vector3(0.075f, 0.18f, 0.065f), visual.Find("Bag_Closed").GetComponent<Renderer>().sharedMaterial); }
                Transform contents = visual.Find("Contents");
                foreach (string name in new[] { "CoconutJelly", "LemonJelly", "Ice" })
                {
                    Transform part = contents.Find(name);
                    if (part.childCount == 0) { Box(part, "PlaceholderPortion", new Vector3(0f, name == "Ice" ? -0.055f : -0.12f, 0f), new Vector3(0.035f, 0.025f, 0.035f), AssetDatabase.LoadAssetAtPath<Material>(StallPlaceholderBuilder.MaterialFolder + "/MAT_Placeholder.mat")); }
                    part.gameObject.SetActive(false);
                }
                Transform condensation = Child(visual, "Condensation"); condensation.gameObject.SetActive(false);
                var state = GetOrAdd<TeaBagStateView>(root);
                Set(state, "_bagClosed", visual.Find("Bag_Closed").gameObject); Set(state, "_bagOpen", open.gameObject);
                Set(state, "_coconutJelly", contents.Find("CoconutJelly").gameObject); Set(state, "_lemonJelly", contents.Find("LemonJelly").gameObject);
                Set(state, "_ice", contents.Find("Ice").gameObject); Set(state, "_condensation", condensation.gameObject);
                Set(root.GetComponent<TeaBagItem>(), "_stateView", state);
                Animator animator = GetOrAdd<Animator>(root);
                animator.runtimeAnimatorController = MotionController("TeaBag_Shake", "Shaking", "Visual", "localEulerAnglesRaw.z", 0f, 8f, true);
                Set(state, "_animator", animator);
                PrefabUtility.SaveAsPrefabAsset(root, BagPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static GameObject CreateStation(Dictionary<StallAnchorId, (Vector3 position, Quaternion rotation)> poses, BalanceConfig balance, DrinkRecipe recipe)
        {
            var root = new GameObject("PF_DrinkStation");
            try
            {
                root.AddComponent<PlaceholderAsset>().Configure("DRINK-WAVE", "Final art and portions pending", "Uses saved station poses; pre-portioned tea, strict sequence.");
                GameObject rack = Variant("Assets/TramChanh/Prefabs/Workstations/PF_RedTeaRack.prefab");
                var control = GetOrAdd<TeaRackController>(rack);
                Set(control, "_id", 1); Set(control, "_interactionPoint", rack.transform.Find("InteractionPoint"));
                Set(control, "_balance", balance); Set(control, "_recipe", recipe);
                Set(control, "_bagPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(BagPath).GetComponent<TeaBagItem>());
                var slots = rack.transform.Find("BagSlots"); var data = new SerializedObject(control); var array = data.FindProperty("_bagSlots"); array.arraySize = slots.childCount;
                for (int i = 0; i < slots.childCount; i++) { array.GetArrayElementAtIndex(i).objectReferenceValue = slots.GetChild(i); } data.ApplyModifiedPropertiesWithoutUndo();
                Target(rack, control, false); SaveVariant(rack, "PF_RedTeaRack", root.transform, poses[StallAnchorId.TeaRack]);
                GameObject topping = Variant("Assets/TramChanh/Prefabs/Workstations/PF_ToppingStation.prefab");
                for (int i = 0; i < 2; i++)
                {
                    var bin = topping.transform.Find(i == 0 ? "CoconutJellyBin" : "LemonJellyBin").gameObject;
                    var action = bin.AddComponent<ToppingBin>(); Set(action, "_id", 2 + i); Set(action, "_toppingType", i);
                    Set(action, "_interactionPoint", bin.transform.Find("InteractionPoint")); Target(bin, action, true);
                }
                var coverAnimator = topping.AddComponent<Animator>();
                coverAnimator.runtimeAnimatorController = MotionController("Topping_Cover", "Action", "CoverPivot", "localEulerAnglesRaw.x", 70f, 90f, false);
                var coverView = topping.AddComponent<StationToolView>();
                foreach (var action in topping.GetComponentsInChildren<ToppingBin>()) { Hook(action, "_onAdded", coverView.PlayOnce); }
                SaveVariant(topping, "PF_ToppingStation", root.transform, poses[StallAnchorId.Topping]);
                GameObject ice = Variant("Assets/TramChanh/Prefabs/Workstations/PF_IceBin.prefab");
                var scoop = ice.AddComponent<IceBin>(); Set(scoop, "_id", 4); Set(scoop, "_interactionPoint", ice.transform.Find("InteractionPoint")); Target(ice, scoop, true);
                var scoopTool = CreateTool("PF_IceScoop", new Vector3(0.06f, 0.025f, 0.1f), "Ice_Scoop", false);
                var scoopInstance = (GameObject)PrefabUtility.InstantiatePrefab(scoopTool); scoopInstance.transform.SetParent(ice.transform.Find("ScoopRestPoint"), false);
                Hook(scoop, "_onScooped", scoopInstance.GetComponent<StationToolView>().PlayOnce);
                SaveVariant(ice, "PF_IceBin", root.transform, poses[StallAnchorId.IceBin]);
                var wipe = Child(root.transform, "WipeArea").gameObject;
                wipe.transform.localPosition = poses[StallAnchorId.WipeArea].position; wipe.transform.localRotation = poses[StallAnchorId.WipeArea].rotation;
                var point = Child(wipe.transform, "InteractionPoint"); var wipeAction = wipe.AddComponent<WipeInteraction>(); Set(wipeAction, "_id", 5); Set(wipeAction, "_interactionPoint", point);
                var clothAsset = CreateTool("PF_WipeCloth", new Vector3(0.15f, 0.01f, 0.15f), "Cloth_Wipe", true);
                var cloth = (GameObject)PrefabUtility.InstantiatePrefab(clothAsset); cloth.transform.SetParent(wipe.transform, false);
                Hook(wipeAction, "_onStarted", cloth.GetComponent<StationToolView>().Begin);
                Hook(wipeAction, "_onStopped", cloth.GetComponent<StationToolView>().Stop);
                Target(wipe, wipeAction, true);
                GameObject ready = Variant("Assets/TramChanh/Prefabs/Stall/PF_ReadyCounterPoint.prefab");
                var counter = ready.AddComponent<ReadyCounterPoint>(); Set(counter, "_id", 6); Set(counter, "_interactionPoint", ready.transform.Find("InteractionTrigger"));
                Set(counter, "_drinkPlacementPoint", ready.transform.Find("DrinkPlacement")); Set(counter, "_cakePlacementPoint", ready.transform.Find("CakePlacement")); Target(ready, counter, false);
                var pickupObject = Child(ready.transform, "LobbyPickup").gameObject; pickupObject.transform.localPosition = new Vector3(0f, 0.1f, 0.3f);
                var pickup = pickupObject.AddComponent<ReadyOrderPickupPoint>(); Set(pickup, "_id", 7); Set(pickup, "_interactionPoint", pickupObject.transform); Target(pickupObject, pickup, true);
                SaveVariant(ready, "PF_ReadyCounterPoint", root.transform, poses[StallAnchorId.ReadyCounter]);
                return PrefabUtility.SaveAsPrefabAsset(root, StationPath);
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static GameObject CreateTable()
        {
            var root = Variant("Assets/TramChanh/Prefabs/CustomerArea/PF_YellowCrateTable.prefab");
            var point = root.AddComponent<TableOrderPoint>(); Target(root, point, true);
            return SaveAndDestroy(root, Folder + "/PF_YellowCrateTable.prefab");
        }
        private static GameObject CreateVehicle()
        {
            var root = new GameObject("PF_Placeholder_VehiclePoint"); root.transform.localPosition = new Vector3(2f, 0f, -1.3f);
            root.AddComponent<PlaceholderAsset>().Configure("DEC-010", "Vehicle type pending", "Generic takeaway interaction point, no assumed vehicle model.");
            var anchors = Child(root.transform, "Anchors"); Child(anchors, "InteractionPoint").localPosition = Vector3.up;
            var point = root.AddComponent<VehicleOrderPoint>(); Target(root, point, true);
            // Aimable volume around the customer/vehicle; the bounds fallback was a 16 cm box at ground level.
            var vehicleTrigger = root.GetComponent<BoxCollider>(); vehicleTrigger.center = new Vector3(0f, 0.9f, 0.3f); vehicleTrigger.size = new Vector3(1f, 1.2f, 1f);
            Box(root.transform, "Placeholder", Vector3.up * 0.7f, new Vector3(0.4f, 1.4f, 0.4f), AssetDatabase.LoadAssetAtPath<Material>(StallPlaceholderBuilder.MaterialFolder + "/MAT_Placeholder.mat"));
            return SaveAndDestroy(root, Folder + "/PF_Placeholder_VehiclePoint.prefab");
        }
        private static PromptLocalizationTable CreateText()
        {
            var table = Asset<PromptLocalizationTable>(TextPath);
            var rows = new[]
            {
                ("preview.controls", "WASD: move · E/left mouse: station · F/right mouse: held item · Esc: pause", "WASD: di chuyển · E/chuột trái: quầy · F/chuột phải: vật đang cầm · Esc: tạm dừng"),
                ("preview.key", "[E / LMB]", "[E / chuột trái]"), ("preview.held_key", "[F / RMB]", "[F / chuột phải]"),
                ("order.entry.title", "Customer request", "Khách gọi món"), ("order.entry.enter", "Enter order", "Nhập đơn"), ("order.entry.send", "Send to stall", "Gửi tới quầy"),
                ("item.drink.slice", "Drink (menu name pending)", "Đồ uống (chờ tên món)"), ("order.point.take", "Take order", "Nhận yêu cầu của khách"), ("order.point.continue", "Continue order", "Tiếp tục nhập đơn"), ("order.entry.close", "Close", "Đóng"),
                ("drink.rack.take_bag", "Take pre-portioned tea bag", "Lấy túi trà chia sẵn"), ("drink.bag.open", "Open tea bag", "Mở túi trà"),
                ("drink.add_coconut", "Add coconut jelly", "Thêm thạch dừa"), ("drink.add_lemon", "Add lemon jelly", "Thêm thạch chanh"), ("drink.add_ice", "Scoop ice", "Thêm đá"),
                ("drink.bag.shake", "Hold to shake", "Giữ để lắc"), ("drink.wipe", "Hold to wipe", "Giữ để lau"),
                ("ready.place_item", "Place at Ready", "Đặt đồ uống hoàn tất"), ("ready.pick_up_order", "Pick up ready order", "Nhận đơn đã xong"),
                ("hands.full", "Hands full", "Đang cầm vật phẩm"), ("drink.rack.empty", "Tea rack empty", "Giá trà đã hết"), ("stall.no_ticket.drink", "Lobby ticket required", "Cần phiếu nước từ Lobby"),
                ("drink.need_open", "Open the bag first", "Mở túi trước"), ("drink.need_coconut_first", "Add coconut jelly first", "Thêm thạch dừa trước"), ("drink.need_lemon_first", "Add lemon jelly first", "Thêm thạch chanh trước"),
                ("drink.need_ice_first", "Add ice first", "Thêm đá trước"), ("drink.need_shake_first", "Shake first", "Lắc trước"),
                ("ready.not_finished", "Finish shaking and wiping first", "Hoàn thành lắc và lau trước"), ("ready.no_order", "Item has no live order", "Không có đơn hợp lệ"), ("ready.slot_full", "Ready slot occupied", "Chỗ đặt đang bận"),
                ("ready.already_ready", "Already Ready", "Đã hoàn tất"), ("ready.no_ready_order", "No ready order", "Chưa có đơn hoàn tất"),
                ("interaction.paused", "Paused", "Đang tạm dừng"), ("interaction.stall_role_required", "Stall role required", "Cần vai trò quầy"), ("interaction.lobby_role_required", "Lobby role required", "Cần vai trò Lobby"),
                ("interaction.held_continuous_not_supported", "Unsupported held action", "Không hỗ trợ thao tác này"), ("interaction.item_not_held", "Hold the item first", "Cần cầm vật phẩm"),
                ("drink.recipe.not_configured", "Recipe is not configured", "Thiếu thiết lập công thức"), ("drink.rack.not_configured", "Tea rack is not configured", "Thiếu thiết lập giá trà"),
                ("ready.counter.not_configured", "Ready counter is not configured", "Thiếu thiết lập chỗ đặt"), ("ready.pickup.not_configured", "Pickup point is not configured", "Thiếu thiết lập chỗ nhận"),
                ("ready.need_prepared_item", "Hold a finished item", "Cần cầm đồ uống hoàn tất"), ("ready.item_missing_placement_point", "Item placement is not configured", "Thiếu điểm đặt vật phẩm")
            };
            var combined = new List<(string, string, string)>(rows);
            var source = AssetDatabase.LoadAssetAtPath<PromptLocalizationTable>("Assets/TramChanh/ScriptableObjects/UI/SO_PromptText_OrderEntry.asset");
            var sourceEntries = new SerializedObject(source).FindProperty("_entries");
            var keys = new HashSet<string>(); foreach (var row in rows) { keys.Add(row.Item1); }
            for (int i = 0; i < sourceEntries.arraySize; i++)
            {
                var row = sourceEntries.GetArrayElementAtIndex(i); string key = row.FindPropertyRelative("_key").stringValue;
                if (keys.Add(key)) { combined.Add((key, row.FindPropertyRelative("_en").stringValue, row.FindPropertyRelative("_vi").stringValue)); }
            }
            rows = combined.ToArray();
            var serialized = new SerializedObject(table); var entries = serialized.FindProperty("_entries"); entries.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                var row = entries.GetArrayElementAtIndex(i); row.FindPropertyRelative("_key").stringValue = rows[i].Item1;
                row.FindPropertyRelative("_en").stringValue = rows[i].Item2; row.FindPropertyRelative("_vi").stringValue = rows[i].Item3;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo(); return table;
        }
        private static GameObject CreateTool(string name, Vector3 scale, string animation, bool loop)
        {
            string path = "Assets/TramChanh/Prefabs/Tools/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) { return existing; }
            var root = new GameObject(name);
            root.AddComponent<PlaceholderAsset>().Configure("DRINK-WAVE station tool", "Final art pending", "Station-only visual tool; never held by player.");
            Box(root.transform, "Visual", Vector3.zero, scale, AssetDatabase.LoadAssetAtPath<Material>(StallPlaceholderBuilder.MaterialFolder + "/MAT_Placeholder.mat"));
            root.AddComponent<Animator>().runtimeAnimatorController = MotionController(animation, loop ? "Active" : "Action", "Visual", "m_LocalPosition.x", 0f, 0.05f, loop);
            root.AddComponent<StationToolView>(); return SaveAndDestroy(root, path);
        }
        private static RuntimeAnimatorController MotionController(string name, string parameter, string path, string property, float rest, float moved, bool loop)
        {
            string folder = "Assets/TramChanh/Art/Animations/DrinkWave/";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(folder + "AC_" + name + ".controller");
            if (controller != null && controller.layers.Length > 0) { return controller; }
            // Provisional display motion curves, independent of recipe hold completion.
            var idle = new AnimationClip { name = "AN_" + name + "_Idle" };
            idle.SetCurve(path, typeof(Transform), property, AnimationCurve.Constant(0f, 0.25f, rest));
            var savedIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>(folder + idle.name + ".anim");
            if (savedIdle != null) { Object.DestroyImmediate(idle); idle = savedIdle; }
            else { AssetDatabase.CreateAsset(idle, folder + idle.name + ".anim"); }
            var motion = new AnimationClip { name = "AN_" + name };
            motion.SetCurve(path, typeof(Transform), property, new AnimationCurve(new Keyframe(0f, rest), new Keyframe(0.125f, moved), new Keyframe(0.25f, rest)));
            var settings = AnimationUtility.GetAnimationClipSettings(motion); settings.loopTime = loop; AnimationUtility.SetAnimationClipSettings(motion, settings);
            var savedMotion = AssetDatabase.LoadAssetAtPath<AnimationClip>(folder + motion.name + ".anim");
            if (savedMotion != null) { Object.DestroyImmediate(motion); motion = savedMotion; }
            else { AssetDatabase.CreateAsset(motion, folder + motion.name + ".anim"); }
            if (controller == null) { controller = AnimatorController.CreateAnimatorControllerAtPath(folder + "AC_" + name + ".controller"); }
            if (controller.layers.Length == 0) { controller.AddLayer("Base Layer"); }
            controller.AddParameter(parameter, loop ? AnimatorControllerParameterType.Bool : AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine; var idleState = machine.AddState("Idle"); idleState.motion = idle; machine.defaultState = idleState;
            var activeState = machine.AddState("Active"); activeState.motion = motion;
            var enter = idleState.AddTransition(activeState); enter.hasExitTime = false; enter.duration = 0f; enter.AddCondition(AnimatorConditionMode.If, 0f, parameter);
            var exit = activeState.AddTransition(idleState); exit.duration = 0f; exit.hasExitTime = !loop;
            if (loop) { exit.AddCondition(AnimatorConditionMode.IfNot, 0f, parameter); } else { exit.exitTime = 1f; }
            EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
            return controller;
        }
        private static void Hook(Object target, string field, UnityAction callback)
        {
            var info = target.GetType().GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            UnityEventTools.AddPersistentListener((UnityEvent)info.GetValue(target), callback); EditorUtility.SetDirty(target);
        }
        private static GameObject Variant(string path) => (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        private static void SaveVariant(GameObject instance, string name, Transform parent, (Vector3 position, Quaternion rotation) pose)
        {
            GameObject asset = SaveAndDestroy(instance, Folder + "/" + name + ".prefab");
            var nested = (GameObject)PrefabUtility.InstantiatePrefab(asset); nested.transform.SetParent(parent, false);
            nested.transform.localPosition = pose.position; nested.transform.localRotation = pose.rotation;
        }
        private static GameObject SaveAndDestroy(GameObject root, string path)
        {
            try { return PrefabUtility.SaveAsPrefabAsset(root, path); } finally { Object.DestroyImmediate(root); }
        }
        private static void Target(GameObject root, MonoBehaviour action, bool addTrigger)
        {
            root.layer = TramChanhLayers.InteractableIndex;
            var reference = GetOrAdd<InteractableRef>(root); Set(reference, "_behaviour", action);
            if (addTrigger || root.GetComponentsInChildren<Collider>().Length == 0)
            {
                var trigger = root.AddComponent<BoxCollider>(); trigger.isTrigger = true;
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    var bounds = renderers[0].bounds; foreach (var renderer in renderers) { bounds.Encapsulate(renderer.bounds); }
                    trigger.center = root.transform.InverseTransformPoint(bounds.center); trigger.size = Vector3.Scale(bounds.size, new Vector3(1f / root.transform.lossyScale.x, 1f / root.transform.lossyScale.y, 1f / root.transform.lossyScale.z));
                }
                else { trigger.center = Vector3.zero; trigger.size = new Vector3(0.16f, 0.12f, 0.16f); }
            }
            foreach (var collider in root.GetComponentsInChildren<Collider>()) { collider.gameObject.layer = TramChanhLayers.InteractableIndex; }
        }
        private static T GetOrAdd<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }
        private static T Asset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            return asset;
        }
        private static Transform Child(Transform parent, string name)
        {
            Transform found = parent.Find(name); if (found != null) { return found; }
            var child = new GameObject(name).transform; child.SetParent(parent, false); return child;
        }
        private static void Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = name; box.transform.SetParent(parent, false);
            box.transform.localPosition = position; box.transform.localScale = scale; box.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(box.GetComponent<Collider>());
        }
        private static void Set(Object target, string name, Object value) { var data = new SerializedObject(target); data.FindProperty(name).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Set(Object target, string name, string value) { var data = new SerializedObject(target); data.FindProperty(name).stringValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Set(Object target, string name, int value) { var data = new SerializedObject(target); data.FindProperty(name).intValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Set(Object target, string name, float value) { var data = new SerializedObject(target); data.FindProperty(name).floatValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) { return; }
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/')); AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
    }
}
