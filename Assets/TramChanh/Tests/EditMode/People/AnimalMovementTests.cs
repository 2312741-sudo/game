using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.People;
using UnityEngine;

namespace TramChanh.Tests.EditMode.People
{
    /// <summary>AI-ANIMALS: route over the waypoint graph, separation steering, level of detail, and brain + path follower together.</summary>
    public sealed class AnimalMovementTests
    {
        private static Vector3 V(float x, float z) => new Vector3(x, 0f, z);

        // A sidewalk lane along X (0..20) and a road lane two metres away that must not be used.
        private static WaypointGraph Street()
        {
            var g = new WaypointGraph();
            var side = new List<int>();
            var road = new List<int>();
            for (int i = 0; i <= 4; i++)
            {
                side.Add(g.AddNode("S" + i, V(i * 5f, 0f)));
                road.Add(g.AddNode("R" + i, V(i * 5f, 2f)));
            }

            g.AddLane("SidewalkNorth", side, false);
            g.AddLane("RoadEast", road, false);
            return g;
        }

        private static readonly string[] Sidewalks = { "SidewalkNorth" };

        [Test]
        public void ShortTrip_GoesStraight()
        {
            var route = new List<Vector3>();
            bool graph = AnimalRoute.Build(Street(), Sidewalks, V(0.5f, 0f), V(2.5f, 0f), route);
            Assert.That(graph, Is.False);
            Assert.That(route.Count, Is.EqualTo(2));
        }

        [Test]
        public void LongTrip_FollowsTheSidewalkNodes_AndStartsAndEndsOnTheRequestedPoints()
        {
            var route = new List<Vector3>();
            bool graph = AnimalRoute.Build(Street(), Sidewalks, V(0.5f, 0.3f), V(14.5f, 0.3f), route);
            Assert.That(graph, Is.True);
            WaypointGraphTests.AssertNear(V(0.5f, 0.3f), route[0], 1e-4f);
            WaypointGraphTests.AssertNear(V(14.5f, 0.3f), route[route.Count - 1], 1e-4f);
            Assert.That(route.Count, Is.EqualTo(2 + 4), "nodes S0, S1, S2, S3 in between");
            for (int i = 1; i < route.Count - 1; i++) { Assert.That(route[i].z, Is.EqualTo(0f), "sidewalk row, not the road row"); }
        }

        [Test]
        public void NoGraph_OrNoNodeNearby_OrUnreachable_GoesStraight()
        {
            var route = new List<Vector3>();
            Assert.That(AnimalRoute.Build(null, Sidewalks, V(0, 0), V(10, 0), route), Is.False);
            Assert.That(route.Count, Is.EqualTo(2));

            Assert.That(AnimalRoute.Build(Street(), Sidewalks, V(0, 30), V(10, 30), route), Is.False, "30 m from every node");

            var islands = new WaypointGraph();
            islands.AddNode(null, V(0, 0));
            islands.AddNode(null, V(10, 0));
            Assert.That(AnimalRoute.Build(islands, null, V(0, 0.1f), V(10, 0.1f), route), Is.False, "two unconnected nodes");
            Assert.That(route.Count, Is.EqualTo(2));
        }

        [Test]
        public void RouteFeedsAPathFollower_ThatWalksItExactly()
        {
            var route = new List<Vector3>();
            AnimalRoute.Build(Street(), Sidewalks, V(0f, 1f), V(20f, 1f), route);
            var follower = new PathFollower(route, 2f, 0.05f);
            for (int i = 0; i < 200 && !follower.Arrived; i++) { follower.Step(0.1); }
            Assert.That(follower.Arrived, Is.True);
            WaypointGraphTests.AssertNear(V(20f, 1f), follower.Position, 1e-4f);
        }

