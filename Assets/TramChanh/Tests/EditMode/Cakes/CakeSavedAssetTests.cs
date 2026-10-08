using NUnit.Framework;
using TramChanh.Cakes;
using TramChanh.Content;
using TramChanh.EditorTools.Placeholders;
using UnityEditor;
using UnityEngine;

namespace TramChanh.Tests.EditMode.Cakes
{
    public sealed class CakeSavedAssetTests
    {
        [Test] public void TC_CAKE_SavedStationPinsAllRuntimeReferencesAndCanonicalAnchors()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CakeWavePrefabBuilder.StationPath); Assert.That(prefab, Is.Not.Null);
            var station = prefab.GetComponent<CakeStation>(); Assert.That(station, Is.Not.Null); var serialized = new SerializedObject(station);
            foreach (string field in new[] { "_cup", "_cakePrefab", "_cakePlacement", "_lidPivot", "_scissors", "_sauceBag", "_wrappingArea", "_flipAction" })
            { Assert.That(serialized.FindProperty(field).objectReferenceValue, Is.Not.Null, field); }
            foreach (string name in new[] { "BatterArea", "BatterSource", "PF_Grill_Elmich", "RollArea", "WrappingArea", "PF_Spatula_WoodHandle", "PF_Scissors_RedGray", "PF_SauceBag" }) { Assert.That(prefab.transform.Find(name), Is.Not.Null, name); }
            Assert.That(prefab.GetComponentsInChildren<CakeStationPoint>().Length, Is.EqualTo(5));
            foreach (var component in prefab.GetComponentsInChildren<MonoBehaviour>(true)) { Assert.That(component, Is.Not.Null, "Missing script"); }
        }
        [Test] public void TC_CAKE_GrillUsesCanonicalRearHingeWithoutMovingSource()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CakeWavePrefabBuilder.StationPath);
            var canonical = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/Workstations/PF_Grill_Elmich.prefab");
            var saved = new SerializedObject(prefab.GetComponent<CakeStation>()).FindProperty("_lidPivot").objectReferenceValue as Transform;
            Transform original = null; foreach (var t in canonical.GetComponentsInChildren<Transform>(true)) { if (t.name == "LidPivot") { original = t; } }
            Assert.That(saved, Is.Not.Null); Assert.That(original, Is.Not.Null); Assert.That(saved.localPosition, Is.EqualTo(original.localPosition)); Assert.That(saved.localPosition.z, Is.GreaterThan(0f));
        }
        [Test] public void TC_CAKE_RecipeSeedsAreExplicitDevTbdAndCapacityDoesNotSetTarget()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<CakeRecipe>(CakeWavePrefabBuilder.RecipePath); var cup = AssetDatabase.LoadAssetAtPath<MeasureCupDefinition>(CakeWavePrefabBuilder.CupPath);
            Assert.That(recipe.IsConfigured, Is.True); Assert.That(cup.IsConfigured, Is.True); Assert.That(cup.CapacityMl, Is.EqualTo(500f)); Assert.That(recipe.TargetBatterMl, Is.Not.EqualTo(cup.CapacityMl));
            Assert.That(new SerializedObject(recipe).FindProperty("_provisionalNotes").stringValue, Does.Contain("DEV TEST SEED ONLY").And.Contain("TBD"));
            Assert.That(new SerializedObject(cup).FindProperty("_provisionalNotes").stringValue, Does.Contain("DEV TEST SEED ONLY"));
        }
        [Test] public void TC_CAKE_ReplacementToolsHaveGripAndCakeRollIsVertical()
        {
            foreach (string tool in new[] { "PF_BatterMeasureCup_500ml", "PF_Spatula_WoodHandle", "PF_Scissors_RedGray", "PF_SauceBag", "PF_CakeWrappingPaper" })
            { var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CakeWavePrefabBuilder.Folder + "/" + tool + ".prefab"); Assert.That(prefab.transform.Find("Anchors/HandGrip"), Is.Not.Null, tool); }
            var cake = AssetDatabase.LoadAssetAtPath<GameObject>(CakeWavePrefabBuilder.CakePath); var rolled = cake.transform.Find("SM_Cake_RolledVertical");
            Assert.That(rolled.localScale.y, Is.GreaterThan(rolled.localScale.x)); Assert.That(cake.transform.Find("Anchors/PlacementPoint"), Is.Not.Null);
        }
    }
}
