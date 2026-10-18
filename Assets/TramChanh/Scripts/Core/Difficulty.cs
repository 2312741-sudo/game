using System;

namespace TramChanh.Core
{
    public enum Difficulty { Chill = 0, Normal = 1, Rush = 2 }

    /// <summary>What a difficulty changes. Scales above 1 stretch the value, below 1 shrink it.</summary>
    public readonly struct DifficultyProfile
    {
        public DifficultyProfile(string name, double arrivalIntervalScale, double patienceScale, double goalScale, double supplierPriceScale)
        {
            Name = name;
            ArrivalIntervalScale = arrivalIntervalScale;
            PatienceScale = patienceScale;
            GoalScale = goalScale;
            SupplierPriceScale = supplierPriceScale;
        }

        public string Name { get; }
        /// <summary>Multiplies the time between customers (above 1 is calmer).</summary>
        public double ArrivalIntervalScale { get; }
        public double PatienceScale { get; }
        public double GoalScale { get; }
        public double SupplierPriceScale { get; }
    }

    public static class Difficulties
    {
        public const string Key = "settings.difficulty";

        public static DifficultyProfile Profile(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Chill: return new DifficultyProfile("Thư thả", 1.25, 1.25, 0.8, 0.9);
                case Difficulty.Rush: return new DifficultyProfile("Cao điểm", 0.8, 0.8, 1.3, 1.1);
                default: return new DifficultyProfile("Bình thường", 1.0, 1.0, 1.0, 1.0);
            }
        }

        public static Difficulty Next(Difficulty difficulty) => (Difficulty)(((int)difficulty + 1) % 3);

        public static Difficulty Load(ISettingsStore store)
        {
            if (store == null) { throw new ArgumentNullException(nameof(store)); }
            if (!store.TryGetFloat(Key, out float value) || float.IsNaN(value) || float.IsInfinity(value)) { return Difficulty.Normal; }
            int index = (int)Math.Round(value);
            return index < 0 || index > 2 ? Difficulty.Normal : (Difficulty)index;
        }

        public static void Save(ISettingsStore store, Difficulty difficulty)
        {
            if (store == null) { throw new ArgumentNullException(nameof(store)); }
            store.SetFloat(Key, (int)difficulty);
        }
    }
}
