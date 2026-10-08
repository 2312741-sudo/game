using System;
using System.IO;
using TramChanh.Cakes;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TramChanh.EditorTools.Placeholders
{
    /// <summary>Saved replaceable tools. All quantities/timings below are explicit DEV seeds, real values TBD.</summary>
    public static class CakeWavePrefabBuilder
    {
        public const string Folder = "Assets/TramChanh/Prefabs/ACCEL01/Cakes";
        public const string DataFolder = "Assets/TramChanh/Data/ACCEL01/Cakes";
        public const string StationPath = Folder + "/PF_CakeStation.prefab";
        public const string RecipePath = DataFolder + "/SO_CakeRecipe_DEV_TBD.asset";
        public const string CupPath = DataFolder + "/SO_MeasureCup_500ml_DEV_TBD.asset";
        public const string DefinitionPath = DataFolder + "/SO_Item_Cake_DEV_TBD.asset";
        public const string CakePath = Folder + "/PF_Cake_Prepared.prefab";
        public const string CupPrefabPath = Folder + "/PF_BatterMeasureCup_500ml.prefab";
        private const string Notes = "DEV TEST SEED ONLY. Not real Tram Chanh quantities, timings, sauce or menu. DEC-007/008/013/019 remain TBD.";
        [MenuItem("Tram Chanh/Placeholders/Create ACCEL Cake Station")]
        public static void CreateSaved()
        {
            Directory.CreateDirectory(Folder); Directory.CreateDirectory(DataFolder); AssetDatabase.Refresh();
            var definition = Asset<ItemDefinition>(DefinitionPath);
            Set(definition, "_id", "cake.dev.tbd"); Set(definition, "_displayName", "Bánh thử nghiệm (công thức TBD)"); Set(definition, "_kind", (int)ItemKind.Cake);
            var recipe = Asset<CakeRecipe>(RecipePath);
            Set(recipe, "_itemDefinition", definition); Set(recipe, "_targetBatterMl", 120f); Set(recipe, "_batterToleranceMl", 10f);
            Set(recipe, "_cookedThreshold", 4f); Set(recipe, "_burnThreshold", 12f); Set(recipe, "_openLidHeatFactor", .25f);
            Set(recipe, "_sauce", "DEV_TBD"); Set(recipe, "_cutHoldSeconds", .7f); Set(recipe, "_sauceHoldSeconds", .7f); Set(recipe, "_rollHoldSeconds", .7f);
            Set(recipe, "_outOfTolerancePolicy", (int)BatterOutOfTolerancePolicy.AllowWithPenalty); Set(recipe, "_deviationPenaltyPerMl", .5f); Set(recipe, "_provisionalNotes", Notes);
            var cupDefinition = Asset<MeasureCupDefinition>(CupPath);
            Set(cupDefinition, "_fillRateMlPerSecond", 60f); Set(cupDefinition, "_provisionalNotes", Notes);
            var amber = Material("Batter_DEV", new Color(1f, .72f, .22f));
            var red = Material("Tools_Red", new Color(.72f, .09f, .06f));
            var gray = Material("Tools_Steel", new Color(.6f, .62f, .65f));
            var wood = Material("Tools_Wood", new Color(.42f, .23f, .1f));
            var paper = Material("WrappingPaper", new Color(.92f, .87f, .72f));
            var cake = new GameObject("PF_Cake_Prepared");
            var item = cake.AddComponent<CakeItem>();
            Transform anchors = Child(cake.transform, "Anchors"); Set(item, "_handGrip", Child(anchors, "HandGrip")); Child(anchors, "PlacementPoint");
            GameObject flat = Box(cake.transform, "SM_Cake_Flat", new Vector3(.2f, .006f, .15f), Vector3.zero, amber);
            // GT-008: vertical long rolled shape; no horizontal-roll presentation.
            GameObject rolled = Box(cake.transform, "SM_Cake_RolledVertical", new Vector3(.04f, .15f, .035f), new Vector3(0f, .075f, 0f), amber);
            GameObject wrap = Box(cake.transform, "SM_Cake_WrappingPaper", new Vector3(.045f, .10f, .04f), new Vector3(0f, .055f, 0f), paper);
            rolled.SetActive(false); wrap.SetActive(false); Set(item, "_flatVisual", flat); Set(item, "_rolledVisual", rolled); Set(item, "_paperVisual", wrap);
            Save(cake, CakePath); Set(definition, "_preparedPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(CakePath));
            var cupObject = new GameObject("PF_BatterMeasureCup_500ml");
            var cup = cupObject.AddComponent<BatterMeasureCup>();
            Transform ca = Child(cupObject.transform, "Anchors"); Set(cup, "_handGrip", Child(ca, "HandGrip")); Child(ca, "PourPoint"); Child(ca, "PlacementPoint");
            // Open top, visible graduation markings, a separate configurable liquid level.
            Box(cupObject.transform, "Cup_Base", new Vector3(.09f, .005f, .09f), Vector3.zero, gray);
            Box(cupObject.transform, "Cup_Back", new Vector3(.09f, .11f, .004f), new Vector3(0f, .055f, .045f), gray);
            Box(cupObject.transform, "Cup_Left", new Vector3(.004f, .11f, .09f), new Vector3(-.045f, .055f, 0f), gray);
            Box(cupObject.transform, "Cup_Right", new Vector3(.004f, .11f, .09f), new Vector3(.045f, .055f, 0f), gray);
            for (int i = 1; i <= 5; i++) { Box(cupObject.transform, "Graduation_" + (i * 100), new Vector3(.027f, .002f, .004f), new Vector3(.022f, i * .02f, -.045f), red); }
            var liquid = Box(cupObject.transform, "BatterLevel", new Vector3(.08f, .001f, .08f), new Vector3(0f, .005f, 0f), amber); liquid.SetActive(false);
            Set(cup, "_liquid", liquid.transform); Set(cup, "_definition", cupDefinition); Save(cupObject, CupPrefabPath);
            Tool("PF_Spatula_WoodHandle", wood, new Vector3(.03f, .015f, .16f));
            Tool("PF_Scissors_RedGray", red, new Vector3(.06f, .015f, .14f));
            Tool("PF_SauceBag", red, new Vector3(.055f, .12f, .055f));
            Tool("PF_CakeWrappingPaper", paper, new Vector3(.21f, .002f, .18f));
            var station = new GameObject("PF_CakeStation"); var controller = station.AddComponent<CakeStation>();
            var batter = Child(station.transform, "BatterArea"); batter.localPosition = new Vector3(-.32f, 0f, -.07f);
            var cupInstance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CupPrefabPath), batter);
            cup = cupInstance.GetComponent<BatterMeasureCup>(); Set(cup, "_home", batter); Set(controller, "_cup", cup);
            var source = Child(station.transform, "BatterSource"); source.localPosition = new Vector3(-.32f, .04f, .14f);
            Box(source, "BatterContainer", new Vector3(.13f, .08f, .13f), Vector3.zero, gray);
            Point(source, controller, CakeStationAction.Fill, 1102);
            var grill = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/Workstations/PF_Grill_Elmich.prefab"), station.transform);
            grill.name = "PF_Grill_Elmich"; grill.transform.localPosition = new Vector3(-.04f, 0f, .03f);
            Transform lid = Find(grill.transform, "LidPivot"), placement = Find(grill.transform, "CakePlacementPoint");
            Set(controller, "_lidPivot", lid); Set(controller, "_cakePlacement", placement);
            Point(grill.transform, controller, CakeStationAction.Grill, 1103);
            var roll = Child(station.transform, "RollArea"); roll.localPosition = new Vector3(.25f, .015f, -.07f);
            Box(roll, "RollBoard", new Vector3(.23f, .01f, .20f), Vector3.zero, wood); Point(roll, controller, CakeStationAction.RollArea, 1104);
            var spatula = ToolInstance(station.transform, "PF_Spatula_WoodHandle", new Vector3(.16f, .02f, .15f));
            var scissors = ToolInstance(station.transform, "PF_Scissors_RedGray", new Vector3(.35f, .02f, .15f)); Set(controller, "_scissors", scissors.transform);
            var sauceBag = ToolInstance(station.transform, "PF_SauceBag", new Vector3(.43f, .04f, .02f)); Set(controller, "_sauceBag", sauceBag.transform);
            Point(sauceBag.transform, controller, CakeStationAction.Sauce, 1105, "DEV_TBD");
            var wrapping = Child(station.transform, "WrappingArea"); wrapping.localPosition = new Vector3(.25f, .02f, -.28f);
            ToolInstance(wrapping, "PF_CakeWrappingPaper", Vector3.zero); Point(wrapping, controller, CakeStationAction.Wrap, 1106); Set(controller, "_wrappingArea", wrapping);
            var flip = station.AddComponent<PlaceholderFlipAction>(); Set(flip, "_rollArea", roll); Set(flip, "_spatula", spatula.transform); Set(controller, "_flipAction", flip);
            Set(controller, "_cakePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(CakePath).GetComponent<CakeItem>());
            Set(controller, "_preheatSeconds", 1f); Set(controller, "_openLidDegrees", 70f);
            Save(station, StationPath); AssetDatabase.SaveAssets();
            Debug.Log("[TramChanh] Saved cake station; all recipe/timing seeds explicitly DEV/TBD.");
        }
        private static void Tool(string name, Material material, Vector3 size)
        {
            var root = new GameObject(name); Transform a = Child(root.transform, "Anchors"); Child(a, "HandGrip"); Child(a, "InteractionPoint"); Child(a, "PlacementPoint");
            Box(root.transform, "SM_" + name.Substring(3), size, Vector3.zero, material); Save(root, Folder + "/" + name + ".prefab");
        }
        private static GameObject ToolInstance(Transform parent, string name, Vector3 position)
        { var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/" + name + ".prefab"), parent); obj.transform.localPosition = position; return obj; }
        private static void Point(Transform root, CakeStation station, CakeStationAction action, int id, string sauce = null)
        {
            var component = root.gameObject.AddComponent<CakeStationPoint>(); Set(component, "_station", station); Set(component, "_action", (int)action); Set(component, "_id", id); Set(component, "_sauce", sauce ?? "");
            Set(component, "_interactionPoint", root); root.gameObject.layer = TramChanhLayers.InteractableIndex;
            var collider = root.gameObject.AddComponent<BoxCollider>(); collider.size = new Vector3(.14f, .1f, .14f);
        }
        private static GameObject Box(Transform parent, string name, Vector3 size, Vector3 position, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = size;
            obj.gameObject.layer = TramChanhLayers.InteractableIndex; obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
        }
        private static Transform Child(Transform parent, string name) { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
        private static Transform Find(Transform root, string name) { foreach (var t in root.GetComponentsInChildren<Transform>(true)) { if (t.name == name) { return t; } } throw new InvalidOperationException("Missing " + name); }
        private static void Save(GameObject root, string path) { PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root); }
        private static T Asset<T>(string path) where T : ScriptableObject { var asset = AssetDatabase.LoadAssetAtPath<T>(path); if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); } return asset; }
        private static Material Material(string name, Color color)
        {
            string path = DataFolder + "/" + name + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", color); EditorUtility.SetDirty(material); return material;
        }
        private static void Set(Object target, string field, object value)
        {
            var serialized = new SerializedObject(target); var property = serialized.FindProperty(field);
            if (property == null) { throw new InvalidOperationException(target.GetType().Name + ": " + field); }
            if (value is Object obj) { property.objectReferenceValue = obj; }
            else if (value is string text) { property.stringValue = text; }
            else if (value is int number) { property.intValue = number; }
            else if (value is float scalar) { property.floatValue = scalar; }
            else { throw new ArgumentException("Unsupported seed type."); }
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
    }
}
