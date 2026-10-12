using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using UnityEngine;

namespace TramChanh.People
{
    /// <summary>Output of <see cref="WaypointGraphBuilder.Build"/>: the graph (always non-null) plus diagnostics.</summary>
    public sealed class WaypointGraphBuildResult
    {
        internal WaypointGraphBuildResult(WaypointGraph graph, List<string> errors, List<string> warnings)
        {
            Graph = graph;
            Errors = errors.AsReadOnly();
            Warnings = warnings.AsReadOnly();
        }

        public WaypointGraph Graph { get; }
        public IReadOnlyList<string> Errors { get; }
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// Builds a <see cref="WaypointGraph"/> from named anchors authored in the environment prefab
    /// (contract: Docs/Design/NPC_AND_MAP_EXPANSION.md §5).
    /// <list type="bullet">
    /// <item>Lane points are named <c>WP_&lt;Lane&gt;_&lt;nn&gt;</c> (2+ digits) and ordered by nn.</item>
    /// <item>Pedestrian lanes: SidewalkNorth, SidewalkSouth, Alley. Road lanes: RoadEast, RoadWest. All open (not looped).</item>
    /// <item>Every other anchor is linked to its nearest pedestrian lane node (never a road node).</item>
    /// <item>The closest node pair of each two pedestrian lanes is connected when within <c>laneLinkDistance</c>.</item>
    /// </list>
    /// Node order is deterministic: lanes in ordinal name order, then other anchors in input order.
    /// </summary>
    public static class WaypointGraphBuilder
    {
        public const string LanePrefix = "WP_";
        public const string SidewalkNorth = "SidewalkNorth";
        public const string SidewalkSouth = "SidewalkSouth";
        public const string Alley = "Alley";
        public const string RoadEast = "RoadEast";
        public const string RoadWest = "RoadWest";
        public const string StreetEntryWest = "StreetEntryWest";
        public const string StreetEntryEast = "StreetEntryEast";
        public const string TableApproachPrefix = "TableApproach_";

        private static readonly string[] PedestrianLanes = { Alley, SidewalkNorth, SidewalkSouth };
        private static readonly string[] RoadLanes = { RoadEast, RoadWest };

        /// <summary>True for SidewalkNorth, SidewalkSouth and Alley.</summary>
        public static bool IsPedestrianLane(string lane)
        {
            return Array.IndexOf(PedestrianLanes, lane) >= 0;
        }

        /// <summary>True for RoadEast and RoadWest.</summary>
        public static bool IsRoadLane(string lane)
        {
            return Array.IndexOf(RoadLanes, lane) >= 0;
        }

        public static WaypointGraphBuildResult Build(IEnumerable<KeyValuePair<string, Vector3>> anchors, float laneLinkDistance = 4f)
        {
            if (anchors == null)
            {
                throw new ArgumentNullException(nameof(anchors));
            }

            if (!VectorMath.IsFinite(laneLinkDistance) || laneLinkDistance < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(laneLinkDistance), laneLinkDistance, "Must be finite and >= 0.");
            }

            var errors = new List<string>();
            var warnings = new List<string>();
            var graph = new WaypointGraph();

            // 1. Collect and validate anchors.
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var lanePoints = new SortedDictionary<string, List<LanePoint>>(StringComparer.Ordinal);
            var others = new List<KeyValuePair<string, Vector3>>();
            int order = -1;
            foreach (var anchor in anchors)
            {
                order++;
                string name = anchor.Key;
                if (string.IsNullOrEmpty(name))
                {
                    errors.Add("Anchor with an empty name at " + Format(anchor.Value) + ".");
                    continue;
                }

                if (!seen.Add(name))
                {
                    errors.Add("Duplicate anchor name '" + name + "'.");
                    continue;
                }

                if (!VectorMath.IsFinite(anchor.Value))
                {
                    errors.Add("Anchor '" + name + "' has a non-finite position.");
                    continue;
                }

                if (name.StartsWith(LanePrefix, StringComparison.Ordinal))
                {
                    if (TryParseLanePoint(name, out string lane, out int number))
                    {
                        if (!lanePoints.TryGetValue(lane, out var list))
                        {
                            list = new List<LanePoint>();
                            lanePoints.Add(lane, list);
                        }

                        list.Add(new LanePoint(name, number, anchor.Value, order));
                    }
                    else
                    {
                        warnings.Add("Anchor '" + name + "' starts with '" + LanePrefix + "' but is not WP_<Lane>_<nn> (2+ digits); ignored.");
                    }

                    continue;
                }

                others.Add(anchor);
            }

            // 2. Lanes, ordered by number.
            var pedestrianLaneNames = new List<string>();
            foreach (var entry in lanePoints)
            {
                string lane = entry.Key;
                var points = entry.Value;
                points.Sort((a, b) => a.Number != b.Number ? a.Number.CompareTo(b.Number) : a.Order.CompareTo(b.Order));

                // Same number twice (e.g. _02 and _002): error, the later anchor in input order is dropped.
                for (int i = points.Count - 1; i >= 1; i--)
                {
                    if (points[i].Number == points[i - 1].Number)
                    {
                        errors.Add("Lane '" + lane + "' has two points numbered " + points[i].Number + " ('" + points[i - 1].Name + "', '" + points[i].Name + "'); '" + points[i].Name + "' is ignored.");
                        points.RemoveAt(i);
                    }
                }

                for (int i = 1; i < points.Count; i++)
                {
                    if (points[i].Number != points[i - 1].Number + 1)
                    {
                        warnings.Add("Lane '" + lane + "' has a gap in numbering between " + points[i - 1].Name + " and " + points[i].Name + ".");
                    }
                }

                if (points.Count < 2)
                {
                    errors.Add("Lane '" + lane + "' has " + points.Count + " point(s); at least 2 are required.");
                    continue;
                }

                bool pedestrian = IsPedestrianLane(lane);
                if (!pedestrian && !IsRoadLane(lane))
                {
                    warnings.Add("Unknown lane '" + lane + "'; it is added but not linked to anchors or other lanes.");
                }

                var nodes = new int[points.Count];
                for (int i = 0; i < points.Count; i++)
                {
                    nodes[i] = graph.AddNode(points[i].Name, points[i].Position);
                }

                graph.AddLane(lane, nodes, false);
                if (pedestrian)
                {
                    pedestrianLaneNames.Add(lane);
                }
            }

