using System;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Drinks.Domain
{
    public sealed class DrinkPreparation : IPreparedItem
    {
        private OrderItemRef _boundItem;
        private IStallTicketQueue _tickets;
        public PreparationId PreparationId { get; }
        public TeaBagState State { get; private set; } = TeaBagState.Stored;
        public ItemKind Kind => ItemKind.Drink;
        public OrderItemRef BoundItem => _tickets == null || _tickets.IsBound(_boundItem) ? _boundItem : default;
        public bool IsFinished => State == TeaBagState.Wiped || State == TeaBagState.Ready;
        public int Quality => 100;

        public DrinkPreparation(PreparationId preparationId, SequenceMode mode = SequenceMode.Strict)
        {
            if (!preparationId.IsValid)
            {
                throw new ArgumentException("A drink requires a stable preparation identity.", nameof(preparationId));
            }
            if (mode != SequenceMode.Strict)
            {
                throw new NotSupportedException("Only the approved strict sequence is implemented.");
            }
            PreparationId = preparationId;
        }

        public Result TryPickUp() => Advance(TeaBagState.Stored, TeaBagState.PickedUp, "drink.bag.not_stored");
        public Result Open() => Advance(TeaBagState.PickedUp, TeaBagState.Opened, "drink.need_pickup");
        public Result AddCoconutJelly() => Advance(TeaBagState.Opened, TeaBagState.CoconutJellyAdded, "drink.need_open");
        public Result AddLemonJelly() => Advance(TeaBagState.CoconutJellyAdded, TeaBagState.LemonJellyAdded, "drink.need_coconut_first");
        public Result AddIce() => Advance(TeaBagState.LemonJellyAdded, TeaBagState.IceAdded, "drink.need_lemon_first");
        public Result Shake() => Advance(TeaBagState.IceAdded, TeaBagState.Shaken, "drink.need_ice_first");
        public Result Wipe() => Advance(TeaBagState.Shaken, TeaBagState.Wiped, "drink.need_shake_first");

        public Result MarkReady()
        {
            if (State == TeaBagState.Ready)
            {
                return Result.Fail("ready.already_ready");
            }
            // The shelf commits order/slot state before publishing its notifications.
            return Advance(TeaBagState.Wiped, TeaBagState.Ready, "ready.not_finished");
        }

        internal void BeginBoundPickup(OrderItemRef binding, IStallTicketQueue tickets)
        {
            if (State != TeaBagState.Stored || binding.PreparationId != PreparationId)
            {
                throw new InvalidOperationException("Pickup requires a stored bag and its matching ticket claim.");
            }
            _boundItem = binding;
            _tickets = tickets;
            State = TeaBagState.PickedUp;
        }

        internal void RollbackBoundPickup()
        {
            State = TeaBagState.Stored;
            _boundItem = default;
            _tickets = null;
        }

        private Result Advance(TeaBagState required, TeaBagState next, string reason)
        {
            if (State != required)
            {
                return Result.Fail(reason);
            }
            State = next;
            return Result.Success();
        }
    }
}
