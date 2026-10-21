using System;
using System.Collections.Generic;
using UnityEngine;

namespace TramChanh.UI.Menu
{
    /// <summary>
    /// Saves and loads the binding-override JSON text through an <see cref="IRebindTextStore"/> and rejects text that is clearly corrupt.
    /// Pure of the Input System so it is testable; <see cref="RebindStore"/> couples it to an InputActionAsset.
    /// </summary>
    public sealed class RebindJsonStore
    {
        public const string DefaultKey = "settings.input_overrides";

        [Serializable]
        private sealed class OverrideEntry
        {
            public string action = string.Empty;
            public string id = string.Empty;
            public string path = string.Empty;
        }

        [Serializable]
        private sealed class OverrideFile
        {
            public List<OverrideEntry> bindings;
        }

        private readonly IRebindTextStore _store;
        private readonly string _key;

        public RebindJsonStore(IRebindTextStore store, string key = DefaultKey)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _key = string.IsNullOrEmpty(key) ? DefaultKey : key;
        }

        public void Save(string json)
        {
            if (!LooksValid(json)) { _store.Delete(_key); return; }
            _store.Set(_key, json);
        }

        /// <summary>True and the text when something valid is stored. Corrupt text is deleted and reported as nothing stored.</summary>
        public bool TryLoad(out string json)
        {
            json = null;
            if (!_store.TryGet(_key, out string raw) || string.IsNullOrWhiteSpace(raw)) { return false; }
            if (!LooksValid(raw))
            {
                _store.Delete(_key);
                return false;
            }
            json = raw;
            return true;
        }

        public void Clear() => _store.Delete(_key);

        /// <summary>True when the text is a JSON object with a "bindings" list whose entries each have a path.</summary>
        public static bool LooksValid(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) { return false; }
            string trimmed = json.Trim();
            if (trimmed[0] != '{' || trimmed[trimmed.Length - 1] != '}') { return false; }
            if (trimmed.IndexOf("\"bindings\"", StringComparison.Ordinal) < 0) { return false; }
            try
            {
                OverrideFile file = JsonUtility.FromJson<OverrideFile>(trimmed);
                if (file == null || file.bindings == null) { return false; }
                foreach (OverrideEntry entry in file.bindings)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.path)) { return false; }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
