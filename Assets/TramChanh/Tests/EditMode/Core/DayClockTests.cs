using System;
using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class DayClockTests
    {
        private const double Day = 1440d;
        private const double Eps = 1e-6;

        private static DayClock Clock(double start = 18d, double scale = 18d) =>
            new DayClock(Day, start, OpeningHours.Stall, scale);

        [Test]
        public void StartsAtTheStartHour_AndIsOpenAtEighteen()
        {
            DayClock clock = Clock();

            Assert.That(clock.HourOfDay, Is.EqualTo(18d).Within(Eps));
            Assert.That(clock.IsOpen, Is.True);
        }

        [Test]
        public void OpenHours_RunAtNormalSpeed_OneGameHourPerSixtySeconds()
        {
            DayClock clock = Clock();

            clock.Advance(180d);

            Assert.That(clock.HourOfDay, Is.EqualTo(21d).Within(Eps));
            Assert.That(clock.IsOpen, Is.True);
        }

        [Test]
        public void ReachingMidnight_ClosesTheStall_AndWrapsToZero()
        {
            DayClock clock = Clock();

            clock.Advance(360d);

            Assert.That(clock.HourOfDay, Is.EqualTo(0d).Within(Eps));
            Assert.That(clock.IsOpen, Is.False);
        }

        [Test]
        public void ClosedHours_RunEighteenTimesFaster_AndReopenAtEighteen()
        {
            DayClock clock = Clock(0d);

            clock.Advance(30d);
            Assert.That(clock.HourOfDay, Is.EqualTo(9d).Within(Eps));
            Assert.That(clock.IsOpen, Is.False);

            clock.Advance(30d);
            Assert.That(clock.HourOfDay, Is.EqualTo(18d).Within(Eps));
            Assert.That(clock.IsOpen, Is.True);
        }

        [Test]
        public void OneLongStep_IsSplitAtClosingAndOpening()
        {
            DayClock clock = Clock();

            // 360 s open (18:00 to 24:00) plus 60 s closed at 18x (00:00 to 18:00), in a single call.
            clock.Advance(420d);

            Assert.That(clock.HourOfDay, Is.EqualTo(18d).Within(Eps));
            Assert.That(clock.IsOpen, Is.True);
        }

        [Test]
        public void OneLongStep_NeverOvershootsTheOpeningTime()
        {
            DayClock clock = Clock(0d);

            // 90 s at 18x would reach 27:00; the clock must stop being closed exactly at 18:00 and continue open speed.
            clock.Advance(90d);

            Assert.That(clock.HourOfDay, Is.EqualTo(18d + 0.5d).Within(Eps));
            Assert.That(clock.IsOpen, Is.True);
        }

        [TestCase(0d)]
        [TestCase(-5d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void NonPositiveOrInvalidDeltas_AreNoOps(double delta)
        {
            DayClock clock = Clock(20d);

            clock.Advance(delta);

            Assert.That(clock.HourOfDay, Is.EqualTo(20d).Within(Eps));
        }

        [Test]
        public void ConstructorValidatesArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DayClock(0d, 18d, OpeningHours.Stall, 18d));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DayClock(Day, 24d, OpeningHours.Stall, 18d));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DayClock(Day, 18d, OpeningHours.Stall, 0.5d));
            Assert.Throws<ArgumentNullException>(() => new DayClock(Day, 18d, null, 18d));
        }

        [Test]
        public void HourOfDay_StaysInsideZeroToTwentyFour_OverManySmallSteps()
        {
            DayClock clock = Clock();

            for (int i = 0; i < 20000; i++)
            {
                clock.Advance(0.1d);
                Assert.That(clock.HourOfDay, Is.GreaterThanOrEqualTo(0d));
                Assert.That(clock.HourOfDay, Is.LessThan(24d));
            }
        }
    }
}
