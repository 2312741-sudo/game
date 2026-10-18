using System;

namespace TramChanh.UI.Hud
{
    public enum PatienceLevel { Calm, Warning, Danger }

    /// <summary>Engine-free look of a patience bar: how full it is and which colour band it is in.</summary>
    public readonly struct PatienceBar
    {
        public const double WarningBelow = 0.5d;
        public const double DangerBelow = 0.25d;

        private PatienceBar(bool visible, double fill, PatienceLevel level)
        {
            IsVisible = visible;
            Fill = fill;
            Level = level;
        }

        public bool IsVisible { get; }
        /// <summary>0 (about to leave) to 1 (just arrived).</summary>
        public double Fill { get; }
        public PatienceLevel Level { get; }
        public float WidthPercent => (float)(Fill * 100d);

        /// <summary>A negative or NaN value means the order has no patience tracking, so the bar is hidden.</summary>
        public static PatienceBar For(double left)
        {
            if (double.IsNaN(left) || double.IsInfinity(left) || left < 0d) { return new PatienceBar(false, 0d, PatienceLevel.Calm); }
            double fill = Math.Min(1d, left);
            PatienceLevel level = fill < DangerBelow ? PatienceLevel.Danger : (fill < WarningBelow ? PatienceLevel.Warning : PatienceLevel.Calm);
            return new PatienceBar(true, fill, level);
        }
    }
}
