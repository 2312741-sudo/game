using System;
using TramChanh.Core;

namespace TramChanh.Drinks.Domain
{
    /// <summary>Stored pre-portioned bags only; no quantities, refill or preparation steps.</summary>
    public sealed class TeaRackInventory
    {
        private readonly TeaBagPickup[] _bags;
        public int Capacity => _bags.Length;
        public int Stock
        {
            get
            {
                int count = 0;
                foreach (TeaBagPickup bag in _bags)
                {
                    if (bag != null && bag.State == TeaBagState.Stored)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public TeaRackInventory(int capacity, int initialStock)
        {
            if (capacity < 0 || initialStock < 0 || initialStock > capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(initialStock));
            }
            _bags = new TeaBagPickup[capacity];
            for (int i = 0; i < initialStock; i++)
            {
                _bags[i] = new TeaBagPickup();
            }
        }

        public TeaBagPickup BagAt(int index) => _bags[index];

        public int NextStoredIndex()
        {
            for (int i = 0; i < _bags.Length; i++)
            {
                if (_bags[i] != null && _bags[i].State == TeaBagState.Stored)
                {
                    return i;
                }
            }
            return -1;
        }

        public Availability CanTake(bool handsEmpty)
        {
            if (!handsEmpty)
            {
                return Availability.Blocked("hands.full");
            }
            return Stock > 0 ? Availability.Available : Availability.Blocked("drink.rack.empty");
        }
    }
}
