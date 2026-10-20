using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class PriceListTests
    {
        private static readonly ItemKind[] Drink = { ItemKind.Drink };
        private static readonly ItemKind[] Cake = { ItemKind.Cake };
        private static readonly ItemKind[] Combo = { ItemKind.Drink, ItemKind.Cake };

        [Test]
        public void Tiers_SplitAtTheConfiguredLimits()
        {
            var prices = new PriceList();
            Assert.That(prices.TierFor(0), Is.EqualTo(QualityTier.Poor));
            Assert.That(prices.TierFor(59), Is.EqualTo(QualityTier.Poor));
            Assert.That(prices.TierFor(60), Is.EqualTo(QualityTier.Good));
            Assert.That(prices.TierFor(89), Is.EqualTo(QualityTier.Good));
            Assert.That(prices.TierFor(90), Is.EqualTo(QualityTier.Perfect));
            Assert.That(prices.TierFor(100), Is.EqualTo(QualityTier.Perfect));
        }

        [Test]
        public void GoodDrinkAndCake_PayTheBasePrice()
        {
            var prices = new PriceList();
            Assert.That(prices.Price(Drink, 75).Amount, Is.EqualTo(20000));
            Assert.That(prices.Price(Cake, 75).Amount, Is.EqualTo(20000));
        }

        [Test]
        public void PoorQuality_PaysLessAndPerfectPaysMore()
        {
            var prices = new PriceList();
            Assert.That(prices.Price(Drink, 20).Amount, Is.EqualTo(12000));   // 20000 * 0.6
            Assert.That(prices.Price(Drink, 100).Amount, Is.EqualTo(26000));  // 20000 * 1.3
        }

        [Test]
        public void Combo_GetsTheDiscountOnlyWhenBothKindsArePresent()
        {
            var prices = new PriceList();
            Assert.That(prices.Price(Combo, 75).Amount, Is.EqualTo(40000), "The real menu has no discount for the basic drink and cake.");
            var withDiscount = new PriceList(comboDiscountPercent: 10);
            Assert.That(withDiscount.Price(Combo, 75).Amount, Is.EqualTo(36000)); // 40000 * 0.9
            Assert.That(withDiscount.Price(new[] { ItemKind.Drink, ItemKind.Drink }, 75).Amount, Is.EqualTo(40000), "No discount without a cake.");
        }

        [Test]
        public void Amount_IsRoundedToFiveHundred()
        {
            var prices = new PriceList(drinkPrice: 24100, cakePrice: 0, comboDiscountPercent: 0);
            Assert.That(prices.Price(Drink, 75).Amount % PriceList.RoundTo, Is.EqualTo(0));
            Assert.That(prices.Price(Drink, 75).Amount, Is.EqualTo(24000));
        }

        [Test]
        public void EmptyOrNullOrder_IsWorthNothing_AndQualityIsClamped()
        {
            var prices = new PriceList();
            Assert.That(prices.Price(new List<ItemKind>(), 80).Amount, Is.EqualTo(0));
            Assert.That(prices.Price(null, 80).Amount, Is.EqualTo(0));
            Assert.That(prices.Price(Drink, 500).Quality, Is.EqualTo(100));
            Assert.That(prices.Price(Drink, -5).Quality, Is.EqualTo(0));
        }

        [Test]
        public void Format_UsesDotsAndDong()
        {
            Assert.That(PriceList.Format(0), Is.EqualTo("0đ"));
            Assert.That(PriceList.Format(25000), Is.EqualTo("25.000đ"));
            Assert.That(PriceList.Format(1234500), Is.EqualTo("1.234.500đ"));
        }

        [Test]
        public void FastService_AddsTheSpeedBonus_AndTheKindMultiplierApplies()
        {
            var prices = new PriceList();
            Assert.That(prices.Price(Drink, 75, 0.9d, 1f).Amount, Is.EqualTo(23000));   // 20000 * 1.15
            Assert.That(prices.Price(Drink, 75, 0.5d, 1f).Amount, Is.EqualTo(20000));   // too slow for the bonus
            Assert.That(prices.Price(Drink, 75, 0.5d, 1.2f).Amount, Is.EqualTo(24000)); // regular customer tip
            Assert.That(prices.Price(Drink, 75).Amount, Is.EqualTo(20000), "The short overload has no bonus.");
        }

        [Test]
        public void EachItemCanCarryItsOwnUnitPrice()
        {
            var prices = new PriceList();
            var order = new[]
            {
                new System.Collections.Generic.KeyValuePair<ItemKind, int>(ItemKind.Drink, 30000),
                new System.Collections.Generic.KeyValuePair<ItemKind, int>(ItemKind.Cake, 20000),
            };
            Assert.That(prices.PriceItems(order, 75, 0d, 1f).Amount, Is.EqualTo(50000));
            Assert.That(MenuBook.UnitPrice(MenuBook.PassionfruitId, ItemKind.Drink, prices), Is.EqualTo(30000));
            Assert.That(MenuBook.UnitPrice("drink.slice", ItemKind.Drink, prices), Is.EqualTo(20000));
            Assert.That(MenuBook.UnitPrice("cake.dev.tbd", ItemKind.Cake, prices), Is.EqualTo(20000));
        }
    }
}
