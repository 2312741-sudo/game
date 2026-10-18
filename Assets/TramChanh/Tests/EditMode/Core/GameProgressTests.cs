using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class GameProgressTests
    {
        private sealed class MemoryStore : ISettingsStore
        {
            public readonly Dictionary<string, float> Values = new Dictionary<string, float>();
            public bool TryGetFloat(string key, out float value) => Values.TryGetValue(key, out value);
            public void SetFloat(string key, float value) { Values[key] = value; }
        }

        [Test]
        public void NewProgress_StartsOnDayOne()
        {
            var p = new GameProgress();
            Assert.That(p.Day, Is.EqualTo(1));
            Assert.That(p.ServedToday, Is.EqualTo(0));
        }

        [Test]
        public void EndDay_ReturnsTodayAndStartsTheNext()
        {
            var p = new GameProgress();
            p.RecordServed(); p.RecordServed(); p.RecordServed(); p.RecordFailed();
            DaySummary s = p.EndDay();
            Assert.That(s.Day, Is.EqualTo(1));
            Assert.That(s.Served, Is.EqualTo(3));
            Assert.That(s.Failed, Is.EqualTo(1));
            Assert.That(s.ServedPercent, Is.EqualTo(75));
            Assert.That(p.Day, Is.EqualTo(2));
            Assert.That(p.ServedToday, Is.EqualTo(0));
            Assert.That(p.FailedToday, Is.EqualTo(0));
            Assert.That(p.TotalServed, Is.EqualTo(3));
            Assert.That(p.TotalFailed, Is.EqualTo(1));
        }

        [Test]
        public void BestDay_KeepsTheHighestServedCount()
        {
            var p = new GameProgress();
            p.RecordServed(); p.RecordServed(); p.EndDay();
            p.RecordServed(); p.EndDay();
            Assert.That(p.BestServedInADay, Is.EqualTo(2));
        }

        [Test]
        public void ServedPercent_IsZeroWithNoOrders()
        {
            Assert.That(new DaySummary(1, 0, 0).ServedPercent, Is.EqualTo(0));
        }

        [Test]
        public void SaveThenLoad_RoundTripsTotalsButNotTodaysPartialCounts()
        {
            var store = new MemoryStore();
            var p = new GameProgress();
            p.RecordServed(); p.RecordServed(); p.RecordFailed(); p.EndDay();
            p.RecordServed();
            p.Save(store);
            var loaded = new GameProgress();
            loaded.Load(store);
            Assert.That(loaded.Day, Is.EqualTo(2));
            Assert.That(loaded.TotalServed, Is.EqualTo(2));
            Assert.That(loaded.TotalFailed, Is.EqualTo(1));
            Assert.That(loaded.BestServedInADay, Is.EqualTo(2));
            Assert.That(loaded.ServedToday, Is.EqualTo(0));
        }

        [Test]
        public void Load_FromEmptyStoreOrBadValues_UsesSafeDefaults()
        {
            var store = new MemoryStore();
            store.Values[GameProgress.DayKey] = -5f;
            store.Values[GameProgress.TotalServedKey] = float.NaN;
            var p = new GameProgress();
            p.Load(store);
            Assert.That(p.Day, Is.EqualTo(1));
            Assert.That(p.TotalServed, Is.EqualTo(0));
        }

        [Test]
        public void RecordSale_AddsMoneyQualityAndPerfectCount()
        {
            var p = new GameProgress();
            p.RecordSale(new Sale(32500, QualityTier.Perfect, 100));
            p.RecordSale(new Sale(15000, QualityTier.Poor, 40));
            Assert.That(p.ServedToday, Is.EqualTo(2));
            Assert.That(p.EarnedToday, Is.EqualTo(47500));
            Assert.That(p.Money, Is.EqualTo(47500));
            DaySummary s = p.EndDay();
            Assert.That(s.Earned, Is.EqualTo(47500));
            Assert.That(s.AverageQuality, Is.EqualTo(70));
            Assert.That(s.Perfect, Is.EqualTo(1));
            Assert.That(p.EarnedToday, Is.EqualTo(0));
            Assert.That(p.Money, Is.EqualTo(47500));
            Assert.That(p.TotalEarned, Is.EqualTo(47500));
            Assert.That(p.BestEarnedInADay, Is.EqualTo(47500));
        }

        [Test]
        public void TrySpend_RefusesMoreThanYouHave()
        {
            var p = new GameProgress();
            p.RecordSale(new Sale(10000, QualityTier.Good, 80));
            Assert.That(p.TrySpend(10500), Is.False);
            Assert.That(p.Money, Is.EqualTo(10000));
            Assert.That(p.TrySpend(4000), Is.True);
            Assert.That(p.Money, Is.EqualTo(6000));
            Assert.That(p.TrySpend(-1), Is.False);
        }

        [Test]
        public void SaveThenLoad_RoundTripsMoney()
        {
            var store = new MemoryStore();
            var p = new GameProgress();
            p.RecordSale(new Sale(40500, QualityTier.Good, 75));
            p.EndDay();
            p.Save(store);
            var loaded = new GameProgress();
            loaded.Load(store);
            Assert.That(loaded.Money, Is.EqualTo(40500));
            Assert.That(loaded.TotalEarned, Is.EqualTo(40500));
            Assert.That(loaded.BestEarnedInADay, Is.EqualTo(40500));
        }

        [Test]
        public void AverageQuality_IsZeroWithNoServedOrders()
        {
            Assert.That(new DaySummary(1, 0, 2).AverageQuality, Is.EqualTo(0));
        }

        [Test]
        public void AddBonus_AddsAndAFineNeverTakesMoreThanTheCash()
        {
            var p = new GameProgress();
            Assert.That(p.AddBonus(50000), Is.EqualTo(50000));
            Assert.That(p.Money, Is.EqualTo(50000));
            Assert.That(p.AddBonus(-20000), Is.EqualTo(-20000));
            Assert.That(p.AddBonus(-999999), Is.EqualTo(-30000));
            Assert.That(p.Money, Is.EqualTo(0));
        }
    }
}
