using System;

namespace TramChanh.Core
{
    /// <summary>Scene-local positive IDs. Seed is the last issued value.</summary>
    public sealed class SequentialIdGenerator : IIdGenerator
    {
        private int _lastIssued;

        public SequentialIdGenerator(int seed = 0)
        {
            if (seed < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(seed));
            }
            _lastIssued = seed;
        }

        public int Next()
        {
            _lastIssued = checked(_lastIssued + 1);
            return _lastIssued;
        }
    }
}
