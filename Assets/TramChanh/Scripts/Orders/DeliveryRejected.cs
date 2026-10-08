using TramChanh.Core;

namespace TramChanh.Orders
{
    public readonly struct DeliveryRejected
    {
        public OrderId Order { get; }
        public DeliveryTarget AttemptedTarget { get; }
        public string ReasonKey { get; }
        public DeliveryRejected(OrderId order, DeliveryTarget attemptedTarget, string reasonKey)
        { Order = order; AttemptedTarget = attemptedTarget; ReasonKey = reasonKey; }
    }
}
