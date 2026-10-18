using System;

namespace TramChanh.Core
{
    /// <summary>What happened in one finished day.</summary>
    public readonly struct DaySummary
    {
        public DaySummary(int day, int served, int failed, int earned = 0, int qualitySum = 0, int perfect = 0)
        {
            Day = day;
            Served = served;
            Failed = failed;
            Earned = earned;
            QualitySum = qualitySum;
            Perfect = perfect;
        }

        public int Day { get; }
        public int Served { get; }
        public int Failed { get; }
        /// <summary>Money earned during the day.</summary>
        public int Earned { get; }
        public int QualitySum { get; }
        /// <summary>Orders delivered at Perfect quality.</summary>
        public int Perfect { get; }

        /// <summary>Mean quality of the served orders, 0 to 100. No served orders gives 0.</summary>
        public int AverageQuality => Served == 0 ? 0 : (int)Math.Round((double)QualitySum / Served);

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
        /// <summary>Money is saved in units of <see cref="PriceList.RoundTo"/> so a float keeps it exact.</summary>
        public const string MoneyKey = "progress.money_units";
        public const string TotalEarnedKey = "progress.total_earned_units";
        public const string BestEarnedKey = "progress.best_earned_units";

        public int Day { get; private set; } = 1;
        public int ServedToday { get; private set; }
        public int FailedToday { get; private set; }
        public int TotalServed { get; private set; }
        public int TotalFailed { get; private set; }
        public int BestServedInADay { get; private set; }
        /// <summary>Cash in hand. Grows with sales; later steps will spend it.</summary>
        public int Money { get; private set; }
        public int EarnedToday { get; private set; }
        public int QualitySumToday { get; private set; }
        public int PerfectToday { get; private set; }
        public int TotalEarned { get; private set; }
        public int BestEarnedInADay { get; private set; }

        public void RecordServed() { ServedToday++; }

        /// <summary>Counts a served order together with what it earned.</summary>
        public void RecordSale(Sale sale)
        {
            ServedToday++;
            EarnedToday += sale.Amount;
            QualitySumToday += sale.Quality;
            if (sale.Tier == QualityTier.Perfect) { PerfectToday++; }
            Money += sale.Amount;
        }

        /// <summary>Takes money out of the cash in hand. Returns false and changes nothing when there is not enough.</summary>
        public bool TrySpend(int amount)
        {
            if (amount < 0 || amount > Money) { return false; }
            Money -= amount;
            return true;
        }
        public void RecordFailed() { FailedToday++; }

        /// <summary>Closes the current day, folds it into the totals, starts the next day and returns what happened.</summary>
        public DaySummary EndDay()
        {
            var summary = new DaySummary(Day, ServedToday, FailedToday, EarnedToday, QualitySumToday, PerfectToday);
            TotalEarned += EarnedToday;
            if (EarnedToday > BestEarnedInADay) { BestEarnedInADay = EarnedToday; }
            TotalServed += ServedToday;
            TotalFailed += FailedToday;
            if (ServedToday > BestServedInADay) { BestServedInADay = ServedToday; }
            Day++;
            ServedToday = 0;
            FailedToday = 0;
            EarnedToday = 0;
            QualitySumToday = 0;
            PerfectToday = 0;
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
            Money = 0;
            EarnedToday = 0;
            QualitySumToday = 0;
            PerfectToday = 0;
            TotalEarned = 0;
            BestEarnedInADay = 0;
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
            Money = Math.Max(0, ReadInt(store, MoneyKey, 0)) * PriceList.RoundTo;
            TotalEarned = Math.Max(0, ReadInt(store, TotalEarnedKey, 0)) * PriceList.RoundTo;
            BestEarnedInADay = Math.Max(0, ReadInt(store, BestEarnedKey, 0)) * PriceList.RoundTo;
        }

        public void Save(ISettingsStore store)
        {
            if (store == null) { throw new ArgumentNullException(nameof(store)); }
            store.SetFloat(DayKey, Day);
            store.SetFloat(TotalServedKey, TotalServed);
            store.SetFloat(TotalFailedKey, TotalFailed);
            store.SetFloat(BestServedKey, BestServedInADay);
            store.SetFloat(MoneyKey, Money / PriceList.RoundTo);
            store.SetFloat(TotalEarnedKey, TotalEarned / PriceList.RoundTo);
            store.SetFloat(BestEarnedKey, BestEarnedInADay / PriceList.RoundTo);
        }

        private static int ReadInt(ISettingsStore store, string key, int fallback)
        {
            if (!store.TryGetFloat(key, out float value) || float.IsNaN(value) || float.IsInfinity(value)) { return fallback; }
            return (int)Math.Round(value);
        }
    }
}
