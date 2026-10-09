using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Customers
{
    /// <summary>One dine-in table a customer can sit at. The takeaway vehicle is not a seat.</summary>
    public interface ICustomerSeat
    {
        /// <summary>Scene table number (1..10), equal to the point's <c>TableId</c>.</summary>
        int TableNumber { get; }

        /// <summary>True while the order served at this table is not terminal (not Completed/Failed).</summary>
        bool HasLiveOrder { get; }

        /// <summary>Places a customer request through the Lobby order point. Never throws on gameplay failure.</summary>
        Result<OrderId> Request(CustomerId customer, IReadOnlyList<ItemRequest> items);
    }
}
