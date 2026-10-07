namespace TramChanh.Drinks.Domain
{
    // DRINK-001 pickup slice only. Held corresponds to documented PickedUp;
    // the full preparation sequence and ticket binding are deferred.
    public enum TeaBagState
    {
        Stored,
        Held
    }
}
