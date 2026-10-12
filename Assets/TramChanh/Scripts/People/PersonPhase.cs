namespace TramChanh.People
{
    /// <summary>
    /// Lifecycle of a simulated customer (Docs/Design/NPC_AND_MAP_EXPANSION.md §3). Ordering itself stays in
    /// the Lobby/Orders flow; a person is <see cref="Seated"/> while that flow runs.
    /// </summary>
    public enum PersonPhase
    {
        Arriving,
        Seated,
        Waiting,
        Eating,
        Leaving,
        Gone
    }
}
