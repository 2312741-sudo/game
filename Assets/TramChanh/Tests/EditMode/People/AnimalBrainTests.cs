using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.People;
using UnityEngine;

namespace TramChanh.Tests.EditMode.People
{
    /// <summary>AI-ANIMALS: every transition of the street-animal state machine, cooldowns and seeded repeatability.</summary>
    public sealed class AnimalBrainTests
    {
        private const float Step = 0.1f;
        private static readonly Vector3 Home = new Vector3(10f, 0f, 5f);

        private static AnimalProfile Profile()
        {
            AnimalProfile p = AnimalProfile.For(AnimalSpecies.Dog, 2f, 0.5f);
            p.IdleMin = 1f;
            p.IdleMax = 2f;
            p.RestMin = 3f;
            p.RestMax = 4f;
            p.RestChance = 0f;
            p.BegChance = 1f;
            p.InvestigateChance = 1f;
            return p;
        }

        private static AnimalSenses None() => new AnimalSenses();

        private static AnimalSenses Threat(Vector3 at, float distance)
        {
            return new AnimalSenses { ThreatDetected = true, ThreatPosition = at, ThreatDistance = distance };
        }

        private static AnimalSenses Food(Vector3 at) => new AnimalSenses { FoodDetected = true, FoodPosition = at };

        private static AnimalSenses Interest(Vector3 at) => new AnimalSenses { InterestDetected = true, InterestPosition = at };

        private static void Run(AnimalBrain brain, float seconds, AnimalSenses senses, Vector3 position)
        {
            for (float t = 0f; t < seconds - 1e-4f; t += Step) { brain.Update(Step, position, in senses); }
        }

        private static bool RunUntil(AnimalBrain brain, AnimalState state, AnimalSenses senses, Vector3 position, float limitSeconds = 30f)
        {
            for (float t = 0f; t < limitSeconds && brain.State != state; t += Step) { brain.Update(Step, position, in senses); }
            return brain.State == state;
        }

        private static AnimalBrain BrainIn(AnimalState target, AnimalProfile profile)
        {
            var brain = new AnimalBrain(profile, Home, 7);
            switch (target)
            {
                case AnimalState.Wander:
                    Assert.That(RunUntil(brain, AnimalState.Wander, None(), Home), Is.True);
                    break;
                case AnimalState.Rest:
                    profile.RestChance = 1f;
                    Assert.That(RunUntil(brain, AnimalState.Rest, None(), Home), Is.True);
                    break;
                case AnimalState.Beg:
                    Assert.That(RunUntil(brain, AnimalState.Beg, Food(Home + new Vector3(3f, 0f, 0f)), Home), Is.True);
                    break;
                case AnimalState.Investigate:
                    Assert.That(RunUntil(brain, AnimalState.Investigate, Interest(Home + new Vector3(0f, 0f, 6f)), Home), Is.True);
                    break;
            }

            return brain;
        }

        [Test]
        public void Starts_InIdle_WithoutDestination()
        {
            var brain = new AnimalBrain(Profile(), Home, 1);
            Assert.That(brain.State, Is.EqualTo(AnimalState.Idle));
            Assert.That(brain.HasDestination, Is.False);
            Assert.That(brain.TransitionCount, Is.EqualTo(0));
        }

