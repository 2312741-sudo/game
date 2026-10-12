using System;
using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Stall.Layout;

namespace TramChanh.Tests.EditMode.Stall
{
    /// <summary>
    /// Pure C# tests for <see cref="StallCounterLayoutRule"/>. No Unity types: they also run in the .NET harness.
    /// Counter footprint used throughout: GT-001 1.8 m wide (X) x 0.8 m deep (Z), centred on the origin,
    /// so X in [-0.9, 0.9] and Z in [-0.4, 0.4].
    /// NUnit 3.5 only: no Is.AnyOf, no Does.Not.Contain; use Has.No.Member instead.
    /// </summary>
    [TestFixture]
    public class StallCounterLayoutRuleTests
    {
        private const float Tolerance = StallCounterLayoutRule.DefaultToleranceMetres;
        private const float Epsilon = 1e-5f;

        private static FootprintRect Counter() => FootprintRect.FromCenter("Counter", 0f, 0f, 1.8f, 0.8f);

        // ------------------------------------------------------------ overlaps

        [Test]
        public void TouchingEdges_AreNotOverlaps_AlongX()
        {
            var props = new List<FootprintRect>
            {
                new FootprintRect("Left", 0f, 0f, 0.5f, 0.5f),
                new FootprintRect("Right", 0.5f, 0f, 1f, 0.5f),
            };

            IReadOnlyList<PropOverlap> overlaps = StallCounterLayoutRule.FindPropOverlaps(props);

            Assert.That(overlaps, Has.Count.EqualTo(0));
        }

        [Test]
        public void TouchingEdges_AreNotOverlaps_AlongZ()
        {
            var props = new List<FootprintRect>
            {
                new FootprintRect("Back", 0f, -0.4f, 0.3f, 0f),
                new FootprintRect("Front", 0f, 0f, 0.3f, 0.4f),
            };

            IReadOnlyList<PropOverlap> overlaps = StallCounterLayoutRule.FindPropOverlaps(props);

            Assert.That(overlaps, Has.Count.EqualTo(0));
        }

        [Test]
        public void RealOverlap_IsDetected_WithOverlapAreaInSquareMetres()
        {
            var props = new List<FootprintRect>
            {
                new FootprintRect("TeaRack", 0f, 0f, 0.5f, 0.5f),
                new FootprintRect("IceBin", 0.25f, 0f, 0.75f, 0.5f),
            };

            IReadOnlyList<PropOverlap> overlaps = StallCounterLayoutRule.FindPropOverlaps(props);

            Assert.That(overlaps, Has.Count.EqualTo(1));
            Assert.That(overlaps[0].First, Is.EqualTo("TeaRack"));
            Assert.That(overlaps[0].Second, Is.EqualTo("IceBin"));
            Assert.That(overlaps[0].OverlapX, Is.EqualTo(0.25f).Within(Epsilon));
            Assert.That(overlaps[0].OverlapZ, Is.EqualTo(0.5f).Within(Epsilon));
            Assert.That(overlaps[0].AreaSquareMetres, Is.EqualTo(0.125f).Within(Epsilon));
        }

        [Test]
        public void OverlapSmallerThanTolerance_IsIgnored()
        {
            // 0.01 m along X is inside the 0.02 m tolerance, even though Z overlaps fully.
            var props = new List<FootprintRect>
            {
                new FootprintRect("A", 0f, 0f, 0.5f, 0.5f),
                new FootprintRect("B", 0.49f, 0f, 1f, 0.5f),
            };

            IReadOnlyList<PropOverlap> overlaps = StallCounterLayoutRule.FindPropOverlaps(props, Tolerance);

            Assert.That(overlaps, Has.Count.EqualTo(0));
        }

        [Test]
        public void OverlapLargerThanTolerance_OnBothAxes_IsDetected()
        {
            var props = new List<FootprintRect>
            {
                new FootprintRect("A", 0f, 0f, 0.5f, 0.5f),
                new FootprintRect("B", 0.47f, 0.47f, 1f, 1f),
            };

            IReadOnlyList<PropOverlap> overlaps = StallCounterLayoutRule.FindPropOverlaps(props, Tolerance);

            Assert.That(overlaps, Has.Count.EqualTo(1));
            Assert.That(overlaps[0].OverlapX, Is.EqualTo(0.03f).Within(Epsilon));
        }

        [Test]
        public void DisjointProps_ReportNothing()
        {
            var props = new List<FootprintRect>
            {
                new FootprintRect("TeaRack", -0.85f, -0.3f, -0.55f, -0.1f),
                new FootprintRect("ToppingStation", -0.5f, -0.3f, -0.2f, -0.1f),
                new FootprintRect("ReadyCounter", 0f, 0.1f, 0.3f, 0.35f),
            };

            IReadOnlyList<PropOverlap> overlaps = StallCounterLayoutRule.FindPropOverlaps(props, Tolerance);

            Assert.That(overlaps, Has.Count.EqualTo(0));
        }

