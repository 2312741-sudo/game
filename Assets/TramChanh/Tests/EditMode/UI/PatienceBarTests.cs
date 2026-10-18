using NUnit.Framework;
using TramChanh.UI.Hud;

namespace TramChanh.Tests.EditMode.UI
{
    public sealed class PatienceBarTests
    {
        [Test]
        public void Bands_FollowTheThresholds()
        {
            Assert.That(PatienceBar.For(1.0).Level, Is.EqualTo(PatienceLevel.Calm));
            Assert.That(PatienceBar.For(0.5).Level, Is.EqualTo(PatienceLevel.Calm));
            Assert.That(PatienceBar.For(0.49).Level, Is.EqualTo(PatienceLevel.Warning));
            Assert.That(PatienceBar.For(0.25).Level, Is.EqualTo(PatienceLevel.Warning));
            Assert.That(PatienceBar.For(0.24).Level, Is.EqualTo(PatienceLevel.Danger));
            Assert.That(PatienceBar.For(0.0).Level, Is.EqualTo(PatienceLevel.Danger));
        }

        [Test]
        public void WidthIsThePercentageAndNeverExceedsAHundred()
        {
            Assert.That(PatienceBar.For(0.4).WidthPercent, Is.EqualTo(40f).Within(0.001f));
            Assert.That(PatienceBar.For(3.0).WidthPercent, Is.EqualTo(100f).Within(0.001f));
        }

        [Test]
        public void UntrackedOrBadValues_HideTheBar()
        {
            Assert.That(PatienceBar.For(-1d).IsVisible, Is.False);
            Assert.That(PatienceBar.For(double.NaN).IsVisible, Is.False);
            Assert.That(PatienceBar.For(double.PositiveInfinity).IsVisible, Is.False);
            Assert.That(PatienceBar.For(0d).IsVisible, Is.True);
        }
    }
}
