using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class ReputationTests
    {
        private sealed class MemoryStore : ISettingsStore
        {
            public readonly Dictionary<string, float> Values = new Dictionary<string, float>();
            public bool TryGetFloat(string key, out float value) => Values.TryGetValue(key, out value);
            public void SetFloat(string key, float value) { Values[key] = value; }
        }

        [Test]
        public void StartsWithOneStar()
        {
            var r = new Reputation();
            Assert.That(r.Stars, Is.EqualTo(1));
            Assert.That(r.Points, Is.EqualTo(100));
        }

        [Test]
        public void GoodDay_RaisesAndBadDay_Lowers_WithinLimits()
        {
            var r = new Reputation();
            int up = r.ApplyDay(new DaySummary(1, 10, 0, 300000, 1000, 5)); // served 100 %, quality 100 -> score 100 -> +32
            Assert.That(up, Is.EqualTo(32));
            Assert.That(r.Points, Is.EqualTo(132));
            int down = r.ApplyDay(new DaySummary(2, 0, 10)); // score 0 -> -48 capped to -30
            Assert.That(down, Is.EqualTo(-30));
            Assert.That(r.Points, Is.EqualTo(102));
        }

        [Test]
        public void DayWithoutOrders_ChangesNothing()
        {
            var r = new Reputation();
            Assert.That(r.ApplyDay(new DaySummary(1, 0, 0)), Is.EqualTo(0));
            Assert.That(Reputation.DayScore(new DaySummary(1, 0, 0)), Is.EqualTo(-1));
        }

        [Test]
        public void Points_StayBetweenZeroAndFiveStars()
        {
            var r = new Reputation();
            Assert.That(r.AddPoints(100000), Is.EqualTo(400));
            Assert.That(r.Stars, Is.EqualTo(5));
            Assert.That(r.AddPoints(-100000), Is.EqualTo(-500));
            Assert.That(r.Stars, Is.EqualTo(0));
        }

        [Test]
        public void MoreStars_MeansShorterArrivalInterval()
        {
            var r = new Reputation();
            double one = r.ArrivalFactor;
            r.AddPoints(300);
            Assert.That(r.ArrivalFactor, Is.LessThan(one));
            Assert.That(new Reputation().ArrivalFactor, Is.EqualTo(0.96).Within(1e-9));
        }

        [Test]
        public void StarText_AndSaveLoad()
        {
            Assert.That(Reputation.StarText(3), Is.EqualTo("★★★☆☆"));
            Assert.That(Reputation.StarText(99), Is.EqualTo("★★★★★"));
            var store = new MemoryStore();
            var r = new Reputation();
            r.AddPoints(150);
            r.Save(store);
            var loaded = new Reputation();
            loaded.Load(store);
            Assert.That(loaded.Points, Is.EqualTo(250));
            store.Values[Reputation.PointsKey] = float.NaN;
            loaded.Load(store);
            Assert.That(loaded.Points, Is.EqualTo(Reputation.StartPoints));
        }
    }
}
