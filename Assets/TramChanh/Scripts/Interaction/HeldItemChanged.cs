namespace TramChanh.Interaction
{
    public readonly struct HeldItemChanged
    {
        public IHoldable Previous { get; }
        public IHoldable Current { get; }

        public HeldItemChanged(IHoldable previous, IHoldable current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
