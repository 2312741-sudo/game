using System.Collections.Generic;

namespace TramChanh.Props
{
    /// <summary>
    /// Plain C# bookkeeping for discarded props: at most <see cref="Capacity"/> alive at once, each with an expiry time.
    /// No UnityEngine types, so it is tested in EditMode. Time is passed in (seconds), never read here.
    /// </summary>
    public sealed class DebrisBudget
    {
        private struct Entry
        {
            public int Id;
            public double ExpiresAt;
        }

        private readonly List<Entry> _entries;

        public DebrisBudget(int capacity)
        {
            Capacity = capacity < 1 ? 1 : capacity;
            _entries = new List<Entry>(Capacity + 1);
        }

        public int Capacity { get; }
        public int Count => _entries.Count;

        /// <summary>
        /// Adds a prop. When the budget is already full the oldest prop (earliest registered) is removed and its id returned
        /// so the caller can destroy it; otherwise returns -1. Ids must be >= 0 and unique among live entries.
        /// </summary>
        public int Register(int id, double now, double lifetimeSeconds)
        {
            int evicted = -1;
            if (_entries.Count >= Capacity)
            {
                evicted = _entries[0].Id;
                _entries.RemoveAt(0);
            }
            _entries.Add(new Entry { Id = id, ExpiresAt = now + (lifetimeSeconds < 0d ? 0d : lifetimeSeconds) });
            return evicted;
        }

        /// <summary>Removes and returns the id of one expired prop, or false when none has expired yet. Call in a loop.</summary>
        public bool TryTakeExpired(double now, out int id)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].ExpiresAt <= now)
                {
                    id = _entries[i].Id;
                    _entries.RemoveAt(i);
                    return true;
                }
            }
            id = -1;
            return false;
        }

        /// <summary>Forget a prop that was destroyed elsewhere.</summary>
        public bool Release(int id)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Id == id)
                {
                    _entries.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public bool Contains(int id)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Id == id) { return true; }
            }
            return false;
        }

        /// <summary>Seconds left for a prop (0 when unknown or already expired).</summary>
        public double Remaining(int id, double now)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Id == id)
                {
                    double left = _entries[i].ExpiresAt - now;
                    return left > 0d ? left : 0d;
                }
            }
            return 0d;
        }
    }
}
