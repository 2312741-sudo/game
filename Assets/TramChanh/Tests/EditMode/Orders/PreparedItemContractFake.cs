using System;
using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Orders
{
    public sealed class PreparedItemContractFake : IPreparedItem
    {
        public ItemKind Kind => ItemKind.Drink;
        public OrderItemRef BoundItem => default;
        public bool Finished { get; set; }
        public bool IsFinished => Finished;
        public int Quality => 100;
        public bool WasMarkedReady { get; private set; }
        public int State => WasMarkedReady ? 1 : 0;
        public int EventCount => 0;
        public Result MarkReady()
        {
            if (!Finished) { return Result.Fail("ready.not_finished"); }
            if (WasMarkedReady) { return Result.Fail("ready.already_ready"); }
            WasMarkedReady = true;
            return Result.Success();
        }
    }
}
