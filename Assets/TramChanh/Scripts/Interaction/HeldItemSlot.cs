using TramChanh.Core;

namespace TramChanh.Interaction
{
    /// <summary>One logical slot; publishes successful changes for the hand view.</summary>
    public sealed class HeldItemSlot : IHeldItemSlot
    {
        private readonly IEventBus _events;
        public IHoldable Current { get; private set; }

        public HeldItemSlot(IEventBus events = null)
        {
            _events = events;
        }
        public bool TryPickUp(IHoldable item)
        {
            if (item == null || Current != null)
            {
                return false;
            }
            Current = item;
            item.OnPickedUp(this);
            _events?.Publish(new HeldItemChanged(null, item));
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
            _events?.Publish(new HeldItemChanged(previous, null));
            return true;
        }
    }
}
