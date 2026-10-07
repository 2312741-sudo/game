using TramChanh.Core;

namespace TramChanh.Orders
{
    public readonly struct OrderEntryRequested
    {
        public OrderId OrderId { get; }
        public OrderEntryRequested(OrderId orderId) { OrderId = orderId; }
    }
}
