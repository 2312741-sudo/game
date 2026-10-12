using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using TramChanh.People;
using UnityEngine;

namespace TramChanh.Tests.EditMode.People
{
    /// <summary>PEOPLE-D: building the waypoint graph from authored anchors (contract §5 of NPC_AND_MAP_EXPANSION.md).</summary>
    public sealed class WaypointGraphBuilderTests
    {
        private static Vector3 V(float x, float z) => WaypointGraphTests.V(x, z);

        private static KeyValuePair<string, Vector3> A(string name, float x, float z) =>
            new KeyValuePair<string, Vector3>(name, V(x, z));

        private static string Wp(string lane, int n) => "WP_" + lane + "_" + n.ToString("00", CultureInfo.InvariantCulture);

        /// <summary>A minimal valid layout: one sidewalk and both entries.</summary>
        private static List<KeyValuePair<string, Vector3>> Minimal()
        {
            return new List<KeyValuePair<string, Vector3>>
            {
                A(Wp("SidewalkNorth", 1), -8, 3),
                A(Wp("SidewalkNorth", 2), 0, 3),
                A(Wp("SidewalkNorth", 3), 8, 3),
                A("StreetEntryWest", -9, 3),
                A("StreetEntryEast", 9, 3),
            };
        }

        private static int Node(WaypointGraph g, string name)
        {
            Assert.That(g.TryFind(name, out int node), Is.True, "missing node " + name);
            return node;
        }

        private static string[] NeighbourNames(WaypointGraph g, string name) =>
            g.Neighbours(Node(g, name)).Select(g.NameOf).ToArray();

        private static void AssertClean(WaypointGraphBuildResult r)
        {
            Assert.That(r.Errors, Is.Empty, string.Join(" | ", r.Errors));
            Assert.That(r.Warnings, Is.Empty, string.Join(" | ", r.Warnings));
        }

        private static void AssertHasMessage(IReadOnlyList<string> messages, params string[] fragments)
        {
            Assert.That(messages.Any(m => fragments.All(f => m.Contains(f))), Is.True,
                "no message containing [" + string.Join(", ", fragments) + "] in: " + string.Join(" | ", messages));
        }

        [Test]
        public void Minimal_Layout_BuildsCleanly()
        {
            var r = WaypointGraphBuilder.Build(Minimal());
            AssertClean(r);
            Assert.That(r.Graph.Count, Is.EqualTo(5));
            Assert.That(r.Graph.LaneNames, Is.EqualTo(new[] { "SidewalkNorth" }));
            Assert.That(NeighbourNames(r.Graph, "StreetEntryWest"), Is.EqualTo(new[] { "WP_SidewalkNorth_01" }));
            Assert.That(NeighbourNames(r.Graph, "StreetEntryEast"), Is.EqualTo(new[] { "WP_SidewalkNorth_03" }));
        }

        [Test]
        public void Lanes_AreOrderedByNumber_NotByInputOrderOrText()
        {
            var anchors = new List<KeyValuePair<string, Vector3>>
            {
                A("WP_SidewalkNorth_100", 3, 3),
                A("WP_SidewalkNorth_09", 0, 3),
                A("WP_SidewalkNorth_99", 2, 3),
                A("WP_SidewalkNorth_10", 1, 3),
                A("StreetEntryWest", -1, 3),
                A("StreetEntryEast", 4, 3),
            };
            var r = WaypointGraphBuilder.Build(anchors);
            Assert.That(r.Errors, Is.Empty, string.Join(" | ", r.Errors));
            Assert.That(r.Graph.TryGetLane("SidewalkNorth", out var nodes, out bool loop), Is.True);
            Assert.That(loop, Is.False);
            Assert.That(nodes.Select(r.Graph.NameOf).ToArray(),
                Is.EqualTo(new[] { "WP_SidewalkNorth_09", "WP_SidewalkNorth_10", "WP_SidewalkNorth_99", "WP_SidewalkNorth_100" }));
            // consecutive lane points are connected, non-consecutive are not
            Assert.That(NeighbourNames(r.Graph, "WP_SidewalkNorth_10"), Is.EquivalentTo(new[] { "WP_SidewalkNorth_09", "WP_SidewalkNorth_99" }));
            // 10 -> 99 is a numbering gap
            AssertHasMessage(r.Warnings, "gap", "WP_SidewalkNorth_10", "WP_SidewalkNorth_99");
        }

