namespace TramChanh.Orders
{
    public readonly struct OrderDeliveryInfo
    {
        public int DeliveryAttempts { get; }
        public int QualityScore { get; }
        public OrderDeliveryInfo(int deliveryAttempts, int qualityScore)
        { DeliveryAttempts = deliveryAttempts; QualityScore = qualityScore; }
    }
}
