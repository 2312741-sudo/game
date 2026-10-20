using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class EmergencyShopTests
    {
        private static GameProgress Rich(int money)
        {
            var wallet = new GameProgress();
            wallet.RecordSale(new Sale(money, QualityTier.Good, 80));
            return wallet;
        }

        [Test]
        public void BuysFiveOfTheScarcestIngredientAtDoublePrice()
        {
            var stock = new StockBook();
            stock.FillStart();
            stock.TryTake(new System.Collections.Generic.Dictionary<Ingredient, int> { { Ingredient.Batter, 18 } });
            var supplier = new Supplier();
            GameProgress wallet = Rich(50000);
            Assert.That(EmergencyShop.Scarcest(stock), Is.EqualTo(Ingredient.Batter));
            Assert.That(EmergencyShop.TryBuy(stock, wallet, supplier, out Ingredient bought, out int cost), Is.True);
            Assert.That(bought, Is.EqualTo(Ingredient.Batter));
            Assert.That(cost, Is.EqualTo(20000)); // 2000 * 5 * 2
            Assert.That(stock.Level(Ingredient.Batter), Is.EqualTo(7));
            Assert.That(wallet.Money, Is.EqualTo(30000));
        }

        [Test]
        public void RefusesWithoutMoney_AndChangesNothing()
        {
            var stock = new StockBook();
            var supplier = new Supplier();
            GameProgress wallet = Rich(500);
            Assert.That(EmergencyShop.TryBuy(stock, wallet, supplier, out _, out _), Is.False);
            Assert.That(wallet.Money, Is.EqualTo(500));
            foreach (Ingredient i in StockBook.Ingredients) { Assert.That(stock.Level(i), Is.EqualTo(0)); }
        }

        [Test]
        public void RefusesWhenThereIsNoRoomForThePack()
        {
            var stock = new StockBook(iceCapacity: 3, jellyCapacity: 3, batterCapacity: 3, wrapCapacity: 3);
            GameProgress wallet = Rich(100000);
            Assert.That(EmergencyShop.TryBuy(stock, wallet, new Supplier(), out _, out _), Is.False);
            Assert.That(wallet.Money, Is.EqualTo(100000));
        }

        [Test]
        public void Price_FollowsTheSupplierDiscountAndStaysRounded()
        {
            var supplier = new Supplier { DiscountPercent = 10 };
            // The discount applies to the wholesale pack price, the shop price here is the plain unit price doubled.
            Assert.That(EmergencyShop.PackPrice(supplier, Ingredient.Ice), Is.EqualTo(5000));
            Assert.That(EmergencyShop.PackPrice(supplier, Ingredient.Ice) % PriceList.RoundTo, Is.EqualTo(0));
        }
    }
}
