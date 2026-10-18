using System;
using System.Collections.Generic;
using System.Globalization;

namespace TramChanh.Core
{
    public enum QualityTier { Poor = 0, Good = 1, Perfect = 2 }

    /// <summary>What one finished order earned.</summary>
    public readonly struct Sale
    {
        public Sale(int amount, QualityTier tier, int quality)
        {
            Amount = amount;
            Tier = tier;
            Quality = quality;
        }

        public int Amount { get; }
        public QualityTier Tier { get; }
        public int Quality { get; }
    }

    /// <summary>
    /// Prices and quality tiers. Pure C#. The numbers are first guesses (design doc GAMEPLAY_DESIGN.md, section 4)
    /// and are meant to be tuned in one place.
    /// </summary>
    public sealed class PriceList
    {
        public const int RoundTo = 500;

        public int DrinkPrice { get; }
        public int CakePrice { get; }
        /// <summary>Discount in percent when one order holds both a drink and a cake.</summary>
        public int ComboDiscountPercent { get; }
        /// <summary>Quality below this is Poor.</summary>
        public int PoorBelow { get; }
        /// <summary>Quality at or above this is Perfect.</summary>
        public int PerfectFrom { get; }
        public float PoorMultiplier { get; }
        public float GoodMultiplier { get; }
        public float PerfectMultiplier { get; }
        /// <summary>Bonus in percent for an order served while at least <see cref="FastLeftFraction"/> of the patience was left.</summary>
        public int SpeedBonusPercent { get; }
        public double FastLeftFraction { get; }

        public PriceList(int drinkPrice = 25000, int cakePrice = 20000, int comboDiscountPercent = 10,
            int poorBelow = 60, int perfectFrom = 90,
            float poorMultiplier = 0.6f, float goodMultiplier = 1.0f, float perfectMultiplier = 1.3f,
            int speedBonusPercent = 15, double fastLeftFraction = 2d / 3d)
        {
            DrinkPrice = Math.Max(0, drinkPrice);
            CakePrice = Math.Max(0, cakePrice);
            ComboDiscountPercent = Math.Min(100, Math.Max(0, comboDiscountPercent));
            PoorBelow = poorBelow;
            PerfectFrom = Math.Max(poorBelow, perfectFrom);
            PoorMultiplier = poorMultiplier;
            GoodMultiplier = goodMultiplier;
            PerfectMultiplier = perfectMultiplier;
            SpeedBonusPercent = Math.Max(0, speedBonusPercent);
            FastLeftFraction = fastLeftFraction;
        }

        public int BasePrice(ItemKind kind) => kind == ItemKind.Cake ? CakePrice : DrinkPrice;

        public QualityTier TierFor(int quality)
        {
            if (quality < PoorBelow) { return QualityTier.Poor; }
            return quality >= PerfectFrom ? QualityTier.Perfect : QualityTier.Good;
        }

        public float MultiplierFor(QualityTier tier)
        {
            switch (tier)
            {
                case QualityTier.Poor: return PoorMultiplier;
                case QualityTier.Perfect: return PerfectMultiplier;
                default: return GoodMultiplier;
            }
        }

        /// <summary>Price of one delivered order. An empty order is worth nothing. Quality is clamped to 0..100.</summary>
        public Sale Price(IReadOnlyList<ItemKind> items, int quality)
        {
            return Price(items, quality, 0d, 1f);
        }

        /// <summary>
        /// Price with the waiting bonus: <paramref name="patienceLeft"/> is the share of the customer's patience still left
        /// when the order was handed over (0 to 1), <paramref name="payMultiplier"/> is the customer kind's pay factor.
        /// </summary>
        public Sale Price(IReadOnlyList<ItemKind> items, int quality, double patienceLeft, float payMultiplier)
        {
            quality = Math.Min(100, Math.Max(0, quality));
            QualityTier tier = TierFor(quality);
            if (items == null || items.Count == 0) { return new Sale(0, tier, quality); }

            long subtotal = 0;
            bool hasDrink = false;
            bool hasCake = false;
            foreach (ItemKind kind in items)
            {
                subtotal += BasePrice(kind);
                if (kind == ItemKind.Cake) { hasCake = true; } else { hasDrink = true; }
            }
            double amount = subtotal;
            if (hasDrink && hasCake) { amount *= (100 - ComboDiscountPercent) / 100.0; }
            amount *= MultiplierFor(tier);
            if (patienceLeft >= FastLeftFraction) { amount *= 1d + SpeedBonusPercent / 100.0; }
            if (payMultiplier > 0f) { amount *= payMultiplier; }
            // The small epsilon keeps a price such as 25000 * 1.15 (28749.999...) on the side of 28750 before rounding.
            int rounded = (int)(Math.Round(amount / RoundTo + 1e-9, MidpointRounding.AwayFromZero) * RoundTo);
            return new Sale(Math.Max(0, rounded), tier, quality);
        }

        /// <summary>Vietnamese money text, for example 25.000đ.</summary>
        public static string Format(long amount)
        {
            return amount.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', '.') + "đ";
        }
    }
}
