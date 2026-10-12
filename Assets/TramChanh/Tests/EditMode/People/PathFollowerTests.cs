using System;
using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.People;
using UnityEngine;

namespace TramChanh.Tests.EditMode.People
{
    /// <summary>PEOPLE-D: constant-speed path following driven by game-clock deltas.</summary>
    public sealed class PathFollowerTests
    {
        private static Vector3 V(float x, float z) => WaypointGraphTests.V(x, z);

        private static void Near(Vector3 expected, Vector3 actual, float tolerance = 1e-4f) =>
            WaypointGraphTests.AssertNear(expected, actual, tolerance);

        [Test]
        public void StartsAtFirstPoint_FacingFirstSegment()
        {
            var f = new PathFollower(new[] { V(0, 0), V(0, 4) }, 2f);
            Near(V(0, 0), f.Position, 0f);
            Near(V(0, 1), f.Forward);
            Assert.That(f.Arrived, Is.False);
            Assert.That(f.CurrentSpeed, Is.EqualTo(2f));
            Assert.That(f.RemainingDistance, Is.EqualTo(4f).Within(1e-5f));
        }

        [Test]
        public void MovesAtConstantSpeed_AndArrivesExactly()
        {
            var f = new PathFollower(new[] { V(0, 0), V(3, 0), V(3, 4) }, 1f, 0f);
            f.Step(1d);
            Near(V(1, 0), f.Position);
            Assert.That(f.RemainingDistance, Is.EqualTo(6f).Within(1e-5f));
            f.Step(2.5d);
            Near(V(3, 0.5f), f.Position);
            Near(V(0, 1), f.Forward);
            f.Step(3.5d);
            Assert.That(f.Arrived, Is.True);
            Near(V(3, 4), f.Position, 0f);
            Assert.That(f.RemainingDistance, Is.EqualTo(0f));
            Assert.That(f.CurrentSpeed, Is.EqualTo(0f));
        }

        [Test]
        public void ManySmallSteps_ArriveExactlyOnTheLastPoint()
        {
            var end = new Vector3(-7.37f, 0.12f, 2.53f);
            var f = new PathFollower(new[] { V(0, 0), V(-3.1f, 1.7f), end }, 1.3f);
            int steps = 0;
            while (!f.Arrived && steps < 100000)
            {
                f.Step(1d / 60d);
                steps++;
            }

            Assert.That(f.Arrived, Is.True);
            Near(end, f.Position, 0f);
        }

        [Test]
        public void NeverOvershoots_TheFinalPoint()
        {
            var f = new PathFollower(new[] { V(0, 0), V(10, 0) }, 3f, 0f);
            float lastX = 0f;
            for (int i = 0; i < 100; i++)
            {
                f.Step(0.37d);
                Assert.That(f.Position.x, Is.LessThanOrEqualTo(10f));
                Assert.That(f.Position.x, Is.GreaterThanOrEqualTo(lastX));
                lastX = f.Position.x;
            }

            Assert.That(f.Arrived, Is.True);
            Near(V(10, 0), f.Position, 0f);
        }

        [Test]
        public void PassesThroughCorners_WithoutCuttingThem()
        {
            var f = new PathFollower(new[] { V(0, 0), V(1, 0), V(1, 1) }, 1f, 0f);
            f.Step(1.5d);
            // 1 m along x then 0.5 m along z; a corner-cutting follower would land elsewhere.
            Near(V(1, 0.5f), f.Position);
        }

        [Test]
        public void SnapsToTheEnd_WithinArriveRadius()
        {
            var f = new PathFollower(new[] { V(0, 0), V(1, 0) }, 1f, 0.1f);
            f.Step(0.85d);
            Assert.That(f.Arrived, Is.False);
            f.Step(0.06d);
            Assert.That(f.Arrived, Is.True);
            Near(V(1, 0), f.Position, 0f);
        }

        [Test]
        public void Forward_IsHorizontalUnit_AndKeepsLastNonZero()
        {
            // second segment is purely vertical (a step), third goes -x
            var path = new[] { V(0, 0), new Vector3(2f, 0f, 2f), new Vector3(2f, 1f, 2f), new Vector3(0f, 1f, 2f) };
            var f = new PathFollower(path, 1f, 0f);
            float s = (float)(1d / Math.Sqrt(2d));
            Near(new Vector3(s, 0f, s), f.Forward);

            f.Step(Math.Sqrt(8d) + 0.5d); // halfway up the vertical segment
            Near(new Vector3(2f, 0.5f, 2f), f.Position);
            Near(new Vector3(s, 0f, s), f.Forward);
            Assert.That(f.Forward.y, Is.EqualTo(0f));

            f.Step(1d);
            Near(V(-1, 0), f.Forward);
            f.Step(10d);
            Assert.That(f.Arrived, Is.True);
            Near(V(-1, 0), f.Forward);
        }

