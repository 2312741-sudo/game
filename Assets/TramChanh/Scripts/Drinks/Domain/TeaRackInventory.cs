using System;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Drinks.Domain
{
    /// <summary>Stored pre-portioned bags only; no quantities, refill or preparation steps.</summary>
    public sealed class TeaRackInventory
    {
        private readonly DrinkPreparation[] _bags;
        public int Capacity => _bags.Length;
        public int Stock
        {
            get
            {
                int count = 0;
                foreach (DrinkPreparation bag in _bags)
                {
                    if (bag != null && bag.State == TeaBagState.Stored)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public TeaRackInventory(int capacity, int initialStock, IIdGenerator ids = null)
        {
            if (capacity < 0 || initialStock < 0 || initialStock > capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(initialStock));
            }
            ids ??= new SequentialIdGenerator();
            _bags = new DrinkPreparation[capacity];
            for (int i = 0; i < initialStock; i++)
            {
                _bags[i] = new DrinkPreparation(new PreparationId(ids.Next()));
            }
        }

        public DrinkPreparation BagAt(int index) => _bags[index];

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


        public Result TryTakeBag(bool handsEmpty, IStallTicketQueue tickets, Func<int, bool> transferToHands)
        {
            Availability available = CanTake(handsEmpty);
            if (!available.IsAvailable)
            {
                return Result.Fail(available.ReasonKey);
            }
            if (tickets == null || !tickets.HasPending(ItemKind.Drink))
            {
                return Result.Fail("stall.no_ticket.drink");
            }
            if (transferToHands == null)
            {
                throw new ArgumentNullException(nameof(transferToHands));
            }
            int index = NextStoredIndex();
            DrinkPreparation bag = _bags[index];
            Result<OrderItemRef> claim = tickets.ClaimNext(ItemKind.Drink, bag.PreparationId);
            if (!claim.IsSuccess)
            {
                return Result.Fail(claim.ReasonKey);
            }
            bag.BeginBoundPickup(claim.Value, tickets);
            if (!transferToHands(index))
            {
                bag.RollbackBoundPickup();
                if (!tickets.Release(claim.Value).IsSuccess)
                {
                    throw new InvalidOperationException("A refused pickup must release its ticket claim.");
                }
                return Result.Fail("hands.full");
            }
            return Result.Success();
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
