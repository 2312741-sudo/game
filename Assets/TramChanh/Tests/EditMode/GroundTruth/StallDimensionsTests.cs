using NUnit.Framework;
using TramChanh.Core.GroundTruth;

namespace TramChanh.Tests.EditMode.GroundTruth
{
    /// <summary>GT-001 constants match the source documents exactly.</summary>
    public sealed class StallDimensionsTests
    {
        [Test]
        public void GT_001_Constants_MatchSourceDocuments()
        {
            Assert.That(StallDimensions.Width, Is.EqualTo(1.8f));
            Assert.That(StallDimensions.Depth, Is.EqualTo(0.8f));
            Assert.That(StallDimensions.CounterHeight, Is.EqualTo(1.0f));
            Assert.That(StallDimensions.CounterToRoof, Is.EqualTo(1.2f));
            Assert.That(StallDimensions.ApproxTotalHeight, Is.EqualTo(2.2f).Within(1e-5f));
        }
    }
}
