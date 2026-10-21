using System.Collections.Generic;
using TramChanh.Core;

namespace TramChanh.Save
{
    /// <summary>In-memory ISettingsStore. Lets the holders' own Save/Load write and read a snapshot.</summary>
    public sealed class DictionarySettingsStore : ISettingsStore
    {
        private readonly Dictionary<string, float> _values = new Dictionary<string, float>();
        private readonly List<string> _order = new List<string>();

        public bool TryGetFloat(string key, out float value) { return _values.TryGetValue(key, out value); }

        public void SetFloat(string key, float value)
        {
            if (!_values.ContainsKey(key)) { _order.Add(key); }
            _values[key] = value;
        }

        public IReadOnlyList<string> Keys => _order;
    }
}
