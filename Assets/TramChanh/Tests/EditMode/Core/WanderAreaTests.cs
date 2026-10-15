using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class WanderAreaTests
    {
        [TestCase(0.0, 0.0)]
        [TestCase(0.25, 0.5)]
        [TestCase(0.9, 1.0)]
        [TestCase(0.5, 0.999999)]
        public void Pick_StaysInsideTheCircle(double u1, double u2)
        {
            WanderArea.Pick(-12f, -2.8f, 1.5f, u1, u2, out float x, out float z);
            float dx = x + 12f, dz = z + 2.8f;
            Assert.That(dx * dx + dz * dz, Is.LessThanOrEqualTo(1.5f * 1.5f + 1e-4f));
        }

        [Test]
        public void Pick_WithZeroRadius_ReturnsTheCentre()
        {
            WanderArea.Pick(3f, 4f, 0f, 0.7, 0.3, out float x, out float z);
            Assert.That(x, Is.EqualTo(3f).Within(1e-5f));
            Assert.That(z, Is.EqualTo(4f).Within(1e-5f));
        }

        [Test]
        public void Pick_IgnoresNaNInputsInsteadOfThrowing()
        {
            WanderArea.Pick(0f, 0f, 2f, double.NaN, double.NaN, out float x, out float z);
            Assert.That(float.IsNaN(x) || float.IsNaN(z), Is.False);
        }

        [TestCase(2f, 0.5f, 4f)]
        [TestCase(0f, 0.5f, 0f)]
        [TestCase(2f, 0f, 0f)]
        [TestCase(-1f, 0.4f, 0f)]
        public void WalkSeconds_IsDistanceOverSpeedAndNeverNegative(float distance, float speed, float expected)
        {
            Assert.That(WanderArea.WalkSeconds(distance, speed), Is.EqualTo(expected).Within(1e-5f));
        }
    }
}
