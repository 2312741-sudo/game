using System.Collections.Generic;
using TramChanh.Core;

namespace TramChanh.Orders
{
    /// <summary>
    /// Additive shelf capability: take the Ready items of an order that still has items in preparation, so a finished drink can
    /// be served while the cake is still on the grill. Separate from <see cref="IReadyShelfPickup"/>, which keeps whole-order pickup.
    /// </summary>
    public interface IReadyShelfPartialPickup
    {
        /// <summary>Oldest order that is still in preparation and has at least one Ready item on the shelf; default when none.</summary>
        OrderId NextPartialOrder { get; }
        Result<IReadOnlyList<IPreparedItem>> PickUpPartial(OrderId orderId, ActorRef actor);
    }
}
