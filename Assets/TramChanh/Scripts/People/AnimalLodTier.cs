namespace TramChanh.People
{
    /// <summary>How closely an animal is simulated, by distance to the player.</summary>
    public enum AnimalLodTier
    {
        /// <summary>Close: sense 10 times a second, move every frame, animate, show debug text.</summary>
        Near = 0,

        /// <summary>Medium: think 3 times a second, move 10 times a second.</summary>
        Mid = 1,

        /// <summary>Far: think about once a second, move a few times a second.</summary>
        Far = 2,

        /// <summary>Very far: a heartbeat only (every two seconds).</summary>
        Dormant = 3,
    }
}
