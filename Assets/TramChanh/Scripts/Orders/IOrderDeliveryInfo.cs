namespace TramChanh.Orders
{
    public interface IOrderDeliveryInfo
    {
        int DeliveryAttempts { get; }
        int QualityScore { get; }
    }
}