        [Test]
        public void LanePointName_Parsing()
        {
            Assert.That(WaypointGraphBuilder.TryParseLanePoint("WP_Alley_07", out string lane, out int n), Is.True);
            Assert.That(lane, Is.EqualTo("Alley"));
            Assert.That(n, Is.EqualTo(7));
            Assert.That(WaypointGraphBuilder.TryParseLanePoint("WP_Back_Lane_12", out lane, out n), Is.True);
            Assert.That(lane, Is.EqualTo("Back_Lane"));
            Assert.That(WaypointGraphBuilder.TryParseLanePoint("WP_Alley_7", out _, out _), Is.False, "needs 2 digits");
            Assert.That(WaypointGraphBuilder.TryParseLanePoint("WP_Alley_0x", out _, out _), Is.False);
            Assert.That(WaypointGraphBuilder.TryParseLanePoint("WP__01", out _, out _), Is.False, "empty lane");
            Assert.That(WaypointGraphBuilder.TryParseLanePoint("TableApproach_01", out _, out _), Is.False);
        }

        [Test]
        public void Anchors_LinkToNearestPedestrianNode_NeverToRoad()
        {
            var anchors = Minimal();
            anchors.Add(A(Wp("RoadEast", 1), -8, 5));
            anchors.Add(A(Wp("RoadEast", 2), 8, 5));
            anchors.Add(A(Wp("RoadWest", 1), 8, 6));
            anchors.Add(A(Wp("RoadWest", 2), -8, 6));
            // Each of these is closer to a road node than to any sidewalk node.
            anchors.Add(A("BikeParking_01", 7.5f, 4.9f));
            anchors.Add(A("TableApproach_01", -7.8f, 4.8f));
            anchors.Add(A("ExtraProp", 0.2f, 1f));

            var r = WaypointGraphBuilder.Build(anchors);
            AssertClean(r);
            Assert.That(NeighbourNames(r.Graph, "BikeParking_01"), Is.EqualTo(new[] { "WP_SidewalkNorth_03" }));
            Assert.That(NeighbourNames(r.Graph, "TableApproach_01"), Is.EqualTo(new[] { "WP_SidewalkNorth_01" }));
            Assert.That(NeighbourNames(r.Graph, "ExtraProp"), Is.EqualTo(new[] { "WP_SidewalkNorth_02" }));

            // Road lanes are open lanes of their own and connect to nothing else.
            foreach (string road in new[] { "RoadEast", "RoadWest" })
            {
                Assert.That(r.Graph.TryGetLane(road, out var nodes, out bool loop), Is.True);
                Assert.That(loop, Is.False);
                foreach (int node in nodes)
                {
                    foreach (int other in r.Graph.Neighbours(node))
                    {
                        Assert.That(r.Graph.NameOf(other), Does.StartWith("WP_" + road + "_"));
                    }
                }
            }
        }

        [Test]
        public void PedestrianLanes_AreLinkedAtTheirClosestPair_WhenWithinDistance()
        {
            var anchors = Minimal();
            // Alley runs south from near SidewalkNorth_02 (0,3): closest pair is (0,3)-(0.5,1).
            anchors.Add(A(Wp("Alley", 1), 0.5f, 1f));
            anchors.Add(A(Wp("Alley", 2), 0.5f, -2f));
            // SidewalkSouth is 3.5 m below the alley end and far from SidewalkNorth.
            anchors.Add(A(Wp("SidewalkSouth", 1), -8, -5.5f));
            anchors.Add(A(Wp("SidewalkSouth", 2), 0.5f, -5.5f));
            anchors.Add(A(Wp("SidewalkSouth", 3), 8, -5.5f));

            var r = WaypointGraphBuilder.Build(anchors);
            AssertClean(r);
            Assert.That(NeighbourNames(r.Graph, "WP_Alley_01"), Is.EquivalentTo(new[] { "WP_Alley_02", "WP_SidewalkNorth_02" }));
            Assert.That(NeighbourNames(r.Graph, "WP_Alley_02"), Is.EquivalentTo(new[] { "WP_Alley_01", "WP_SidewalkSouth_02" }));
            // North and South are 8.5 m apart: no direct link, only through the alley.
            for (int i = 1; i <= 3; i++)
            {
                Assert.That(NeighbourNames(r.Graph, Wp("SidewalkNorth", i)).Any(n => n.StartsWith("WP_SidewalkSouth_", StringComparison.Ordinal)), Is.False);
            }

            Assert.That(NeighbourNames(r.Graph, "WP_SidewalkSouth_01"), Is.EqualTo(new[] { "WP_SidewalkSouth_02" }));

            var path = new List<Vector3>();
            Assert.That(r.Graph.TryFindPath(Node(r.Graph, "StreetEntryWest"), Node(r.Graph, "WP_SidewalkSouth_03"), path), Is.True);
        }

