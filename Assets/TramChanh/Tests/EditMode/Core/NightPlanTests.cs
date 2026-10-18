using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class NightPlanTests
    {
        private sealed class MemoryStore : ISettingsStore
        {
            public readonly Dictionary<string, float> Values = new Dictionary<string, float>();
            public bool TryGetFloat(string key, out float value) => Values.TryGetValue(key, out value);
            public void SetFloat(string key, float value) { Values[key] = value; }
        }

        [Test]
        public void FirstTwoNights_AreAlwaysClear()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                Assert.That(NightPlan.ForDay(1, seed), Is.EqualTo(NightEvent.Clear));
                Assert.That(NightPlan.ForDay(2, seed), Is.EqualTo(NightEvent.Clear));
            }
        }

        [Test]
        public void SameDayAndSeed_GiveTheSameEvent_AndInspectorNeverComesBeforeDayFour()
        {
            for (int day = 3; day < 200; day++)
            {
                Assert.That(NightPlan.ForDay(day, 5), Is.EqualTo(NightPlan.ForDay(day, 5)));
            }
            for (int seed = 0; seed < 200; seed++) { Assert.That(NightPlan.ForDay(3, seed), Is.Not.EqualTo(NightEvent.Inspector)); }
        }

        [Test]
        public void LaterNights_ShowVariety()
        {
            var seen = new HashSet<NightEvent>();
            for (int day = 3; day < 300; day++) { seen.Add(NightPlan.ForDay(day, 1)); }
            Assert.That(seen.Count, Is.EqualTo(6));
        }

        [Test]
        public void Rain_SlowsAndFestivalRushesOnlyInItsWindow()
        {
            Assert.That(NightPlan.ArrivalScaleAt(NightEvent.Rain, 19.0), Is.GreaterThan(1d));
            Assert.That(NightPlan.ArrivalScaleAt(NightEvent.HotNight, 19.0), Is.LessThan(1d));
            Assert.That(NightPlan.ArrivalScaleAt(NightEvent.Festival, 20.5), Is.EqualTo(0.4).Within(1e-9));
            Assert.That(NightPlan.ArrivalScaleAt(NightEvent.Festival, 21.0), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(NightPlan.ArrivalScaleAt(NightEvent.Clear, 20.5), Is.EqualTo(1.0));
        }

        [Test]
        public void PowerCut_BlocksArrivalsForTwentyMinutes()
        {
            Assert.That(NightPlan.ArrivalsBlocked(NightEvent.PowerCut, 20.99), Is.False);
            Assert.That(NightPlan.ArrivalsBlocked(NightEvent.PowerCut, 21.0), Is.True);
            Assert.That(NightPlan.ArrivalsBlocked(NightEvent.PowerCut, 21.33), Is.True);
            Assert.That(NightPlan.ArrivalsBlocked(NightEvent.PowerCut, 21.34), Is.False);
            Assert.That(NightPlan.ArrivalsBlocked(NightEvent.Rain, 21.1), Is.False);
        }

        [Test]
        public void HotNight_DoublesIce()
        {
            Assert.That(NightPlan.IceUsePerDrink(NightEvent.HotNight), Is.EqualTo(2));
            Assert.That(NightPlan.IceUsePerDrink(NightEvent.Clear), Is.EqualTo(1));
        }

        [Test]
        public void Inspector_RewardsAGoodDay_AndFinesABadOne()
        {
            NightPlan.Inspect(new DaySummary(4, 9, 1, 0, 9 * 90, 3), out int money, out int points);
            Assert.That(money, Is.EqualTo(NightPlan.InspectorReward));
            Assert.That(points, Is.EqualTo(30));
            NightPlan.Inspect(new DaySummary(4, 3, 7, 0, 3 * 90, 0), out money, out points);
            Assert.That(money, Is.EqualTo(-NightPlan.InspectorFineMax));
            Assert.That(points, Is.EqualTo(-20));
            NightPlan.Inspect(new DaySummary(4, 0, 0), out money, out _);
            Assert.That(money, Is.LessThan(0), "An empty day does not pass.");
        }

        [Test]
        public void NamesAndAnnouncementsExistForEveryEvent()
        {
            foreach (NightEvent night in new[] { NightEvent.Clear, NightEvent.Rain, NightEvent.HotNight, NightEvent.PowerCut, NightEvent.Festival, NightEvent.Inspector })
            {
                Assert.That(NightPlan.Name(night), Is.Not.Empty);
                Assert.That(NightPlan.Announcement(night), Is.Not.Empty);
            }
        }

        [Test]
        public void Goal_GrowsWithTheDay_ScalesWithDifficulty_AndChecksTheDay()
        {
            Assert.That(DailyGoal.ForDay(1, Difficulty.Normal).ServedTarget, Is.EqualTo(6));
            Assert.That(DailyGoal.ForDay(5, Difficulty.Normal).ServedTarget, Is.EqualTo(10));
            Assert.That(DailyGoal.ForDay(500, Difficulty.Normal).ServedTarget, Is.EqualTo(20));
            Assert.That(DailyGoal.ForDay(1, Difficulty.Chill).ServedTarget, Is.EqualTo(5));
            Assert.That(DailyGoal.ForDay(1, Difficulty.Rush).ServedTarget, Is.EqualTo(8));
            DailyGoal goal = DailyGoal.ForDay(1, Difficulty.Normal);
            Assert.That(goal.IsMet(new DaySummary(1, 6, 1)), Is.True);
            Assert.That(goal.IsMet(new DaySummary(1, 5, 0)), Is.False, "Too few served.");
            Assert.That(goal.IsMet(new DaySummary(1, 6, 6)), Is.False, "Too many customers left.");
            Assert.That(goal.Describe(), Does.Contain("6 khách"));
        }

        [Test]
        public void GoalBonus_IsCapped()
        {
            Assert.That(DailyGoal.ForDay(1, Difficulty.Normal).BonusMoney, Is.EqualTo(25000));
            Assert.That(DailyGoal.ForDay(1000, Difficulty.Normal).BonusMoney, Is.EqualTo(100000));
        }

        [Test]
        public void Difficulty_CyclesAndSaves()
        {
            Assert.That(Difficulties.Next(Difficulty.Chill), Is.EqualTo(Difficulty.Normal));
            Assert.That(Difficulties.Next(Difficulty.Rush), Is.EqualTo(Difficulty.Chill));
            var store = new MemoryStore();
            Assert.That(Difficulties.Load(store), Is.EqualTo(Difficulty.Normal));
            Difficulties.Save(store, Difficulty.Rush);
            Assert.That(Difficulties.Load(store), Is.EqualTo(Difficulty.Rush));
            store.Values[Difficulties.Key] = 77f;
            Assert.That(Difficulties.Load(store), Is.EqualTo(Difficulty.Normal));
            Assert.That(Difficulties.Profile(Difficulty.Rush).ArrivalIntervalScale, Is.LessThan(1d));
            Assert.That(Difficulties.Profile(Difficulty.Chill).PatienceScale, Is.GreaterThan(1d));
        }
    }
}
