using TramChanh.Core;

namespace TramChanh.Orders
{
    /// <summary>Lobby delivery capability, separate from intake and preparation.</summary>
    public interface IOrderDelivery
    {
        Result Deliver(OrderId order, ActorRef actor, DeliveryTarget target);
        Result Complete(OrderId order);
    }
}
