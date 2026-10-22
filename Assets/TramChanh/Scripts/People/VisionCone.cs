using System;
using UnityEngine;

namespace TramChanh.People
{
    /// <summary>Field-of-view test on the ground plane: inside a distance and inside an angle either side of the facing.</summary>
    public static class VisionCone
    {
        /// <param name="halfAngleDegrees">Angle from the facing to the edge of the cone; 180 sees all around.</param>
        public static bool Contains(Vector3 origin, Vector3 forward, Vector3 target, float distance, float halfAngleDegrees)
        {
            double dx = (double)target.x - origin.x;
            double dz = (double)target.z - origin.z;
            double d2 = dx * dx + dz * dz;
            if (d2 > (double)distance * distance) { return false; }
            if (d2 < 1e-8) { return true; }
            if (halfAngleDegrees >= 180f) { return true; }
            double fl = Math.Sqrt((double)forward.x * forward.x + (double)forward.z * forward.z);
            if (fl < 1e-6) { return true; }
            double dot = (dx * forward.x + dz * forward.z) / (Math.Sqrt(d2) * fl);
            double limit = Math.Cos(halfAngleDegrees * Math.PI / 180d);
            return dot >= limit;
        }
    }
}
