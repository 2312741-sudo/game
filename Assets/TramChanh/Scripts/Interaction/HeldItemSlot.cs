namespace TramChanh.Interaction
{
    /// <summary>One logical slot. No product or pickup gameplay is supplied by this preview.</summary>
    public sealed class HeldItemSlot : IHeldItemSlot
    {
        public IHoldable Current { get; private set; }
        public bool TryPickUp(IHoldable item)
        {
            if (item == null || Current != null)
            {
                return false;
            }
            Current = item;
            item.OnPickedUp(this);
            return true;
        }
        public bool TryRelease()
        {
            if (Current == null)
            {
                return false;
            }
            IHoldable previous = Current;
            Current = null;
            previous.OnReleased();
            return true;
        }
    }
}
