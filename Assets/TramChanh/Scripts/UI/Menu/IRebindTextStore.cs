namespace TramChanh.UI.Menu
{
    /// <summary>Where the saved binding overrides text lives. PlayerPrefs in the game, a dictionary in tests.</summary>
    public interface IRebindTextStore
    {
        bool TryGet(string key, out string value);
        void Set(string key, string value);
        void Delete(string key);
    }
}
