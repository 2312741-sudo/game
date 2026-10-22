using System;
using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.People;
using UnityEngine;

namespace TramChanh.Tests.EditMode.People
{
    /// <summary>AI-ANIMALS: view cone, wall blocking, hearing, smell, and a fake perception driving the brain.</summary>
    public sealed class AnimalPerceptionTests
    {
        private sealed class FakeThreats : IAnimalThreatSource
        {
            public readonly List<AnimalThreat> Items = new List<AnimalThreat>();
            public void Collect(List<AnimalThreat> into) { into.AddRange(Items); }
        }

        private sealed class FakeFood : IAnimalFoodSource
        {
            public bool Present;
            public Vector3 At;
            public bool TryFindFood(Vector3 from, float range, out Vector3 position)
            {
                position = At;
                return Present && Vector3.Distance(from, At) <= range;
            }
        }

        private sealed class FakeWalls : ILineOfSight
        {
            public bool Blocked;
            public bool IsBlocked(Vector3 from, Vector3 to) { return Blocked; }
        }

        private sealed class ScriptedPerception : IAnimalPerception
        {
            public AnimalSenses Next;
            public AnimalSenses Sense(Vector3 position, Vector3 forward) { return Next; }
        }

        private static readonly Vector3 Forward = new Vector3(0f, 0f, 1f);

        private static AnimalProfile Profile()
        {
            AnimalProfile p = AnimalProfile.For(AnimalSpecies.Dog, 1f, 0.5f);
            p.ViewDistance = 8f;
            p.ViewHalfAngleDegrees = 60f;
            p.HearDistance = 4f;
            p.SmellRange = 5f;
            return p;
        }

        [TestCase(0f, 5f, true)]      // straight ahead
        [TestCase(2.9f, 5f, true)]    // inside 60 degrees
        [TestCase(8.7f, 5f, false)]   // about 60.1 degrees
        [TestCase(5f, 0f, false)]     // 90 degrees to the side
        [TestCase(0f, -3f, false)]    // behind
        [TestCase(0f, 8.1f, false)]   // too far
        [TestCase(0f, 7.9f, true)]
        public void Cone_ChecksDistanceAndAngle(float x, float z, bool expected)
        {
            Assert.That(VisionCone.Contains(Vector3.zero, Forward, new Vector3(x, 0f, z), 8f, 60f), Is.EqualTo(expected));
        }

        [Test]
        public void Cone_IgnoresHeight_AndSeesAllAroundAt180()
        {
            Assert.That(VisionCone.Contains(Vector3.zero, Forward, new Vector3(0f, 50f, 3f), 8f, 30f), Is.True);
            Assert.That(VisionCone.Contains(Vector3.zero, Forward, new Vector3(0f, 0f, -3f), 8f, 180f), Is.True);
        }

        [Test]
        public void SeesPlayerInFront_AndTreatsItAsInterest()
        {
            var threats = new FakeThreats();
            threats.Items.Add(new AnimalThreat(new Vector3(0f, 0f, 6f), 1f, true));
            var perception = new AnimalPerception(Profile(), threats, null, new FakeWalls());
            AnimalSenses s = perception.Sense(Vector3.zero, Forward);
            Assert.That(s.ThreatDetected, Is.True);
            Assert.That(s.ThreatDistance, Is.EqualTo(6f).Within(1e-4f));
            Assert.That(s.InterestDetected, Is.True);
            Assert.That(s.InterestPosition, Is.EqualTo(new Vector3(0f, 0f, 6f)));
        }

        [Test]
        public void PlayerBehind_BeyondHearing_IsNotNoticed()
        {
            var threats = new FakeThreats();
            threats.Items.Add(new AnimalThreat(new Vector3(0f, 0f, -6f), 1f, true));
            var perception = new AnimalPerception(Profile(), threats, null, new FakeWalls());
            Assert.That(perception.Sense(Vector3.zero, Forward).ThreatDetected, Is.False);
        }

        [Test]
        public void PlayerBehind_ButWithinHearing_IsHeard_ButNotInteresting()
        {
            var threats = new FakeThreats();
            threats.Items.Add(new AnimalThreat(new Vector3(0f, 0f, -3f), 1f, true));
            var perception = new AnimalPerception(Profile(), threats, null, new FakeWalls());
            AnimalSenses s = perception.Sense(Vector3.zero, Forward);
            Assert.That(s.ThreatDetected, Is.True);
            Assert.That(s.InterestDetected, Is.False, "heard but not seen: no curiosity");
        }

