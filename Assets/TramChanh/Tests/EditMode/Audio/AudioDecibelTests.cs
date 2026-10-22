using NUnit.Framework;
using TramChanh.Audio;

namespace TramChanh.Tests.EditMode.Audio
{
    public sealed class AudioDecibelTests
    {
        [TestCase(1f, 0f)]
        [TestCase(0.5f, -6.0206f)]
        [TestCase(0.1f, -20f)]
        [TestCase(0.01f, -40f)]
        public void LinearToDb_UsesTwentyLogTen(float linear, float expectedDb)
        {
            Assert.That(AudioDecibel.LinearToDb(linear), Is.EqualTo(expectedDb).Within(0.001f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(1e-9f)]
        public void LinearToDb_SilenceIsClampedToMinusEighty(float linear)
        {
            Assert.That(AudioDecibel.LinearToDb(linear), Is.EqualTo(-80f));
        }

        [TestCase(1.5f)]
        [TestCase(float.PositiveInfinity)]
        public void LinearToDb_AboveOneIsZeroDb(float linear)
        {
            Assert.That(AudioDecibel.LinearToDb(linear), Is.EqualTo(0f));
        }

        [Test]
        public void DbToLinear_RoundTripsInsideRange()
        {
            foreach (float v in new[] { 0.05f, 0.25f, 0.5f, 0.8f, 1f })
            {
                Assert.That(AudioDecibel.DbToLinear(AudioDecibel.LinearToDb(v)), Is.EqualTo(v).Within(1e-4f));
            }
        }

        [Test]
        public void DbToLinear_EdgesAreExact()
        {
            Assert.That(AudioDecibel.DbToLinear(-80f), Is.EqualTo(0f));
            Assert.That(AudioDecibel.DbToLinear(-200f), Is.EqualTo(0f));
            Assert.That(AudioDecibel.DbToLinear(0f), Is.EqualTo(1f));
            Assert.That(AudioDecibel.DbToLinear(float.NaN), Is.EqualTo(0f));
        }
    }
}
