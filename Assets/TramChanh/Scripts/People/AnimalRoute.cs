using System.Collections.Generic;
using UnityEngine;

namespace TramChanh.People
{
    /// <summary>
    /// Builds the polyline an animal walks. A short trip goes straight. A long trip goes from the start to the nearest waypoint,
    /// along the A* path of <see cref="WaypointGraph.TryFindPath"/>, then to the target. If no waypoint is close enough,
    /// no path exists, or the graph route is absurdly longer than the straight line, it goes straight. Feed the result to <see cref="PathFollower"/>.
    /// </summary>
    public static class AnimalRoute
    {
        /// <summary>Trips shorter than this (metres, horizontal) always go straight.</summary>
        public const float DirectMetres = 3f;

        /// <summary>A waypoint further than this from the start or the target is not used.</summary>
        public const float MaxSnapMetres = 5f;

        /// <summary>A graph route longer than this times the straight distance (plus 2 m) is rejected.</summary>
        public const float MaxDetourFactor = 3f;

        /// <summary>Clears <paramref name="result"/> and fills it with the route. Returns true when waypoints were used.</summary>
        /// <param name="graph">May be null (straight line).</param>
        /// <param name="lanes">Lane names animals may use (the sidewalks); null or empty means any node.</param>
        public static bool Build(WaypointGraph graph, IReadOnlyList<string> lanes, Vector3 from, Vector3 to, List<Vector3> result)
        {
            result.Clear();
            result.Add(from);
            float straight = Flat(from, to);
            bool usedGraph = false;
            if (graph != null && graph.Count > 0 && straight > DirectMetres)
            {
                int a = Nearest(graph, lanes, from);
                int b = Nearest(graph, lanes, to);
                if (a >= 0 && b >= 0
                    && Flat(from, graph.PositionOf(a)) <= MaxSnapMetres
                    && Flat(to, graph.PositionOf(b)) <= MaxSnapMetres)
                {
                    var nodes = new List<Vector3>();
                    if (graph.TryFindPath(a, b, nodes) && RouteLength(from, nodes, to) <= straight * MaxDetourFactor + 2f)
                    {
                        for (int i = 0; i < nodes.Count; i++)
                        {
                            result.Add(new Vector3(nodes[i].x, from.y, nodes[i].z));
                        }

                        usedGraph = true;
                    }
                }
            }

            result.Add(to);
            return usedGraph;
        }

        private static int Nearest(WaypointGraph graph, IReadOnlyList<string> lanes, Vector3 position)
        {
            if (lanes == null || lanes.Count == 0) { return graph.Nearest(position, null); }
            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < lanes.Count; i++)
            {
                int node = graph.Nearest(position, lanes[i]);
                if (node < 0) { continue; }
                float d = Flat(position, graph.PositionOf(node));
                if (d < bestDistance) { bestDistance = d; best = node; }
            }

            return best;
        }

        private static float RouteLength(Vector3 from, List<Vector3> nodes, Vector3 to)
        {
            float length = 0f;
            Vector3 previous = from;
            for (int i = 0; i < nodes.Count; i++)
            {
                length += Flat(previous, nodes[i]);
                previous = nodes[i];
            }

            return length + Flat(previous, to);
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
