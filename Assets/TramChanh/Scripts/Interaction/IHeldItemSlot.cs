namespace TramChanh.Interaction
{
    public interface IHeldItemSlot
    {
        IHoldable Current { get; }
        bool TryPickUp(IHoldable item);
        bool TryRelease();
    }
}
