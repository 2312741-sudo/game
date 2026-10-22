using NUnit.Framework;
using TramChanh.Effects;
using UnityEngine;

namespace TramChanh.Tests.EditMode.Effects
{
    public sealed class CatenaryCurveTests
    {
        [Test]
        public void FX_WIRE_001_EndsAreExact()
        {
            var a = new Vector3(0f, 3f, 1f);
            var b = new Vector3(4f, 0.5f, 1f);
            Vector3[] points = CatenaryCurve.Sample(a, b, 0.6f, 20);
            Assert.AreEqual(21, points.Length);
            Assert.AreEqual(a, points[0]);
            Assert.AreEqual(b, points[20]);
        }

        [Test]
        public void FX_WIRE_002_MiddleHangsExactlySagBelowTheChord()
        {
            var a = new Vector3(-2f, 2f, 0f);
            var b = new Vector3(2f, 2f, 0f);
            Vector3[] points = CatenaryCurve.Sample(a, b, 0.5f, 10);
            Assert.AreEqual(1.5f, points[5].y, 1e-3f);
            Assert.AreEqual(0f, points[5].x, 1e-4f);
        }

        [Test]
        public void FX_WIRE_003_SymmetricAndNeverAboveTheChord()
        {
            var a = new Vector3(0f, 2f, 0f);
            var b = new Vector3(6f, 2f, 0f);
            Vector3[] p = CatenaryCurve.Sample(a, b, 0.8f, 12);
            for (int i = 0; i <= 12; i++)
            {
                Assert.AreEqual(p[i].y, p[12 - i].y, 1e-4f, "symmetry at " + i);
                Assert.LessOrEqual(p[i].y, 2f + 1e-5f);
            }
            for (int i = 0; i < 6; i++)
            {
                Assert.Less(p[i + 1].y, p[i].y, "falls toward the middle");
            }
        }

        [Test]
        public void FX_WIRE_004_ZeroSagIsAStraightLine()
        {
            Vector3[] p = CatenaryCurve.Sample(Vector3.zero, new Vector3(4f, 2f, 0f), 0f, 4);
            Assert.AreEqual(1f, p[2].x, 1e-5f);
            Assert.AreEqual(1f, p[2].y, 1e-5f);
        }

        [Test]
        public void FX_WIRE_005_VerticalSpanDoesNotThrowOrProduceNaN()
        {
            Vector3[] p = CatenaryCurve.Sample(new Vector3(0f, 3f, 0f), new Vector3(0f, 0f, 0f), 0.5f, 6);
            foreach (Vector3 v in p)
            {
                Assert.IsFalse(float.IsNaN(v.y));
            }
        }

        [Test]
        public void FX_WIRE_006_NeedsTwoPoints()
        {
            Assert.Throws<System.ArgumentException>(() => CatenaryCurve.Fill(Vector3.zero, Vector3.one, 0.1f, new Vector3[1]));
        }
    }
}
