using System;
using System.Collections.Generic;

namespace TramChanh.Save
{
    /// <summary>One saved number under the key the owning holder (GameProgress, Reputation, ...) uses.</summary>
    [Serializable]
    public sealed class SaveEntry
    {
        public string key;
        public float value;

        public SaveEntry() { }

        public SaveEntry(string key, float value)
        {
            this.key = key;
            this.value = value;
        }
    }

    /// <summary>The save file content. Versioned; a file with another version is refused, never half-read.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        /// <summary>Informational only (ISO 8601, UTC).</summary>
        public string savedAtUtc;
        public List<SaveEntry> entries = new List<SaveEntry>();

        public bool TryGet(string key, out float value)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i] != null && entries[i].key == key)
                    {
                        value = entries[i].value;
                        return true;
                    }
                }
            }
            value = 0f;
            return false;
        }
    }
}
