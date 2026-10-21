using UnityEngine;

namespace TramChanh.UI.Menu
{
    /// <summary>PlayerPrefs-backed <see cref="IRebindTextStore"/>.</summary>
    public sealed class PlayerPrefsRebindTextStore : IRebindTextStore
    {
        public bool TryGet(string key, out string value)
        {
            value = null;
            if (!PlayerPrefs.HasKey(key)) { return false; }
            value = PlayerPrefs.GetString(key, string.Empty);
            return true;
        }

        public void Set(string key, string value)
        {
            PlayerPrefs.SetString(key, value ?? string.Empty);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
