using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TramChanh.People;

namespace TramChanh.Tests.EditMode.People
{
    /// <summary>PEOPLE-D: ambient crowd spawn budget, timing, determinism and pause-safety.</summary>
    public sealed class CrowdSchedulerTests
    {
        private static readonly string[] Lanes = { "SidewalkNorth", "SidewalkSouth", "Alley", "RoadEast", "RoadWest" };

        private sealed class Recorder
        {
            public readonly List<CrowdSpawn> Spawns = new List<CrowdSpawn>();
            public readonly List<int> Despawns = new List<int>();

            public Recorder(CrowdScheduler s)
            {
                s.Spawned += Spawns.Add;
                s.Despawned += Despawns.Add;
            }
        }

        [Test]
        public void FirstSpawn_HappensAfterOneInterval()
        {
            var s = new CrowdScheduler(Lanes, 5, 2f, 1);
            var r = new Recorder(s);
            s.Tick(1.5d);
            Assert.That(r.Spawns, Is.Empty);
            s.Tick(0.5d);
            Assert.That(r.Spawns.Count, Is.EqualTo(1));
            Assert.That(s.ActiveCount, Is.EqualTo(1));
            Assert.That(r.Spawns[0].AgentId, Is.EqualTo(1));
            Assert.That(Lanes, Does.Contain(r.Spawns[0].Lane));
        }

        [Test]
        public void SpawnsOncePerInterval_WithSmallTicks()
        {
            var s = new CrowdScheduler(Lanes, 100, 1f, 3);
            var r = new Recorder(s);
            for (int i = 0; i < 600; i++)
            {
                s.Tick(1d / 60d); // 10 s total
            }

            Assert.That(r.Spawns.Count, Is.EqualTo(10));
        }

        [Test]
        public void AtMostOneSpawnPerTick_EvenForAHugeDelta()
        {
            var s = new CrowdScheduler(Lanes, 100, 1f, 3);
            var r = new Recorder(s);
            s.Tick(1000d);
            Assert.That(r.Spawns.Count, Is.EqualTo(1));
            s.Tick(1000d);
            Assert.That(r.Spawns.Count, Is.EqualTo(2));
        }

        [Test]
        public void RespectsMaxAgents_AndSpawnsAgainWhenASlotFrees()
        {
            var s = new CrowdScheduler(Lanes, 3, 0.5f, 9);
            var r = new Recorder(s);
            for (int i = 0; i < 100; i++)
            {
                s.Tick(0.5d);
                Assert.That(s.ActiveCount, Is.LessThanOrEqualTo(3));
            }

            Assert.That(r.Spawns.Count, Is.EqualTo(3));
            s.NotifyFinished(r.Spawns[1].AgentId);
            Assert.That(s.ActiveCount, Is.EqualTo(2));
            // The timer was capped while full, so the freed slot is refilled on the next tick, and only once.
            s.Tick(0.01d);
            Assert.That(r.Spawns.Count, Is.EqualTo(4));
            s.Tick(0.01d);
            Assert.That(r.Spawns.Count, Is.EqualTo(4));
            Assert.That(s.ActiveCount, Is.EqualTo(3));
        }

        [Test]
        public void ZeroMaxAgents_NeverSpawns()
        {
            var s = new CrowdScheduler(Lanes, 0, 0.1f, 1);
            var r = new Recorder(s);
            for (int i = 0; i < 100; i++)
            {
                s.Tick(1d);
            }

            Assert.That(r.Spawns, Is.Empty);
        }

