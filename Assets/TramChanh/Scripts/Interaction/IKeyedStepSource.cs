namespace TramChanh.Interaction
{
    /// <summary>
    /// An interactable that can name the next step of its workflow without being aimed at (cake station: one key, no mouse).
    /// The returned target is run through the normal action driver, so holds, queries and blocked reasons behave as when aimed.
    /// </summary>
    public interface IKeyedStepSource
    {
        /// <summary>The target to run for the keyed step, or null when nothing should happen (e.g. the cake is still cooking).</summary>
        IInteractable ResolveKeyedStep(InteractionContext context);
    }
}
