namespace TramChanh.Orders
{
    public enum OrderStatus
    {
        WaitingForLobby,
        TakingOrder,
        Entered,
        SentToStall,
        InPreparation,
        Ready,
        PickedUpByLobby,
        Delivered,
        Completed,
        Failed
    }
}
