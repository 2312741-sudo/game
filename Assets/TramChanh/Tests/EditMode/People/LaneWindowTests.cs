using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.People;
using UnityEngine;

namespace TramChanh.Tests.EditMode.People
{
    public sealed class LaneWindowTests
    {
        private static List<Vector3> Lane(float fromX, float toX, float step, float z = 0f)
        {
            var points = new List<Vector3>();
            for (float x = fromX; x <= toX + 1e-3f; x += step) { points.Add(new Vector3(x, 0f, z)); }
            return points;
        }

        [Test]
        public void ZeroOrNegativeRadius_KeepsTheWholeLane()
        {
            List<Vector3> lane = Lane(-250f, 250f, 2.5f);

            Assert.That(LaneWindow.TryGetRange(lane, Vector3.zero, 0f, out int first, out int last), Is.True);
            Assert.That(first, Is.EqualTo(0));
            Assert.That(last, Is.EqualTo(lane.Count - 1));
            Assert.That(LaneWindow.TryGetRange(lane, Vector3.zero, -5f, out first, out last), Is.True);
            Assert.That(last, Is.EqualTo(lane.Count - 1));
        }

        [Test]
        public void PositiveRadius_KeepsOnlyThePointsAroundTheCentre()
        {
            List<Vector3> lane = Lane(-250f, 250f, 2.5f);

            Assert.That(LaneWindow.TryGetRange(lane, Vector3.zero, 60f, out int first, out int last), Is.True);

            Assert.That(lane[first].x, Is.EqualTo(-60f).Within(1e-3f));
            Assert.That(lane[last].x, Is.EqualTo(60f).Within(1e-3f));
        }

        [Test]
        public void TheOffsetOfALaneFromTheCentreCountsOnTheGroundPlane()
        {
            List<Vector3> lane = Lane(-250f, 250f, 2.5f, 14.5f);

            LaneWindow.TryGetRange(lane, Vector3.zero, 60f, out int first, out int last);

            // sqrt(60^2 - 14.5^2) is about 58.2, so the last point at or inside it is x = 57.5.
            Assert.That(lane[last].x, Is.EqualTo(57.5f).Within(1e-3f));
            Assert.That(lane[first].x, Is.EqualTo(-57.5f).Within(1e-3f));
        }

        [Test]
        public void CentreNearOneEnd_GivesAShortRunInsideTheLane()
        {
            List<Vector3> lane = Lane(0f, 100f, 2.5f);

            Assert.That(LaneWindow.TryGetRange(lane, new Vector3(5f, 0f, 0f), 10f, out int first, out int last), Is.True);

            Assert.That(first, Is.EqualTo(0));
            Assert.That(lane[last].x, Is.EqualTo(15f).Within(1e-3f));
        }

        [Test]
        public void ALaneEntirelyOutsideTheRadius_IsRejected()
        {
            List<Vector3> lane = Lane(100f, 200f, 2.5f);

            Assert.That(LaneWindow.TryGetRange(lane, Vector3.zero, 60f, out _, out _), Is.False);
        }

        [Test]
        public void TooFewPoints_AreRejected()
        {
            Assert.That(LaneWindow.TryGetRange(null, Vector3.zero, 60f, out _, out _), Is.False);
            Assert.That(LaneWindow.TryGetRange(new List<Vector3> { Vector3.zero }, Vector3.zero, 60f, out _, out _), Is.False);
            Assert.That(LaneWindow.TryGetRange(new List<Vector3> { Vector3.zero, new Vector3(1f, 0f, 0f) }, Vector3.zero, float.NaN, out int first, out int last), Is.True);
            Assert.That(last - first, Is.EqualTo(1));
        }
    }
}
