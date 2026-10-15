using System;

namespace TramChanh.Core
{
    /// <summary>
    /// Picks wander targets for ambient animals: a point inside a circle around a home position. Pure maths so it can be tested.
    /// </summary>
    public static class WanderArea
    {
        /// <summary>
        /// Maps two numbers in [0, 1) to a point in the circle of <paramref name="radius"/> around (<paramref name="centerX"/>, <paramref name="centerZ"/>).
        /// The square root keeps the points evenly spread over the area instead of bunching at the centre.
        /// </summary>
        public static void Pick(float centerX, float centerZ, float radius, double u1, double u2, out float x, out float z)
        {
            if (radius < 0f || float.IsNaN(radius)) { radius = 0f; }
            double a = Clamp01(u1) * 2d * Math.PI;
            double r = radius * Math.Sqrt(Clamp01(u2));
            x = centerX + (float)(r * Math.Cos(a));
            z = centerZ + (float)(r * Math.Sin(a));
        }

        /// <summary>Seconds needed to walk <paramref name="distance"/> metres at <paramref name="speed"/> m/s; never negative, never infinite.</summary>
        public static float WalkSeconds(float distance, float speed)
        {
            if (speed <= 0f || distance <= 0f) { return 0f; }
            return distance / speed;
        }

        private static double Clamp01(double v) => double.IsNaN(v) ? 0d : (v < 0d ? 0d : (v > 0.999999d ? 0.999999d : v));
    }
}
