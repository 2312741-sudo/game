using System.Collections.Generic;
using TramChanh.Core;

namespace TramChanh.Orders
{
    public interface IReadyShelfPickup
    {
        OrderId NextReadyOrder { get; }
        // Commit T7 and clear all order slots before publishing; observer faults cannot undo pickup.
        Result<IReadOnlyList<IPreparedItem>> PickUp(OrderId orderId, ActorRef actor);
    }
}