        [Test]
        public void Push_PointsAwayFromNeighbours_AndIsZeroBeyondTheRadius()
        {
            var others = new List<Vector3> { V(1f, 0f) };
            Vector3 push = SeparationSteering.Push(V(0.5f, 0f), others, 1f);
            Assert.That(push.x, Is.LessThan(0f));
            Assert.That(push.z, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(SeparationSteering.Push(V(5f, 0f), others, 1f), Is.EqualTo(Vector3.zero));
            Assert.That(SeparationSteering.Push(V(0f, 0f), null, 1f), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Push_IsLimitedToUnitLength_AndHandlesAnExactOverlap()
        {
            var crowd = new List<Vector3> { V(0.1f, 0f), V(0f, 0.1f), V(-0.1f, 0f), V(0f, -0.1f), V(0.1f, 0.1f) };
            Assert.That(SeparationSteering.Push(V(0f, 0f), crowd, 2f).magnitude, Is.LessThanOrEqualTo(1f + 1e-4f));
            var same = new List<Vector3> { V(1f, 1f) };
            Vector3 push = SeparationSteering.Push(V(1f, 1f), same, 1f);
            Assert.That(float.IsNaN(push.x) || float.IsNaN(push.z), Is.False);
            Assert.That(push.magnitude, Is.GreaterThan(0f));
        }

        [Test]
        public void Offset_BuildsUnderPush_IsCapped_AndFadesWhenThePushStops()
        {
            var steering = new SeparationSteering();
            for (int i = 0; i < 100; i++) { steering.Step(0.1f, new Vector3(1f, 0f, 0f), 1.2f, 2.5f, 0.6f); }
            Assert.That(steering.Offset.magnitude, Is.LessThanOrEqualTo(0.6f + 1e-4f));
            Assert.That(steering.Offset.x, Is.GreaterThan(0.3f));
            for (int i = 0; i < 100; i++) { steering.Step(0.1f, Vector3.zero, 1.2f, 2.5f, 0.6f); }
            Assert.That(steering.Offset.magnitude, Is.LessThan(0.01f));
            steering.Step(0f, new Vector3(1f, 0f, 0f), 1f, 1f, 1f);
            Assert.That(steering.Offset.magnitude, Is.LessThan(0.01f), "zero seconds changes nothing");
        }

        [Test]
        public void Lod_TiersGrowWithDistance_AndFarAnimalsThinkLessOften()
        {
            Assert.That(AnimalLod.TierFor(0f), Is.EqualTo(AnimalLodTier.Near));
            Assert.That(AnimalLod.TierFor(AnimalLod.NearMetres + 0.1f), Is.EqualTo(AnimalLodTier.Mid));
            Assert.That(AnimalLod.TierFor(AnimalLod.MidMetres + 0.1f), Is.EqualTo(AnimalLodTier.Far));
            Assert.That(AnimalLod.TierFor(AnimalLod.FarMetres + 0.1f), Is.EqualTo(AnimalLodTier.Dormant));
            Assert.That(AnimalLod.TierFor(float.NaN), Is.EqualTo(AnimalLodTier.Near));
            Assert.That(AnimalLod.TierFor(-5f), Is.EqualTo(AnimalLodTier.Near));

            AnimalLodTier[] tiers = { AnimalLodTier.Near, AnimalLodTier.Mid, AnimalLodTier.Far, AnimalLodTier.Dormant };
            for (int i = 1; i < tiers.Length; i++)
            {
                Assert.That(AnimalLod.ThinkInterval(tiers[i]), Is.GreaterThan(AnimalLod.ThinkInterval(tiers[i - 1])));
                Assert.That(AnimalLod.MoveInterval(tiers[i]), Is.GreaterThan(AnimalLod.MoveInterval(tiers[i - 1])));
            }

            Assert.That(AnimalLod.MoveInterval(AnimalLodTier.Near), Is.EqualTo(0f), "near animals move every frame");
        }

        private sealed class MovingPlayer : IAnimalThreatSource
        {
            public Vector3 At;
            public void Collect(List<AnimalThreat> into) { into.Add(new AnimalThreat(At, 1f, true)); }
        }

        private sealed class OpenField : ILineOfSight
        {
            public bool IsBlocked(Vector3 from, Vector3 to) { return false; }
        }

        // Brain + perception + route + path follower, the same loop the MonoBehaviour runs, without any Unity scene.
        [Test]
        public void Integration_DogRunsAwayFromAPlayerWhoWalksUpToIt_ThenCalmsDown()
        {
            AnimalProfile profile = AnimalProfile.For(AnimalSpecies.Dog, 1f, 0.5f);
            Vector3 home = V(10f, 0f);
            var player = new MovingPlayer { At = V(10f, 8f) };
            var perception = new AnimalPerception(profile, player, null, new OpenField());
            var brain = new AnimalBrain(profile, home, 21);
            WaypointGraph graph = Street();
            Vector3 position = home;
            Vector3 forward = V(0f, 1f);
            PathFollower follower = null;
            int plan = 0;
            var route = new List<Vector3>();
            bool fled = false;
            float closest = float.PositiveInfinity;
            const float dt = 0.1f;

            for (int step = 0; step < 900; step++)
            {
                // The player walks toward the dog at 1.2 m/s for the first 6 seconds, then stands still.
                if (step < 60 && player.At.z > position.z) { player.At = V(player.At.x, player.At.z - 1.2f * dt); }

                AnimalSenses senses = perception.Sense(position, forward);
                brain.Update(dt, position, in senses);
                if (brain.State == AnimalState.Flee) { fled = true; }

                if (brain.HasDestination && brain.DestinationId != plan)
                {
                    plan = brain.DestinationId;
                    AnimalRoute.Build(graph, Sidewalks, position, brain.Destination, route);
                    follower = new PathFollower(route, profile.WalkSpeed * brain.SpeedMultiplier, 0.05f);
                }

                if (follower != null && brain.HasDestination)
                {
                    follower.Step(dt);
                    Vector3 moved = follower.Position - position;
                    if (moved.sqrMagnitude > 1e-8f) { forward = moved.normalized; }
                    position = follower.Position;
                    if (follower.Arrived) { follower = null; brain.ReportArrived(); }
                }
                else
                {
                    follower = null;
                }

                closest = Mathf.Min(closest, Mathf.Abs(player.At.z - position.z));
                float dx = position.x - home.x, dz = position.z - home.z;
                Assert.That(Mathf.Sqrt(dx * dx + dz * dz), Is.LessThanOrEqualTo(9f), "stayed near home at step " + step);
            }

            Assert.That(fled, Is.True, "the dog ran when the player came within " + profile.FleeDistance + " m (closest approach " + closest + ")");
            Assert.That(brain.State, Is.Not.EqualTo(AnimalState.Flee), "it calmed down once the player stood still at a distance");
        }
    }
}
