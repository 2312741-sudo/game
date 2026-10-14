using UnityEngine;

namespace TramChanh.Core
{
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        public bool TryGetFloat(string key, out float value)
        {
            value = 0f;
            if (!PlayerPrefs.HasKey(key)) { return false; }
            value = PlayerPrefs.GetFloat(key);
            return true;
        }

        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }
    }
}
