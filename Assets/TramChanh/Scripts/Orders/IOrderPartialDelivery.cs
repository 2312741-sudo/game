using TramChanh.Core;

namespace TramChanh.Orders
{
    /// <summary>
    /// Additive delivery capability: hand over the items the Lobby took off the shelf while the rest of the order is still being
    /// made. The order stays open; the remaining items are delivered the normal way (whole pickup, deliver, complete).
    /// </summary>
    public interface IOrderPartialDelivery
    {
        Result DeliverPartial(OrderId order, ActorRef actor, DeliveryTarget target);
    }
}
