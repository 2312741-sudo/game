using System;

namespace TramChanh.Core
{
    /// <summary>Coarse part of the in-game day used for lighting and opening decisions.</summary>
    public enum DayPhase
    {
        Dawn,
        Day,
        Dusk,
        Night,
    }

    /// <summary>Lighting values for one moment of the in-game day.</summary>
    public readonly struct DayLighting
    {
        public DayLighting(DayPhase phase, double hourOfDay, double sunAngleDegrees, double lightIntensity, double ambientFactor)
        {
            Phase = phase;
            HourOfDay = hourOfDay;
            SunAngleDegrees = sunAngleDegrees;
            LightIntensity = lightIntensity;
            AmbientFactor = ambientFactor;
        }

        public DayPhase Phase { get; }
        public double HourOfDay { get; }

        /// <summary>Sun elevation above the horizon in degrees: -90 (nadir) to 90 (zenith). 0 at 06:00 and 18:00.</summary>
        public double SunAngleDegrees { get; }

        /// <summary>Normalised direct-light strength in [0, 1]. 0 whenever the sun is at or below the horizon.</summary>
        public double LightIntensity { get; }

        /// <summary>Normalised ambient fill in [0, 1], constant within each phase.</summary>
        public double AmbientFactor { get; }
    }

    /// <summary>
    /// Pure mapping from elapsed game seconds to a 24-hour in-game day.
    ///
    /// Hour-of-day is a half-open value in [0, 24): it wraps back to 0 exactly at each
    /// full day length, so 24.0 is never returned. The input is the clock's elapsed
    /// seconds (<see cref="IGameClock.Now"/>), so pausing the clock freezes the day.
    ///
    /// Phase boundaries (start inclusive, end exclusive):
    ///   Dawn  05:00 - 07:00
    ///   Day   07:00 - 17:00
    ///   Dusk  17:00 - 19:00
    ///   Night 19:00 - 05:00 (wraps midnight)
    /// </summary>
    public static class DayCycle
    {
        /// <summary>Real seconds that make one full 24-hour in-game day (24 minutes, so 1 game hour = 60 s).</summary>
        public const double DefaultSecondsPerGameDay = 1440.0;

        public const double HoursPerDay = 24.0;
        public const double DawnStartHour = 5.0;
        public const double DayStartHour = 7.0;
        public const double DuskStartHour = 17.0;
        public const double NightStartHour = 19.0;

        public const double DawnAmbient = 0.6;
        public const double DayAmbient = 1.0;
        public const double DuskAmbient = 0.6;
        public const double NightAmbient = 0.25;

        /// <summary>
        /// Maps elapsed game seconds to hour-of-day in [0, 24).
        /// </summary>
        /// <param name="elapsedSeconds">Finite, non-negative game time, e.g. <see cref="IGameClock.Now"/>.</param>
        /// <param name="secondsPerGameDay">Real seconds per full in-game day. Must be finite and positive.</param>
        /// <param name="startHour">Hour-of-day at elapsed 0. Must be in [0, 24).</param>
        public static double HourOfDay(double elapsedSeconds, double secondsPerGameDay = DefaultSecondsPerGameDay, double startHour = 0.0)
        {
            ValidateElapsed(elapsedSeconds);
            if (double.IsNaN(secondsPerGameDay) || double.IsInfinity(secondsPerGameDay) || secondsPerGameDay <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(secondsPerGameDay), "Seconds per game day must be finite and positive.");
            }
            ValidateHour(startHour, nameof(startHour));

            // Days are counted from the fraction of a day, so the wrap is exact at each full day.
            double dayFraction = elapsedSeconds / secondsPerGameDay;
            double hour = (startHour + (dayFraction - Math.Floor(dayFraction)) * HoursPerDay) % HoursPerDay;
            if (hour < 0)
            {
                hour += HoursPerDay;
            }
            // Guard against rounding that could land exactly on 24.
            return hour >= HoursPerDay ? 0.0 : hour;
        }

        /// <summary>Phase for an hour-of-day in [0, 24).</summary>
        public static DayPhase PhaseOf(double hourOfDay)
        {
            ValidateHour(hourOfDay, nameof(hourOfDay));

            if (hourOfDay >= DawnStartHour && hourOfDay < DayStartHour)
            {
                return DayPhase.Dawn;
            }
            if (hourOfDay >= DayStartHour && hourOfDay < DuskStartHour)
            {
                return DayPhase.Day;
            }
            if (hourOfDay >= DuskStartHour && hourOfDay < NightStartHour)
            {
                return DayPhase.Dusk;
            }
            return DayPhase.Night;
        }

        /// <summary>Sun elevation in degrees for an hour-of-day in [0, 24). Sunrise 06:00, noon 90, sunset 18:00, midnight -90.</summary>
        public static double SunAngleDegrees(double hourOfDay)
        {
            ValidateHour(hourOfDay, nameof(hourOfDay));
            return 90.0 * Math.Sin(2.0 * Math.PI * (hourOfDay - 6.0) / HoursPerDay);
        }

        /// <summary>Direct-light strength in [0, 1]: the sine of the sun elevation, clamped at 0 below the horizon.</summary>
        public static double LightIntensity(double hourOfDay)
        {
            double sine = Math.Sin(SunAngleDegrees(hourOfDay) * Math.PI / 180.0);
            return sine > 0 ? sine : 0.0;
        }

        /// <summary>Ambient fill factor for a phase.</summary>
        public static double AmbientFactor(DayPhase phase)
        {
            switch (phase)
            {
                case DayPhase.Dawn: return DawnAmbient;
                case DayPhase.Day: return DayAmbient;
                case DayPhase.Dusk: return DuskAmbient;
                case DayPhase.Night: return NightAmbient;
                default: throw new ArgumentOutOfRangeException(nameof(phase));
            }
        }

        /// <summary>Full lighting snapshot for an hour-of-day in [0, 24).</summary>
        public static DayLighting LightingAt(double hourOfDay)
        {
            DayPhase phase = PhaseOf(hourOfDay);
            return new DayLighting(
                phase,
                hourOfDay,
                SunAngleDegrees(hourOfDay),
                LightIntensity(hourOfDay),
                AmbientFactor(phase));
        }

        private static void ValidateElapsed(double elapsedSeconds)
        {
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), "Elapsed seconds must be finite and non-negative.");
            }
        }

        private static void ValidateHour(double hour, string paramName)
        {
            if (double.IsNaN(hour) || double.IsInfinity(hour) || hour < 0 || hour >= HoursPerDay)
            {
                throw new ArgumentOutOfRangeException(paramName, "Hour of day must be in [0, 24).");
            }
        }
    }
}
