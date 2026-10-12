using TramChanh.Core;

namespace TramChanh.Customers
{
    /// <summary>Plain C# visual event: a customer sat down and their order was accepted by the Lobby point.</summary>
    public readonly struct CustomerSeatedEvent
    {
        public CustomerSeatedEvent(int seatIndex, CustomerId customer, OrderId order)
        {
            SeatIndex = seatIndex;
            Customer = customer;
            Order = order;
        }

        public int SeatIndex { get; }
        public CustomerId Customer { get; }
        public OrderId Order { get; }
    }
}
