using System.Collections.Generic;
using TramChanh.Core;

namespace TramChanh.Orders
{
    public readonly struct OrderEntryRequested
    {
        public OrderId OrderId { get; }
        public IReadOnlyList<ItemRequest> RequestedItems { get; }
        public OrderEntryRequested(OrderId orderId, IReadOnlyList<ItemRequest> requestedItems)
        {
            OrderId = orderId;
            RequestedItems = requestedItems;
        }
    }
}
