using System.Collections.Generic;
using TramChanh.Core;

namespace TramChanh.Orders
{
    public readonly struct OrderEntryConfirmed
    {
        public OrderId OrderId { get; }
        public IReadOnlyList<ItemRequest> Items { get; }
        public OrderEntryConfirmed(OrderId orderId, IReadOnlyList<ItemRequest> items)
        {
            OrderId = orderId;
            Items = items;
        }
    }
}
