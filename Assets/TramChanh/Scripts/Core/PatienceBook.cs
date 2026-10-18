using System;
using System.Collections.Generic;

namespace TramChanh.Core
{
    /// <summary>
    /// Keeps the patience of every waiting order. Pure C#: the caller passes the game-clock time, so a paused game
    /// (clock not advancing) never makes anybody leave. Freezing an entry stops its countdown (the order is on its way
    /// to the customer).
    /// </summary>
    public sealed class PatienceBook
    {
        private struct Entry
        {
            public CustomerKind Kind;
            public double Start;
            public double Patience;
            public bool Frozen;
            public double FrozenLeft;
        }

        private readonly Dictionary<OrderId, Entry> _entries = new Dictionary<OrderId, Entry>();

        public int Count => _entries.Count;
        public bool Has(OrderId id) => _entries.ContainsKey(id);

        public void Add(OrderId id, CustomerKind kind, double now, double patienceSeconds)
        {
            if (!id.IsValid) { return; }
            if (double.IsNaN(patienceSeconds) || patienceSeconds <= 0d) { patienceSeconds = 1d; }
            _entries[id] = new Entry { Kind = kind, Start = now, Patience = patienceSeconds };
        }

        public bool Remove(OrderId id) => _entries.Remove(id);

        public bool TryGetKind(OrderId id, out CustomerKind kind)
        {
            if (_entries.TryGetValue(id, out Entry e)) { kind = e.Kind; return true; }
            kind = default;
            return false;
        }

        /// <summary>Share of the patience still left, 1 (just arrived) to 0 (gone). Unknown order: 0.</summary>
        public double LeftFraction(OrderId id, double now)
        {
            if (!_entries.TryGetValue(id, out Entry e)) { return 0d; }
            if (e.Frozen) { return e.FrozenLeft; }
            return Clamp01(1d - (now - e.Start) / e.Patience);
        }

        /// <summary>Stops the countdown and remembers how much patience was left.</summary>
        public void Freeze(OrderId id, double now)
        {
            if (!_entries.TryGetValue(id, out Entry e) || e.Frozen) { return; }
            e.FrozenLeft = Clamp01(1d - (now - e.Start) / e.Patience);
            e.Frozen = true;
            _entries[id] = e;
        }

        /// <summary>Adds every order whose patience ran out to <paramref name="into"/> and removes them from the book.</summary>
        public void CollectExpired(double now, List<OrderId> into)
        {
            if (into == null) { throw new ArgumentNullException(nameof(into)); }
            int first = into.Count;
            foreach (KeyValuePair<OrderId, Entry> pair in _entries)
            {
                if (!pair.Value.Frozen && now - pair.Value.Start >= pair.Value.Patience) { into.Add(pair.Key); }
            }
            for (int i = first; i < into.Count; i++) { _entries.Remove(into[i]); }
        }

        private static double Clamp01(double v) => v < 0d ? 0d : (v > 1d ? 1d : v);
    }
}
