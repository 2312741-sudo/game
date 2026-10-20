using System;

namespace TramChanh.Core
{
    /// <summary>
    /// A streak of well-served customers within one night. A sale counts toward the streak when it is not Poor and the customer
    /// still had at least a third of their patience left. Each further sale in the streak adds 5% to its price, up to 25%.
    /// A Poor or slow sale, or a customer who leaves, ends the streak.
    /// </summary>
    public sealed class ServiceCombo
    {
        public const double GoodFraction = 1d / 3d;
        public const int StepPercent = 5;
        public const int MaxPercent = 25;

        public int Streak { get; private set; }
        public int BestStreak { get; private set; }

        /// <summary>Percent added to the next sale if it keeps the streak.</summary>
        public int BonusPercentFor(int streak) => Math.Min(MaxPercent, Math.Max(0, streak - 1) * StepPercent);

        /// <summary>Records a sale and returns the extra money earned by the streak (0 when the sale breaks it).</summary>
        public int OnSale(Sale sale, double patienceLeftFraction)
        {
            bool good = sale.Tier != QualityTier.Poor && patienceLeftFraction >= GoodFraction;
            if (!good)
            {
                Streak = 0;
                return 0;
            }
            Streak++;
            if (Streak > BestStreak) { BestStreak = Streak; }
            int percent = BonusPercentFor(Streak);
            return (int)(Math.Round(sale.Amount * percent / 100d / PriceList.RoundTo, MidpointRounding.AwayFromZero) * PriceList.RoundTo);
        }

        public void OnFailed() { Streak = 0; }

        public void NewNight()
        {
            Streak = 0;
            BestStreak = 0;
        }
    }
}
