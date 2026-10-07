using System;
using UnityEngine;

namespace TramChanh.UI.Localization
{
    [CreateAssetMenu(menuName = "Tram Chanh/Prompt Localization Table")]
    public sealed class PromptLocalizationTable : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            [SerializeField] private string _key;
            [SerializeField] private string _en;
            [SerializeField] private string _vi;
            public string Key => _key;
            public string English => _en;
            public string Vietnamese => _vi;
        }
        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();
        public int EntryCount => _entries.Length;
        public string Resolve(string key, string language)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }
            foreach (Entry entry in _entries)
            {
                if (entry.Key == key)
                {
                    return language == "vi" ? entry.Vietnamese : entry.English;
                }
            }
            return key;
        }
    }
}
