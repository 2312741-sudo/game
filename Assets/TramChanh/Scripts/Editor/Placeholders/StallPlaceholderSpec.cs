using TramChanh.Core.GroundTruth;
using TramChanh.Stall.Anchors;
using UnityEngine;

namespace TramChanh.EditorTools.Placeholders
{
    /// <summary>
    /// Geometry for the ART-STALL-001 phase A placeholder.
    ///
    /// Two kinds of values live here and must not be confused:
    /// - Ground truth (GT-001) comes from <see cref="StallDimensions"/> only.
    /// - Everything else (wheel size, post size, sign size, anchor seeds) is a
    ///   provisional blockout value. It exists only to produce a usable placeholder
    ///   and is NOT a real-world measurement. Final values come from the real stall
    ///   reference and are edited in the prefab, not here.
    ///
    /// Axes: origin = ground centre, +Y up, +Z = customer / Lobby side (front),
    /// -Z = worker side (back).
    /// </summary>
    internal static class StallPlaceholderSpec
    {
        // ---- Ground truth (GT-001) -------------------------------------------------
        public const float Width = StallDimensions.Width;
        public const float Depth = StallDimensions.Depth;
        public const float CounterTop = StallDimensions.CounterHeight;
        public const float TotalHeight = StallDimensions.ApproxTotalHeight;

        // ---- Provisional blockout values (NOT real-world measurements) -------------
        public const float WheelDiameter = 0.10f;
        public const float WheelWidth = 0.04f;
        public const float WheelInset = 0.08f;
        public const float CounterSlabThickness = 0.04f;
        public const float RoofThickness = 0.04f;
        public const float PostSize = 0.05f;

        // New-sign placeholder box. Real sign size is not yet measured: this is a
        // provisional volume inside the stall footprint, replaced by ART-BRAND-001.
        public static readonly Vector3 SignSize = new Vector3(1.70f, 0.30f, 0.12f);

        public static float StructureBottom => WheelDiameter;
        public static float CounterSlabBottom => CounterTop - CounterSlabThickness;
        public static float RoofBottom => TotalHeight - RoofThickness;

        /// <summary>
        /// Seed positions used only when an anchor does not exist yet. Once the
        /// prefab exists, rebuilding keeps the positions edited in the prefab.
        /// All seeds are provisional (DEC-011) and sit on the counter surface.
        /// </summary>
        public static Vector3 SeedPosition(StallAnchorId id)
        {
            const float back = -0.15f;
            switch (id)
            {
                case StallAnchorId.TeaRack: return new Vector3(-0.75f, CounterTop, back);
                case StallAnchorId.Topping: return new Vector3(-0.55f, CounterTop, back);
                case StallAnchorId.IceBin: return new Vector3(-0.35f, CounterTop, back);
                case StallAnchorId.WipeArea: return new Vector3(-0.15f, CounterTop, back);
                case StallAnchorId.ReadyCounter: return new Vector3(0f, CounterTop, 0.30f);
                case StallAnchorId.BatterArea: return new Vector3(0.15f, CounterTop, back);
                case StallAnchorId.Grill: return new Vector3(0.35f, CounterTop, back);
                case StallAnchorId.RollArea: return new Vector3(0.52f, CounterTop, back);
                case StallAnchorId.Sauce: return new Vector3(0.67f, CounterTop, back);
                case StallAnchorId.Wrap: return new Vector3(0.80f, CounterTop, back);
                default: return new Vector3(0f, CounterTop, 0f);
            }
        }
    }
}
