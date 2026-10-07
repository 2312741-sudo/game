using System;
using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class GameClockTests
    {
        [Test]
        public void CX_002_ManualClock_AdvancePauseResumeDoesNotCatchUp()
        {
            var clock = new ManualClock();
            clock.Advance(2.5);
            Assert.That(clock.Now, Is.EqualTo(2.5));
            Assert.That(clock.DeltaTime, Is.EqualTo(2.5));
            clock.Pause();
            Assert.That(clock.DeltaTime, Is.Zero);
            clock.Advance(10);
            Assert.That(clock.Now, Is.EqualTo(2.5));
            Assert.That(clock.DeltaTime, Is.Zero);
            Assert.That(clock.IsPaused, Is.True);
            clock.Resume();
            clock.Advance(0.5);
            Assert.That(clock.Now, Is.EqualTo(3));
            Assert.That(clock.IsPaused, Is.False);
        }

        [TestCase(-1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void CX_002_ManualClock_RejectsInvalidDeltaWithoutChangingTime(double seconds)
        {
            var clock = new ManualClock();
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(seconds));
            Assert.That(clock.Now, Is.Zero);
        }
    }
}
