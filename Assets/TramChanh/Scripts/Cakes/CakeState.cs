namespace TramChanh.Cakes
{
    public enum CakeState { Waiting, BatterMeasured, BatterPoured, Cooking, Cooked, Flipped, Cut, Sauced, Rolled, Wrapped, Ready, Delivered, Ruined }
    public enum BatterMeasureResult { WithinTolerance, Under, Over }
    public enum GrillState { Off, Preheating, Ready, Open, Cooking, Finished, Overcooked }
}
