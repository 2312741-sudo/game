namespace TramChanh.Interaction.Preview
{
    /// <summary>Plain preview state, unrelated to a tea bag, recipe, grill or order.</summary>
    public sealed class InspectionState
    {
        public bool IsHighlighted { get; private set; }
        public void Toggle() => IsHighlighted = !IsHighlighted;
    }
}
