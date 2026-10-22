namespace TramChanh.People
{
    /// <summary>
    /// Level of detail for the animal AI. Animals near the player think and move often; far animals only update now and then.
    /// A few dozen animals stay cheap because most of them are in the Far or Dormant tier.
    /// </summary>
    public static class AnimalLod
    {
        public const float NearMetres = 12f;
        public const float MidMetres = 30f;
        public const float FarMetres = 60f;

        /// <summary>NaN and negative distances count as Near (safest: full detail).</summary>
        public static AnimalLodTier TierFor(float distanceToPlayer)
        {
            if (float.IsNaN(distanceToPlayer) || distanceToPlayer <= NearMetres) { return AnimalLodTier.Near; }
            if (distanceToPlayer <= MidMetres) { return AnimalLodTier.Mid; }
            if (distanceToPlayer <= FarMetres) { return AnimalLodTier.Far; }
            return AnimalLodTier.Dormant;
        }

        /// <summary>Seconds between perception + brain updates.</summary>
        public static float ThinkInterval(AnimalLodTier tier)
        {
            switch (tier)
            {
                case AnimalLodTier.Near: return 0.1f;
                case AnimalLodTier.Mid: return 0.3f;
                case AnimalLodTier.Far: return 1f;
                default: return 2f;
            }
        }

        /// <summary>Seconds between movement updates; 0 means every frame.</summary>
        public static float MoveInterval(AnimalLodTier tier)
        {
            switch (tier)
            {
                case AnimalLodTier.Near: return 0f;
                case AnimalLodTier.Mid: return 0.1f;
                case AnimalLodTier.Far: return 0.4f;
                default: return 1f;
            }
        }
    }
}
