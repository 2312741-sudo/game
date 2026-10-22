namespace TramChanh.UI.Menu
{
    /// <summary>What happened when the pointer was released. WasDrag is false for a plain click that never moved far enough.</summary>
    public readonly struct DragRelease
    {
        public DragRelease(bool wasDrag, DropOutcome outcome)
        {
            WasDrag = wasDrag;
            Outcome = outcome;
        }

        public bool WasDrag { get; }
        public DropOutcome Outcome { get; }
    }
}