        [Test]
        public void Idle_BecomesWander_WithDestinationInsideTheHomeCircle()
        {
            AnimalProfile profile = Profile();
            var brain = new AnimalBrain(profile, Home, 3);
            Assert.That(RunUntil(brain, AnimalState.Wander, None(), Home), Is.True);
            Assert.That(brain.HasDestination, Is.True);
            float dx = brain.Destination.x - Home.x, dz = brain.Destination.z - Home.z;
            Assert.That(dx * dx + dz * dz, Is.LessThanOrEqualTo(profile.WanderRadius * profile.WanderRadius + 1e-3f));
            Assert.That(brain.SpeedMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void Wander_ReturnsToIdle_WhenArrived()
        {
            AnimalBrain brain = BrainIn(AnimalState.Wander, Profile());
            brain.ReportArrived();
            brain.Update(Step, Home, None());
            Assert.That(brain.State, Is.EqualTo(AnimalState.Idle));
            Assert.That(brain.HasDestination, Is.False);
        }

        [Test]
        public void Wander_ReturnsToIdle_WhenBlocked()
        {
            AnimalBrain brain = BrainIn(AnimalState.Wander, Profile());
            brain.ReportBlocked();
            brain.Update(Step, Home, None());
            Assert.That(brain.State, Is.EqualTo(AnimalState.Idle));
        }

        [Test]
        public void Wander_ReturnsToIdle_WhenItTakesTooLong()
        {
            AnimalBrain brain = BrainIn(AnimalState.Wander, Profile());
            Assert.That(RunUntil(brain, AnimalState.Idle, None(), Home, 60f), Is.True);
        }

        [Test]
        public void Idle_BecomesRest_WhenRestChanceIsOne_AndRestEndsInIdle()
        {
            AnimalBrain brain = BrainIn(AnimalState.Rest, Profile());
            Assert.That(brain.IsSitting, Is.True);
            Assert.That(brain.HasDestination, Is.False);
            Assert.That(RunUntil(brain, AnimalState.Idle, None(), Home, 10f), Is.True);
            Assert.That(brain.PreviousState, Is.EqualTo(AnimalState.Rest));
        }

        [TestCase(AnimalState.Idle)]
        [TestCase(AnimalState.Wander)]
        [TestCase(AnimalState.Rest)]
        [TestCase(AnimalState.Beg)]
        [TestCase(AnimalState.Investigate)]
        public void AnyState_FleesWhenAThreatIsInsideFleeDistance(AnimalState from)
        {
            AnimalProfile profile = Profile();
            AnimalBrain brain = BrainIn(from, profile);
            Vector3 threat = Home + new Vector3(1f, 0f, 0f);
            brain.Update(Step, Home, Threat(threat, profile.FleeDistance - 0.1f));
            Assert.That(brain.State, Is.EqualTo(AnimalState.Flee));
            Assert.That(brain.PreviousState, Is.EqualTo(from));
            Assert.That(brain.HasDestination, Is.True);
            Assert.That(brain.SpeedMultiplier, Is.EqualTo(profile.RunMultiplier));
            Assert.That(brain.Destination.x, Is.LessThan(Home.x), "runs away from the threat that stands on the +X side");
        }

        [Test]
        public void ThreatBeyondFleeDistance_DoesNotMakeItFlee()
        {
            AnimalProfile profile = Profile();
            var brain = new AnimalBrain(profile, Home, 1);
            brain.Update(Step, Home, Threat(Home + new Vector3(5f, 0f, 0f), profile.FleeDistance + 0.5f));
            Assert.That(brain.State, Is.Not.EqualTo(AnimalState.Flee));
        }

        [Test]
        public void Flee_DestinationStaysInsideTheLeash()
        {
            AnimalProfile profile = Profile();
            profile.FleeLeash = 2f;
            var brain = new AnimalBrain(profile, Home, 1);
            Vector3 position = Home + new Vector3(1.8f, 0f, 0f);
            brain.Update(Step, position, Threat(position + new Vector3(-1f, 0f, 0f), 1f));
            Assert.That(brain.State, Is.EqualTo(AnimalState.Flee));
            float dx = brain.Destination.x - Home.x, dz = brain.Destination.z - Home.z;
            Assert.That(Mathf.Sqrt(dx * dx + dz * dz), Is.LessThanOrEqualTo(2f + 1e-3f));
        }

        [Test]
        public void Flee_DoesNotEndBeforeMinimumSeconds_ThenRests()
        {
            AnimalProfile profile = Profile();
            var brain = new AnimalBrain(profile, Home, 1);
            Vector3 threat = Home + new Vector3(1f, 0f, 0f);
            brain.Update(Step, Home, Threat(threat, 1f));
            brain.ReportArrived();
            Run(brain, profile.FleeMinSeconds - 0.5f, None(), Home);
            Assert.That(brain.State, Is.EqualTo(AnimalState.Flee), "safe already, but the minimum time has not passed");
            Run(brain, 1f, None(), Home);
            Assert.That(brain.State, Is.EqualTo(AnimalState.Rest));
        }

        [Test]
        public void Flee_KeepsRunning_WhileTheThreatStaysClose_ThenGivesUpAtTheMaximum()
        {
            AnimalProfile profile = Profile();
            var brain = new AnimalBrain(profile, Home, 1);
            Vector3 threat = Home + new Vector3(1f, 0f, 0f);
            AnimalSenses scary = Threat(threat, 1f);
            brain.Update(Step, Home, scary);
            int firstDestination = brain.DestinationId;
            brain.ReportArrived();
            brain.Update(Step, Home, scary);
            Assert.That(brain.State, Is.EqualTo(AnimalState.Flee));
            Assert.That(brain.DestinationId, Is.GreaterThan(firstDestination), "a new, further run target was given");
            bool rested = false;
            for (float t = 0f; t < profile.FleeMaxSeconds + 1f; t += Step)
            {
                brain.Update(Step, Home, in scary);
                rested |= brain.State == AnimalState.Rest;
            }

            Assert.That(rested, Is.True, "after the maximum flee time it calms down for a moment (a threat that stays close scares it again)");
        }

        [Test]
        public void Flee_RestsAfterwards_AndBlocksImmediateCuriosity()
        {
            AnimalProfile profile = Profile();
            var brain = new AnimalBrain(profile, Home, 1);
            brain.Update(Step, Home, Threat(Home + new Vector3(1f, 0f, 0f), 1f));
            Run(brain, profile.FleeMinSeconds + 0.5f, None(), Home);
            Assert.That(brain.State, Is.EqualTo(AnimalState.Rest));
            Assert.That(brain.InvestigateCooldownLeft, Is.GreaterThan(0f));
            Assert.That(brain.BegCooldownLeft, Is.GreaterThan(0f));
        }

        [Test]
        public void Beg_WalksToASpotNextToTheFood_ThenSits()
        {
            AnimalProfile profile = Profile();
            Vector3 food = Home + new Vector3(3f, 0f, 0f);
            AnimalBrain brain = BrainIn(AnimalState.Beg, profile);
            Assert.That(brain.HasDestination, Is.True);
            float dx = brain.Destination.x - food.x, dz = brain.Destination.z - food.z;
            Assert.That(Mathf.Sqrt(dx * dx + dz * dz), Is.EqualTo(profile.BegStopDistance).Within(1e-3f));
            Assert.That(brain.HasFocus, Is.True);
            Assert.That(brain.IsSitting, Is.False);
            brain.ReportArrived();
            Assert.That(brain.IsSitting, Is.True);
        }

        [Test]
        public void Beg_Ends_WhenTheFoodIsGone_AndCooldownBlocksBegging_ThenAllowsIt()
        {
            AnimalProfile profile = Profile();
            profile.BegCooldown = 20f;
            Vector3 food = Home + new Vector3(3f, 0f, 0f);
            AnimalBrain brain = BrainIn(AnimalState.Beg, profile);
            brain.ReportArrived();
            Run(brain, profile.BegLoseSeconds + 0.5f, None(), Home);
            Assert.That(brain.State, Is.EqualTo(AnimalState.Idle));
            Assert.That(brain.BegCooldownLeft, Is.GreaterThan(0f));

            // Food is back, but the cooldown is running: it must not beg.
            Run(brain, 10f, Food(food), Home);
            Assert.That(brain.State, Is.Not.EqualTo(AnimalState.Beg));

            // After the cooldown it begs again.
            Assert.That(RunUntil(brain, AnimalState.Beg, Food(food), Home, 30f), Is.True);
        }

        [Test]
        public void Beg_Ends_AfterTheMaximumTime_EvenWithFoodAround()
        {
            AnimalProfile profile = Profile();
            profile.BegMaxSeconds = 5f;
            Vector3 food = Home + new Vector3(3f, 0f, 0f);
            AnimalBrain brain = BrainIn(AnimalState.Beg, profile);
            brain.ReportArrived();
            Run(brain, 5.5f, Food(food), Home);
            Assert.That(brain.State, Is.EqualTo(AnimalState.Idle));
        }

        [Test]
        public void ZeroBegChance_NeverBegs()
        {
            AnimalProfile profile = Profile();
            profile.BegChance = 0f;
            var brain = new AnimalBrain(profile, Home, 4);
            bool begged = false;
            AnimalSenses food = Food(Home + new Vector3(3f, 0f, 0f));
            for (int i = 0; i < 3000; i++)
            {
                brain.Update(Step, Home, in food);
                begged |= brain.State == AnimalState.Beg;
            }

            Assert.That(begged, Is.False);
        }

        [Test]
        public void AnimalThatCannotBeg_IgnoresFood()
        {
            AnimalProfile profile = Profile();
            profile.CanBeg = false;
            var brain = new AnimalBrain(profile, Home, 4);
            Run(brain, 30f, Food(Home + new Vector3(3f, 0f, 0f)), Home);
            Assert.That(brain.PreviousState, Is.Not.EqualTo(AnimalState.Beg));
            Assert.That(brain.State, Is.Not.EqualTo(AnimalState.Beg));
        }

        [Test]
        public void Investigate_StopsShortOfThePlayer_LooksThenGivesUp_WithCooldown()
        {
            AnimalProfile profile = Profile();
            Vector3 player = Home + new Vector3(0f, 0f, 6f);
            AnimalBrain brain = BrainIn(AnimalState.Investigate, profile);
            float dx = brain.Destination.x - player.x, dz = brain.Destination.z - player.z;
            Assert.That(Mathf.Sqrt(dx * dx + dz * dz), Is.EqualTo(profile.InvestigateStopDistance).Within(1e-3f));
            Assert.That(brain.HasFocus, Is.True);
            Assert.That(brain.Focus, Is.EqualTo(player));

            brain.ReportArrived();
            Run(brain, profile.InvestigateLookSeconds + 0.5f, Interest(player), Home);
            Assert.That(brain.State, Is.EqualTo(AnimalState.Idle));
            Assert.That(brain.InvestigateCooldownLeft, Is.GreaterThan(0f));
            Run(brain, 3f, Interest(player), Home);
            Assert.That(brain.State, Is.Not.EqualTo(AnimalState.Investigate), "cooldown blocks a second look");
        }

        [Test]
        public void Investigate_TimesOut_WhenItNeverArrives()
        {
            AnimalProfile profile = Profile();
            profile.InvestigateMaxSeconds = 4f;
            AnimalBrain brain = BrainIn(AnimalState.Investigate, profile);
            Run(brain, 4.5f, Interest(Home + new Vector3(0f, 0f, 6f)), Home);
            Assert.That(brain.State, Is.EqualTo(AnimalState.Idle));
        }

        [Test]
        public void NonCuriousSpecies_NeverInvestigates()
        {
            AnimalProfile profile = Profile();
            profile.Curious = false;
            var brain = new AnimalBrain(profile, Home, 4);
            bool looked = false;
            AnimalSenses interest = Interest(Home + new Vector3(0f, 0f, 6f));
            for (int i = 0; i < 1000; i++)
            {
                brain.Update(Step, Home, in interest);
                looked |= brain.State == AnimalState.Investigate;
            }

            Assert.That(looked, Is.False);
        }

        [Test]
        public void ZeroNegativeAndNanSeconds_AreIgnored()
        {
            var brain = new AnimalBrain(Profile(), Home, 1);
            brain.Update(0f, Home, None());
            brain.Update(-1f, Home, None());
            brain.Update(float.NaN, Home, None());
            brain.Update(float.PositiveInfinity, Home, None());
            Assert.That(brain.StateSeconds, Is.EqualTo(0f));
            Assert.That(brain.State, Is.EqualTo(AnimalState.Idle));
        }

        private static List<string> Trace(int seed, bool certain = false)
        {
            AnimalProfile profile = AnimalProfile.For(AnimalSpecies.Cat, 3f, 0.4f);
            profile.RestChance = 0.5f;
            if (certain) { profile.BegChance = 1f; profile.InvestigateChance = 1f; }
            var brain = new AnimalBrain(profile, Home, seed);
            var trace = new List<string>();
            for (int i = 0; i < 4000; i++)
            {
                AnimalSenses senses = new AnimalSenses();
                if (i % 700 > 400 && i % 700 < 500) { senses = Food(Home + new Vector3(2f, 0f, 1f)); }
                if (i % 900 > 600 && i % 900 < 640) { senses = Threat(Home + new Vector3(1f, 0f, 0f), 1f); }
                if (i % 1100 > 800 && i % 1100 < 900) { senses = Interest(Home + new Vector3(0f, 0f, 5f)); }
                brain.Update(Step, Home, in senses);
                if (brain.HasDestination && brain.DestinationId % 2 == 0) { brain.ReportArrived(); }
                trace.Add(brain.State + "/" + brain.DestinationId + "/" + brain.Destination.x.ToString("0.000") + "/" + brain.Destination.z.ToString("0.000"));
            }

            return trace;
        }

        [Test]
        public void SameSeed_GivesTheSameBehaviour()
        {
            CollectionAssert.AreEqual(Trace(42), Trace(42));
        }

        [Test]
        public void DifferentSeeds_GiveDifferentBehaviour()
        {
            CollectionAssert.AreNotEqual(Trace(1), Trace(2));
        }

        [Test]
        public void Trace_VisitsEveryState()
        {
            var seen = new HashSet<AnimalState>();
            foreach (string line in Trace(5, true)) { seen.Add((AnimalState)System.Enum.Parse(typeof(AnimalState), line.Split('/')[0])); }
            foreach (AnimalState state in System.Enum.GetValues(typeof(AnimalState)))
            {
                Assert.That(seen.Contains(state), Is.True, state + " was never reached");
            }
        }
    }
}
