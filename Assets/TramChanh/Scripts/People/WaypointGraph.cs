using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace TramChanh.People
{
    /// <summary>
    /// Undirected waypoint graph with optional node names and named lanes (ordered node lists).
    /// Pure C#: no MonoBehaviour, physics, time or randomness. Deterministic for a given build order.
    /// </summary>
    public sealed class WaypointGraph
    {
        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly List<string> _names = new List<string>();
        private readonly List<List<int>> _adjacency = new List<List<int>>();
        private readonly List<ReadOnlyCollection<int>> _adjacencyViews = new List<ReadOnlyCollection<int>>();
        private readonly Dictionary<string, int> _byName = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, Lane> _lanes = new Dictionary<string, Lane>(StringComparer.Ordinal);
        private readonly List<string> _laneOrder = new List<string>();
        private readonly ReadOnlyCollection<string> _laneNamesView;

        public WaypointGraph()
        {
            _laneNamesView = _laneOrder.AsReadOnly();
        }

        /// <summary>Number of nodes.</summary>
        public int Count => _positions.Count;

        /// <summary>Lane names in the order they were added.</summary>
        public IReadOnlyCollection<string> LaneNames => _laneNamesView;

        /// <summary>
        /// Adds a node and returns its index. <paramref name="name"/> may be null for an unnamed node;
        /// a duplicate non-null name throws <see cref="ArgumentException"/>, as does a non-finite position.
        /// </summary>
        public int AddNode(string name, Vector3 position)
        {
            if (!VectorMath.IsFinite(position))
            {
                throw new ArgumentException("Waypoint position must be finite.", nameof(position));
            }

            if (name != null && _byName.ContainsKey(name))
            {
                throw new ArgumentException("A waypoint named '" + name + "' already exists.", nameof(name));
            }

            int index = _positions.Count;
            _positions.Add(position);
            _names.Add(name);
            var neighbours = new List<int>();
            _adjacency.Add(neighbours);
            _adjacencyViews.Add(neighbours.AsReadOnly());
            if (name != null)
            {
                _byName.Add(name, index);
            }

            return index;
        }

        /// <summary>Connects two nodes with an undirected edge. Idempotent; connecting a node to itself is a no-op.</summary>
        public void Connect(int a, int b)
        {
            CheckNode(a, nameof(a));
            CheckNode(b, nameof(b));
            if (a == b || _adjacency[a].Contains(b))
            {
                return;
            }

            _adjacency[a].Add(b);
            _adjacency[b].Add(a);
        }

        public bool TryFind(string name, out int node)
        {
            if (name != null && _byName.TryGetValue(name, out node))
            {
                return true;
            }

            node = -1;
            return false;
        }

        public Vector3 PositionOf(int node)
        {
            CheckNode(node, nameof(node));
            return _positions[node];
        }

        /// <summary>The node's name, or null for an unnamed node.</summary>
        public string NameOf(int node)
        {
            CheckNode(node, nameof(node));
            return _names[node];
        }

        /// <summary>Neighbours in the order the edges were added.</summary>
        public IReadOnlyList<int> Neighbours(int node)
        {
            CheckNode(node, nameof(node));
            return _adjacencyViews[node];
        }

        /// <summary>
        /// Registers an ordered lane and connects consecutive nodes (and last to first when
        /// <paramref name="loop"/> is true and the lane has 3+ nodes). Throws for a null/empty or duplicate
        /// lane name, fewer than 2 nodes, or an invalid node index.
        /// </summary>
        public void AddLane(string lane, IReadOnlyList<int> nodes, bool loop)
        {
            if (string.IsNullOrEmpty(lane))
            {
                throw new ArgumentException("Lane name must be non-empty.", nameof(lane));
            }

            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (_lanes.ContainsKey(lane))
            {
                throw new ArgumentException("Lane '" + lane + "' already exists.", nameof(lane));
            }

            if (nodes.Count < 2)
            {
                throw new ArgumentException("Lane '" + lane + "' needs at least 2 nodes.", nameof(nodes));
            }

            var copy = new int[nodes.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                CheckNode(nodes[i], nameof(nodes));
                copy[i] = nodes[i];
            }

            for (int i = 1; i < copy.Length; i++)
            {
                Connect(copy[i - 1], copy[i]);
            }

            if (loop && copy.Length > 2)
            {
                Connect(copy[copy.Length - 1], copy[0]);
            }

            _lanes.Add(lane, new Lane(Array.AsReadOnly(copy), loop));
            _laneOrder.Add(lane);
        }

        public bool TryGetLane(string lane, out IReadOnlyList<int> nodes, out bool loop)
        {
            if (lane != null && _lanes.TryGetValue(lane, out var found))
            {
                nodes = found.Nodes;
                loop = found.Loop;
                return true;
            }

            nodes = null;
            loop = false;
            return false;
        }

        /// <summary>
        /// A* shortest path by Euclidean edge length. Clears <paramref name="result"/> and fills it with the
        /// node positions from <paramref name="from"/> to <paramref name="to"/> inclusive. Returns false (and
        /// leaves <paramref name="result"/> empty) when <paramref name="to"/> is unreachable. Ties are broken by
        /// lowest node index, so the result is deterministic.
        /// </summary>
        public bool TryFindPath(int from, int to, List<Vector3> result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            CheckNode(from, nameof(from));
            CheckNode(to, nameof(to));
            result.Clear();

            if (from == to)
            {
                result.Add(_positions[from]);
                return true;
            }

            int n = _positions.Count;
            var g = new double[n];
            var parent = new int[n];
            var closed = new bool[n];
            for (int i = 0; i < n; i++)
            {
                g[i] = double.PositiveInfinity;
                parent[i] = -1;
            }

            Vector3 goal = _positions[to];
            var open = new MinHeap(n);
            g[from] = 0d;
            open.Push(VectorMath.Distance(_positions[from], goal), from);

            while (open.Count > 0)
            {
                int current = open.Pop();
                if (closed[current])
                {
                    continue;
                }

                if (current == to)
                {
                    var reversed = new List<int>();
                    for (int v = to; v != -1; v = parent[v])
                    {
                        reversed.Add(v);
                    }

                    for (int i = reversed.Count - 1; i >= 0; i--)
                    {
                        result.Add(_positions[reversed[i]]);
                    }

                    return true;
                }

                closed[current] = true;
                var neighbours = _adjacency[current];
                for (int k = 0; k < neighbours.Count; k++)
                {
                    int next = neighbours[k];
                    if (closed[next])
                    {
                        continue;
                    }

                    double tentative = g[current] + VectorMath.Distance(_positions[current], _positions[next]);
                    if (tentative < g[next])
                    {
                        g[next] = tentative;
                        parent[next] = current;
                        open.Push(tentative + VectorMath.Distance(_positions[next], goal), next);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Index of the node nearest to <paramref name="position"/>, optionally restricted to the nodes of
        /// <paramref name="laneOrNull"/>. Returns -1 when the graph is empty or the lane does not exist.
        /// Ties resolve to the lowest index (or the earliest lane position).
        /// </summary>
        public int Nearest(Vector3 position, string laneOrNull)
        {
            int best = -1;
            double bestSqr = double.PositiveInfinity;
            if (laneOrNull == null)
            {
                for (int i = 0; i < _positions.Count; i++)
                {
                    double d = VectorMath.SqrDistance(position, _positions[i]);
                    if (d < bestSqr)
                    {
                        bestSqr = d;
                        best = i;
                    }
                }

                return best;
            }

            if (!_lanes.TryGetValue(laneOrNull, out var lane))
            {
                return -1;
            }

            for (int i = 0; i < lane.Nodes.Count; i++)
            {
                int node = lane.Nodes[i];
                double d = VectorMath.SqrDistance(position, _positions[node]);
                if (d < bestSqr)
                {
                    bestSqr = d;
                    best = node;
                }
            }

            return best;
        }

        /// <summary>True when <paramref name="to"/> can be reached from <paramref name="from"/> (breadth-first).</summary>
        internal bool IsReachable(int from, int to)
        {
            CheckNode(from, nameof(from));
            CheckNode(to, nameof(to));
            var seen = ReachableFrom(from);
            return seen[to];
        }

        internal bool[] ReachableFrom(int from)
        {
            CheckNode(from, nameof(from));
            var seen = new bool[_positions.Count];
            var queue = new Queue<int>();
            seen[from] = true;
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                int v = queue.Dequeue();
                foreach (int w in _adjacency[v])
                {
                    if (!seen[w])
                    {
                        seen[w] = true;
                        queue.Enqueue(w);
                    }
                }
            }

            return seen;
        }

        private void CheckNode(int node, string paramName)
        {
            if (node < 0 || node >= _positions.Count)
            {
                throw new ArgumentOutOfRangeException(paramName, node, "Waypoint index out of range.");
            }
        }

        private readonly struct Lane
        {
            public Lane(IReadOnlyList<int> nodes, bool loop)
            {
                Nodes = nodes;
                Loop = loop;
            }

            public IReadOnlyList<int> Nodes { get; }
            public bool Loop { get; }
        }

        /// <summary>Binary min-heap keyed by (priority, node index) for deterministic ordering.</summary>
        private sealed class MinHeap
        {
            private readonly List<double> _keys;
            private readonly List<int> _items;

            public MinHeap(int capacity)
            {
                _keys = new List<double>(capacity);
                _items = new List<int>(capacity);
            }

            public int Count => _items.Count;

            public void Push(double key, int item)
            {
                _keys.Add(key);
                _items.Add(item);
                int i = _items.Count - 1;
                while (i > 0)
                {
                    int p = (i - 1) / 2;
                    if (!Less(i, p))
                    {
                        break;
                    }

                    Swap(i, p);
                    i = p;
                }
            }

            public int Pop()
            {
                int top = _items[0];
                int last = _items.Count - 1;
                Swap(0, last);
                _keys.RemoveAt(last);
                _items.RemoveAt(last);
                int i = 0;
                while (true)
                {
                    int l = 2 * i + 1;
                    int r = l + 1;
                    int m = i;
                    if (l < _items.Count && Less(l, m))
                    {
                        m = l;
                    }

                    if (r < _items.Count && Less(r, m))
                    {
                        m = r;
                    }

                    if (m == i)
                    {
                        break;
                    }

                    Swap(i, m);
                    i = m;
                }

                return top;
            }

            private bool Less(int a, int b)
            {
                return _keys[a] < _keys[b] || (_keys[a] == _keys[b] && _items[a] < _items[b]);
            }

            private void Swap(int a, int b)
            {
                double k = _keys[a];
                _keys[a] = _keys[b];
                _keys[b] = k;
                int t = _items[a];
                _items[a] = _items[b];
                _items[b] = t;
            }
        }
    }
}
