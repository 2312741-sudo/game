using TramChanh.Core;

namespace TramChanh.Orders
{
    public readonly struct OrderSendRequested
    {
        public OrderId OrderId { get; }
        public OrderSendRequested(OrderId orderId) { OrderId = orderId; }
    }
}
