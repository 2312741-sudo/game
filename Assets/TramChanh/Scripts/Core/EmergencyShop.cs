using System;

namespace TramChanh.Core
{
    /// <summary>
    /// The corner shop 40 m away, for when something runs out in the middle of a night: a small pack at double the wholesale price.
    /// Pure C#; the game decides when the button works (open, not paused) and keeps the cooldown.
    /// </summary>
    public static class EmergencyShop
    {
        public const int PackUnits = 5;
        public const double PriceFactor = 2d;
        public const double CooldownSeconds = 30d;

        public static int PackPrice(Supplier supplier, Ingredient ingredient)
        {
            return (int)(Math.Round(supplier.UnitPrice(ingredient) * PackUnits * PriceFactor / PriceList.RoundTo, MidpointRounding.AwayFromZero) * PriceList.RoundTo);
        }

        /// <summary>The ingredient that is the emptiest compared to its capacity.</summary>
        public static Ingredient Scarcest(StockBook stock)
        {
            Ingredient best = StockBook.Ingredients[0];
            double bestRatio = double.MaxValue;
            foreach (Ingredient ingredient in StockBook.Ingredients)
            {
                double ratio = (double)stock.Level(ingredient) / Math.Max(1, stock.Capacity(ingredient));
                if (ratio < bestRatio) { bestRatio = ratio; best = ingredient; }
            }
            return best;
        }

        /// <summary>
        /// Buys <see cref="PackUnits"/> of the scarcest ingredient. False (and nothing changes) when the stock has no room for
        /// the pack or the wallet cannot pay.
        /// </summary>
        public static bool TryBuy(StockBook stock, GameProgress wallet, Supplier supplier, out Ingredient bought, out int cost)
        {
            bought = Scarcest(stock);
            cost = PackPrice(supplier, bought);
            if (stock.Space(bought) < PackUnits) { return false; }
            if (!wallet.TrySpend(cost)) { return false; }
            stock.Add(bought, PackUnits);
            return true;
        }
    }
}
