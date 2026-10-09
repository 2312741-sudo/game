namespace TramChanh.Customers
{
    /// <summary>Plain C# event: an arrival picked a seat but the Lobby point refused the request; the seat stays free.</summary>
    public readonly struct CustomerRequestFailedEvent
    {
        public CustomerRequestFailedEvent(int seatIndex, string reasonKey)
        {
            SeatIndex = seatIndex;
            ReasonKey = reasonKey;
        }

        public int SeatIndex { get; }
        public string ReasonKey { get; }
    }
}
