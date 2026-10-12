namespace TramChanh.People
{
    /// <summary>One ambient agent spawned by <see cref="CrowdScheduler"/>.</summary>
    public readonly struct CrowdSpawn
    {
        public CrowdSpawn(int agentId, string lane, bool reverse)
        {
            AgentId = agentId;
            Lane = lane;
            Reverse = reverse;
        }

        /// <summary>Unique per scheduler, starting at 1.</summary>
        public int AgentId { get; }

        /// <summary>Lane name the agent walks/drives along.</summary>
        public string Lane { get; }

        /// <summary>True to walk the lane from its last node to its first. Always false for road lanes.</summary>
        public bool Reverse { get; }
    }
}
