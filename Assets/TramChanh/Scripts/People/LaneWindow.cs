using System;
using System.Collections.Generic;
using UnityEngine;

namespace TramChanh.People
{
    /// <summary>
    /// Keeps ambient agents near the stall on a long street. Given the points of a lane, picks the contiguous run of points
    /// within a radius (measured on the ground plane) of a centre, so a pedestrian walks at most about two radii instead of the
    /// whole lane. A radius of 0 or less means "no window": the whole lane. Pure: no scene access.
    /// </summary>
    public static class LaneWindow
    {
        /// <summary>
        /// Finds the run of consecutive points within <paramref name="radius"/> of <paramref name="center"/> that contains the point
        /// nearest to the centre. <paramref name="first"/> and <paramref name="last"/> are inclusive indices. Returns false when the
        /// lane has fewer than two usable points, or no point is within the radius.
        /// </summary>
        public static bool TryGetRange(IReadOnlyList<Vector3> points, Vector3 center, float radius, out int first, out int last)
        {
            first = 0;
            last = -1;
            if (points == null || points.Count < 2) { return false; }
            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0f)
            {
                last = points.Count - 1;
                return true;
            }

            int nearest = -1;
            float best = float.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                float distance = Flat(points[i], center);
                if (distance < best) { best = distance; nearest = i; }
            }

            if (nearest < 0 || best > radius) { return false; }
            first = nearest;
            last = nearest;
            while (first > 0 && Flat(points[first - 1], center) <= radius) { first--; }
            while (last < points.Count - 1 && Flat(points[last + 1], center) <= radius) { last++; }
            return last > first;
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
