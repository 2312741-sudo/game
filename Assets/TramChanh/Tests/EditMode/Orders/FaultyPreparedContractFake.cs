using System;
using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Orders
{
    public sealed class FaultyPreparedContractFake : IPreparedItem
    {
        private readonly PreparedContractFault _fault;
        private readonly Action _publish;
        private bool _rejected;
        public ItemKind Kind { get; private set; } = ItemKind.Drink;
        public OrderItemRef BoundItem { get; private set; } = new OrderItemRef(new OrderId(1), new OrderItemId(2), new PreparationId(3));
        public bool Finished { get; set; }
        public bool IsFinished => Finished;
        public int Quality { get; private set; } = 100;
        public int State { get; private set; }
        public int EventCount { get; private set; }
        public FaultyPreparedContractFake(PreparedContractFault fault, bool finished, Action publish = null) { _fault = fault; Finished = finished; _publish = publish; }
        public Result MarkReady()
        {
            if (!IsFinished)
            {
                if (_fault == PreparedContractFault.LatchUnfinishedRejection && _rejected) { return Result.Fail("ready.already_ready"); }
                _rejected = true;
                if (_fault == PreparedContractFault.FailureChangesKind) { Kind = ItemKind.Cake; }
                if (_fault == PreparedContractFault.FailureChangesSourceState) { State++; }
                if (_fault == PreparedContractFault.EventsOnFailure) { EventCount++; _publish?.Invoke(); }
                return Result.Fail("ready.not_finished");
            }
            if (_fault == PreparedContractFault.RejectedLatchPoisonsSuccess && _rejected) { return Result.Fail("ready.already_ready"); }
            if (State == 1)
            {
                if (_fault == PreparedContractFault.RepeatedFailureChangesQuality) { Quality--; }
                return Result.Fail("ready.already_ready");
            }
            State = 1;
            if (_fault == PreparedContractFault.SuccessChangesBinding) { BoundItem = default; }
            if (_fault == PreparedContractFault.EventsOnSuccess) { EventCount++; _publish?.Invoke(); }
            return Result.Success();
        }
    }
}