        [Test]
        public void ThreeMutuallyOverlappingProps_ReportEachPairOnce()
        {
            var props = new List<FootprintRect>
            {
                new FootprintRect("A", 0f, 0f, 0.4f, 0.4f),
                new FootprintRect("B", 0.2f, 0f, 0.6f, 0.4f),
                new FootprintRect("C", 0.1f, 0.1f, 0.5f, 0.5f),
            };

            IReadOnlyList<PropOverlap> overlaps = StallCounterLayoutRule.FindPropOverlaps(props, Tolerance);

            Assert.That(overlaps, Has.Count.EqualTo(3));
            Assert.That(overlaps[0].First, Is.EqualTo("A"));
            Assert.That(overlaps[0].Second, Is.EqualTo("B"));
            Assert.That(overlaps[1].First, Is.EqualTo("A"));
            Assert.That(overlaps[1].Second, Is.EqualTo("C"));
            Assert.That(overlaps[2].First, Is.EqualTo("B"));
            Assert.That(overlaps[2].Second, Is.EqualTo("C"));
        }

        [Test]
        public void NullOrEmptyList_ReportsNothing()
        {
            Assert.That(StallCounterLayoutRule.FindPropOverlaps(null), Has.Count.EqualTo(0));
            Assert.That(StallCounterLayoutRule.FindPropOverlaps(new List<FootprintRect>()), Has.Count.EqualTo(0));
        }

        [Test]
        public void NegativeTolerance_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                StallCounterLayoutRule.FindPropOverlaps(new List<FootprintRect>(), -0.01f));
        }

        // ------------------------------------------------------------ outside the counter

        [Test]
        public void PropOutsideCounterFootprint_IsReported()
        {
            var props = new List<FootprintRect>
            {
                new FootprintRect("Overhang", 0.85f, -0.1f, 1.1f, 0.1f),
            };

            IReadOnlyList<string> outside = StallCounterLayoutRule.FindPropsOutsideCounter(Counter(), props, Tolerance);

            Assert.That(outside, Has.Count.EqualTo(1));
            Assert.That(outside, Has.Member("Overhang"));
        }

        [Test]
        public void PropOnCounterEdges_IsNotReported()
        {
            var props = new List<FootprintRect>
            {
                new FootprintRect("Edge", -0.9f, -0.4f, 0.9f, 0.4f),
            };

            IReadOnlyList<string> outside = StallCounterLayoutRule.FindPropsOutsideCounter(Counter(), props, Tolerance);

            Assert.That(outside, Has.No.Member("Edge"));
            Assert.That(outside, Has.Count.EqualTo(0));
        }

        [Test]
        public void SmallExcursionWithinTolerance_IsNotReported()
        {
            var props = new List<FootprintRect>
            {
                new FootprintRect("Nudged", -0.9f, -0.4f, 0.91f, 0.4f),
            };

            IReadOnlyList<string> outside = StallCounterLayoutRule.FindPropsOutsideCounter(Counter(), props, Tolerance);

            Assert.That(outside, Has.No.Member("Nudged"));
        }

        [Test]
        public void PropBeyondFrontOrBackDepth_IsReported()
        {
            var props = new List<FootprintRect>
            {
                new FootprintRect("TooFar", -0.1f, 0.2f, 0.1f, 0.6f),
            };

            IReadOnlyList<string> outside = StallCounterLayoutRule.FindPropsOutsideCounter(Counter(), props, Tolerance);

            Assert.That(outside, Has.Member("TooFar"));
        }

        // ------------------------------------------------------------ red tea rack behind the counter

        [Test]
        public void RedTeaRack_OnSellerSide_IsBehindCounter()
        {
            // Stall-local: rack at X -0.72, Z -0.13 (TeaRackAnchor), 0.3 m x 0.2 m footprint.
            FootprintRect rack = FootprintRect.FromCenter("PF_RedTeaRack", -0.72f, -0.13f, 0.3f, 0.2f);

            Assert.That(StallCounterLayoutRule.IsBehindCounter(Counter(), rack, Tolerance), Is.True);
        }

        [Test]
        public void RedTeaRack_OnCustomerSide_IsNotBehindCounter()
        {
            FootprintRect rack = FootprintRect.FromCenter("PF_RedTeaRack", -0.72f, 0.25f, 0.3f, 0.2f);

            Assert.That(StallCounterLayoutRule.IsBehindCounter(Counter(), rack, Tolerance), Is.False);
        }

        [Test]
        public void RedTeaRack_OffTheEndOfCounter_IsNotBehindCounter()
        {
            FootprintRect rack = FootprintRect.FromCenter("PF_RedTeaRack", -1.0f, -0.13f, 0.3f, 0.2f);

            Assert.That(StallCounterLayoutRule.IsBehindCounter(Counter(), rack, Tolerance), Is.False);
        }

        // ------------------------------------------------------------ footprint helpers

        [Test]
        public void FromCenter_BuildsExpectedEdges()
        {
            FootprintRect counter = Counter();

            Assert.That(counter.MinX, Is.EqualTo(-0.9f).Within(Epsilon));
            Assert.That(counter.MaxX, Is.EqualTo(0.9f).Within(Epsilon));
            Assert.That(counter.MinZ, Is.EqualTo(-0.4f).Within(Epsilon));
            Assert.That(counter.MaxZ, Is.EqualTo(0.4f).Within(Epsilon));
            Assert.That(counter.Area, Is.EqualTo(1.44f).Within(Epsilon));
        }

        [Test]
        public void InvertedFootprint_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new FootprintRect("Bad", 1f, 0f, 0f, 1f));
        }
    }
}
