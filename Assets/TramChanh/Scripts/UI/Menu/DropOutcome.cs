namespace TramChanh.UI.Menu
{
    /// <summary>Result of a drop: the verdict, whether something was bought, and a Vietnamese line for the player.</summary>
    public readonly struct DropOutcome
    {
        public DropOutcome(DropVerdict verdict, bool bought, string message)
        {
            Verdict = verdict;
            Bought = bought;
            Message = message ?? string.Empty;
        }

        public DropVerdict Verdict { get; }
        public bool Bought { get; }
        public string Message { get; }
    }
}
