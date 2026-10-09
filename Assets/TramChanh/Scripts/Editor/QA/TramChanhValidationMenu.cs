using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TramChanh.App;
using TramChanh.Core.GroundTruth;
using TramChanh.Interaction;
using TramChanh.Stall.Anchors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TramChanh.EditorTools
{
    /// <summary>
    /// MAIN-105 read-only QA commands ("Tram Chanh/QA/...") and the batch entry point
    /// <see cref="RunAllBatch"/> for <c>-batchmode -executeMethod</c>.
    /// Never saves scenes, prefabs, assets or settings: scenes that were not already open are opened
    /// additively and closed again without saving; prefabs are read as assets, never edited.
    /// Results go to the Console and to Temp/TramChanhQA/report.txt (Temp is not tracked by Git).
    /// </summary>
    public static class TramChanhValidationMenu
    {
        public const string MenuRoot = "Tram Chanh/QA/";
        public const string ScenesFolder = "Assets/TramChanh/Scenes";
        public const string PrefabsFolder = "Assets/TramChanh/Prefabs";
        public const string MainScenePath = TramChanhSceneMenu.Main;
        public const string ReportRelativePath = "Temp/TramChanhQA/report.txt";
        public const int ExpectedStallAnchorCount = 10;
        /// <summary>TABLES-10 dine-in tables: optional environment anchors TABLE_01..TABLE_10 (Docs/Coordination/TABLE_ANCHOR_CONTRACT.md).</summary>
        public const int ExpectedTableCount = 10;
        public const string TableSeatAnchor = "Seat";
        public const string CustomerAreaName = "CustomerArea";
        private static readonly Regex ExactTableName = new Regex("^TABLE_(0[1-9]|10)$");
        // Anything that looks like a table id but is not exact, e.g. "TABLE_1", "Table_01", "TABLE-03", "TABLE_11", "table 4".
        private static readonly Regex TableLikeName = new Regex(
            "^table[ _-]?\\d+$", RegexOptions.IgnoreCase);

        public static string TableAnchorName(int tableNumber) => "TABLE_" + tableNumber.ToString("00");

        public static readonly string[] RequiredEnvironmentAnchors =
        {
            TramChanhMainBootstrap.StallRootAnchor,
            TramChanhMainBootstrap.PlayerSpawnAnchor,
            TramChanhMainBootstrap.TableAnchor,
            TramChanhMainBootstrap.CakeTableAnchor,
            TramChanhMainBootstrap.VehicleAnchor,
        };

        private delegate void Check(QaReport report);

        // ---------------------------------------------------------------- menu

        [MenuItem(MenuRoot + "Run All", false, 0)]
        public static void RunAllMenu() => Execute("Run All", AllChecks(), consoleOnly: false);

        [MenuItem(MenuRoot + "Validate Missing Scripts", false, 20)]
        public static void ValidateMissingScriptsMenu() => Execute("Validate Missing Scripts", new Check[] { ValidateMissingScripts }, false);

        [MenuItem(MenuRoot + "Validate Prefab and Scene References", false, 21)]
        public static void ValidateReferencesMenu() => Execute("Validate Prefab and Scene References", new Check[] { ValidateReferences }, false);

        [MenuItem(MenuRoot + "Validate Main Scene Required Objects", false, 22)]
        public static void ValidateMainSceneMenu() => Execute("Validate Main Scene Required Objects", new Check[] { ValidateMainScene }, false);

        [MenuItem(MenuRoot + "Validate Interaction Anchors", false, 23)]
        public static void ValidateInteractionAnchorsMenu() => Execute("Validate Interaction Anchors", new Check[] { ValidateInteractionAnchors }, false);

        [MenuItem(MenuRoot + "Validate Build Profile Scenes", false, 24)]
        public static void ValidateBuildScenesMenu() => Execute("Validate Build Profile Scenes", new Check[] { ValidateBuildScenes }, false);

        /// <summary>Runs every validator with the log capture active and reports only the captured Console section.</summary>
        [MenuItem(MenuRoot + "Collect Console Errors", false, 40)]
        public static void CollectConsoleErrorsMenu() => Execute("Collect Console Errors", AllChecks(), consoleOnly: true);

        /// <summary>
        /// Batch entry point: <c>Unity -batchmode -nographics -projectPath P -executeMethod
        /// TramChanh.EditorTools.TramChanhValidationMenu.RunAllBatch -logFile L</c>.
        /// Exits with 0 when no validator reported an error and 1 otherwise (also on an unexpected exception).
        /// </summary>
        public static void RunAllBatch()
        {
            int code = 1;
            try
            {
                QaReport report = Execute("Run All (batch)", AllChecks(), consoleOnly: false);
                code = report.ErrorCount == 0 ? 0 : 1;
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                code = 1;
            }
            Console.WriteLine("[TramChanhQA] RunAllBatch exit code " + code);
            EditorApplication.Exit(code);
        }

        /// <summary>Runs every validator without writing to the Console; used by tests or other tools.</summary>
        public static int RunAllQuiet(out string reportText)
        {
            QaReport report = RunChecks("Run All (quiet)", AllChecks());
            reportText = report.Format(false);
            return report.ErrorCount;
        }

        private static Check[] AllChecks() => new Check[]
        {
            ValidateMissingScripts,
            ValidateReferences,
            ValidateMainScene,
            ValidateInteractionAnchors,
            ValidateBuildScenes,
        };

        // ---------------------------------------------------------------- runner

        private static QaReport Execute(string title, Check[] checks, bool consoleOnly)
        {
            QaReport report = RunChecks(title, checks);
            string text = report.Format(consoleOnly);
            string path = WriteReport(text);
            string head = "[TramChanhQA] " + title + ": " + (report.ErrorCount == 0 ? "PASS" : "FAIL")
                + " (errors " + report.ErrorCount + ", warnings " + report.WarningCount + ") - report: " + path + "\n";
            if (report.ErrorCount == 0) { Debug.Log(head + text); }
            else { Debug.LogError(head + text); }
            return report;
        }

        private static QaReport RunChecks(string title, Check[] checks)
        {
            var report = new QaReport(title);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                report.Error("Runner", "Exit Play Mode first: QA validation only runs in Edit Mode.");
                return report;
            }

            Application.LogCallback capture = (condition, stackTrace, type) => report.Captured(type, condition, stackTrace);
            Application.logMessageReceived += capture;
            try
            {
                if (EditorUtility.scriptCompilationFailed)
                {
                    report.Error("Console", "Script compilation failed: fix the compile errors shown in the Console first.");
                }
                foreach (Check check in checks)
                {
                    string section = check.Method.Name;
                    try { check(report); }
                    catch (Exception error) { report.Error(section, "Validator threw " + error.GetType().Name + ": " + error.Message); }
                }
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }
            return report;
        }

        private static string WriteReport(string text)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string full = Path.Combine(projectRoot, ReportRelativePath.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllText(full, text, new UTF8Encoding(false));
            }
            catch (Exception error)
            {
                Debug.LogWarning("[TramChanhQA] Could not write " + full + ": " + error.Message);
            }
            return full;
        }

        // ---------------------------------------------------------------- 1. missing scripts

        private static void ValidateMissingScripts(QaReport report)
        {
            const string section = "MissingScripts";
            int objects = 0;
            foreach (string path in FindAssetPaths("t:Prefab", PrefabsFolder))
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null) { report.Error(section, path + ": prefab could not be loaded."); continue; }
                objects += CountMissingScripts(report, section, path, root);
            }
            foreach (string path in FindAssetPaths("t:Scene", ScenesFolder))
            {
                WithScene(report, section, path, scene =>
                {
                    foreach (GameObject root in scene.GetRootGameObjects()) { objects += CountMissingScripts(report, section, path, root); }
                });
            }
            report.Info(section, "Checked " + objects + " GameObjects in prefabs under " + PrefabsFolder + " and scenes under " + ScenesFolder + ".");
        }

        private static int CountMissingScripts(QaReport report, string section, string assetPath, GameObject root)
        {
            int count = 0;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                count++;
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (missing > 0) { report.Error(section, assetPath + " :: " + HierarchyPath(t) + " has " + missing + " missing script(s)."); }
            }
            return count;
        }

        // ---------------------------------------------------------------- 2. references

        private static void ValidateReferences(QaReport report)
        {
            const string section = "References";
            if (!CanDetectDanglingReferences())
            {
                report.Warning(section, "This Editor exposes neither objectReferenceInstanceIDValue nor objectReferenceEntityIdValue; dangling references cannot be detected.");
            }
            int properties = 0;
            foreach (string path in FindAssetPaths("t:Prefab", PrefabsFolder))
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root != null) { properties += CheckReferences(report, section, path, root); }
            }
            foreach (string path in FindAssetPaths("t:Scene", ScenesFolder))
            {
                WithScene(report, section, path, scene =>
                {
                    foreach (GameObject root in scene.GetRootGameObjects()) { properties += CheckReferences(report, section, path, root); }
                });
            }
            report.Info(section, "Checked " + properties + " object-reference properties.");
        }

        private static int CheckReferences(QaReport report, string section, string assetPath, GameObject root)
        {
            int checkedProperties = 0;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = t.gameObject;
                if (PrefabUtility.IsPrefabAssetMissing(go))
                {
                    report.Error(section, assetPath + " :: " + HierarchyPath(t) + " is an instance of a missing prefab asset.");
                }
                foreach (Component component in go.GetComponents<Component>())
                {
                    if (component == null) { continue; } // missing script: reported by Validate Missing Scripts
                    using (var serialized = new SerializedObject(component))
                    {
                        SerializedProperty it = serialized.GetIterator();
                        bool enter = true;
                        while (it.Next(enter))
                        {
                            enter = it.propertyType != SerializedPropertyType.String;
                            if (it.propertyType != SerializedPropertyType.ObjectReference || it.propertyPath == "m_Script") { continue; }
                            checkedProperties++;
                            if (IsDangling(it))
                            {
                                report.Error(section, assetPath + " :: " + HierarchyPath(t) + " [" + component.GetType().Name + "] "
                                    + it.propertyPath + " references a missing object.");
                            }
                        }
                    }
                }
            }
            return checkedProperties;
        }

        // Unity 6 is moving SerializedProperty from instance IDs to EntityId. Reading either property by
        // reflection keeps this file compiling (without obsolete warnings) on 2021.3 references and Unity 6.
        private static readonly PropertyInfo InstanceIdProperty = typeof(SerializedProperty).GetProperty("objectReferenceInstanceIDValue");
        private static readonly PropertyInfo EntityIdProperty = typeof(SerializedProperty).GetProperty("objectReferenceEntityIdValue");

        private static bool CanDetectDanglingReferences() => InstanceIdProperty != null || EntityIdProperty != null;

        /// <summary>A missing ("dangling") reference: a persistent id is stored but the object does not resolve.</summary>
        private static readonly HashSet<string> HeldOnlyInteractables = new HashSet<string>
        {
            "TramChanh.Drinks.Runtime.TeaBagItem",
        };

        private static bool IsDangling(SerializedProperty property)
        {
            if (property.objectReferenceValue != null) { return false; }
            // Prefer EntityId: on Unity 6000.6 the obsolete instance-id getter throws (TargetInvocationException).
            PropertyInfo info = EntityIdProperty ?? InstanceIdProperty;
            if (info == null) { return false; }
            object value = info.GetValue(property, null);
            if (value == null) { return false; }
            if (value is int id) { return id != 0; }
            return !value.Equals(Activator.CreateInstance(value.GetType()));
        }

        // ---------------------------------------------------------------- 3. main scene

        private static void ValidateMainScene(QaReport report)
        {
            const string section = "MainScene";
            if (!File.Exists(MainScenePath)) { report.Error(section, MainScenePath + " does not exist on this branch."); return; }
            WithScene(report, section, MainScenePath, scene =>
            {
                TramChanhMainBootstrap[] bootstraps = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<TramChanhMainBootstrap>(true)).ToArray();
                if (bootstraps.Length != 1)
                {
                    report.Error(section, "Expected exactly one TramChanhMainBootstrap in " + MainScenePath + ", found " + bootstraps.Length + ".");
                    if (bootstraps.Length == 0) { return; }
                }
                TramChanhMainBootstrap bootstrap = bootstraps[0];
                report.Info(section, "Bootstrap: " + HierarchyPath(bootstrap.transform));
                if (!bootstrap.enabled || !bootstrap.gameObject.activeSelf) { report.Error(section, "TramChanhMainBootstrap is disabled or its GameObject is inactive."); }

                GameObject environment = null;
                using (var serialized = new SerializedObject(bootstrap))
                {
                    SerializedProperty it = serialized.GetIterator();
                    bool enter = true;
                    int fields = 0;
                    while (it.NextVisible(enter))
                    {
                        enter = it.propertyType != SerializedPropertyType.String;
                        if (it.propertyPath == "m_Script") { continue; }
                        if (it.isArray && it.propertyType == SerializedPropertyType.Generic && it.depth == 0 && it.arraySize == 0)
                        {
                            report.Error(section, "Bootstrap field " + it.propertyPath + " is an empty array.");
                        }
                        if (it.propertyType != SerializedPropertyType.ObjectReference) { continue; }
                        fields++;
                        if (it.objectReferenceValue == null)
                        {
                            report.Error(section, "Bootstrap field " + it.propertyPath + (IsDangling(it) ? " references a missing object." : " is not assigned."));
                        }
                    }
                    report.Info(section, "Bootstrap object-reference fields checked: " + fields + ".");
                    SerializedProperty env = serialized.FindProperty("_environmentPrefab");
                    environment = env != null ? env.objectReferenceValue as GameObject : null;
                }
                if (environment == null) { report.Error(section, "Bootstrap _environmentPrefab is not assigned; anchors cannot be checked."); return; }
                ValidateEnvironmentAnchors(report, section, AssetDatabase.GetAssetPath(environment), environment);
            });
        }

        private static void ValidateEnvironmentAnchors(QaReport report, string section, string assetPath, GameObject environment)
        {
            Transform stallRoot = null;
            foreach (string name in RequiredEnvironmentAnchors)
            {
                Transform direct = environment.transform.Find(name);
                Transform found = direct != null ? direct : FindDeep(environment.transform, name);
                if (found == null) { report.Error(section, assetPath + " is missing anchor " + name + "."); continue; }
                if (direct == null) { report.Warning(section, assetPath + " anchor " + name + " is not a direct child (found at " + HierarchyPath(found) + ")."); }
                if (name == TramChanhMainBootstrap.StallRootAnchor) { stallRoot = found; }
            }
            ValidateTableAnchors(report, section, assetPath, environment);
            if (stallRoot == null) { return; }
            StallAnchorSet set = stallRoot.GetComponentInChildren<StallAnchorSet>(true);
            if (set == null) { report.Error(section, assetPath + " :: StallRoot contains no StallAnchorSet."); return; }
            StallAnchor[] anchors = set.GetComponentsInChildren<StallAnchor>(true);
            var ids = new HashSet<StallAnchorId>(anchors.Select(a => a.Id));
            StallAnchorId[] expected = (StallAnchorId[])Enum.GetValues(typeof(StallAnchorId));
            if (anchors.Length != ExpectedStallAnchorCount)
            {
                report.Error(section, "StallAnchorSet at " + HierarchyPath(set.transform) + " has " + anchors.Length + " StallAnchor(s); expected " + ExpectedStallAnchorCount + ".");
            }
            foreach (StallAnchorId id in expected.Where(id => !ids.Contains(id)))
            {
                report.Error(section, "StallAnchorSet is missing anchor id " + id + ".");
            }
            if (ids.Count != anchors.Length) { report.Error(section, "StallAnchorSet has duplicate anchor ids."); }
            report.Info(section, "Environment " + assetPath + ": anchors " + string.Join(", ", RequiredEnvironmentAnchors) + "; StallAnchorSet with " + anchors.Length + " anchors.");
        }

        /// <summary>
        /// TABLES-03: TABLE_01..TABLE_10 are optional (the bootstrap falls back to TablePoint/CakeTablePoint and its
        /// fallback layout). When present they must be exact, unique, placed under the root or one CustomerArea child,
        /// carry a Seat child and stay visual-only (the bootstrap adds the gameplay point).
        /// </summary>
        private static void ValidateTableAnchors(QaReport report, string section, string assetPath, GameObject environment)
        {
            Transform root = environment.transform;
            var byName = new Dictionary<string, List<Transform>>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == root) { continue; }
                if (ExactTableName.IsMatch(t.name))
                {
                    if (!byName.TryGetValue(t.name, out List<Transform> list)) { byName[t.name] = list = new List<Transform>(); }
                    list.Add(t);
                }
                else if (TableLikeName.IsMatch(t.name))
                {
                    report.Error(section, assetPath + " :: " + HierarchyPath(t) + " looks like a table anchor but is misnamed; use TABLE_01..TABLE_" + ExpectedTableCount.ToString("00") + " (two digits, upper case).");
                }
            }
            Transform[] areas = root.Cast<Transform>().Where(c => c.name == CustomerAreaName).ToArray();
            if (areas.Length > 1) { report.Error(section, assetPath + " has " + areas.Length + " direct '" + CustomerAreaName + "' children; table anchors need a single CustomerArea."); }
            var missing = new List<string>();
            for (int number = 1; number <= ExpectedTableCount; number++)
            {
                string name = TableAnchorName(number);
                if (!byName.TryGetValue(name, out List<Transform> found)) { missing.Add(name); continue; }
                if (found.Count > 1)
                {
                    report.Error(section, assetPath + " has " + found.Count + " '" + name + "' anchors (" + string.Join(", ", found.Select(HierarchyPath)) + "); table ids must be unique.");
                    continue;
                }
                Transform anchor = found[0];
                bool placed = anchor.parent == root || (areas.Length == 1 && anchor.parent == areas[0]);
                if (!placed) { report.Warning(section, assetPath + " :: " + HierarchyPath(anchor) + " should be a direct child of the environment root or of its single " + CustomerAreaName + " child."); }
                if (anchor.Find(TableSeatAnchor) == null) { report.Warning(section, assetPath + " :: " + HierarchyPath(anchor) + " has no '" + TableSeatAnchor + "' child; the bootstrap uses its default seat offset."); }
                foreach (InteractableRef reference in anchor.GetComponentsInChildren<InteractableRef>(true))
                {
                    report.Error(section, assetPath + " :: " + HierarchyPath(reference.transform) + " carries an InteractableRef; table furniture must be visual-only (the bootstrap adds the gameplay point).");
                }
                foreach (Collider collider in anchor.GetComponentsInChildren<Collider>(true))
                {
                    if (!collider.isTrigger && collider.gameObject.layer != TramChanhLayers.EnvironmentIndex)
                    { report.Warning(section, assetPath + " :: " + HierarchyPath(collider.transform) + " collider is on layer " + collider.gameObject.layer + "; furniture colliders belong on Environment (" + TramChanhLayers.EnvironmentIndex + ")."); }
                }
            }
            if (missing.Count == ExpectedTableCount)
            {
                report.Info(section, assetPath + " has no TABLE_xx anchors: the bootstrap uses TablePoint (TABLE_01), CakeTablePoint (TABLE_02) and its fallback layout for TABLE_03..TABLE_10.");
            }
            else if (missing.Count > 0)
            {
                report.Warning(section, assetPath + " is missing table anchors " + string.Join(", ", missing) + "; those tables use the bootstrap fallback layout.");
            }
            else
            {
                report.Info(section, assetPath + ": TABLE_01..TABLE_" + ExpectedTableCount.ToString("00") + " anchors present.");
            }
        }

        // ---------------------------------------------------------------- 4. interaction anchors

        private static void ValidateInteractionAnchors(QaReport report)
        {
            const string section = "Interaction";
            int layer = TramChanhLayers.InteractableIndex;
            if (LayerMask.LayerToName(layer) != TramChanhLayers.Interactable)
            {
                report.Warning(section, "Layer " + layer + " is named '" + LayerMask.LayerToName(layer) + "', expected '" + TramChanhLayers.Interactable
                    + "'. Run Tram Chanh/Setup/Apply Project Settings (ProjectSettings are not tracked in Git).");
            }
            int prefabs = 0;
            foreach (string path in FindAssetPaths("t:Prefab", PrefabsFolder))
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null) { continue; }
                MonoBehaviour[] interactables = root.GetComponentsInChildren<MonoBehaviour>(true).Where(b => b is IInteractable).ToArray();
                InteractableRef[] refs = root.GetComponentsInChildren<InteractableRef>(true);
                if (interactables.Length == 0 && refs.Length == 0) { continue; }
                prefabs++;
                var targeted = new HashSet<MonoBehaviour>();
                foreach (InteractableRef interactableRef in refs)
                {
                    string where = path + " :: " + HierarchyPath(interactableRef.transform);
                    MonoBehaviour target;
                    using (var serialized = new SerializedObject(interactableRef))
                    {
                        SerializedProperty behaviour = serialized.FindProperty("_behaviour");
                        target = behaviour != null ? behaviour.objectReferenceValue as MonoBehaviour : null;
                    }
                    if (target == null) { report.Error(section, where + " InteractableRef._behaviour is not assigned."); }
                    else if (!(target is IInteractable)) { report.Error(section, where + " InteractableRef._behaviour (" + target.GetType().Name + ") does not implement IInteractable."); }
                    else { targeted.Add(target); }

                    Collider[] colliders = interactableRef.GetComponentsInChildren<Collider>(true)
                        .Where(c => NearestRef(c.transform) == interactableRef).ToArray();
                    if (colliders.Length == 0) { report.Error(section, where + " has no Collider that resolves to this InteractableRef."); continue; }
                    if (!colliders.Any(c => c.gameObject.layer == layer))
                    {
                        report.Error(section, where + " has Collider(s) but none on layer " + layer + " (" + string.Join(", ", colliders.Select(c => c.name + "=" + c.gameObject.layer)) + ").");
                    }
                }
                foreach (MonoBehaviour interactable in interactables.Where(i => !targeted.Contains(i)))
                {
                    // Explicit allowlist: these types expose IInteractable only to forward their held action and are
                    // reached in the world through another interactable (TeaBagItem via the tea rack). Items that are
                    // also picked up from the world (e.g. BatterMeasureCup) are NOT exempt and must keep their ref.
                    if (HeldOnlyInteractables.Contains(interactable.GetType().FullName))
                    {
                        report.Warning(section, path + " :: " + HierarchyPath(interactable.transform) + " [" + interactable.GetType().Name + "] is a held-use item without a world InteractableRef (expected).");
                        continue;
                    }
                    report.Error(section, path + " :: " + HierarchyPath(interactable.transform) + " [" + interactable.GetType().Name + "] is IInteractable but no InteractableRef points to it.");
                }
                foreach (Collider orphan in root.GetComponentsInChildren<Collider>(true).Where(c => c.gameObject.layer == layer && NearestRef(c.transform) == null))
                {
                    report.Warning(section, path + " :: " + HierarchyPath(orphan.transform) + " is on layer " + layer + " but has no InteractableRef in its parents.");
                }
            }
            report.Info(section, "Checked " + prefabs + " interactable prefab(s) under " + PrefabsFolder + ".");
        }

        private static InteractableRef NearestRef(Transform t)
        {
            for (Transform cur = t; cur != null; cur = cur.parent)
            {
                InteractableRef found = cur.GetComponent<InteractableRef>();
                if (found != null) { return found; }
            }
            return null;
        }

        // ---------------------------------------------------------------- 5. build scenes

        private static void ValidateBuildScenes(QaReport report)
        {
            const string section = "BuildScenes";
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            report.Info(section, "EditorBuildSettings.scenes (" + scenes.Length + "): " + (scenes.Length == 0 ? "<empty>" : string.Join(", ", scenes.Select(s => (s.enabled ? "" : "[disabled] ") + s.path))));
            ReportActiveBuildProfile(report, section);
            if (scenes.Length == 0)
            {
                report.Error(section, "The scene list is empty. Use Tram Chanh/Scenes/Set Build Settings To Playable Scenes (this validator never changes it).");
                return;
            }
            if (scenes[0].path != MainScenePath) { report.Error(section, "First build scene is " + scenes[0].path + "; expected " + MainScenePath + "."); }
            else if (!scenes[0].enabled) { report.Error(section, MainScenePath + " is first but disabled."); }
            foreach (EditorBuildSettingsScene s in scenes.Where(s => !File.Exists(s.path)))
            {
                report.Error(section, "Build scene " + s.path + " does not exist.");
            }
        }

        // Build Profiles exist only in Unity 6; reflection keeps this compiling against older references.
        private static void ReportActiveBuildProfile(QaReport report, string section)
        {
            Type profileType = Type.GetType("UnityEditor.Build.Profile.BuildProfile, UnityEditor.CoreModule")
                ?? Type.GetType("UnityEditor.Build.Profile.BuildProfile, UnityEditor");
            MethodInfo getActive = profileType?.GetMethod("GetActiveBuildProfile", BindingFlags.Public | BindingFlags.Static);
            if (getActive == null) { report.Info(section, "Build Profiles API not found; checked EditorBuildSettings only."); return; }
            var profile = getActive.Invoke(null, null) as UnityEngine.Object;
            if (profile == null) { report.Info(section, "No custom Build Profile is active (platform profile uses EditorBuildSettings.scenes)."); return; }
            PropertyInfo overrides = profileType.GetProperty("overrideGlobalScenes");
            bool overridden = overrides != null && overrides.GetValue(profile, null) is bool b && b;
            report.Info(section, "Active Build Profile: " + profile.name + (overridden ? " (overrides the global scene list)" : ""));
            if (overridden && profileType.GetProperty("scenes")?.GetValue(profile, null) is EditorBuildSettingsScene[] own)
            {
                if (own.Length == 0 || own[0].path != MainScenePath || !own[0].enabled)
                {
                    report.Error(section, "Active Build Profile '" + profile.name + "' does not start with an enabled " + MainScenePath + ".");
                }
            }
        }

        // ---------------------------------------------------------------- helpers

        private static IEnumerable<string> FindAssetPaths(string filter, string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) { return Enumerable.Empty<string>(); }
            return AssetDatabase.FindAssets(filter, new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.StartsWith(folder + "/", StringComparison.Ordinal))
                .Distinct()
                .OrderBy(p => p, StringComparer.Ordinal);
        }

        /// <summary>
        /// Gives read access to a scene. A scene that is already open is used as is; otherwise it is opened
        /// additively and closed again without saving. Nothing here saves or marks assets dirty.
        /// </summary>
        private static void WithScene(QaReport report, string section, string path, Action<Scene> inspect)
        {
            Scene existing = SceneManager.GetSceneByPath(path);
            bool wasOpen = existing.IsValid() && existing.isLoaded;
            Scene scene = existing;
            if (!wasOpen)
            {
                try { scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); }
                catch (Exception error) { report.Error(section, path + ": could not open scene (" + error.Message + ")."); return; }
            }
            else if (existing.isDirty)
            {
                report.Warning(section, path + " is open with unsaved changes; the in-memory version was checked, not the file.");
            }
            try { inspect(scene); }
            finally
            {
                if (!wasOpen && scene.IsValid()) { EditorSceneManager.CloseScene(scene, true); }
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t != root && t.name == name) { return t; }
            }
            return null;
        }

        private static string HierarchyPath(Transform t)
        {
            var parts = new List<string>();
            for (Transform cur = t; cur != null; cur = cur.parent) { parts.Add(cur.name); }
            parts.Reverse();
            return string.Join("/", parts);
        }

        // ---------------------------------------------------------------- report

        private enum Severity { Info, Warning, Error }

        private sealed class QaReport
        {
            private readonly string _title;
            private readonly List<(Severity Severity, string Section, string Message)> _entries = new List<(Severity, string, string)>();
            private readonly List<string> _console = new List<string>();
            private int _consoleErrors;
            private int _consoleWarnings;

            public QaReport(string title) { _title = title; }

            public int ErrorCount => _entries.Count(e => e.Severity == Severity.Error) + _consoleErrors;
            public int WarningCount => _entries.Count(e => e.Severity == Severity.Warning) + _consoleWarnings;

            public void Info(string section, string message) => _entries.Add((Severity.Info, section, message));
            public void Warning(string section, string message) => _entries.Add((Severity.Warning, section, message));
            public void Error(string section, string message) => _entries.Add((Severity.Error, section, message));

            public void Captured(LogType type, string condition, string stackTrace)
            {
                if (type == LogType.Log) { return; }
                if (type == LogType.Warning) { _consoleWarnings++; }
                else { _consoleErrors++; }
                string first = string.IsNullOrEmpty(stackTrace) ? "" : " @ " + stackTrace.Split('\n')[0].Trim();
                _console.Add(type + ": " + condition + first);
            }

            public string Format(bool consoleOnly)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Tram Chanh QA - " + _title + " (Unity " + Application.unityVersion + ", " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ")");
                sb.AppendLine("Read-only validation: no scene, prefab, asset or setting was saved.");
                if (!consoleOnly)
                {
                    foreach (IGrouping<string, (Severity Severity, string Section, string Message)> group in _entries.GroupBy(e => e.Section))
                    {
                        sb.AppendLine();
                        sb.AppendLine("== " + group.Key);
                        foreach ((Severity severity, string _, string message) in group)
                        {
                            sb.AppendLine("  " + severity.ToString().ToUpperInvariant() + ": " + message);
                        }
                    }
                }
                sb.AppendLine();
                sb.AppendLine("== Console capture (Application.logMessageReceived during this run)");
                sb.AppendLine("  errors/exceptions/asserts: " + _consoleErrors + ", warnings: " + _consoleWarnings);
                foreach (string line in _console) { sb.AppendLine("  " + line); }
                sb.AppendLine();
                sb.AppendLine("SUMMARY: " + (ErrorCount == 0 ? "PASS" : "FAIL") + " - validator errors "
                    + _entries.Count(e => e.Severity == Severity.Error) + ", validator warnings " + _entries.Count(e => e.Severity == Severity.Warning)
                    + ", console errors " + _consoleErrors + ", console warnings " + _consoleWarnings);
                return sb.ToString();
            }
        }
    }
}
