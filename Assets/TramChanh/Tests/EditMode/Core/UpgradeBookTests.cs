using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class UpgradeBookTests
    {
        private sealed class MemoryStore : ISettingsStore
        {
            public readonly Dictionary<string, float> Values = new Dictionary<string, float>();
            public bool TryGetFloat(string key, out float value) => Values.TryGetValue(key, out value);
            public void SetFloat(string key, float value) { Values[key] = value; }
        }

        private static GameProgress Rich(int money)
        {
            var wallet = new GameProgress();
            wallet.RecordSale(new Sale(money, QualityTier.Good, 80));
            return wallet;
        }

        [Test]
        public void Status_ExplainsWhyAnUpgradeCannotBeBoughtYet()
        {
            var book = new UpgradeBook();
            var rep = new Reputation(); // 1 star
            Assert.That(book.StatusOf(UpgradeId.BiggerIceBin, Rich(0), rep), Is.EqualTo(UpgradeStatus.NeedsMoney));
            Assert.That(book.StatusOf(UpgradeId.BiggerIceBin, Rich(150000), rep), Is.EqualTo(UpgradeStatus.Available));
            Assert.That(book.StatusOf(UpgradeId.StringLights, Rich(999999), rep), Is.EqualTo(UpgradeStatus.NeedsStars));
        }

        [Test]
        public void TryBuy_TakesMoneyOnce_AndAppliesTheEffect()
        {
            var book = new UpgradeBook();
            var rep = new Reputation();
            GameProgress wallet = Rich(200000);
            Assert.That(book.TryBuy(UpgradeId.BiggerIceBin, wallet, rep), Is.True);
            Assert.That(wallet.Money, Is.EqualTo(50000));
            Assert.That(book.IceCapacityBonus, Is.EqualTo(20));
            Assert.That(book.TryBuy(UpgradeId.BiggerIceBin, wallet, rep), Is.False, "Already owned.");
            Assert.That(wallet.Money, Is.EqualTo(50000));
        }

        [Test]
        public void Refused_LeavesMoneyAlone()
        {
            var book = new UpgradeBook();
            GameProgress wallet = Rich(100);
            Assert.That(book.TryBuy(UpgradeId.Pantry, wallet, new Reputation()), Is.False);
            Assert.That(wallet.Money, Is.EqualTo(100));
            Assert.That(book.Owns(UpgradeId.Pantry), Is.False);
        }

        [Test]
        public void Effects_AreOffUntilOwned_AndAllDifferent()
        {
            var book = new UpgradeBook();
            Assert.That(book.PatienceMultiplier, Is.EqualTo(1d));
            Assert.That(book.SupplierDiscountPercent, Is.EqualTo(0));
            Assert.That(book.PriceBonusPercent, Is.EqualTo(0));
            foreach (UpgradeId id in new[] { UpgradeId.StringLights, UpgradeId.WholesaleDeal, UpgradeId.GoldenMenuBoard, UpgradeId.Pantry }) { book.Grant(id); }
            Assert.That(book.PatienceMultiplier, Is.EqualTo(1.15).Within(1e-9));
            Assert.That(book.SupplierDiscountPercent, Is.EqualTo(10));
            Assert.That(book.PriceBonusPercent, Is.EqualTo(10));
            Assert.That(book.OtherCapacityBonus, Is.EqualTo(20));
        }

        [Test]
        public void SaveThenLoad_KeepsTheOwnedOnes()
        {
            var store = new MemoryStore();
            var book = new UpgradeBook();
            book.Grant(UpgradeId.WholesaleDeal);
            book.Save(store);
            var loaded = new UpgradeBook();
            loaded.Load(store);
            Assert.That(loaded.Owns(UpgradeId.WholesaleDeal), Is.True);
            Assert.That(loaded.Owns(UpgradeId.Pantry), Is.False);
        }

        [Test]
        public void Catalogue_HasFiveUpgradesWithRisingStarRequirements()
        {
            Assert.That(UpgradeBook.All.Count, Is.EqualTo(5));
            for (int i = 0; i < UpgradeBook.All.Count; i++) { Assert.That((int)UpgradeBook.All[i].Id, Is.EqualTo(i)); }
            Assert.That(UpgradeBook.Info(UpgradeId.GoldenMenuBoard).RequiredStars, Is.EqualTo(4));
        }

        [Test]
        public void StockCapacity_FollowsUpgradeBonus_AndSupplierDiscountsPacks()
        {
            var stock = new StockBook();
            stock.SetCapacityBonus(20, 20);
            Assert.That(stock.Capacity(Ingredient.Ice), Is.EqualTo(60));
            Assert.That(stock.Capacity(Ingredient.Batter), Is.EqualTo(50));
            var supplier = new Supplier { DiscountPercent = 10 };
            Assert.That(supplier.PackPrice(Ingredient.Ice), Is.EqualTo(4500));
            supplier.PriceScale = 1.1;
            Assert.That(supplier.PackPrice(Ingredient.Ice), Is.EqualTo(5000));
        }

        [Test]
        public void IceUse_CanBeDoubled()
        {
            var need = new Dictionary<Ingredient, int>();
            IngredientUse.Add(ItemKind.Drink, need, 2);
            Assert.That(need[Ingredient.Ice], Is.EqualTo(2));
            Assert.That(need[Ingredient.Jelly], Is.EqualTo(1));
        }
    }
}
