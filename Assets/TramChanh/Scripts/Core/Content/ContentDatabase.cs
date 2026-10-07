using System;
using System.Collections.Generic;
using TramChanh.Core;

namespace TramChanh.Content
{
    /// <summary>Copies asset identities into values; the order domain retains no Unity objects.</summary>
    public sealed class ContentDatabase : IContentDatabase
    {
        private readonly Dictionary<string, ItemKind> _kinds = new Dictionary<string, ItemKind>(StringComparer.Ordinal);

        public ContentDatabase(IEnumerable<ItemDefinition> definitions)
        {
            if (definitions == null) { throw new ArgumentNullException(nameof(definitions)); }
            foreach (ItemDefinition definition in definitions)
            {
                if (definition == null) { throw new ArgumentException("Content definition is missing.", nameof(definitions)); }
                Add(definition.Id, definition.Kind);
            }
        }

        public ContentDatabase(IEnumerable<KeyValuePair<string, ItemKind>> definitions)
        {
            if (definitions == null) { throw new ArgumentNullException(nameof(definitions)); }
            foreach (KeyValuePair<string, ItemKind> definition in definitions) { Add(definition.Key, definition.Value); }
        }

        public bool TryGetKind(string itemDefinitionId, out ItemKind kind)
        {
            kind = default;
            return itemDefinitionId != null && _kinds.TryGetValue(itemDefinitionId, out kind);
        }

        private void Add(string id, ItemKind kind)
        {
            if (string.IsNullOrWhiteSpace(id) || (kind != ItemKind.Drink && kind != ItemKind.Cake) || _kinds.ContainsKey(id))
            {
                throw new ArgumentException("Content identities must be unique, non-empty and use a supported item kind.");
            }
            _kinds.Add(id, kind);
        }
    }
}
