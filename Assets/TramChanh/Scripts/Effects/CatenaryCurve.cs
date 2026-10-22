using System;
using UnityEngine;

namespace TramChanh.Effects
{
    /// <summary>
    /// Sag of a hanging wire between two points (Line Renderer positions). The shape is a true catenary,
    /// y = a * (cosh(h / a) - cosh(x / a)), where h is half the horizontal span and <c>a</c> is solved so that the lowest
    /// point hangs exactly <c>sag</c> metres below the straight line between the two ends. When the ends are at different
    /// heights the sag is measured straight down from the chord, which is a good approximation for a loose wire.
    /// Pure maths (Vector3 is only a value type), no scene access.
    /// </summary>
    public static class CatenaryCurve
    {
        private const double LargestRatio = 40.0; // cosh(40) is still far inside double range

        /// <summary>Fills <paramref name="points"/> (at least 2 entries) from <paramref name="start"/> to <paramref name="end"/>.</summary>
        public static void Fill(Vector3 start, Vector3 end, float sag, Vector3[] points)
        {
            if (points == null || points.Length < 2)
            {
                throw new ArgumentException("Need at least two points.", nameof(points));
            }
            int last = points.Length - 1;
            double horizontal = Math.Sqrt((double)(end.x - start.x) * (end.x - start.x) + (double)(end.z - start.z) * (end.z - start.z));
            double a = SolveA(horizontal * 0.5, sag);
            for (int i = 0; i <= last; i++)
            {
                float t = (float)i / last;
                Vector3 p = Vector3.Lerp(start, end, t);
                p.y -= SagBelowChord(t, horizontal * 0.5, a, sag);
                points[i] = p;
            }
            points[0] = start;
            points[last] = end;
        }

        /// <summary>Allocating convenience form of <see cref="Fill"/>.</summary>
        public static Vector3[] Sample(Vector3 start, Vector3 end, float sag, int segments)
        {
            if (segments < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(segments));
            }
            var points = new Vector3[segments + 1];
            Fill(start, end, sag, points);
            return points;
        }

        /// <summary>Vertical distance below the chord at parameter t (0 at the ends, equal to the sag at t = 0.5).</summary>
        public static float SagAt(float t, float horizontalSpan, float sag)
        {
            double half = horizontalSpan * 0.5;
            return SagBelowChord(t, half, SolveA(half, sag), sag);
        }

        private static float SagBelowChord(float t, double half, double a, float sag)
        {
            if (a <= 0.0 || half <= 0.0 || sag <= 0f)
            {
                return 0f;
            }
            double x = (2.0 * t - 1.0) * half;
            return (float)(a * (Math.Cosh(half / a) - Math.Cosh(x / a)));
        }

        /// <summary>Solves a * (cosh(half / a) - 1) = sag for a by bisection. Returns 0 when there is no sag to draw.</summary>
        private static double SolveA(double half, float sag)
        {
            if (half <= 1e-6 || sag <= 1e-6f)
            {
                return 0.0;
            }
            // g(a) = a * (cosh(half / a) - 1) decreases as a grows: tight wire has a small a.
            double low = half / LargestRatio;
            double high = Math.Max(half * 1e6, 1.0);
            if (low * (Math.Cosh(half / low) - 1.0) < sag)
            {
                return low; // an extreme sag: keep the deepest allowed curve instead of overflowing
            }
            for (int i = 0; i < 80; i++)
            {
                double mid = 0.5 * (low + high);
                double g = mid * (Math.Cosh(half / mid) - 1.0);
                if (g > sag)
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }
            return 0.5 * (low + high);
        }
    }
}
