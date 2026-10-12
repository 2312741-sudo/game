using System;
using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class OpeningHoursTests
    {
        [Test]
        public void OH_001_Stall_OpensAtEighteenInclusive()
        {
            Assert.That(OpeningHours.Stall.OpenHour, Is.EqualTo(18.0));
            Assert.That(OpeningHours.Stall.IsOpen(18.0), Is.True);
        }

        [Test]
        public void OH_001_Stall_ClosesAtTwentyFourExclusive()
        {
            Assert.That(OpeningHours.Stall.CloseHour, Is.EqualTo(24.0));
            Assert.That(OpeningHours.Stall.IsOpen(23.999999), Is.True);
            Assert.That(OpeningHours.Stall.IsOpen(24.0), Is.False);
        }

        [TestCase(0.0)]
        [TestCase(6.0)]
        [TestCase(12.0)]
        [TestCase(17.999)]
        public void OH_002_Stall_ClosedBeforeOpening(double hour)
        {
            Assert.That(OpeningHours.Stall.IsOpen(hour), Is.False);
        }

        [TestCase(18.0)]
        [TestCase(19.0)]
        [TestCase(21.5)]
        [TestCase(23.5)]
        public void OH_002_Stall_OpenInsideWindow(double hour)
        {
            Assert.That(OpeningHours.Stall.IsOpen(hour), Is.True);
        }

        [Test]
        public void OH_003_CustomWindow_UsesSameHalfOpenRule()
        {
            var lunch = new OpeningHours(11.0, 14.0);
            Assert.That(lunch.IsOpen(10.999), Is.False);
            Assert.That(lunch.IsOpen(11.0), Is.True);
            Assert.That(lunch.IsOpen(13.999), Is.True);
            Assert.That(lunch.IsOpen(14.0), Is.False);
        }

        [Test]
        public void OH_003_CustomWindow_CanCloseAtMidnightInclusiveOfTwentyFour()
        {
            var late = new OpeningHours(0.0, 24.0);
            Assert.That(late.IsOpen(0.0), Is.True);
            Assert.That(late.IsOpen(24.0), Is.False);
        }

        [TestCase(-0.1)]
        [TestCase(24.1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void OH_004_IsOpen_RejectsHoursOutsideZeroToTwentyFour(double hour)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => OpeningHours.Stall.IsOpen(hour));
        }

        [TestCase(-1.0, 18.0)]
        [TestCase(24.0, 24.0)]
        [TestCase(18.0, 18.0)]
        [TestCase(18.0, 17.0)]
        [TestCase(double.NaN, 24.0)]
        public void OH_004_Constructor_RejectsInvalidWindow(double open, double close)
        {
            Assert.That(() => new OpeningHours(open, close), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void OH_005_Stall_OpenStatusFrozenWhileClockPaused()
        {
            const double hourSeconds = DayCycle.DefaultSecondsPerGameDay / 24.0;
            var clock = new ManualClock();

            // Paused at 18:00 (open) for a long time: still open.
            clock.Advance(18.0 * hourSeconds);
            clock.Pause();
            clock.Advance(10 * DayCycle.DefaultSecondsPerGameDay);
            Assert.That(OpeningHours.Stall.IsOpen(DayCycle.HourOfDay(clock.Now)), Is.True);

            // Resume, then reach 24:00 exactly: closed, and it stays closed while paused.
            clock.Resume();
            clock.Advance(6.0 * hourSeconds);
            Assert.That(OpeningHours.Stall.IsOpen(DayCycle.HourOfDay(clock.Now)), Is.False);
            clock.Pause();
            clock.Advance(hourSeconds);
            Assert.That(OpeningHours.Stall.IsOpen(DayCycle.HourOfDay(clock.Now)), Is.False);
        }

        [Test]
        public void OH_006_Stall_OpensOnTheNextDayAfterWrap()
        {
            const double day = DayCycle.DefaultSecondsPerGameDay;
            double eighteenOnDayTwo = day + 18.0 * (day / 24.0);
            Assert.That(OpeningHours.Stall.IsOpen(DayCycle.HourOfDay(eighteenOnDayTwo)), Is.True);

            double noonOnDayTwo = day + 12.0 * (day / 24.0);
            Assert.That(OpeningHours.Stall.IsOpen(DayCycle.HourOfDay(noonOnDayTwo)), Is.False);
        }
    }
}
