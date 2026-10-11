using System;
using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.People;
using UnityEngine;

namespace TramChanh.Tests.EditMode.People
{
    /// <summary>PEOPLE-D: waypoint graph structure, lanes and A* paths.</summary>
    public sealed class WaypointGraphTests
    {
        internal static Vector3 V(float x, float z) => new Vector3(x, 0f, z);

        internal static void AssertNear(Vector3 expected, Vector3 actual, float tolerance = 1e-4f)
        {
            Assert.That(Vector3.Distance(expected, actual), Is.LessThanOrEqualTo(tolerance),
                "expected (" + expected.x + ", " + expected.y + ", " + expected.z + ") but was (" + actual.x + ", " + actual.y + ", " + actual.z + ")");
        }

        [Test]
        public void AddNode_AssignsSequentialIndices_AndStoresNameAndPosition()
        {
            var g = new WaypointGraph();
            Assert.That(g.Count, Is.EqualTo(0));
            int a = g.AddNode("A", V(1, 2));
            int b = g.AddNode(null, V(3, 4));
            int c = g.AddNode(null, V(5, 6));
            Assert.That(new[] { a, b, c }, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(g.Count, Is.EqualTo(3));
            Assert.That(g.NameOf(a), Is.EqualTo("A"));
            Assert.That(g.NameOf(b), Is.Null);
            AssertNear(V(3, 4), g.PositionOf(b));
            Assert.That(g.TryFind("A", out int found), Is.True);
            Assert.That(found, Is.EqualTo(a));
            Assert.That(g.TryFind("missing", out found), Is.False);
            Assert.That(g.TryFind(null, out found), Is.False);
        }

        [Test]
        public void AddNode_DuplicateName_Throws_ButUnnamedNodesMayRepeat()
        {
            var g = new WaypointGraph();
            g.AddNode("A", V(0, 0));
            Assert.Throws<ArgumentException>(() => g.AddNode("A", V(1, 1)));
            Assert.That(g.Count, Is.EqualTo(1));
            g.AddNode(null, V(0, 0));
            g.AddNode(null, V(0, 0));
            Assert.That(g.Count, Is.EqualTo(3));
        }

        [Test]
        public void AddNode_NonFinitePosition_Throws()
        {
            var g = new WaypointGraph();
            Assert.Throws<ArgumentException>(() => g.AddNode("n", new Vector3(float.NaN, 0f, 0f)));
            Assert.Throws<ArgumentException>(() => g.AddNode("i", new Vector3(0f, float.PositiveInfinity, 0f)));
            Assert.That(g.Count, Is.EqualTo(0));
        }

        [Test]
        public void Connect_IsUndirected_AndIdempotent()
        {
            var g = new WaypointGraph();
            int a = g.AddNode("A", V(0, 0));
            int b = g.AddNode("B", V(1, 0));
            g.Connect(a, b);
            g.Connect(a, b);
            g.Connect(b, a);
            g.Connect(a, a);
            Assert.That(g.Neighbours(a), Is.EqualTo(new[] { b }));
            Assert.That(g.Neighbours(b), Is.EqualTo(new[] { a }));
        }

        [Test]
        public void InvalidIndices_Throw()
        {
            var g = new WaypointGraph();
            int a = g.AddNode("A", V(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => g.Connect(a, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => g.PositionOf(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => g.NameOf(5));
            Assert.Throws<ArgumentOutOfRangeException>(() => g.Neighbours(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => g.TryFindPath(a, 3, new List<Vector3>()));
            Assert.Throws<ArgumentNullException>(() => g.TryFindPath(a, a, null));
        }

        [Test]
        public void AddLane_ConnectsConsecutiveNodes_AndIsQueryable()
        {
            var g = new WaypointGraph();
            int a = g.AddNode(null, V(0, 0));
            int b = g.AddNode(null, V(1, 0));
            int c = g.AddNode(null, V(2, 0));
            g.AddLane("L", new[] { a, b, c }, false);

            Assert.That(g.Neighbours(a), Is.EqualTo(new[] { b }));
            Assert.That(g.Neighbours(b), Is.EquivalentTo(new[] { a, c }));
            Assert.That(g.Neighbours(c), Is.EqualTo(new[] { b }));
            Assert.That(g.TryGetLane("L", out var nodes, out bool loop), Is.True);
            Assert.That(nodes, Is.EqualTo(new[] { a, b, c }));
            Assert.That(loop, Is.False);
            Assert.That(g.LaneNames, Is.EqualTo(new[] { "L" }));
            Assert.That(g.TryGetLane("nope", out nodes, out loop), Is.False);
            Assert.That(nodes, Is.Null);
        }

        [Test]
        public void AddLane_Loop_ConnectsLastToFirst()
        {
            var g = new WaypointGraph();
            int a = g.AddNode(null, V(0, 0));
            int b = g.AddNode(null, V(1, 0));
            int c = g.AddNode(null, V(1, 1));
            g.AddLane("Ring", new[] { a, b, c }, true);
            Assert.That(g.Neighbours(a), Is.EquivalentTo(new[] { b, c }));
            Assert.That(g.TryGetLane("Ring", out _, out bool loop) && loop, Is.True);
        }

        [Test]
        public void AddLane_CopiesInput_AndRejectsBadArguments()
        {
            var g = new WaypointGraph();
            int a = g.AddNode(null, V(0, 0));
            int b = g.AddNode(null, V(1, 0));
            var list = new List<int> { a, b };
            g.AddLane("L", list, false);
            list[0] = b;
            g.TryGetLane("L", out var nodes, out _);
            Assert.That(nodes[0], Is.EqualTo(a));

            Assert.Throws<ArgumentException>(() => g.AddLane("L", new[] { a, b }, false), "duplicate lane");
            Assert.Throws<ArgumentException>(() => g.AddLane("One", new[] { a }, false), "single node");
            Assert.Throws<ArgumentException>(() => g.AddLane("", new[] { a, b }, false), "empty name");
            Assert.Throws<ArgumentNullException>(() => g.AddLane("N", null, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => g.AddLane("Bad", new[] { a, 9 }, false));
            Assert.That(g.LaneNames, Is.EqualTo(new[] { "L" }));
        }

        [Test]
        public void TryFindPath_PicksShortestRoute_NotFewestHops()
        {
            // a -> far -> d is 2 hops but long; a -> b -> c -> d is 3 hops and shorter.
            var g = new WaypointGraph();
            int a = g.AddNode("a", V(0, 0));
            int b = g.AddNode("b", V(1, 0));
            int c = g.AddNode("c", V(2, 0));
            int d = g.AddNode("d", V(3, 0));
            int far = g.AddNode("far", V(1.5f, 10f));
            g.Connect(a, far);
            g.Connect(far, d);
            g.Connect(a, b);
            g.Connect(b, c);
            g.Connect(c, d);

            var path = new List<Vector3> { V(99, 99) };
            Assert.That(g.TryFindPath(a, d, path), Is.True);
            Assert.That(path.Count, Is.EqualTo(4));
            AssertNear(V(0, 0), path[0]);
            AssertNear(V(1, 0), path[1]);
            AssertNear(V(2, 0), path[2]);
            AssertNear(V(3, 0), path[3]);

            Assert.That(g.TryFindPath(d, a, path), Is.True);
            AssertNear(V(3, 0), path[0]);
            AssertNear(V(0, 0), path[3]);
        }

        [Test]
        public void TryFindPath_UsesEdgeLength_OnAGrid()
        {
            // 3x3 grid with a diagonal shortcut from the centre to the corner.
            var g = new WaypointGraph();
            var id = new int[3, 3];
            for (int x = 0; x < 3; x++)
            {
                for (int z = 0; z < 3; z++)
                {
                    id[x, z] = g.AddNode(null, V(x, z));
                }
            }

            for (int x = 0; x < 3; x++)
            {
                for (int z = 0; z < 3; z++)
                {
                    if (x < 2) g.Connect(id[x, z], id[x + 1, z]);
                    if (z < 2) g.Connect(id[x, z], id[x, z + 1]);
                }
            }

            g.Connect(id[1, 1], id[2, 2]);
            g.Connect(id[0, 0], id[1, 1]);
            var path = new List<Vector3>();
            Assert.That(g.TryFindPath(id[0, 0], id[2, 2], path), Is.True);
            Assert.That(path.Count, Is.EqualTo(3));
            AssertNear(V(1, 1), path[1]);
        }

        [Test]
        public void TryFindPath_SameNode_ReturnsSinglePoint()
        {
            var g = new WaypointGraph();
            int a = g.AddNode(null, V(4, 5));
            var path = new List<Vector3> { V(1, 1), V(2, 2) };
            Assert.That(g.TryFindPath(a, a, path), Is.True);
            Assert.That(path.Count, Is.EqualTo(1));
            AssertNear(V(4, 5), path[0]);
        }

        [Test]
        public void TryFindPath_Unreachable_ReturnsFalse_AndClearsResult()
        {
            var g = new WaypointGraph();
            int a = g.AddNode(null, V(0, 0));
            int b = g.AddNode(null, V(1, 0));
            int island = g.AddNode(null, V(5, 5));
            int island2 = g.AddNode(null, V(6, 5));
            g.Connect(a, b);
            g.Connect(island, island2);
            var path = new List<Vector3> { V(7, 7) };
            Assert.That(g.TryFindPath(a, island2, path), Is.False);
            Assert.That(path, Is.Empty);
        }

        [Test]
        public void TryFindPath_IsDeterministic_ForEqualCostAlternatives()
        {
            WaypointGraph Build()
            {
                var g = new WaypointGraph();
                int a = g.AddNode(null, V(0, 0));
                int up = g.AddNode(null, V(1, 1));
                int down = g.AddNode(null, V(1, -1));
                int b = g.AddNode(null, V(2, 0));
                g.Connect(a, up);
                g.Connect(a, down);
                g.Connect(up, b);
                g.Connect(down, b);
                return g;
            }

            var first = new List<Vector3>();
            Build().TryFindPath(0, 3, first);
            for (int i = 0; i < 5; i++)
            {
                var again = new List<Vector3>();
                Build().TryFindPath(0, 3, again);
                Assert.That(again.Count, Is.EqualTo(first.Count));
                for (int k = 0; k < again.Count; k++)
                {
                    AssertNear(first[k], again[k], 0f);
                }
            }
        }

        [Test]
        public void Nearest_WithoutLane_SearchesAllNodes()
        {
            var g = new WaypointGraph();
            Assert.That(g.Nearest(V(0, 0), null), Is.EqualTo(-1));
            g.AddNode(null, V(0, 0));
            int close = g.AddNode(null, V(5, 5));
            g.AddNode(null, V(10, 10));
            Assert.That(g.Nearest(V(5.4f, 4.4f), null), Is.EqualTo(close));
        }

        [Test]
        public void Nearest_WithLaneFilter_IgnoresCloserNodesOutsideTheLane()
        {
            var g = new WaypointGraph();
            int s0 = g.AddNode("S0", V(0, 3));
            int s1 = g.AddNode("S1", V(4, 3));
            int r0 = g.AddNode("R0", V(0, 0));
            int r1 = g.AddNode("R1", V(4, 0));
            g.AddLane("Sidewalk", new[] { s0, s1 }, false);
            g.AddLane("Road", new[] { r0, r1 }, false);

            var probe = V(3.5f, 0.5f);
            Assert.That(g.Nearest(probe, null), Is.EqualTo(r1));
            Assert.That(g.Nearest(probe, "Road"), Is.EqualTo(r1));
            Assert.That(g.Nearest(probe, "Sidewalk"), Is.EqualTo(s1));
            Assert.That(g.Nearest(probe, "Unknown"), Is.EqualTo(-1));
        }
    }
}