        [Test]
        public void NotifyFinished_DespawnsOnce_AndIgnoresUnknownIds()
        {
            var s = new CrowdScheduler(Lanes, 5, 1f, 4);
            var r = new Recorder(s);
            s.Tick(1d);
            s.Tick(1d);
            int id = r.Spawns[0].AgentId;
            s.NotifyFinished(id);
            s.NotifyFinished(id);
            s.NotifyFinished(12345);
            Assert.That(r.Despawns, Is.EqualTo(new[] { id }));
            Assert.That(s.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void AgentIds_AreUnique_AndNeverReused()
        {
            var s = new CrowdScheduler(Lanes, 2, 1f, 5);
            var r = new Recorder(s);
            for (int i = 0; i < 50; i++)
            {
                s.Tick(1d);
                if (s.ActiveCount == 2)
                {
                    s.NotifyFinished(r.Spawns[r.Spawns.Count - 2].AgentId);
                }
            }

            var ids = r.Spawns.Select(x => x.AgentId).ToList();
            Assert.That(ids, Is.Unique);
            Assert.That(ids, Is.Ordered);
        }

        private static List<string> Run(int seed)
        {
            var s = new CrowdScheduler(Lanes, 4, 0.75f, seed);
            var log = new List<string>();
            s.Spawned += x => log.Add("+" + x.AgentId + ":" + x.Lane + ":" + x.Reverse);
            s.Despawned += id => log.Add("-" + id);
            for (int i = 0; i < 2000; i++)
            {
                s.Tick(0.1d);
                if (i % 13 == 0 && s.ActiveCount > 0)
                {
                    // finish the oldest active agent
                    var plus = log.Where(e => e[0] == '+').Select(e => int.Parse(e.Substring(1, e.IndexOf(':') - 1))).ToList();
                    var minus = new HashSet<int>(log.Where(e => e[0] == '-').Select(e => int.Parse(e.Substring(1))));
                    s.NotifyFinished(plus.First(p => !minus.Contains(p)));
                }
            }

            return log;
        }

        [Test]
        public void SameSeed_GivesTheSameSequence()
        {
            var a = Run(42);
            var b = Run(42);
            Assert.That(a.Count, Is.GreaterThan(50));
            Assert.That(b, Is.EqualTo(a));
        }

        [Test]
        public void DifferentSeeds_GiveDifferentSequences()
        {
            Assert.That(Run(1), Is.Not.EqualTo(Run(2)));
        }

        [Test]
        public void UsesEveryLane_AndBothDirectionsOnSidewalks_ButNeverReversesRoads()
        {
            var s = new CrowdScheduler(Lanes, 1000, 1f, 11);
            var r = new Recorder(s);
            for (int i = 0; i < 500; i++)
            {
                s.Tick(1d);
            }

            Assert.That(r.Spawns.Select(x => x.Lane).Distinct(), Is.EquivalentTo(Lanes));
            Assert.That(r.Spawns.Where(x => x.Lane.StartsWith("Road", StringComparison.Ordinal)).All(x => !x.Reverse), Is.True);
            var walkers = r.Spawns.Where(x => !x.Lane.StartsWith("Road", StringComparison.Ordinal)).ToList();
            Assert.That(walkers.Any(x => x.Reverse), Is.True);
            Assert.That(walkers.Any(x => !x.Reverse), Is.True);
        }

        [TestCase(0d)]
        [TestCase(-5d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void PausedOrInvalidDeltas_DoNothing(double delta)
        {
            var s = new CrowdScheduler(Lanes, 5, 1f, 1);
            var r = new Recorder(s);
            s.Tick(0.9d);
            for (int i = 0; i < 100; i++)
            {
                s.Tick(delta);
            }

            Assert.That(r.Spawns, Is.Empty);
            s.Tick(0.1d);
            Assert.That(r.Spawns.Count, Is.EqualTo(1), "timer kept its progress through the pause");
        }

        [Test]
        public void Pausing_DoesNotChangeTheSequence()
        {
            List<int> Spawned(bool withPauses)
            {
                var s = new CrowdScheduler(Lanes, 3, 0.5f, 77);
                var times = new List<int>();
                int tick = 0;
                s.Spawned += x => times.Add(tick);
                for (tick = 0; tick < 300; tick++)
                {
                    s.Tick(0.125d);
                    if (withPauses)
                    {
                        s.Tick(0d);
                        s.Tick(double.NaN);
                    }

                    if (tick % 10 == 9 && s.ActiveCount > 0)
                    {
                        s.NotifyFinished(times.Count - s.ActiveCount + 1);
                    }
                }

                return times;
            }

            Assert.That(Spawned(true), Is.EqualTo(Spawned(false)));
        }

        [Test]
        public void Constructor_RejectsInvalidArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new CrowdScheduler(null, 1, 1f, 0));
            Assert.Throws<ArgumentException>(() => new CrowdScheduler(new string[0], 1, 1f, 0));
            Assert.Throws<ArgumentException>(() => new CrowdScheduler(new[] { "A", null }, 1, 1f, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CrowdScheduler(Lanes, -1, 1f, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CrowdScheduler(Lanes, 1, 0f, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CrowdScheduler(Lanes, 1, float.NaN, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CrowdScheduler(Lanes, 1, float.PositiveInfinity, 0));
        }

        [Test]
        public void CopiesTheLaneList()
        {
            var lanes = new List<string> { "SidewalkNorth" };
            var s = new CrowdScheduler(lanes, 5, 1f, 0);
            var r = new Recorder(s);
            lanes[0] = "Changed";
            s.Tick(1d);
            Assert.That(r.Spawns[0].Lane, Is.EqualTo("SidewalkNorth"));
        }
    }
}
