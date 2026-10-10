using System;
using System.Collections.Generic;

namespace TramChanh.People
{
    /// <summary>
    /// Decides when ambient pedestrians/vehicles appear and on which lane. No rendering, no engine time:
    /// the owner calls <see cref="Tick"/> with the game-clock delta and <see cref="NotifyFinished"/> when an
    /// agent reaches the end of its lane.
    /// <para>
    /// Timing: an internal accumulator grows by each positive finite delta; once it reaches the spawn interval
    /// and a slot is free, one agent spawns and the interval is subtracted. The accumulator is capped at one
    /// interval, so a long pause or a full crowd never produces a burst: at most one spawn per Tick.
    /// </para>
    /// <para>
    /// Randomness: a seeded <see cref="Random"/> picks the lane and direction, two draws per spawn, so a given
    /// seed and Tick/NotifyFinished sequence always produces the same spawns. Road lanes (RoadEast/RoadWest)
    /// are one-way and are never reversed.
    /// </para>
    /// </summary>
    public sealed class CrowdScheduler
    {
        // Tolerance (1 microsecond) so float intervals and summed deltas (e.g. ten 0.1 s ticks for 1 s) still trigger.
        private const double TimeEpsilon = 1e-6;
        private readonly string[] _lanes;
        private readonly int _maxAgents;
        private readonly double _interval;
        private readonly Random _random;
        private readonly HashSet<int> _active = new HashSet<int>();
        private double _accumulator;
        private int _nextId = 1;

        public CrowdScheduler(IReadOnlyList<string> lanes, int maxAgents, float spawnIntervalSeconds, int seed)
        {
            if (lanes == null)
            {
                throw new ArgumentNullException(nameof(lanes));
            }

            if (lanes.Count == 0)
            {
                throw new ArgumentException("At least one lane is required.", nameof(lanes));
            }

            _lanes = new string[lanes.Count];
            for (int i = 0; i < _lanes.Length; i++)
            {
                if (string.IsNullOrEmpty(lanes[i]))
                {
                    throw new ArgumentException("Lane names must be non-empty.", nameof(lanes));
                }

                _lanes[i] = lanes[i];
            }

            if (maxAgents < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAgents), maxAgents, "Must be >= 0.");
            }

            if (float.IsNaN(spawnIntervalSeconds) || float.IsInfinity(spawnIntervalSeconds) || spawnIntervalSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(spawnIntervalSeconds), spawnIntervalSeconds, "Must be finite and > 0.");
            }

            _maxAgents = maxAgents;
            _interval = spawnIntervalSeconds;
            _random = new Random(seed);
        }

        public event Action<CrowdSpawn> Spawned;

        public event Action<int> Despawned;

        public int ActiveCount => _active.Count;

        /// <summary>Advances the spawn timer. &lt;= 0, NaN and infinity are ignored (pause-safe).</summary>
        public void Tick(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0d)
            {
                return;
            }

            _accumulator = Math.Min(_accumulator + seconds, _interval);
            if (_accumulator + TimeEpsilon < _interval || _active.Count >= _maxAgents)
            {
                return;
            }

            _accumulator = Math.Max(0d, _accumulator - _interval);
            string lane = _lanes[_random.Next(_lanes.Length)];
            bool reverse = _random.Next(2) == 1;
            if (WaypointGraphBuilder.IsRoadLane(lane))
            {
                reverse = false;
            }

            int id = _nextId++;
            _active.Add(id);
            Spawned?.Invoke(new CrowdSpawn(id, lane, reverse));
        }

        /// <summary>Frees the agent's slot and raises <see cref="Despawned"/>. Unknown or already finished ids are ignored.</summary>
        public void NotifyFinished(int agentId)
        {
            if (_active.Remove(agentId))
            {
                Despawned?.Invoke(agentId);
            }
        }
    }
}
