using NUnit.Framework;
using TramChanh.Core.GroundTruth;
using TramChanh.EditorTools.ProjectSetup;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TramChanh.Tests.EditMode.Setup
{
    /// <summary>CX-001: project settings required by the architecture are in place.</summary>
    public sealed class ProjectSetupTests
    {
        [OneTimeSetUp]
        public void ApplySetup()
        {
            // Idempotent; normally already applied on editor load.
            TramChanhProjectSetup.Apply(logChanges: false);
        }

        [Test]
        public void Layers_AreConfigured()
        {
            foreach ((int index, string name) in TramChanhLayers.All)
            {
                Assert.That(LayerMask.LayerToName(index), Is.EqualTo(name));
            }
        }

        [Test]
        public void ColorSpace_IsLinear()
        {
            Assert.That(PlayerSettings.colorSpace, Is.EqualTo(ColorSpace.Linear));
        }

        [Test]
        public void RenderPipeline_IsUrp()
        {
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
        }
    }
}