        [Test]
        public void PedestrianLanes_BeyondLinkDistance_StayDisconnected()
        {
            var anchors = Minimal();
            anchors.Add(A(Wp("SidewalkSouth", 1), -8, -3f));
            anchors.Add(A(Wp("SidewalkSouth", 2), 8, -3f));

            var near = WaypointGraphBuilder.Build(anchors, 6f);
            AssertClean(near);
            Assert.That(NeighbourNames(near.Graph, "WP_SidewalkSouth_01"), Does.Contain("WP_SidewalkNorth_01"));

            var far = WaypointGraphBuilder.Build(anchors);
            AssertClean(far); // nothing required lives on the south sidewalk
            Assert.That(NeighbourNames(far.Graph, "WP_SidewalkSouth_01"), Is.EqualTo(new[] { "WP_SidewalkSouth_02" }));
            Assert.That(far.Graph.TryFindPath(Node(far.Graph, "StreetEntryWest"), Node(far.Graph, "WP_SidewalkSouth_01"), new List<Vector3>()), Is.False);
        }

        [Test]
        public void RoadLanes_AreNeverCrossLinked_EvenWhenClose()
        {
            var anchors = Minimal();
            anchors.Add(A(Wp("RoadEast", 1), -8, 3.5f));
            anchors.Add(A(Wp("RoadEast", 2), 8, 3.5f));
            var r = WaypointGraphBuilder.Build(anchors);
            AssertClean(r);
            Assert.That(NeighbourNames(r.Graph, "WP_RoadEast_01"), Is.EqualTo(new[] { "WP_RoadEast_02" }));
            Assert.That(NeighbourNames(r.Graph, "WP_RoadEast_02"), Is.EqualTo(new[] { "WP_RoadEast_01" }));
        }

        [Test]
        public void Error_DuplicateAnchorName_KeepsFirst()
        {
            var anchors = Minimal();
            anchors.Add(A("StreetEntryWest", 50, 50));
            anchors.Add(A(Wp("SidewalkNorth", 2), 60, 60));
            var r = WaypointGraphBuilder.Build(anchors);
            AssertHasMessage(r.Errors, "Duplicate", "StreetEntryWest");
            AssertHasMessage(r.Errors, "Duplicate", "WP_SidewalkNorth_02");
            Assert.That(r.Errors.Count, Is.EqualTo(2));
            WaypointGraphTests.AssertNear(V(-9, 3), r.Graph.PositionOf(Node(r.Graph, "StreetEntryWest")));
            WaypointGraphTests.AssertNear(V(0, 3), r.Graph.PositionOf(Node(r.Graph, "WP_SidewalkNorth_02")));
        }

        [Test]
        public void Error_LaneWithOnePoint()
        {
            var anchors = Minimal();
            anchors.Add(A(Wp("Alley", 1), 0, 1));
            var r = WaypointGraphBuilder.Build(anchors);
            AssertHasMessage(r.Errors, "Alley", "at least 2");
            Assert.That(r.Graph.LaneNames, Is.EqualTo(new[] { "SidewalkNorth" }));
            Assert.That(r.Graph.TryFind("WP_Alley_01", out _), Is.False);
        }

        [Test]
        public void Error_DuplicateLaneNumber()
        {
            var anchors = Minimal();
            anchors.Add(A("WP_SidewalkNorth_002", 1, 3));
            var r = WaypointGraphBuilder.Build(anchors);
            AssertHasMessage(r.Errors, "SidewalkNorth", "numbered 2", "'WP_SidewalkNorth_002' is ignored");
            Assert.That(r.Errors.Count, Is.EqualTo(1), string.Join(" | ", r.Errors));
            Assert.That(r.Graph.TryFind("WP_SidewalkNorth_002", out _), Is.False);
            Assert.That(r.Graph.TryGetLane("SidewalkNorth", out var nodes, out _), Is.True);
            Assert.That(nodes.Count, Is.EqualTo(3));
        }

