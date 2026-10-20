namespace TramChanh.Interaction
{
    public enum PlayerActionPhase { Started, Completed, Cancelled }

    /// <summary>
    /// The player began, finished or abandoned an action. Presentation only (avatar poses): gameplay never reads it.
    /// Press actions publish a single Completed; Hold/Continuous actions publish Started then Completed or Cancelled.
    /// </summary>
    public readonly struct PlayerActionEvent
    {
        public string PromptKey { get; }
        public InteractionKind Kind { get; }
        public PlayerActionPhase Phase { get; }
        /// <summary>Seconds a Hold action takes (0 for Press and Continuous).</summary>
        public float Duration { get; }

        public PlayerActionEvent(string promptKey, InteractionKind kind, PlayerActionPhase phase, float duration = 0f)
        {
            PromptKey = promptKey;
            Kind = kind;
            Phase = phase;
            Duration = duration;
        }
    }
}
