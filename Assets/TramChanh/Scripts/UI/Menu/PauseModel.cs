namespace TramChanh.UI.Menu
{
    public enum PauseState { Playing, Paused }

    public enum PauseTransition { None, Entered, Exited }

    /// <summary>
    /// Pure pause state machine. Fed once per frame with "the user paused" and "a modal (title, day summary) owns the screen".
    /// It remembers the time scale at the moment of pausing so resume restores it (the game fast-forwards while the stall is closed).
    /// No Unity types: the owner applies <see cref="FrozenTimeScale"/> and <see cref="SavedTimeScale"/>.
    /// </summary>
    public sealed class PauseModel
    {
        public const float FrozenTimeScale = 0f;

        public PauseState State { get; private set; } = PauseState.Playing;
        public float SavedTimeScale { get; private set; } = 1f;
        public bool IsPaused => State == PauseState.Paused;

        /// <summary>
        /// Call every frame. A modal always wins: pausing is refused while it is open and an open pause ends when it appears.
        /// </summary>
        public PauseTransition Sync(bool userPaused, bool modalOpen, float currentTimeScale)
        {
            bool wantPaused = userPaused && !modalOpen;
            if (wantPaused && State == PauseState.Playing)
            {
                // A scale of 0 (or garbage) would make resume freeze the game forever, so fall back to normal speed.
                SavedTimeScale = currentTimeScale > 0f && !float.IsNaN(currentTimeScale) && !float.IsInfinity(currentTimeScale) ? currentTimeScale : 1f;
                State = PauseState.Paused;
                return PauseTransition.Entered;
            }
            if (!wantPaused && State == PauseState.Paused)
            {
                State = PauseState.Playing;
                return PauseTransition.Exited;
            }
            return PauseTransition.None;
        }

        /// <summary>Leaves the paused state without a frame update (scene reload, destroy). Returns the scale to restore.</summary>
        public float Release()
        {
            State = PauseState.Playing;
            return SavedTimeScale;
        }
    }
}