        [Test]
        public void Warning_GapInNumbering_IsNotAnError()
        {
            var anchors = new List<KeyValuePair<string, Vector3>>
            {
                A(Wp("SidewalkNorth", 1), -8, 3),
                A(Wp("SidewalkNorth", 2), -4, 3),
                A(Wp("SidewalkNorth", 5), 8, 3),
                A("StreetEntryWest", -9, 3),
                A("StreetEntryEast", 9, 3),
            };
            var r = WaypointGraphBuilder.Build(anchors);
            Assert.That(r.Errors, Is.Empty, string.Join(" | ", r.Errors));
            Assert.That(r.Warnings.Count, Is.EqualTo(1));
            AssertHasMessage(r.Warnings, "gap", "WP_SidewalkNorth_02", "WP_SidewalkNorth_05");
            Assert.That(NeighbourNames(r.Graph, "WP_SidewalkNorth_02"), Is.EquivalentTo(new[] { "WP_SidewalkNorth_01", "WP_SidewalkNorth_05" }));
        }

        [Test]
        public void Warning_MalformedLanePointName_IsIgnored()
        {
            var anchors = Minimal();
            anchors.Add(A("WP_SidewalkNorth_4", 9, 3));
            var r = WaypointGraphBuilder.Build(anchors);
            Assert.That(r.Errors, Is.Empty);
            AssertHasMessage(r.Warnings, "WP_SidewalkNorth_4");
            Assert.That(r.Graph.TryFind("WP_SidewalkNorth_4", out _), Is.False);
        }

        [Test]
        public void Warning_UnknownLane_IsAddedButNotLinked()
        {
            var anchors = Minimal();
            anchors.Add(A(Wp("Rooftop", 1), 0, 3.5f));
            anchors.Add(A(Wp("Rooftop", 2), 1, 3.5f));
            var r = WaypointGraphBuilder.Build(anchors);
            Assert.That(r.Errors, Is.Empty);
            AssertHasMessage(r.Warnings, "Unknown lane", "Rooftop");
            Assert.That(NeighbourNames(r.Graph, "WP_Rooftop_01"), Is.EqualTo(new[] { "WP_Rooftop_02" }));
        }

        [Test]
        public void Error_TableApproachUnreachableFromStreetEntryWest()
        {
            var anchors = Minimal();
            // Alley 10 m away from the sidewalk: not linked, and the approach attaches to it.
            anchors.Add(A(Wp("Alley", 1), 0, -7));
            anchors.Add(A(Wp("Alley", 2), 0, -12));
            anchors.Add(A("TableApproach_04", 0.5f, -7.5f));
            anchors.Add(A("TableApproach_01", 0.5f, 2.5f));
            var r = WaypointGraphBuilder.Build(anchors);
            Assert.That(r.Errors.Count, Is.EqualTo(1), string.Join(" | ", r.Errors));
            AssertHasMessage(r.Errors, "TableApproach_04", "not reachable", "StreetEntryWest");
        }

        [Test]
        public void Error_StreetEntryEastUnreachable()
        {
            var anchors = new List<KeyValuePair<string, Vector3>>
            {
                A(Wp("SidewalkNorth", 1), -8, 3),
                A(Wp("SidewalkNorth", 2), -4, 3),
                A(Wp("SidewalkSouth", 1), 4, -6),
                A(Wp("SidewalkSouth", 2), 8, -6),
                A("StreetEntryWest", -9, 3),
                A("StreetEntryEast", 9, -6),
            };
            var r = WaypointGraphBuilder.Build(anchors);
            Assert.That(r.Errors.Count, Is.EqualTo(1), string.Join(" | ", r.Errors));
            AssertHasMessage(r.Errors, "StreetEntryEast", "not reachable");
        }

        [Test]
        public void Error_MissingStreetEntries()
        {
            var anchors = Minimal().Where(a => !a.Key.StartsWith("StreetEntry", StringComparison.Ordinal)).ToList();
            var r = WaypointGraphBuilder.Build(anchors);
            AssertHasMessage(r.Errors, "Missing", "StreetEntryWest");
            AssertHasMessage(r.Errors, "Missing", "StreetEntryEast");
        }

