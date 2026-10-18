using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class StockBookTests
    {
        private sealed class MemoryStore : ISettingsStore
        {
            public readonly Dictionary<string, float> Values = new Dictionary<string, float>();
            public bool TryGetFloat(string key, out float value) => Values.TryGetValue(key, out value);
            public void SetFloat(string key, float value) { Values[key] = value; }
        }

        private static Dictionary<Ingredient, int> NeedFor(params ItemKind[] kinds)
        {
            var need = new Dictionary<Ingredient, int>();
            foreach (ItemKind kind in kinds) { IngredientUse.Add(kind, need); }
            return need;
        }

        [Test]
        public void DrinkUsesIceAndJelly_CakeUsesBatterAndWrap()
        {
            Dictionary<Ingredient, int> drink = NeedFor(ItemKind.Drink);
            Assert.That(drink[Ingredient.Ice], Is.EqualTo(1));
            Assert.That(drink[Ingredient.Jelly], Is.EqualTo(1));
            Assert.That(drink.ContainsKey(Ingredient.Batter), Is.False);
            Dictionary<Ingredient, int> both = NeedFor(ItemKind.Cake, ItemKind.Drink, ItemKind.Drink);
            Assert.That(both[Ingredient.Ice], Is.EqualTo(2));
            Assert.That(both[Ingredient.Batter], Is.EqualTo(1));
            Assert.That(both[Ingredient.Wrap], Is.EqualTo(1));
        }

        [Test]
        public void FillStart_GivesTheDayOneStock()
        {
            var stock = new StockBook();
            stock.FillStart();
            Assert.That(stock.Level(Ingredient.Ice), Is.EqualTo(30));
            Assert.That(stock.Level(Ingredient.Batter), Is.EqualTo(20));
        }

        [Test]
        public void TryTake_IsAllOrNothing()
        {
            var stock = new StockBook();
            stock.Add(Ingredient.Ice, 1);
            stock.Add(Ingredient.Jelly, 5);
            Dictionary<Ingredient, int> two = NeedFor(ItemKind.Drink, ItemKind.Drink);
            Assert.That(stock.TryTake(two), Is.False);
            Assert.That(stock.Level(Ingredient.Ice), Is.EqualTo(1));
            Assert.That(stock.Level(Ingredient.Jelly), Is.EqualTo(5), "Nothing was taken when ice was short.");
            Assert.That(stock.FirstShort(two), Is.EqualTo(Ingredient.Ice));
            Assert.That(stock.TryTake(NeedFor(ItemKind.Drink)), Is.True);
            Assert.That(stock.Level(Ingredient.Ice), Is.EqualTo(0));
            Assert.That(stock.Level(Ingredient.Jelly), Is.EqualTo(4));
        }

        [Test]
        public void Add_NeverGoesAboveCapacity_AndIgnoresNonPositive()
        {
            var stock = new StockBook(iceCapacity: 10);
            Assert.That(stock.Add(Ingredient.Ice, 25), Is.EqualTo(10));
            Assert.That(stock.Level(Ingredient.Ice), Is.EqualTo(10));
            Assert.That(stock.Add(Ingredient.Ice, 1), Is.EqualTo(0));
            Assert.That(stock.Add(Ingredient.Jelly, -4), Is.EqualTo(0));
            Assert.That(stock.Level(Ingredient.Jelly), Is.EqualTo(0));
        }

        [Test]
        public void Overnight_MeltsTheIceOnly()
        {
            var stock = new StockBook();
            stock.FillStart();
            stock.Overnight();
            Assert.That(stock.Level(Ingredient.Ice), Is.EqualTo(0));
            Assert.That(stock.Level(Ingredient.Jelly), Is.EqualTo(30));
        }

        [Test]
        public void SaveThenLoad_RoundTrips_AndEmptyStoreStartsFull()
        {
            var store = new MemoryStore();
            var stock = new StockBook();
            stock.Add(Ingredient.Batter, 7);
            stock.Save(store);
            var loaded = new StockBook();
            loaded.Load(store);
            Assert.That(loaded.Level(Ingredient.Batter), Is.EqualTo(7));
            Assert.That(loaded.Level(Ingredient.Ice), Is.EqualTo(0));

            var fresh = new StockBook();
            fresh.Load(new MemoryStore());
            Assert.That(fresh.Level(Ingredient.Ice), Is.EqualTo(30), "No saved data means the day-one stock.");
        }

        [Test]
        public void Load_ClampsBadValues()
        {
            var store = new MemoryStore();
            store.Values[StockBook.KeyPrefix + Ingredient.Ice] = 9999f;
            store.Values[StockBook.KeyPrefix + Ingredient.Jelly] = -5f;
            store.Values[StockBook.KeyPrefix + Ingredient.Batter] = float.NaN;
            var stock = new StockBook(iceCapacity: 40);
            stock.Load(store);
            Assert.That(stock.Level(Ingredient.Ice), Is.EqualTo(40));
            Assert.That(stock.Level(Ingredient.Jelly), Is.EqualTo(0));
            Assert.That(stock.Level(Ingredient.Batter), Is.EqualTo(0));
        }

        [Test]
        public void Supplier_SellsAPackOnlyWithMoneyAndRoom()
        {
            var supplier = new Supplier();
            var stock = new StockBook(iceCapacity: 40);
            var wallet = new GameProgress();
            Assert.That(supplier.TryBuyPack(stock, wallet, Ingredient.Ice), Is.False, "No money.");
            wallet.RecordSale(new Sale(5000, QualityTier.Good, 80));
            Assert.That(supplier.PackPrice(Ingredient.Ice), Is.EqualTo(5000));
            Assert.That(supplier.TryBuyPack(stock, wallet, Ingredient.Ice), Is.True);
            Assert.That(stock.Level(Ingredient.Ice), Is.EqualTo(10));
            Assert.That(wallet.Money, Is.EqualTo(0));

            var full = new StockBook(iceCapacity: 12);
            full.Add(Ingredient.Ice, 5);
            wallet.RecordSale(new Sale(5000, QualityTier.Good, 80));
            Assert.That(supplier.TryBuyPack(full, wallet, Ingredient.Ice), Is.False, "Not enough room for a whole pack.");
            Assert.That(wallet.Money, Is.EqualTo(5000), "No money is taken when the pack does not fit.");
        }
    }
}
