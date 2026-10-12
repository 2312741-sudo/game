using System;
using System.Collections.Generic;

namespace TramChanh.Stall.Layout
{
    /// <summary>
    /// Axis-aligned footprint on the XZ plane, in metres (1 Unity unit = 1 m).
    /// Pure C#: no Unity types, so it can be tested with the .NET harness.
    /// The caller picks the frame (stall-local or world); all rects in one call must share it.
    /// </summary>
    public readonly struct FootprintRect
    {
        public FootprintRect(string name, float minX, float minZ, float maxX, float maxZ)
        {
            if (maxX < minX || maxZ < minZ)
            {
                throw new ArgumentException("Max must not be below min for footprint '" + name + "'.");
            }

            Name = name ?? string.Empty;
            MinX = minX;
            MinZ = minZ;
            MaxX = maxX;
            MaxZ = maxZ;
        }

        public string Name { get; }
        public float MinX { get; }
        public float MinZ { get; }
        public float MaxX { get; }
        public float MaxZ { get; }

        public float Width => MaxX - MinX;
        public float Depth => MaxZ - MinZ;
        public float CenterX => (MinX + MaxX) * 0.5f;
        public float CenterZ => (MinZ + MaxZ) * 0.5f;
        public float Area => Width * Depth;

        public static FootprintRect FromCenter(string name, float centerX, float centerZ, float width, float depth)
        {
            return new FootprintRect(name, centerX - width * 0.5f, centerZ - depth * 0.5f,
                centerX + width * 0.5f, centerZ + depth * 0.5f);
        }
    }

    /// <summary>One pair of props whose XZ footprints overlap by more than the tolerance.</summary>
    public readonly struct PropOverlap
    {
        public PropOverlap(string first, string second, float overlapX, float overlapZ)
        {
            First = first;
            Second = second;
            OverlapX = overlapX;
            OverlapZ = overlapZ;
        }

        public string First { get; }
        public string Second { get; }

        /// <summary>Overlap extent along X, in metres.</summary>
        public float OverlapX { get; }

        /// <summary>Overlap extent along Z, in metres.</summary>
        public float OverlapZ { get; }

        /// <summary>Overlap area in square metres.</summary>
        public float AreaSquareMetres => OverlapX * OverlapZ;
    }

    /// <summary>
    /// Layout rules for props on the stall counter. Stateless pure functions.
    /// Frame convention (ART_INTEGRATION_HANDOFF.md, section 3): stall-local +Z is the customer (front) side,
    /// so the seller side, where the red tea rack stands, is the -Z half of the counter.
    /// </summary>
    public static class StallCounterLayoutRule
    {
        /// <summary>Overlaps and edge excursions up to this many metres are ignored.</summary>
        public const float DefaultToleranceMetres = 0.02f;

        /// <summary>
        /// Returns every pair whose XZ footprints overlap by MORE than <paramref name="tolerance"/> on both axes.
        /// Touching edges (zero overlap) and overlaps no larger than the tolerance are not reported.
        /// Pairs come back in input order, (i, j) with i less than j.
        /// </summary>
        public static IReadOnlyList<PropOverlap> FindPropOverlaps(IReadOnlyList<FootprintRect> props,
            float tolerance = DefaultToleranceMetres)
        {
            ValidateTolerance(tolerance);
            var result = new List<PropOverlap>();
            if (props == null)
            {
                return result;
            }

            for (int i = 0; i < props.Count; i++)
            {
                for (int j = i + 1; j < props.Count; j++)
                {
                    FootprintRect a = props[i];
                    FootprintRect b = props[j];
                    float overlapX = Math.Min(a.MaxX, b.MaxX) - Math.Max(a.MinX, b.MinX);
                    float overlapZ = Math.Min(a.MaxZ, b.MaxZ) - Math.Max(a.MinZ, b.MinZ);
                    if (overlapX > tolerance && overlapZ > tolerance)
                    {
                        result.Add(new PropOverlap(a.Name, b.Name, overlapX, overlapZ));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the names of props whose footprint is not fully inside the counter footprint.
        /// An excursion no larger than the tolerance is accepted.
        /// </summary>
        public static IReadOnlyList<string> FindPropsOutsideCounter(FootprintRect counter,
            IReadOnlyList<FootprintRect> props, float tolerance = DefaultToleranceMetres)
        {
            ValidateTolerance(tolerance);
            var names = new List<string>();
            if (props == null)
            {
                return names;
            }

            foreach (FootprintRect prop in props)
            {
                bool inside = prop.MinX >= counter.MinX - tolerance
                    && prop.MaxX <= counter.MaxX + tolerance
                    && prop.MinZ >= counter.MinZ - tolerance
                    && prop.MaxZ <= counter.MaxZ + tolerance;
                if (!inside)
                {
                    names.Add(prop.Name);
                }
            }

            return names;
        }

        /// <summary>
        /// Red tea rack rule: the rack must sit behind the counter, meaning on the seller side of the
        /// counter centre line (the -Z half) and within the counter's X and Z extent.
        /// </summary>
        public static bool IsBehindCounter(FootprintRect counter, FootprintRect rack,
            float tolerance = DefaultToleranceMetres)
        {
            ValidateTolerance(tolerance);
            bool withinX = rack.MinX >= counter.MinX - tolerance && rack.MaxX <= counter.MaxX + tolerance;
            bool onSellerSide = rack.MaxZ <= counter.CenterZ + tolerance;
            bool withinDepth = rack.MinZ >= counter.MinZ - tolerance;
            return withinX && onSellerSide && withinDepth;
        }

        private static void ValidateTolerance(float tolerance)
        {
            if (tolerance < 0f || float.IsNaN(tolerance))
            {
                throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be a non-negative number.");
            }
        }
    }
}
