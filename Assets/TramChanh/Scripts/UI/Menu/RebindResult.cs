using System.Collections.Generic;

namespace TramChanh.UI.Menu
{
    /// <summary>Result of <see cref="RebindModel.TrySet"/>: the outcome, a Vietnamese message and every slot whose key changed.</summary>
    public sealed class RebindResult
    {
        private readonly List<KeyValuePair<string, string>> _changes = new List<KeyValuePair<string, string>>();

        public RebindResult(RebindOutcome outcome, string message)
        {
            Outcome = outcome;
            Message = message ?? string.Empty;
        }

        public RebindOutcome Outcome { get; }
        public string Message { get; }
        /// <summary>Slot id and its new normalized path, for each slot that changed (two entries after a swap).</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Changes => _changes;
        public bool Succeeded => Outcome == RebindOutcome.Applied || Outcome == RebindOutcome.Swapped;

        internal void AddChange(string slotId, string path) => _changes.Add(new KeyValuePair<string, string>(slotId, path));
    }
}
