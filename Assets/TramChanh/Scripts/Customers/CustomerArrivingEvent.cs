using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Customers
{
    /// <summary>
    /// Plain C# visual event (walking mode): a seat was reserved and the customer starts walking to it.
    /// No order exists yet; it is requested through the seat when the customer arrives.
    /// </summary>
    public readonly struct CustomerArrivingEvent
    {
        public CustomerArrivingEvent(int seatIndex, CustomerId customer, IReadOnlyList<ItemRequest> items)
        {
            SeatIndex = seatIndex;
            Customer = customer;
            Items = items;
        }

        public int SeatIndex { get; }
        public CustomerId Customer { get; }
        /// <summary>The menu the customer will order on arrival.</summary>
        public IReadOnlyList<ItemRequest> Items { get; }
    }
}
