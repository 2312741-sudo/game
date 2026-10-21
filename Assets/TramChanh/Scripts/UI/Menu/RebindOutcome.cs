namespace TramChanh.UI.Menu
{
    /// <summary>What happened when the player tried to give a slot a new key.</summary>
    public enum RebindOutcome
    {
        Applied,
        Swapped,
        Unchanged,
        Conflict,
        Reserved,
        WrongDevice,
        UnknownSlot,
        Cancelled
    }
}