        [Test]
        public void Error_NoPedestrianLane()
        {
            var anchors = new List<KeyValuePair<string, Vector3>>
            {
                A(Wp("RoadEast", 1), -8, 5),
                A(Wp("RoadEast", 2), 8, 5),
                A("StreetEntryWest", -9, 3),
                A("StreetEntryEast", 9, 3),
                A("TableApproach_01", 0, 1),
            };
            var r = WaypointGraphBuilder.Build(anchors);
            AssertHasMessage(r.Errors, "No pedestrian lane");
            AssertHasMessage(r.Errors, "StreetEntryEast", "not reachable");
            AssertHasMessage(r.Errors, "TableApproach_01", "not reachable");
            Assert.That(r.Graph.Neighbours(Node(r.Graph, "StreetEntryWest")), Is.Empty);
        }

        [Test]
        public void Error_EmptyNameAndNonFinitePosition()
        {
            var anchors = Minimal();
            anchors.Add(new KeyValuePair<string, Vector3>("", V(0, 0)));
            anchors.Add(new KeyValuePair<string, Vector3>(null, V(0, 0)));
            anchors.Add(new KeyValuePair<string, Vector3>("BikeParking_02", new Vector3(float.NaN, 0, 0)));
            var r = WaypointGraphBuilder.Build(anchors);
            Assert.That(r.Errors.Count, Is.EqualTo(3), string.Join(" | ", r.Errors));
            AssertHasMessage(r.Errors, "BikeParking_02", "non-finite");
            Assert.That(r.Graph.Count, Is.EqualTo(5));
        }