        [Test]
        public void Forward_DefaultsToPlusZ_WhenThereIsNoHorizontalMovement()
        {
            var f = new PathFollower(new[] { V(1, 1), new Vector3(1f, 2f, 1f) }, 1f);
            Near(V(0, 1), f.Forward);
        }

        [TestCase(0d)]
        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void PausedOrInvalidDeltas_DoNothing(double delta)
        {
            var f = new PathFollower(new[] { V(0, 0), V(5, 0) }, 1f);
            f.Step(1d);
            var before = f.Position;
            float remaining = f.RemainingDistance;
            f.Step(delta);
            Near(before, f.Position, 0f);
            Assert.That(f.RemainingDistance, Is.EqualTo(remaining));
            Assert.That(f.Arrived, Is.False);
            Assert.That(float.IsNaN(f.Position.x), Is.False);
        }

        [Test]
        public void PauseAndResume_MatchesUninterruptedWalk()
        {
            var path = new[] { V(0, 0), V(2, 1), V(4, -1), V(6, 0) };
            var a = new PathFollower(path, 1.2f);
            var b = new PathFollower(path, 1.2f);
            for (int i = 0; i < 120; i++)
            {
                a.Step(0.05d);
                b.Step(0.05d);
                b.Step(0d);
                b.Step(double.NaN);
            }

            Near(a.Position, b.Position, 0f);
            Assert.That(b.RemainingDistance, Is.EqualTo(a.RemainingDistance));
        }

        [Test]
        public void SingleLongStep_ArrivesAtTheEnd()
        {
            var f = new PathFollower(new[] { V(0, 0), V(5, 0), V(5, 5), V(0, 5) }, 1.5f, 0f);
            f.Step(3600d);
            Assert.That(f.Arrived, Is.True);
            Near(V(0, 5), f.Position, 0f);
            Near(V(-1, 0), f.Forward);
            f.Step(1d);
            Near(V(0, 5), f.Position, 0f);
        }

        [Test]
        public void OnePointPath_IsArrivedImmediately()
        {
            var f = new PathFollower(new[] { V(2, 3) }, 1f);
            Assert.That(f.Arrived, Is.True);
            Near(V(2, 3), f.Position, 0f);
            Assert.That(f.RemainingDistance, Is.EqualTo(0f));
            Assert.That(f.CurrentSpeed, Is.EqualTo(0f));
            Near(V(0, 1), f.Forward);
            f.Step(1d);
            Near(V(2, 3), f.Position, 0f);
        }

        [Test]
        public void DuplicatePoints_AreHandled()
        {
            var f = new PathFollower(new[] { V(0, 0), V(0, 0), V(1, 0), V(1, 0) }, 1f, 0f);
            Near(V(1, 0), f.Forward);
            f.Step(0.5d);
            Near(V(0.5f, 0), f.Position);
            f.Step(0.5d);
            Assert.That(f.Arrived, Is.True);
            Near(V(1, 0), f.Position, 0f);
        }

        [Test]
        public void CopiesThePath()
        {
            var path = new List<Vector3> { V(0, 0), V(2, 0) };
            var f = new PathFollower(path, 1f, 0f);
            path[1] = V(100, 0);
            path.Add(V(200, 0));
            f.Step(10d);
            Near(V(2, 0), f.Position, 0f);
        }

        [Test]
        public void Constructor_RejectsInvalidArguments()
        {
            var ok = new[] { V(0, 0), V(1, 0) };
            Assert.Throws<ArgumentNullException>(() => new PathFollower(null, 1f));
            Assert.Throws<ArgumentException>(() => new PathFollower(new Vector3[0], 1f));
            Assert.Throws<ArgumentException>(() => new PathFollower(new[] { V(0, 0), new Vector3(float.NaN, 0f, 0f) }, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PathFollower(ok, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PathFollower(ok, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PathFollower(ok, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PathFollower(ok, 1f, -0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PathFollower(ok, 1f, float.PositiveInfinity));
        }

        [Test]
        public void Soak_5000Steps_StaysOnPathAndArrives()
        {
            var path = new List<Vector3>();
            for (int i = 0; i <= 50; i++)
            {
                path.Add(V(i, (i % 2) * 0.7f));
            }

            var f = new PathFollower(path, 0.02f);
            float last = f.RemainingDistance;
            for (int i = 0; i < 5000 && !f.Arrived; i++)
            {
                f.Step(0.25d + (i % 7) * 0.01d);
                Assert.That(f.RemainingDistance, Is.LessThanOrEqualTo(last + 1e-4f));
                Assert.That(f.Position.x, Is.InRange(0f, 50f));
                last = f.RemainingDistance;
            }

            f.Step(1e9d);
            Assert.That(f.Arrived, Is.True);
            Near(path[path.Count - 1], f.Position, 0f);
        }
    }
}
