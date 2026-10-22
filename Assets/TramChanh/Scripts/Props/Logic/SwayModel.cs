using System;

namespace TramChanh.Props
{
    /// <summary>Pure maths for the hanging props: a smooth, bounded wind gust and a clamped nudge impulse.</summary>
    public static class SwayModel
    {
        /// <summary>Sum of two sines (periods about 4.1 s and 2.3 s). Result is always within [-strength, +strength].</summary>
        public static float Gust(double timeSeconds, float strength, float phase = 0f)
        {
            if (strength <= 0f) { return 0f; }
            double a = Math.Sin(2d * Math.PI * timeSeconds / 4.1d + phase);
            double b = Math.Sin(2d * Math.PI * timeSeconds / 2.3d + phase * 1.7d);
            return (float)(strength * (0.65d * a + 0.35d * b));
        }

        /// <summary>Impulse (N*s) for a body brushing past: proportional to speed, capped so it can never fling the prop.</summary>
        public static float NudgeImpulse(float otherSpeed, float perMetrePerSecond, float maxImpulse)
        {
            if (otherSpeed <= 0f || perMetrePerSecond <= 0f || maxImpulse <= 0f) { return 0f; }
            float v = otherSpeed * perMetrePerSecond;
            return v > maxImpulse ? maxImpulse : v;
        }
    }
}