        [Test]
        public void WallBlocksSight_ButNotHearing()
        {
            var threats = new FakeThreats();
            var walls = new FakeWalls { Blocked = true };
            var perception = new AnimalPerception(Profile(), threats, null, walls);

            threats.Items.Add(new AnimalThreat(new Vector3(0f, 0f, 6f), 1f, true));
            Assert.That(perception.Sense(Vector3.zero, Forward).ThreatDetected, Is.False, "in the cone and in range, but behind a wall, and too far to hear");

            threats.Items.Clear();
            threats.Items.Add(new AnimalThreat(new Vector3(0f, 0f, 3f), 1f, true));
            AnimalSenses heard = perception.Sense(Vector3.zero, Forward);
            Assert.That(heard.ThreatDetected, Is.True, "close enough to hear through the wall");
            Assert.That(heard.InterestDetected, Is.False);
        }

        [Test]
        public void LoudThreat_IsHeardFurtherAndScaresFromFurther()
        {
            var threats = new FakeThreats();
            threats.Items.Add(new AnimalThreat(new Vector3(0f, 0f, -7f), 2f, false)); // motorbike behind, 7 m: 7 / 2 = 3.5 <= hearing 4
            var perception = new AnimalPerception(Profile(), threats, null, new FakeWalls());
            AnimalSenses s = perception.Sense(Vector3.zero, Forward);
            Assert.That(s.ThreatDetected, Is.True);
            Assert.That(s.ThreatDistance, Is.EqualTo(3.5f).Within(1e-4f));
            Assert.That(s.InterestDetected, Is.False, "a motorbike is never interesting");
        }

        [Test]
        public void NearestThreatWins()
        {
            var threats = new FakeThreats();
            threats.Items.Add(new AnimalThreat(new Vector3(0f, 0f, 6f), 1f, true));
            threats.Items.Add(new AnimalThreat(new Vector3(1f, 0f, 2f), 1f, false));
            var perception = new AnimalPerception(Profile(), threats, null, new FakeWalls());
            AnimalSenses s = perception.Sense(Vector3.zero, Forward);
            Assert.That(s.ThreatPosition, Is.EqualTo(new Vector3(1f, 0f, 2f)));
        }

        [Test]
        public void SmellsFoodWithinRange_OnlyWhenTheSpeciesCanBeg()
        {
            var food = new FakeFood { Present = true, At = new Vector3(0f, 0f, -4f) };
            AnimalProfile profile = Profile();
            var perception = new AnimalPerception(profile, null, food, null);
            AnimalSenses s = perception.Sense(Vector3.zero, Forward);
            Assert.That(s.FoodDetected, Is.True, "smell ignores the view cone");
            Assert.That(s.FoodPosition, Is.EqualTo(food.At));

            food.At = new Vector3(0f, 0f, -9f);
            Assert.That(perception.Sense(Vector3.zero, Forward).FoodDetected, Is.False);

            food.At = new Vector3(0f, 0f, -4f);
            profile.CanBeg = false;
            Assert.That(perception.Sense(Vector3.zero, Forward).FoodDetected, Is.False);
        }

        [Test]
        public void NoSources_MeansNothingIsSensed()
        {
            var perception = new AnimalPerception(Profile(), null, null, null);
            AnimalSenses s = perception.Sense(Vector3.zero, Forward);
            Assert.That(s.ThreatDetected || s.InterestDetected || s.FoodDetected, Is.False);
        }

        [Test]
        public void Constructor_RejectsNullProfile()
        {
            Assert.Throws<ArgumentNullException>(() => new AnimalPerception(null, null, null, null));
        }

        [Test]
        public void FakePerception_DrivesTheBrain_ThroughTheWholeScript()
        {
            AnimalProfile profile = AnimalProfile.For(AnimalSpecies.Dog, 2f, 0.5f);
            profile.BegChance = 1f;
            profile.InvestigateChance = 1f;
            var brain = new AnimalBrain(profile, Vector3.zero, 9);
            var fake = new ScriptedPerception();

            fake.Next = new AnimalSenses { FoodDetected = true, FoodPosition = new Vector3(3f, 0f, 0f) };
            Advance(brain, fake, 1f);
            Assert.That(brain.State, Is.EqualTo(AnimalState.Beg));

            fake.Next = new AnimalSenses { ThreatDetected = true, ThreatPosition = new Vector3(-1f, 0f, 0f), ThreatDistance = 1f };
            Advance(brain, fake, 0.2f);
            Assert.That(brain.State, Is.EqualTo(AnimalState.Flee));
            Assert.That(brain.Destination.x, Is.GreaterThan(0f));

            fake.Next = new AnimalSenses();
            Advance(brain, fake, profile.FleeMinSeconds + 0.5f);
            Assert.That(brain.State, Is.EqualTo(AnimalState.Rest));
        }

        private static void Advance(AnimalBrain brain, IAnimalPerception perception, float seconds)
        {
            for (float t = 0f; t < seconds - 1e-4f; t += 0.1f)
            {
                AnimalSenses s = perception.Sense(Vector3.zero, Forward);
                brain.Update(0.1f, Vector3.zero, in s);
            }
        }
    }
}
