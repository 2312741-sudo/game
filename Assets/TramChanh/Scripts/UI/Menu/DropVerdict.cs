namespace TramChanh.UI.Menu
{
    /// <summary>Why a drop is accepted or refused. Only Valid buys something.</summary>
    public enum DropVerdict { Valid = 0, OutsideZone = 1, NoMoney = 2, StockFull = 3, NeedsStars = 4, AlreadyOwned = 5 }
}
