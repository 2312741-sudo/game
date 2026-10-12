using System;

namespace TramChanh.Core
{
    /// <summary>
    /// Game day for the main scene: advances the hour of day from real seconds and runs faster while the stall is closed.
    /// Open hours run at normal speed (<c>secondsPerGameDay</c> real seconds per day); outside them the clock is
    /// multiplied by <c>closedTimeScale</c> so the night after closing is skipped quickly. Pure C#, no Unity types.
    /// A step that crosses an opening or closing time is split there, so one long frame never skips past it.
    /// </summary>
    public sealed class DayClock
    {
        private const double HoursPerDay = DayCycle.HoursPerDay;
        private const int MaxSegmentsPerAdvance = 8;

        private readonly double _hoursPerSecond;
        private readonly OpeningHours _hours;
        private readonly double _closedTimeScale;

        public DayClock(double secondsPerGameDay, double startHour, OpeningHours hours, double closedTimeScale)
        {
            if (double.IsNaN(secondsPerGameDay) || double.IsInfinity(secondsPerGameDay) || secondsPerGameDay <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(secondsPerGameDay), "A game day must last a positive number of seconds.");
            }
            if (double.IsNaN(startHour) || double.IsInfinity(startHour) || startHour < 0d || startHour >= HoursPerDay)
            {
                throw new ArgumentOutOfRangeException(nameof(startHour), "Start hour must be in [0, 24).");
            }
            if (double.IsNaN(closedTimeScale) || double.IsInfinity(closedTimeScale) || closedTimeScale < 1d)
            {
                throw new ArgumentOutOfRangeException(nameof(closedTimeScale), "Closed time scale must be at least 1.");
            }
            _hours = hours ?? throw new ArgumentNullException(nameof(hours));
            _hoursPerSecond = HoursPerDay / secondsPerGameDay;
            _closedTimeScale = closedTimeScale;
            HourOfDay = startHour;
        }

        /// <summary>Current hour of day in [0, 24).</summary>
        public double HourOfDay { get; private set; }

        /// <summary>True during the opening window.</summary>
        public bool IsOpen => _hours.IsOpen(HourOfDay);

        /// <summary>Advances by real seconds. Zero (paused), negative or NaN deltas are a no-op.</summary>
        public void Advance(double realSeconds)
        {
            if (double.IsNaN(realSeconds) || double.IsInfinity(realSeconds) || realSeconds <= 0d) { return; }

            double remaining = realSeconds;
            for (int segment = 0; segment < MaxSegmentsPerAdvance && remaining > 0d; segment++)
            {
                bool open = IsOpen;
                double hoursPerRealSecond = _hoursPerSecond * (open ? 1d : _closedTimeScale);
                double hoursToBoundary = HoursToNextBoundary(open);
                double secondsToBoundary = hoursToBoundary / hoursPerRealSecond;
                if (remaining < secondsToBoundary)
                {
                    HourOfDay = Wrap(HourOfDay + remaining * hoursPerRealSecond);
                    return;
                }

                HourOfDay = Wrap(HourOfDay + hoursToBoundary);
                remaining -= secondsToBoundary;
            }
        }

        private double HoursToNextBoundary(bool open)
        {
            if (open) { return _hours.CloseHour - HourOfDay; }
            return HourOfDay < _hours.OpenHour
                ? _hours.OpenHour - HourOfDay
                : HoursPerDay - HourOfDay + _hours.OpenHour;
        }

        private static double Wrap(double hour)
        {
            if (hour >= HoursPerDay) { hour -= HoursPerDay; }
            return hour < 0d ? 0d : hour;
        }
    }
}
