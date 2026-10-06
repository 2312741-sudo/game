using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TramChanh.Core.GroundTruth;
using TramChanh.Core.Provisional;
using TramChanh.EditorTools.Placeholders;
using TramChanh.Stall.Anchors;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TramChanh.Tests.EditMode.Placeholders
{
    /// <summary>ART-STALL-001 phase A placeholder: GT-001, GT-002, anchors, colliders.</summary>
    public sealed class StallPlaceholderTests
    {
        private StallPlaceholderBuilder.PlaceholderMaterials _materials;
        private GameObject _stall;

        [SetUp]
        public void SetUp()
        {
            _materials = StallPlaceholderBuilder.PlaceholderMaterials.CreateTransient();
            _stall = StallPlaceholderBuilder.BuildStallHierarchy(_materials);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_stall);
            Object.DestroyImmediate(_materials.Stall);
            Object.DestroyImmediate(_materials.Wheel);
            Object.DestroyImmediate(_materials.SignHousing);
            Object.DestroyImmediate(_materials.SignTop);
        }

        [Test]
        public void GT_001_Placeholder_FootprintAndHeight()
        {
            Bounds bounds = RendererBounds(_stall);

            Assert.That(bounds.size.x, Is.EqualTo(StallDimensions.Width).Within(StallDimensions.FootprintTolerance), "width");
            Assert.That(bounds.size.z, Is.EqualTo(StallDimensions.Depth).Within(StallDimensions.FootprintTolerance), "depth");
            Assert.That(bounds.max.y, Is.EqualTo(StallDimensions.ApproxTotalHeight).Within(StallDimensions.TotalHeightTolerance), "total height");
            Assert.That(bounds.min.y, Is.EqualTo(0f).Within(0.001f), "pivot = ground centre");
            Assert.That(bounds.center.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(bounds.center.z, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void GT_001_Placeholder_CounterTopIsOneMeter()
        {
            Bounds counter = RendererBounds(_stall.transform.Find("Counter").gameObject);

            Assert.That(counter.max.y, Is.EqualTo(StallDimensions.CounterHeight).Within(StallDimensions.CounterHeightTolerance));
        }

        [Test]
        public void GT_002_Placeholder_HasOnlyTheNewSign()
        {
            Transform signGroup = _stall.transform.Find("Sign");

            Assert.That(signGroup.childCount, Is.EqualTo(1));
            Assert.That(signGroup.GetChild(0).name, Is.EqualTo("PF_Sign_TramChanh_New"));

            string[] forbidden = { "old", "letter", "illuminated" };
            foreach (Transform t in _stall.GetComponentsInChildren<Transform>(true))
            {
                string lower = t.name.ToLowerInvariant();
                Assert.That(forbidden.Any(lower.Contains), Is.False, $"Unexpected old-sign object '{t.name}'.");
            }
        }

        [Test]
        public void Placeholder_HasFinalHierarchyGroups()
        {
            foreach (string group in StallPlaceholderBuilder.StallGroups)
            {
                Assert.That(_stall.transform.Find(group), Is.Not.Null, group);
            }
        }

        [Test]
        public void Placeholder_EveryAnchorExistsOnceAndIsUnconfirmed()
        {
            StallAnchor[] anchors = _stall.GetComponentsInChildren<StallAnchor>(true);
            var ids = (StallAnchorId[])Enum.GetValues(typeof(StallAnchorId));

            Assert.That(anchors.Select(a => a.Id), Is.EquivalentTo(ids));
            Assert.That(anchors.All(a => !a.PositionConfirmed), Is.True,
                "Station positions are provisional until the real stall reference is supplied (DEC-011).");
        }

        [Test]
        public void Placeholder_PreservesEditedAnchorPoses()
        {
            var edited = new Vector3(0.123f, 1f, -0.2f);
            var preserved = new Dictionary<StallAnchorId, StallPlaceholderBuilder.AnchorPose>
            {
                [StallAnchorId.Grill] = new StallPlaceholderBuilder.AnchorPose(edited, Quaternion.identity, true, "photo REF-001"),
            };

            GameObject rebuilt = StallPlaceholderBuilder.BuildStallHierarchy(_materials, preserved);
            try
            {
                var set = rebuilt.GetComponent<StallAnchorSet>();
                Assert.That(set.TryGet(StallAnchorId.Grill, out StallAnchor grill), Is.True);
                Assert.That(grill.transform.localPosition, Is.EqualTo(edited));
                Assert.That(grill.PositionConfirmed, Is.True);
                Assert.That(grill.PositionSource, Is.EqualTo("photo REF-001"));
            }
            finally
            {
                Object.DestroyImmediate(rebuilt);
            }
        }

        [Test]
        public void Placeholder_UsesPrimitiveCollidersOnly()
        {
            Assert.That(_stall.GetComponentsInChildren<MeshCollider>(true), Is.Empty);
            Assert.That(_stall.GetComponentsInChildren<BoxCollider>(true), Is.Not.Empty);
        }

        [Test]
        public void Placeholder_IsMarkedAsPlaceholderAndOnEnvironmentLayer()
        {
            Assert.That(_stall.GetComponent<PlaceholderAsset>(), Is.Not.Null);
            foreach (Transform t in _stall.GetComponentsInChildren<Transform>(true))
            {
                Assert.That(t.gameObject.layer, Is.EqualTo(TramChanhLayers.EnvironmentIndex), t.name);
            }
        }

        [Test]
        public void Sign_PivotIsRearCentre()
        {
            GameObject sign = StallPlaceholderBuilder.BuildSignHierarchy(_materials);
            try
            {
                Bounds bounds = RendererBounds(sign);
                Assert.That(bounds.min.z, Is.EqualTo(0f).Within(0.001f), "rear face at pivot");
                Assert.That(bounds.center.x, Is.EqualTo(0f).Within(0.001f));
                Assert.That(bounds.center.y, Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(sign);
            }
        }

        private static Bounds RendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty, root.name);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }
    }
}
