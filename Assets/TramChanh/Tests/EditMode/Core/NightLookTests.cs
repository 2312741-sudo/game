using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class NightLookTests
    {
        [Test]
        public void ClearAndHotNights_ChangeNothing()
        {
            Assert.That(NightLook.For(NightEvent.Clear, 20.0).Lamps, Is.EqualTo(1d));
            Assert.That(NightLook.For(NightEvent.HotNight, 20.0).Sun, Is.EqualTo(1d));
            Assert.That(NightLook.For(NightEvent.Inspector, 22.0).Ambient, Is.EqualTo(1d));
        }

        [Test]
        public void Rain_IsDarkerAllNight_ButKeepsTheLamps()
        {
            NightLookFactors rain = NightLook.For(NightEvent.Rain, 19.0);
            Assert.That(rain.Sun, Is.LessThan(1d));
            Assert.That(rain.Ambient, Is.LessThan(1d));
            Assert.That(rain.Lamps, Is.EqualTo(1d));
        }

        [Test]
        public void PowerCut_KillsTheLampsOnlyInsideItsWindow()
        {
            Assert.That(NightLook.For(NightEvent.PowerCut, 20.9).Lamps, Is.EqualTo(1d));
            Assert.That(NightLook.For(NightEvent.PowerCut, 21.1).Lamps, Is.LessThan(0.1));
            Assert.That(NightLook.For(NightEvent.PowerCut, 21.5).Lamps, Is.EqualTo(1d));
        }

        [Test]
        public void Festival_BrightensTheLampsInsideItsWindow()
        {
            Assert.That(NightLook.For(NightEvent.Festival, 20.5).Lamps, Is.GreaterThan(1d));
            Assert.That(NightLook.For(NightEvent.Festival, 21.5).Lamps, Is.EqualTo(1d));
        }

        [Test]
        public void Approach_MovesInSmallStepsAndNeverOvershoots()
        {
            Assert.That(NightLook.Approach(1d, 0.05, 0.3), Is.EqualTo(0.7).Within(1e-9));
            Assert.That(NightLook.Approach(0.1, 0.05, 0.3), Is.EqualTo(0.05).Within(1e-9));
            Assert.That(NightLook.Approach(0.2, 1d, 0.3), Is.EqualTo(0.5).Within(1e-9));
            Assert.That(NightLook.Approach(double.NaN, 0.5, 0.1), Is.EqualTo(0.5));
        }
    }
}
