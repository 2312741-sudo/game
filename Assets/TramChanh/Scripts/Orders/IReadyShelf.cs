namespace TramChanh.Orders
{
    /// <summary>Composition-root union; Stall and Lobby receive their narrow interfaces.</summary>
    public interface IReadyShelf : IReadyShelfPlacement, IReadyShelfPickup
    {
    }
}
