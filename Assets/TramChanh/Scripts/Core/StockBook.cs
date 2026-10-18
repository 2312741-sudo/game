using System;
using System.Collections.Generic;

namespace TramChanh.Core
{
    public enum Ingredient { Ice = 0, Jelly = 1, Batter = 2, Wrap = 3 }

    /// <summary>What one finished item eats from the stock.</summary>
    public static class IngredientUse
    {
        /// <summary>Appends what one item of this kind needs to <paramref name="into"/> (ingredient, amount).</summary>
        public static void Add(ItemKind kind, IDictionary<Ingredient, int> into)
        {
            if (into == null) { throw new ArgumentNullException(nameof(into)); }
            if (kind == ItemKind.Cake)
            {
                Bump(into, Ingredient.Batter, 1);
                Bump(into, Ingredient.Wrap, 1);
            }
            else
            {
                Bump(into, Ingredient.Ice, 1);
                Bump(into, Ingredient.Jelly, 1);
            }
        }

        public static string DisplayName(Ingredient ingredient)
        {
            switch (ingredient)
            {
                case Ingredient.Ice: return "Đá";
                case Ingredient.Jelly: return "Thạch";
                case Ingredient.Batter: return "Bột bánh";
                default: return "Giấy gói";
            }
        }

        private static void Bump(IDictionary<Ingredient, int> map, Ingredient ingredient, int amount)
        {
            map.TryGetValue(ingredient, out int current);
            map[ingredient] = current + amount;
        }
    }

    /// <summary>
    /// Ingredient levels of the stall. Pure C#, saved through <see cref="ISettingsStore"/>.
    /// Every level stays between 0 and its capacity.
    /// </summary>
    public sealed class StockBook
    {
        public const string KeyPrefix = "stock.";
        private static readonly Ingredient[] All = { Ingredient.Ice, Ingredient.Jelly, Ingredient.Batter, Ingredient.Wrap };
        private readonly int[] _level = new int[4];
        private readonly int[] _capacity;

        public StockBook(int iceCapacity = 40, int jellyCapacity = 40, int batterCapacity = 30, int wrapCapacity = 40)
        {
            _capacity = new[] { Math.Max(1, iceCapacity), Math.Max(1, jellyCapacity), Math.Max(1, batterCapacity), Math.Max(1, wrapCapacity) };
        }

        public static IReadOnlyList<Ingredient> Ingredients => All;
        public int Level(Ingredient ingredient) => _level[(int)ingredient];
        public int Capacity(Ingredient ingredient) => _capacity[(int)ingredient];
        public int Space(Ingredient ingredient) => Capacity(ingredient) - Level(ingredient);

        /// <summary>The first stock of day one.</summary>
        public void FillStart()
        {
            _level[(int)Ingredient.Ice] = Math.Min(Capacity(Ingredient.Ice), 30);
            _level[(int)Ingredient.Jelly] = Math.Min(Capacity(Ingredient.Jelly), 30);
            _level[(int)Ingredient.Batter] = Math.Min(Capacity(Ingredient.Batter), 20);
            _level[(int)Ingredient.Wrap] = Math.Min(Capacity(Ingredient.Wrap), 30);
        }

        /// <summary>Adds up to <paramref name="amount"/>, never above capacity. Returns how much was really stored.</summary>
        public int Add(Ingredient ingredient, int amount)
        {
            if (amount <= 0) { return 0; }
            int stored = Math.Min(amount, Space(ingredient));
            _level[(int)ingredient] += stored;
            return stored;
        }

        public bool Has(IReadOnlyDictionary<Ingredient, int> need)
        {
            foreach (KeyValuePair<Ingredient, int> pair in need)
            {
                if (pair.Value > Level(pair.Key)) { return false; }
            }
            return true;
        }

        /// <summary>Takes everything or nothing. Returns false and changes nothing when anything is short.</summary>
        public bool TryTake(IReadOnlyDictionary<Ingredient, int> need)
        {
            if (need == null || !Has(need)) { return false; }
            foreach (KeyValuePair<Ingredient, int> pair in need)
            {
                if (pair.Value > 0) { _level[(int)pair.Key] -= pair.Value; }
            }
            return true;
        }

        /// <summary>The first ingredient the stock cannot cover, or null when all is there.</summary>
        public Ingredient? FirstShort(IReadOnlyDictionary<Ingredient, int> need)
        {
            foreach (KeyValuePair<Ingredient, int> pair in need)
            {
                if (pair.Value > Level(pair.Key)) { return pair.Key; }
            }
            return null;
        }

        /// <summary>Ice melts overnight; the rest keeps.</summary>
        public void Overnight() { _level[(int)Ingredient.Ice] = 0; }

        public void Load(ISettingsStore store)
        {
            if (store == null) { throw new ArgumentNullException(nameof(store)); }
            bool any = false;
            foreach (Ingredient ingredient in All)
            {
                if (store.TryGetFloat(KeyPrefix + ingredient, out float value) && !float.IsNaN(value) && !float.IsInfinity(value))
                {
                    _level[(int)ingredient] = Math.Min(Capacity(ingredient), Math.Max(0, (int)Math.Round(value)));
                    any = true;
                }
            }
            if (!any) { FillStart(); }
        }

        public void Save(ISettingsStore store)
        {
            if (store == null) { throw new ArgumentNullException(nameof(store)); }
            foreach (Ingredient ingredient in All) { store.SetFloat(KeyPrefix + ingredient, Level(ingredient)); }
        }
    }

    /// <summary>The wholesaler: sells ingredients in packs for money from the wallet.</summary>
    public sealed class Supplier
    {
        public const int PackSize = 10;

        /// <summary>Money for one unit. First guesses, tuned in one place.</summary>
        public int UnitPrice(Ingredient ingredient)
        {
            switch (ingredient)
            {
                case Ingredient.Ice: return 500;
                case Ingredient.Jelly: return 1000;
                case Ingredient.Batter: return 2000;
                default: return 500;
            }
        }

        public int PackPrice(Ingredient ingredient) => UnitPrice(ingredient) * PackSize;

        /// <summary>Buys one pack: needs money and room for the whole pack. Returns false and changes nothing otherwise.</summary>
        public bool TryBuyPack(StockBook stock, GameProgress wallet, Ingredient ingredient)
        {
            if (stock == null) { throw new ArgumentNullException(nameof(stock)); }
            if (wallet == null) { throw new ArgumentNullException(nameof(wallet)); }
            if (stock.Space(ingredient) < PackSize) { return false; }
            if (!wallet.TrySpend(PackPrice(ingredient))) { return false; }
            stock.Add(ingredient, PackSize);
            return true;
        }
    }
}
