using NUnit.Framework;
using TramChanh.EditorTools.ProjectSetup;
using UnityEditor;

namespace TramChanh.Tests.EditMode.Interaction
{
    public sealed class PlayerInputSetupTests
    {
        [Test]
        public void CX_011_ProjectSetup_EnablesInputSystemOnAFreshCheckout()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            SerializedProperty handler = settings.FindProperty("activeInputHandler");
            int original = handler.intValue;
            try
            {
                handler.intValue = 0; // Unity's legacy-only setting cannot drive Gameplay actions.
                settings.ApplyModifiedPropertiesWithoutUndo();
                TramChanhProjectSetup.Apply(logChanges: false);
                settings.Update();
                Assert.That(handler.intValue, Is.Not.Zero, "The first-person slice requires the Input System backend.");
            }
            finally
            {
                settings.Update();
                handler.intValue = original;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