        [Test]
        public void Build_RejectsInvalidArguments()
        {
            Assert.Throws<ArgumentNullException>(() => WaypointGraphBuilder.Build(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => WaypointGraphBuilder.Build(Minimal(), -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => WaypointGraphBuilder.Build(Minimal(), float.NaN));
        }

        [Test]
        public void Build_IsDeterministic()
        {
            var a = WaypointGraphBuilder.Build(RealisticStreet.Anchors()).Graph;
            var b = WaypointGraphBuilder.Build(RealisticStreet.Anchors()).Graph;
            Assert.That(b.Count, Is.EqualTo(a.Count));
            for (int i = 0; i < a.Count; i++)
            {
                Assert.That(b.NameOf(i), Is.EqualTo(a.NameOf(i)));
                Assert.That(b.Neighbours(i), Is.EqualTo(a.Neighbours(i)));
            }
        }

        [Test]
        public void RealisticStreet_BuildsWithoutErrorsOrWarnings()
        {
            var r = WaypointGraphBuilder.Build(RealisticStreet.Anchors());
            AssertClean(r);
            Assert.That(r.Graph.LaneNames, Is.EquivalentTo(new[] { "Alley", "RoadEast", "RoadWest", "SidewalkNorth", "SidewalkSouth" }));
        }

        [Test]
        public void RealisticStreet_EveryTableApproachIsReachableFromBothEntries()
        {
            var g = WaypointGraphBuilder.Build(RealisticStreet.Anchors()).Graph;
            int west = Node(g, "StreetEntryWest");
            int east = Node(g, "StreetEntryEast");
            var path = new List<Vector3>();
            for (int t = 1; t <= 10; t++)
            {
                string name = "TableApproach_" + t.ToString("00", CultureInfo.InvariantCulture);
                int approach = Node(g, name);
                Assert.That(g.TryFindPath(west, approach, path), Is.True, name + " from west");
                WaypointGraphTests.AssertNear(g.PositionOf(west), path[0]);
                WaypointGraphTests.AssertNear(g.PositionOf(approach), path[path.Count - 1]);
                Assert.That(g.TryFindPath(east, approach, path), Is.True, name + " from east");
                Assert.That(g.TryFindPath(approach, west, path), Is.True, name + " back to west");
            }

            Assert.That(g.TryFindPath(west, east, path), Is.True);
        }

        [Test]
        public void RealisticStreet_ApproachesSitInFrontOfTheirTable_AndLinkOnlyToPedestrianLanes()
        {
            var g = WaypointGraphBuilder.Build(RealisticStreet.Anchors()).Graph;
            for (int t = 1; t <= 10; t++)
            {
                string name = "TableApproach_" + t.ToString("00", CultureInfo.InvariantCulture);
                Vector3 table = RealisticStreet.Tables[t - 1];
                WaypointGraphTests.AssertNear(new Vector3(table.x, table.y, table.z + 0.8f), g.PositionOf(Node(g, name)));
            }

            for (int node = 0; node < g.Count; node++)
            {
                string name = g.NameOf(node);
                if (name.StartsWith("WP_", StringComparison.Ordinal))
                {
                    continue;
                }

                var neighbours = NeighbourNames(g, name);
                Assert.That(neighbours.Length, Is.EqualTo(1), name);
                Assert.That(neighbours[0], Does.StartWith("WP_Sidewalk").Or.StartWith("WP_Alley"), name);
            }
        }

        [Test]
        public void RealisticStreet_WalkingEveryRoute_ArrivesExactly()
        {
            var g = WaypointGraphBuilder.Build(RealisticStreet.Anchors()).Graph;
            var path = new List<Vector3>();
            for (int t = 1; t <= 10; t++)
            {
                int approach = Node(g, "TableApproach_" + t.ToString("00", CultureInfo.InvariantCulture));
                Assert.That(g.TryFindPath(Node(g, "StreetEntryWest"), approach, path), Is.True);
                var follower = new PathFollower(path, 1.4f);
                int steps = 0;
                while (!follower.Arrived && steps < 100000)
                {
                    follower.Step(1d / 60d);
                    steps++;
                }

                Assert.That(follower.Arrived, Is.True);
                WaypointGraphTests.AssertNear(g.PositionOf(approach), follower.Position, 0f);
            }
        }
    }

    /// <summary>
    /// A realistic street around the stall: 10 tables (scene positions), approaches 0.8 m towards +z,
    /// both sidewalks, an alley joining them, two road lanes, entries at x = ±9 and bike parking near the road.
    /// </summary>
    internal static class RealisticStreet
    {
        public static readonly Vector3[] Tables =
        {
            new Vector3(-2.1f, 0f, 1.7f),
            new Vector3(-3.35f, 0f, 0.2f),
            new Vector3(-4.6f, 0f, 1.7f),
            new Vector3(-5.85f, 0f, 0.2f),
            new Vector3(-7.1f, 0f, 1.7f),
            new Vector3(-3.35f, 0f, -1.8f),
            new Vector3(-5.85f, 0f, -1.8f),
            new Vector3(4.2f, 0f, 0.6f),
            new Vector3(5.6f, 0f, -0.9f),
            new Vector3(4.2f, 0f, -2.2f),
        };

        public static List<KeyValuePair<string, Vector3>> Anchors()
        {
            var list = new List<KeyValuePair<string, Vector3>>();
            void Add(string name, float x, float y, float z) => list.Add(new KeyValuePair<string, Vector3>(name, new Vector3(x, y, z)));
            string N(int n) => n.ToString("00", CultureInfo.InvariantCulture);

            // Sidewalks: x = -10.5 .. 10.5 every 1.5 m, at z = +3.6 (north) and z = -3.6 (south).
            for (int i = 0; i < 15; i++)
            {
                float x = -10.5f + 1.5f * i;
                Add("WP_SidewalkNorth_" + N(i + 1), x, 0f, 3.6f);
                Add("WP_SidewalkSouth_" + N(i + 1), x, 0f, -3.6f);
            }

            // Alley between the two table groups (x = 0.75), from just south of the north sidewalk to just north of the south one.
            for (int i = 0; i < 5; i++)
            {
                Add("WP_Alley_" + N(i + 1), 0.75f, 0f, 2.4f - 1.2f * i);
            }

            // Road lanes north of the north sidewalk.
            for (int i = 0; i < 9; i++)
            {
                Add("WP_RoadEast_" + N(i + 1), -12f + 3f * i, 0f, 6f);
                Add("WP_RoadWest_" + N(i + 1), 12f - 3f * i, 0f, 7.5f);
            }

            Add("StreetEntryWest", -9f, 0f, 4.2f);
            Add("StreetEntryEast", 9f, 0f, 4.2f);
            for (int t = 0; t < Tables.Length; t++)
            {
                Vector3 p = Tables[t];
                Add("TableApproach_" + N(t + 1), p.x, p.y, p.z + 0.8f);
            }

            Add("BikeParking_01", 7.5f, 0f, 5.1f);
            Add("BikeParking_02", 8.5f, 0f, 5.1f);
            return list;
        }
    }
}
