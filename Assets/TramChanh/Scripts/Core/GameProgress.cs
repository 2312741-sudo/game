using System;

namespace TramChanh.Core
{
    /// <summary>What happened in one finished day.</summary>
    public readonly struct DaySummary
    {
        public DaySummary(int day, int served, int failed)
        {
            Day = day;
            Served = served;
            Failed = failed;
        }

        public int Day { get; }
        public int Served { get; }
        public int Failed { get; }

        /// <summary>Share of orders that were served, 0 to 100. Zero orders counts as 0.</summary>
        public int ServedPercent => Served + Failed == 0 ? 0 : (int)Math.Round(100.0 * Served / (Served + Failed));
    }

    /// <summary>
    /// Day counter and order tallies that survive between runs: current day, lifetime totals and the best day.
    /// Pure C#; saved through <see cref="ISettingsStore"/>.
    /// </summary>
    public sealed class GameProgress
    {
        public const string DayKey = "progress.day";
        public const string TotalServedKey = "progress.total_served";
        public const string TotalFailedKey = "progress.total_failed";
        public const string BestServedKey = "progress.best_served";

        public int Day { get; private set; } = 1;
        public int ServedToday { get; private set; }
        public int FailedToday { get; private set; }
        public int TotalServed { get; private set; }
        public int TotalFailed { get; private set; }
        public int BestServedInADay { get; private set; }

        public void RecordServed() { ServedToday++; }
        public void RecordFailed() { FailedToday++; }

        /// <summary>Closes the current day, folds it into the totals, starts the next day and returns what happened.</summary>
        public DaySummary EndDay()
        {
            var summary = new DaySummary(Day, ServedToday, FailedToday);
            TotalServed += ServedToday;
            TotalFailed += FailedToday;
            if (ServedToday > BestServedInADay) { BestServedInADay = ServedToday; }
            Day++;
            ServedToday = 0;
            FailedToday = 0;
            return summary;
        }

        public void Reset()
        {
            Day = 1;
            ServedToday = 0;
            FailedToday = 0;
            TotalServed = 0;
            TotalFailed = 0;
            BestServedInADay = 0;
        }

        /// <summary>Loads the saved totals. Today's partial counts are not saved: a day always starts fresh.</summary>
        public void Load(ISettingsStore store)
        {
            if (store == null) { throw new ArgumentNullException(nameof(store)); }
            Reset();
            Day = Math.Max(1, ReadInt(store, DayKey, 1));
            TotalServed = Math.Max(0, ReadInt(store, TotalServedKey, 0));
            TotalFailed = Math.Max(0, ReadInt(store, TotalFailedKey, 0));
            BestServedInADay = Math.Max(0, ReadInt(store, BestServedKey, 0));
        }

        public void Save(ISettingsStore store)
        {
            if (store == null) { throw new ArgumentNullException(nameof(store)); }
            store.SetFloat(DayKey, Day);
            store.SetFloat(TotalServedKey, TotalServed);
            store.SetFloat(TotalFailedKey, TotalFailed);
            store.SetFloat(BestServedKey, BestServedInADay);
        }

        private static int ReadInt(ISettingsStore store, string key, int fallback)
        {
            if (!store.TryGetFloat(key, out float value) || float.IsNaN(value) || float.IsInfinity(value)) { return fallback; }
            return (int)Math.Round(value);
        }
    }
}
