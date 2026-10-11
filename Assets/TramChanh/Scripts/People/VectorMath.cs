using System;
using UnityEngine;

namespace TramChanh.People
{
    /// <summary>
    /// Small, allocation-free vector helpers written against the raw x/y/z fields so the People
    /// assembly does not depend on engine-side math beyond the <see cref="Vector3"/> struct itself.
    /// Distances are computed in double precision for deterministic accumulation.
    /// </summary>
    internal static class VectorMath
    {
        public static double Distance(Vector3 a, Vector3 b)
        {
            double dx = (double)a.x - b.x;
            double dy = (double)a.y - b.y;
            double dz = (double)a.z - b.z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public static double SqrDistance(Vector3 a, Vector3 b)
        {
            double dx = (double)a.x - b.x;
            double dy = (double)a.y - b.y;
            double dz = (double)a.z - b.z;
            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>Linear interpolation a + (b - a) * t, with t in [0, 1].</summary>
        public static Vector3 Lerp(Vector3 a, Vector3 b, double t)
        {
            return new Vector3(
                (float)(a.x + ((double)b.x - a.x) * t),
                (float)(a.y + ((double)b.y - a.y) * t),
                (float)(a.z + ((double)b.z - a.z) * t));
        }

        public static bool IsFinite(Vector3 v)
        {
            return IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
        }

        public static bool IsFinite(float f)
        {
            return !float.IsNaN(f) && !float.IsInfinity(f);
        }

        public static bool IsFinite(double d)
        {
            return !double.IsNaN(d) && !double.IsInfinity(d);
        }

        /// <summary>
        /// Horizontal (XZ) unit direction from <paramref name="from"/> to <paramref name="to"/>.
        /// Returns false when the horizontal displacement is (near) zero.
        /// </summary>
        public static bool TryHorizontalDirection(Vector3 from, Vector3 to, out Vector3 direction)
        {
            double dx = (double)to.x - from.x;
            double dz = (double)to.z - from.z;
            double len = Math.Sqrt(dx * dx + dz * dz);
            if (len < 1e-6)
            {
                direction = new Vector3(0f, 0f, 0f);
                return false;
            }

            direction = new Vector3((float)(dx / len), 0f, (float)(dz / len));
            return true;
        }
    }
}
