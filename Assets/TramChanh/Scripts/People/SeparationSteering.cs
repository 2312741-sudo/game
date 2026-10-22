using System;
using System.Collections.Generic;
using UnityEngine;

namespace TramChanh.People
{
    /// <summary>
    /// Separation steering: a push away from things that are too close (people, motorbikes, other animals).
    /// The push is added to the path as a small sideways offset that fades back to zero, so the animal steps aside
    /// but keeps following its route. Pure maths.
    /// </summary>
    public sealed class SeparationSteering
    {
        private Vector3 _offset;

        /// <summary>Current sideways offset from the planned path.</summary>
        public Vector3 Offset => _offset;

        /// <summary>
        /// Horizontal push for a point at <paramref name="position"/>: the sum over <paramref name="others"/> closer than
        /// <paramref name="radius"/> of a unit vector away from each, weighted by how close it is (1 at touching, 0 at the radius).
        /// The length is limited to 1.
        /// </summary>
        public static Vector3 Push(Vector3 position, IReadOnlyList<Vector3> others, float radius)
        {
            if (others == null || radius <= 0f) { return Vector3.zero; }
            double px = 0d, pz = 0d;
            for (int i = 0; i < others.Count; i++)
            {
                double dx = (double)position.x - others[i].x;
                double dz = (double)position.z - others[i].z;
                double d = Math.Sqrt(dx * dx + dz * dz);
                if (d >= radius) { continue; }
                if (d < 1e-4)
                {
                    // Exactly on top: push along +X, deterministic.
                    px += 1d;
                    continue;
                }

                double weight = 1d - d / radius;
                px += dx / d * weight;
                pz += dz / d * weight;
            }

            double length = Math.Sqrt(px * px + pz * pz);
            if (length > 1d) { px /= length; pz /= length; }
            return new Vector3((float)px, 0f, (float)pz);
        }

        /// <summary>
        /// Moves the offset along <paramref name="push"/> at <paramref name="pushSpeed"/> m/s, fades it by <paramref name="relaxPerSecond"/>
        /// and limits its length to <paramref name="maxOffset"/>.
        /// </summary>
        public void Step(float seconds, Vector3 push, float pushSpeed, float relaxPerSecond, float maxOffset)
        {
            if (!(seconds > 0f) || float.IsInfinity(seconds)) { return; }
            _offset += push * (pushSpeed * seconds);
            float keep = Mathf.Clamp01(1f - relaxPerSecond * seconds);
            _offset *= keep;
            float length = _offset.magnitude;
            if (length > maxOffset && length > 0f) { _offset *= maxOffset / length; }
        }

        public void Reset()
        {
            _offset = Vector3.zero;
        }
    }
}
