namespace TramChanh.Core
{
    /// <summary>Read-only localization keys for the held item HUD; no gameplay commands.</summary>
    public interface IPreparationFeedback
    {
        string PreparationStateKey { get; }
        string NextActionKey { get; }
    }
}
