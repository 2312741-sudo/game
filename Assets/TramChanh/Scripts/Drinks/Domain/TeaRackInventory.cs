using System;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Drinks.Domain
{
    /// <summary>Stored pre-portioned bags only; no quantities or preparation steps.</summary>
    /// <remarks>
    /// MAIN-102 restock (provisional, DEC-015 TBD): pre-portioned bags are restocked from storage. When the rack
    /// has no stored bag, every stocked slot whose bag has left play for good (Ready, or a discarded orphan) receives a
    /// fresh, unbound Stored bag with a new preparation identity. Stock never exceeds capacity. A slot whose bag
    /// is still held or in preparation is never refilled. Restocking claims no ticket and publishes nothing.
    /// </remarks>
    public sealed class TeaRackInventory
    {
        private readonly DrinkPreparation[] _bags;
        private readonly IIdGenerator _ids;
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
            _ids = ids;
            _bags = new DrinkPreparation[capacity];
            for (int i = 0; i < initialStock; i++)
            {
                _bags[i] = new DrinkPreparation(new PreparationId(ids.Next()));
            }
        }

        public DrinkPreparation BagAt(int index) => _bags[index];

        /// <summary>
        /// A slot may be restocked only when its bag left play for good (Ready or a discarded orphan). A slot that was never
        /// stocked (initial stock below capacity) stays empty, so an intentionally empty rack keeps its configuration.
        /// </summary>
        public static bool IsRestockable(DrinkPreparation bag) => bag != null && (bag.State == TeaBagState.Ready || bag.IsRetired);

        /// <summary>
        /// Restocks terminal slots only while the rack is empty of stored bags. Calls <paramref name="restocked"/> once per
        /// refilled slot index and returns the number of new bags.
        /// </summary>
        public int RestockWhenEmpty(Action<int> restocked = null)
        {
            if (Stock > 0)
            {
                return 0;
            }
            int count = 0;
            for (int i = 0; i < _bags.Length; i++)
            {
                if (!IsRestockable(_bags[i]))
                {
                    continue;
                }
                _bags[i] = new DrinkPreparation(new PreparationId(_ids.Next()));
                count++;
                restocked?.Invoke(i);
            }
            return count;
        }

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
