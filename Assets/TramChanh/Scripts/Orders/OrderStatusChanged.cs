using TramChanh.Core;

namespace TramChanh.Orders
{
    public readonly struct OrderStatusChanged
    {
        public OrderId OrderId { get; }
        public OrderStatus Status { get; }
        public OrderStatusChanged(OrderId orderId, OrderStatus status) { OrderId = orderId; Status = status; }
    }
}
