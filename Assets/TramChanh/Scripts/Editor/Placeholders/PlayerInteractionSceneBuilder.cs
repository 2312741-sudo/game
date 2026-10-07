using System.IO;
using TramChanh.App;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using TramChanh.Core.Provisional;
using TramChanh.Interaction;
using TramChanh.Interaction.Player;
using TramChanh.Interaction.Preview;
using TramChanh.UI.Localization;
using TramChanh.UI.Prompt;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace TramChanh.EditorTools.Placeholders
{
    /// <summary>Editor-only preview seeds. Never rewrites the original blockout, stall or station poses.</summary>
    public static class PlayerInteractionSceneBuilder
    {
        public const string ScenePath = "Assets/TramChanh/Scenes/Test/SCN_InteractionTest.unity";
        public const string PlayerPath = "Assets/TramChanh/Prefabs/NPC/PF_Player.prefab";
        public const string CubePath = "Assets/TramChanh/Prefabs/Workstations/PF_Placeholder_InteractionCube.prefab";
        public const string BalancePath = "Assets/TramChanh/ScriptableObjects/Balance/SO_Balance_PlayerPreview.asset";
        public const string InputPath = "Assets/TramChanh/Settings/Input/TramChanh.inputactions";
        public const string LocalizationPath = "Assets/TramChanh/ScriptableObjects/UI/SO_PromptText_Preview.asset";
        public const string ThemePath = "Assets/TramChanh/Settings/UI/InteractionPreviewTheme.tss";
        public const string PanelPath = "Assets/TramChanh/Settings/UI/SO_Panel_InteractionPreview.asset";

        [MenuItem("Tram Chanh/Placeholders/Create Player Interaction Scene")]
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
            EnsureFolder("Assets/TramChanh/ScriptableObjects/UI");
            EnsureFolder("Assets/TramChanh/Settings/UI");
            InputActionAsset actions = CreateInput();
            GameObject playerPrefab = CreatePlayer(actions);
            GameObject cubePrefab = CreateCube();
            if (!AssetDatabase.CopyAsset(GameplayBlockoutBuilder.ScenePath, ScenePath))
            {
                throw new IOException("Could not copy the saved blockout into the interaction test scene.");
            }
            var scene = EditorSceneManager.OpenScene(ScenePath);
            // Opening a scene unloads unused ScriptableObjects. Load scene data after that boundary.
            BalanceConfig balance = CreateBalance();
            PromptLocalizationTable localization = CreateLocalization();
            GameObject stall = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "PF_Stall_TramChanh")
                {
                    stall = root;
                }
                foreach (Camera camera in root.GetComponentsInChildren<Camera>())
                {
                    camera.enabled = false;
                }
                foreach (AudioListener listener in root.GetComponentsInChildren<AudioListener>())
                {
                    listener.enabled = false;
                }
            }
            Transform tea = stall.transform.Find("Anchors/TeaRackAnchor/PF_RedTeaRack");
            Transform grill = stall.transform.Find("Anchors/GrillAnchor/PF_Grill_Elmich");
            AddInspection(tea.gameObject, tea.Find("InteractionPoint"), 1, "tea_rack");
            AddInspection(grill.gameObject, grill.Find("Anchors/InteractionPoint"), 2, "grill");
            var cube = (GameObject)PrefabUtility.InstantiatePrefab(cubePrefab);
            cube.transform.position = new Vector3(-1.8f, 0.65f, -0.3f); // Provisional test-object placement.
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(-0.72f, 0f, -1.2f); // Provisional worker-side spawn; stall anchors unchanged.

            var ui = new GameObject("InteractionHUD");
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (panel == null)
            {
                if (!File.Exists(ThemePath))
                {
                    File.WriteAllText(ThemePath, "@import url(\"unity-theme://default\");\n");
                    AssetDatabase.ImportAsset(ThemePath, ImportAssetOptions.ForceSynchronousImport);
                }
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = new Vector2Int(1280, 720);
                AssetDatabase.CreateAsset(panel, PanelPath);
            }
            ui.AddComponent<UIDocument>().panelSettings = panel;
            var prompt = ui.AddComponent<InteractionPromptView>();
            SetObject(prompt, "_table", localization);

            var bootstrap = new GameObject("TestBootstrap").AddComponent<PlayerInteractionTestBootstrap>();
            SetObject(bootstrap, "_balance", balance);
            SetObject(bootstrap, "_input", player.GetComponent<PlayerInputReader>());
            SetObject(bootstrap, "_player", player.GetComponent<FirstPersonController>());
            SetObject(bootstrap, "_interactor", player.GetComponent<PlayerInteractor>());
            SetObject(bootstrap, "_prompt", prompt);
            SetInt(bootstrap, "_actorId", 1);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[TramChanh] Saved first-person interaction preview: " + ScenePath);
        }
        private static InputActionAsset CreateInput()
        {
            var existing = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (existing != null)
            {
                return existing;
            }
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap map = asset.AddActionMap("Gameplay");
            InputAction move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            map.AddAction("Look", InputActionType.Value, "<Mouse>/delta", expectedControlLayout: "Vector2");
            InputAction interact = map.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
            interact.AddBinding("<Mouse>/leftButton");
            InputAction held = map.AddAction("UseHeld", InputActionType.Button, "<Keyboard>/f");
            held.AddBinding("<Mouse>/rightButton");
            map.AddAction("Discard", InputActionType.Button, "<Keyboard>/q", interactions: "hold(duration=0.5)");
            map.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            File.WriteAllText(InputPath, asset.ToJson());
            Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
        }
        private static BalanceConfig CreateBalance()
        {
            var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalancePath);
            if (balance != null)
            {
                return balance;
            }
            balance = ScriptableObject.CreateInstance<BalanceConfig>();
            // All numbers below are provisional editor seeds, serialized in SO data.
            SetFloat(balance, "_reachDistance", 1.8f);
            SetFloat(balance, "_moveSpeed", 2.5f);
            SetFloat(balance, "_lookSensitivity", 0.12f);
            SetFloat(balance, "_gravity", -9.81f);
            SetFloat(balance, "_pitchLimit", 85f);
            AssetDatabase.CreateAsset(balance, BalancePath);
            return balance;
        }
        private static PromptLocalizationTable CreateLocalization()
        {
            var table = AssetDatabase.LoadAssetAtPath<PromptLocalizationTable>(LocalizationPath);
            if (table != null)
            {
                return table;
            }
            table = ScriptableObject.CreateInstance<PromptLocalizationTable>();
            var rows = new[]
            {
                ("preview.controls", "WASD: move   Mouse: look   E / click: inspect   Esc: release cursor", "WASD: di chuyển   Chuột: nhìn   E / nhấp: kiểm tra   Esc: thả chuột"),
                ("preview.key", "[E / LMB]", "[E / chuột trái]"),
                ("preview.held_key", "[F / RMB]", "[F / chuột phải]"),
                ("preview.tea_rack.inspect", "Inspect tea rack placeholder", "Kiểm tra giá trà tạm"),
                ("preview.tea_rack.reset", "Tea rack inspected — reset highlight", "Đã kiểm tra giá trà — bỏ đánh dấu"),
                ("preview.grill.inspect", "Inspect grill placeholder", "Kiểm tra bếp nướng tạm"),
                ("preview.grill.reset", "Grill inspected — reset highlight", "Đã kiểm tra bếp — bỏ đánh dấu"),
                ("preview.cube.inspect", "Inspect test cube", "Kiểm tra khối thử nghiệm"),
                ("preview.cube.reset", "Cube inspected — reset highlight", "Đã kiểm tra khối — bỏ đánh dấu"),
                ("preview.blocked", "Inspection temporarily blocked", "Tạm thời không thể kiểm tra"),
            };
            var serialized = new SerializedObject(table);
            SerializedProperty entries = serialized.FindProperty("_entries");
            entries.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_key").stringValue = rows[i].Item1;
                entry.FindPropertyRelative("_en").stringValue = rows[i].Item2;
                entry.FindPropertyRelative("_vi").stringValue = rows[i].Item3;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(table, LocalizationPath);
            return table;
        }
        private static GameObject CreatePlayer(InputActionAsset actions)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            if (existing != null)
            {
                return existing;
            }
            var root = new GameObject("PF_Player");
            root.layer = TramChanhLayers.PlayerIndex;
            var body = root.AddComponent<CharacterController>();
            body.height = 1.7f;
            body.radius = 0.22f;
            body.center = Vector3.up * 0.85f;
            body.stepOffset = 0.25f;
            body.skinWidth = 0.02f;
            body.minMoveDistance = 0f; // Retain movement even when each frame advances less than 1 mm.
            var eye = new GameObject("PlayerCamera");
            eye.transform.SetParent(root.transform, false);
            eye.transform.localPosition = Vector3.up * 1.6f;
            eye.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            eye.tag = "MainCamera";
            eye.layer = TramChanhLayers.PlayerIndex;
            Camera camera = eye.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.fieldOfView = 65f;
            eye.AddComponent<AudioListener>();
            var socket = new GameObject("HandSocket");
            socket.transform.SetParent(eye.transform, false);
            socket.transform.localPosition = new Vector3(0.2f, -0.2f, 0.4f);
            socket.layer = TramChanhLayers.PlayerIndex;
            var input = root.AddComponent<PlayerInputReader>();
            SetObject(input, "_actions", actions);
            var controller = root.AddComponent<FirstPersonController>();
            SetObject(controller, "_camera", camera);
            root.AddComponent<PlayerInteractor>();
            var saved = PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
            Object.DestroyImmediate(root);
            return saved;
        }
        private static GameObject CreateCube()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CubePath);
            if (existing != null)
            {
                return existing;
            }
            var root = new GameObject("PF_Placeholder_InteractionCube");
            root.layer = TramChanhLayers.InteractableIndex;
            root.AddComponent<PlaceholderAsset>().Configure("CX-012 test fixture", "Temporary visual", "Primitive cube used only to verify player interaction.");
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "Visual";
            mesh.transform.SetParent(root.transform, false);
            mesh.transform.localPosition = Vector3.up * 0.2f;
            mesh.transform.localScale = Vector3.one * 0.4f;
            mesh.layer = TramChanhLayers.InteractableIndex;
            mesh.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(StallPlaceholderBuilder.MaterialFolder + "/MAT_Placeholder.mat");
            var point = new GameObject("InteractionPoint");
            point.transform.SetParent(root.transform, false);
            point.transform.localPosition = Vector3.up * 0.2f;
            AddInspection(root, point.transform, 3, "cube");
            var saved = PrefabUtility.SaveAsPrefabAsset(root, CubePath);
            Object.DestroyImmediate(root);
            return saved;
        }
        private static void AddInspection(GameObject target, Transform point, int id, string name)
        {
            var inspection = target.AddComponent<InspectionInteractable>();
            SetInt(inspection, "_id", id);
            SetInt(inspection, "_availability", (int)AvailabilityStatus.Available);
            SetObject(inspection, "_interactionPoint", point);
            var serialized = new SerializedObject(inspection);
            serialized.FindProperty("_inspectPromptKey").stringValue = "preview." + name + ".inspect";
            serialized.FindProperty("_resetPromptKey").stringValue = "preview." + name + ".reset";
            serialized.FindProperty("_blockedReasonKey").stringValue = "preview.blocked";
            serialized.FindProperty("_highlightColor").colorValue = new Color(0.2f, 0.8f, 0.4f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetObject(target.AddComponent<InteractableRef>(), "_behaviour", inspection);
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            var trigger = target.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = target.transform.InverseTransformPoint(bounds.center);
            trigger.size = bounds.size;
        }
        private static void SetObject(Object target, string name, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetFloat(Object target, string name, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetInt(Object target, string name, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
