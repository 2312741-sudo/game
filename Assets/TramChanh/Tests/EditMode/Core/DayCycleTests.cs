using System;
using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class DayCycleTests
    {
        private const double Day = DayCycle.DefaultSecondsPerGameDay; // 1440 s = 24 game hours
        private const double HourSeconds = Day / 24.0;               // 60 s per game hour
        private const double Tolerance = 1e-9;

        // ---- Hour mapping ----

        [TestCase(0.0, 0.0)]
        [TestCase(HourSeconds, 1.0)]
        [TestCase(6.0 * HourSeconds, 6.0)]
        [TestCase(12.5 * HourSeconds, 12.5)]
        [TestCase(18.0 * HourSeconds, 18.0)]
        public void DC_001_HourOfDay_MapsElapsedSecondsLinearlyWithinOneDay(double elapsed, double expectedHour)
        {
            Assert.That(DayCycle.HourOfDay(elapsed), Is.EqualTo(expectedHour).Within(Tolerance));
        }

        [Test]
        public void DC_001_HourOfDay_UsesConfiguredDayLength()
        {
            // 100 s per day: 25 s is a quarter of the day, so 6 hours.
            Assert.That(DayCycle.HourOfDay(25.0, 100.0), Is.EqualTo(6.0).Within(Tolerance));
        }

        [Test]
        public void DC_001_HourOfDay_HonoursStartHourOffset()
        {
            Assert.That(DayCycle.HourOfDay(0.0, Day, 18.0), Is.EqualTo(18.0).Within(Tolerance));
            Assert.That(DayCycle.HourOfDay(6.0 * HourSeconds, Day, 18.0), Is.EqualTo(0.0).Within(Tolerance));
        }

        // ---- Wrap-around at 24 h ----

        [Test]
        public void DC_002_HourOfDay_WrapsToZeroExactlyAtFullDay()
        {
            Assert.That(DayCycle.HourOfDay(Day), Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(DayCycle.HourOfDay(Day + 6.0 * HourSeconds), Is.EqualTo(6.0).Within(Tolerance));
            Assert.That(DayCycle.HourOfDay(5 * Day), Is.EqualTo(0.0).Within(Tolerance));
        }

        [Test]
        public void DC_002_HourOfDay_JustBeforeMidnightStaysBelowTwentyFour()
        {
            double hour = DayCycle.HourOfDay(Day - 1e-6);
            Assert.That(hour, Is.LessThan(24.0));
            Assert.That(hour, Is.AtLeast(23.99));
        }

        [Test]
        public void DC_002_HourOfDay_NeverReturnsTwentyFourOverManyDays()
        {
            for (int day = 0; day < 50; day++)
            {
                for (int step = 0; step < 240; step++)
                {
                    double elapsed = day * Day + step * (Day / 240.0);
                    double hour = DayCycle.HourOfDay(elapsed);
                    Assert.That(hour, Is.AtLeast(0.0));
                    Assert.That(hour, Is.LessThan(24.0));
                }
            }
        }

        [Test]
        public void DC_002_HourOfDay_LargeElapsedKeepsHourInRange()
        {
            double hour = DayCycle.HourOfDay(1e7 + 300.0);
            Assert.That(hour, Is.AtLeast(0.0));
            Assert.That(hour, Is.LessThan(24.0));
        }

        // ---- Pause safety (through the clock, which owns pause semantics) ----

        [Test]
        public void DC_003_HourOfDay_DoesNotChangeWhileClockPaused()
        {
            var clock = new ManualClock();
            clock.Advance(7.0 * HourSeconds);
            double before = DayCycle.HourOfDay(clock.Now);

            clock.Pause();
            clock.Advance(10 * Day);
            clock.Advance(123.4);

            Assert.That(DayCycle.HourOfDay(clock.Now), Is.EqualTo(before).Within(Tolerance));
            Assert.That(DayCycle.HourOfDay(clock.Now), Is.EqualTo(7.0).Within(Tolerance));
        }

        [Test]
        public void DC_003_HourOfDay_ResumesFromFrozenHour()
        {
            var clock = new ManualClock();
            clock.Advance(7.0 * HourSeconds);
            clock.Pause();
            clock.Advance(5 * HourSeconds);
            clock.Resume();
            clock.Advance(HourSeconds);

            Assert.That(DayCycle.HourOfDay(clock.Now), Is.EqualTo(8.0).Within(Tolerance));
        }

        // ---- Invalid input ----

        [TestCase(-1.0)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void DC_004_HourOfDay_RejectsInvalidElapsed(double elapsed)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DayCycle.HourOfDay(elapsed));
        }

        [TestCase(0.0)]
        [TestCase(-5.0)]
        [TestCase(double.NaN)]
        public void DC_004_HourOfDay_RejectsNonPositiveDayLength(double secondsPerDay)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DayCycle.HourOfDay(10.0, secondsPerDay));
        }

        [TestCase(24.0)]
        [TestCase(-0.5)]
        public void DC_004_HourOfDay_RejectsStartHourOutsideRange(double startHour)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DayCycle.HourOfDay(10.0, Day, startHour));
        }

        // ---- Phase boundaries ----

        [TestCase(5.0, DayPhase.Dawn)]
        [TestCase(6.999, DayPhase.Dawn)]
        [TestCase(7.0, DayPhase.Day)]
        [TestCase(12.0, DayPhase.Day)]
        [TestCase(16.999, DayPhase.Day)]
        [TestCase(17.0, DayPhase.Dusk)]
        [TestCase(18.999, DayPhase.Dusk)]
        [TestCase(19.0, DayPhase.Night)]
        [TestCase(23.999, DayPhase.Night)]
        [TestCase(0.0, DayPhase.Night)]
        [TestCase(4.999, DayPhase.Night)]
        public void DC_005_PhaseOf_RespectsHalfOpenBoundaries(double hour, DayPhase expected)
        {
            Assert.That(DayCycle.PhaseOf(hour), Is.EqualTo(expected));
        }

        [Test]
        public void DC_005_PhaseOf_RejectsTwentyFour()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DayCycle.PhaseOf(24.0));
        }

        // ---- Sun angle and light ----

        [TestCase(0.0, -90.0)]
        [TestCase(6.0, 0.0)]
        [TestCase(12.0, 90.0)]
        [TestCase(18.0, 0.0)]
        public void DC_006_SunAngle_IsZeroAtHorizonCrossingsAndExtremesAtMidnightAndNoon(double hour, double expectedDegrees)
        {
            Assert.That(DayCycle.SunAngleDegrees(hour), Is.EqualTo(expectedDegrees).Within(1e-9));
        }

        [Test]
        public void DC_006_LightIntensity_ZeroBelowHorizonAndFullAtNoon()
        {
            Assert.That(DayCycle.LightIntensity(0.0), Is.EqualTo(0.0));
            Assert.That(DayCycle.LightIntensity(3.0), Is.EqualTo(0.0));
            Assert.That(DayCycle.LightIntensity(12.0), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(DayCycle.LightIntensity(9.0), Is.GreaterThan(0.0).And.LessThan(1.0));
        }

        [Test]
        public void DC_006_AmbientFactor_IsConstantPerPhaseAndInRange()
        {
            Assert.That(DayCycle.AmbientFactor(DayPhase.Dawn), Is.EqualTo(DayCycle.DawnAmbient));
            Assert.That(DayCycle.AmbientFactor(DayPhase.Day), Is.EqualTo(DayCycle.DayAmbient));
            Assert.That(DayCycle.AmbientFactor(DayPhase.Dusk), Is.EqualTo(DayCycle.DuskAmbient));
            Assert.That(DayCycle.AmbientFactor(DayPhase.Night), Is.EqualTo(DayCycle.NightAmbient));
            Assert.That(DayCycle.NightAmbient, Is.GreaterThan(0.0));
            Assert.That(DayCycle.DayAmbient, Is.LessThanOrEqualTo(1.0));
        }

        [Test]
        public void DC_006_LightingAt_ComposesPhaseSunIntensityAndAmbient()
        {
            DayLighting noon = DayCycle.LightingAt(12.0);
            Assert.That(noon.Phase, Is.EqualTo(DayPhase.Day));
            Assert.That(noon.HourOfDay, Is.EqualTo(12.0));
            Assert.That(noon.SunAngleDegrees, Is.EqualTo(90.0).Within(1e-9));
            Assert.That(noon.LightIntensity, Is.EqualTo(1.0).Within(1e-9));
            Assert.That(noon.AmbientFactor, Is.EqualTo(DayCycle.DayAmbient));

            DayLighting midnight = DayCycle.LightingAt(0.0);
            Assert.That(midnight.Phase, Is.EqualTo(DayPhase.Night));
            Assert.That(midnight.LightIntensity, Is.EqualTo(0.0));
            Assert.That(midnight.AmbientFactor, Is.EqualTo(DayCycle.NightAmbient));
        }

        [Test]
        public void DC_006_LightingAt_PhasesAreOrderedAcrossTheDay()
        {
            // Phases in the order they occur from dawn, sampled every half hour.
            var seen = new System.Collections.Generic.List<DayPhase>();
            for (double h = 0; h < 24; h += 0.5)
            {
                DayPhase phase = DayCycle.LightingAt(h).Phase;
                if (seen.Count == 0 || seen[seen.Count - 1] != phase)
                {
                    seen.Add(phase);
                }
            }
            // Sampling starts at 00:00, so the night before dawn is the first run.
            Assert.That(seen, Is.EqualTo(new[] { DayPhase.Night, DayPhase.Dawn, DayPhase.Day, DayPhase.Dusk, DayPhase.Night }));
        }
    }
}