            if (pedestrianLaneNames.Count == 0)
            {
                errors.Add("No pedestrian lane (" + string.Join(", ", PedestrianLanes) + ") with at least 2 points.");
            }

            // 3. Cross-links between pedestrian lanes.
            for (int i = 0; i < pedestrianLaneNames.Count; i++)
            {
                for (int j = i + 1; j < pedestrianLaneNames.Count; j++)
                {
                    LinkClosestPair(graph, pedestrianLaneNames[i], pedestrianLaneNames[j], laneLinkDistance);
                }
            }

            // 4. Named anchors, each linked to its nearest pedestrian lane node.
            foreach (var anchor in others)
            {
                int node = graph.AddNode(anchor.Key, anchor.Value);
                int nearest = NearestPedestrianNode(graph, pedestrianLaneNames, anchor.Value);
                if (nearest >= 0)
                {
                    graph.Connect(node, nearest);
                }
            }

            // 5. Reachability from StreetEntryWest.
            CheckReachability(graph, others, errors);

            return new WaypointGraphBuildResult(graph, errors, warnings);
        }

        internal static bool TryParseLanePoint(string name, out string lane, out int number)
        {
            lane = null;
            number = 0;
            if (name == null || !name.StartsWith(LanePrefix, StringComparison.Ordinal))
            {
                return false;
            }

            int underscore = name.LastIndexOf('_');
            if (underscore <= LanePrefix.Length)
            {
                return false;
            }

            string digits = name.Substring(underscore + 1);
            if (digits.Length < 2 || digits.Length > 9)
            {
                return false;
            }

            for (int i = 0; i < digits.Length; i++)
            {
                if (digits[i] < '0' || digits[i] > '9')
                {
                    return false;
                }
            }

            lane = name.Substring(LanePrefix.Length, underscore - LanePrefix.Length);
            number = int.Parse(digits, NumberStyles.None, CultureInfo.InvariantCulture);
            return true;
        }

        private static void LinkClosestPair(WaypointGraph graph, string laneA, string laneB, float maxDistance)
        {
            graph.TryGetLane(laneA, out var nodesA, out _);
            graph.TryGetLane(laneB, out var nodesB, out _);
            int bestA = -1;
            int bestB = -1;
            double best = double.PositiveInfinity;
            for (int i = 0; i < nodesA.Count; i++)
            {
                for (int j = 0; j < nodesB.Count; j++)
                {
                    double d = VectorMath.SqrDistance(graph.PositionOf(nodesA[i]), graph.PositionOf(nodesB[j]));
                    if (d < best)
                    {
                        best = d;
                        bestA = nodesA[i];
                        bestB = nodesB[j];
                    }
                }
            }

            if (bestA >= 0 && Math.Sqrt(best) <= maxDistance)
            {
                graph.Connect(bestA, bestB);
            }
        }

        private static int NearestPedestrianNode(WaypointGraph graph, List<string> pedestrianLanes, Vector3 position)
        {
            int best = -1;
            double bestSqr = double.PositiveInfinity;
            foreach (string lane in pedestrianLanes)
            {
                int candidate = graph.Nearest(position, lane);
                if (candidate < 0)
                {
                    continue;
                }

                double d = VectorMath.SqrDistance(position, graph.PositionOf(candidate));
                if (d < bestSqr)
                {
                    bestSqr = d;
                    best = candidate;
                }
            }

            return best;
        }

        private static void CheckReachability(WaypointGraph graph, List<KeyValuePair<string, Vector3>> others, List<string> errors)
        {
            bool hasEast = graph.TryFind(StreetEntryEast, out int east);
            if (!hasEast)
            {
                errors.Add("Missing anchor '" + StreetEntryEast + "'.");
            }

            if (!graph.TryFind(StreetEntryWest, out int west))
            {
                errors.Add("Missing anchor '" + StreetEntryWest + "'; reachability of table approaches cannot be checked.");
                return;
            }

            bool[] reachable = graph.ReachableFrom(west);
            if (hasEast && !reachable[east])
            {
                errors.Add("'" + StreetEntryEast + "' is not reachable from '" + StreetEntryWest + "'.");
            }

            foreach (var anchor in others)
            {
                if (!anchor.Key.StartsWith(TableApproachPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                if (graph.TryFind(anchor.Key, out int node) && !reachable[node])
                {
                    errors.Add("'" + anchor.Key + "' is not reachable from '" + StreetEntryWest + "'.");
                }
            }
        }

        private static string Format(Vector3 v)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2})", v.x, v.y, v.z);
        }

        private readonly struct LanePoint
        {
            public LanePoint(string name, int number, Vector3 position, int order)
            {
                Name = name;
                Number = number;
                Position = position;
                Order = order;
            }

            public int Order { get; }

            public string Name { get; }
            public int Number { get; }
            public Vector3 Position { get; }
        }
    }
}
