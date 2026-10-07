namespace TramChanh.Orders
{
    public readonly struct OrderItemStatusChanged
    {
        public OrderItemRef Item { get; }
        public OrderItemStatus Status { get; }
        public OrderItemStatusChanged(OrderItemRef item, OrderItemStatus status) { Item = item; Status = status; }
    }
}
