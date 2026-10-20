using System;

namespace TramChanh.Core
{
    /// <summary>How tonight's event changes the light: multipliers on the sun, the ambient light and the lamps. Pure C#.</summary>
    public readonly struct NightLookFactors
    {
        public NightLookFactors(double sun, double ambient, double lamps)
        {
            Sun = sun;
            Ambient = ambient;
            Lamps = lamps;
        }

        public double Sun { get; }
        public double Ambient { get; }
        public double Lamps { get; }

        public static NightLookFactors Normal => new NightLookFactors(1d, 1d, 1d);
    }

    public static class NightLook
    {
        /// <summary>Rain is darker and greyer, a power cut kills the lamps and most of the light, a festival makes the lamps brighter.</summary>
        public static NightLookFactors For(NightEvent night, double hour)
        {
            switch (night)
            {
                case NightEvent.Rain:
                    return new NightLookFactors(0.6, 0.8, 1d);
                case NightEvent.PowerCut:
                    return NightPlan.ArrivalsBlocked(night, hour) ? new NightLookFactors(0.4, 0.45, 0.05) : NightLookFactors.Normal;
                case NightEvent.Festival:
                    return hour >= NightPlan.FestivalFrom && hour < NightPlan.FestivalTo ? new NightLookFactors(1d, 1d, 1.25) : NightLookFactors.Normal;
                default:
                    return NightLookFactors.Normal;
            }
        }

        /// <summary>Moves a current value towards a target, at most <paramref name="step"/> per call, so the change is not a pop.</summary>
        public static double Approach(double current, double target, double step)
        {
            if (double.IsNaN(current) || double.IsNaN(target)) { return double.IsNaN(target) ? 1d : target; }
            step = Math.Abs(step);
            if (current < target) { return Math.Min(target, current + step); }
            return Math.Max(target, current - step);
        }
    }
}
