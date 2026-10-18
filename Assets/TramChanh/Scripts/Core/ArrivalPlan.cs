using System;

namespace TramChanh.Core
{
    /// <summary>
    /// How often customers arrive over the evening. The result is a multiplier on the base arrival interval:
    /// below 1 is busier, above 1 is quieter. Pure C#.
    /// </summary>
    public static class ArrivalPlan
    {
        private static readonly double[] Hours = { 18.0, 19.0, 19.5, 21.5, 23.0, 24.0 };
        private static readonly double[] Factors = { 1.6, 1.6, 0.6, 0.6, 1.0, 1.4 };
        public const double MinScale = 0.3;
        public const double MaxScale = 3.0;
        public const int AssistAfterFailures = 3;
        public const double AssistFactor = 1.5;

        /// <summary>Slow start at 18:00, rush 19:30 to 21:30, calming down, quiet near midnight.</summary>
        public static double HourFactor(double hour)
        {
            if (double.IsNaN(hour)) { return 1d; }
            if (hour <= Hours[0]) { return Factors[0]; }
            for (int i = 1; i < Hours.Length; i++)
            {
                if (hour <= Hours[i])
                {
                    double t = (hour - Hours[i - 1]) / (Hours[i] - Hours[i - 1]);
                    return Factors[i - 1] + (Factors[i] - Factors[i - 1]) * t;
                }
            }
            return Factors[Factors.Length - 1];
        }

        /// <summary>Later days are busier: 6 percent shorter interval per day, no more than 40 percent shorter.</summary>
        public static double DayFactor(int day)
        {
            int extra = Math.Max(0, day - 1);
            return Math.Max(0.6, 1d / (1d + 0.06 * extra));
        }

        /// <summary>After several failed orders in a row the next arrivals come a bit slower, so the player can catch up.</summary>
        public static double Assist(int failureStreak) => failureStreak >= AssistAfterFailures ? AssistFactor : 1d;

        public static double IntervalScale(double hour, int day, int failureStreak)
        {
            double scale = HourFactor(hour) * DayFactor(day) * Assist(failureStreak);
            return Math.Min(MaxScale, Math.Max(MinScale, scale));
        }
    }
}
