namespace TramChanh.Core.GroundTruth
{
    /// <summary>
    /// Real measurements of the Tram Chanh stall (GT-001, ARCHITECTURE.md §1).
    /// Only confirmed real-world values belong in this namespace. Anything not yet
    /// measured on the real stall is provisional data, never a constant here.
    /// Units: meters (1 Unity unit = 1 m).
    /// </summary>
    public static class StallDimensions
    {
        public const float Width = 1.8f;
        public const float Depth = 0.8f;
        public const float CounterHeight = 1.0f;
        public const float CounterToRoof = 1.2f;

        /// <summary>Approximate total height ("~2.2 m" in the source documents).</summary>
        public const float ApproxTotalHeight = CounterHeight + CounterToRoof;

        /// <summary>Validation tolerance for width and depth.</summary>
        public const float FootprintTolerance = 0.02f;

        /// <summary>Validation tolerance for total height (the source value is approximate).</summary>
        public const float TotalHeightTolerance = 0.05f;

        /// <summary>Validation tolerance for the counter top height.</summary>
        public const float CounterHeightTolerance = 0.01f;
    }
}
