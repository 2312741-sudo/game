using System;
using TramChanh.Core;
using UnityEngine;

namespace TramChanh.People
{
    /// <summary>
    /// Finite state machine of one street animal: Idle, Wander, Investigate, Beg, Flee, Rest.
    /// Pure C#: it reads <see cref="AnimalSenses"/> and time, and answers with a state, a destination, a speed multiplier and a point to look at.
    /// It never moves anything. The MonoBehaviour walks the path and calls <see cref="ReportArrived"/> / <see cref="ReportBlocked"/>.
    /// All chance goes through one <see cref="System.Random"/> created from a seed, so the same seed and the same inputs give the same behaviour.
    /// Cooldowns stop an animal from begging or investigating again right after it just did.
    /// </summary>
    public sealed class AnimalBrain
    {
        private readonly AnimalProfile _profile;
        private readonly System.Random _random;
        private readonly Vector3 _home;

        private float _stateDuration;
        private float _decisionTimer;
        private float _begCooldown;
        private float _investigateCooldown;
        private float _loseFoodSeconds;
        private float _holdSeconds;
        private bool _arrived;
        private bool _blocked;

        public AnimalBrain(AnimalProfile profile, Vector3 home, int seed)
        {
            if (profile == null) { throw new ArgumentNullException(nameof(profile)); }
            _profile = profile;
            _home = home;
            _random = new System.Random(seed);
            State = AnimalState.Idle;
            _stateDuration = 0.5f + (float)_random.NextDouble() * Math.Max(0f, profile.IdleMax - 0.5f);
        }

        public AnimalState State { get; private set; }

        /// <summary>The state before the last change.</summary>
        public AnimalState PreviousState { get; private set; }

        /// <summary>How many times the state changed. Handy for tests and for the debug label.</summary>
        public int TransitionCount { get; private set; }

        public float StateSeconds { get; private set; }

        /// <summary>True while the current state wants the animal to walk (a destination was given and not reached).</summary>
        public bool HasDestination { get; private set; }

        public Vector3 Destination { get; private set; }

        /// <summary>Increases every time a new destination is given, so the mover knows to plan a new path.</summary>
        public int DestinationId { get; private set; }

        /// <summary>1 for walking, <see cref="AnimalProfile.RunMultiplier"/> while fleeing.</summary>
        public float SpeedMultiplier => State == AnimalState.Flee ? _profile.RunMultiplier : 1f;

        /// <summary>A point the animal should face while standing (the food, the player). Meaningful only when <see cref="HasFocus"/>.</summary>
        public bool HasFocus { get; private set; }

        public Vector3 Focus { get; private set; }

        /// <summary>Sitting pose: Rest, or Beg after reaching the begging spot.</summary>
        public bool IsSitting => State == AnimalState.Rest || (State == AnimalState.Beg && _arrived);

        public float BegCooldownLeft => _begCooldown;

        public float InvestigateCooldownLeft => _investigateCooldown;

        /// <summary>The mover reached <see cref="Destination"/>.</summary>
        public void ReportArrived()
        {
            _arrived = true;
            HasDestination = false;
        }

        /// <summary>The mover could not continue (a wall in the way).</summary>
        public void ReportBlocked()
        {
            _blocked = true;
            HasDestination = false;
        }

        /// <param name="seconds">Game-clock seconds since the last call. Zero, negative and NaN are ignored (pause-safe).</param>
        /// <param name="position">Where the animal stands now.</param>
        public void Update(float seconds, Vector3 position, in AnimalSenses senses)
        {
            if (!(seconds > 0f) || float.IsInfinity(seconds)) { return; }
            _begCooldown = Math.Max(0f, _begCooldown - seconds);
            _investigateCooldown = Math.Max(0f, _investigateCooldown - seconds);
            _decisionTimer -= seconds;
            StateSeconds += seconds;

            // Reflex: danger beats every other state.
            if (State != AnimalState.Flee && senses.ThreatDetected && senses.ThreatDistance <= _profile.FleeDistance)
            {
                EnterFlee(position, senses);
                return;
            }

            switch (State)
            {
                case AnimalState.Idle:
                    if (TryInterrupt(position, senses)) { return; }
                    if (StateSeconds >= _stateDuration)
                    {
                        if (Roll(_profile.RestChance)) { EnterRest(); } else { EnterWander(position); }
                    }
                    break;
                case AnimalState.Wander:
                    if (TryInterrupt(position, senses)) { return; }
                    if (_arrived || _blocked || StateSeconds >= _stateDuration) { EnterIdle(); }
                    break;
                case AnimalState.Rest:
                    if (TryInterrupt(position, senses)) { return; }
                    if (StateSeconds >= _stateDuration) { EnterIdle(); }
                    break;
                case AnimalState.Beg:
                    UpdateBeg(seconds, senses);
                    break;
                case AnimalState.Investigate:
                    UpdateInvestigate(seconds);
                    break;
                case AnimalState.Flee:
                    UpdateFlee(position, senses);
                    break;
            }
        }

        private bool TryInterrupt(Vector3 position, in AnimalSenses senses)
        {
            if (_decisionTimer > 0f) { return false; }
            bool beg = _profile.CanBeg && senses.FoodDetected && _begCooldown <= 0f;
            bool investigate = _profile.Curious && senses.InterestDetected && _investigateCooldown <= 0f;
            if (!beg && !investigate) { return false; }
            _decisionTimer = _profile.DecisionInterval;
            if (beg && Roll(_profile.BegChance)) { EnterBeg(position, senses); return true; }
            if (investigate && Roll(_profile.InvestigateChance)) { EnterInvestigate(position, senses); return true; }
            return false;
        }

        private void UpdateBeg(float seconds, in AnimalSenses senses)
        {
            if (senses.FoodDetected)
            {
                _loseFoodSeconds = 0f;
                Focus = senses.FoodPosition;
            }
            else
            {
                _loseFoodSeconds += seconds;
            }

            if (_loseFoodSeconds >= _profile.BegLoseSeconds || StateSeconds >= _profile.BegMaxSeconds || _blocked)
            {
                _begCooldown = _profile.BegCooldown;
                EnterIdle();
            }
        }

        private void UpdateInvestigate(float seconds)
        {
            if (_arrived) { _holdSeconds += seconds; }
            if (_holdSeconds >= _profile.InvestigateLookSeconds || StateSeconds >= _profile.InvestigateMaxSeconds || _blocked)
            {
                _investigateCooldown = _profile.InvestigateCooldown;
                EnterIdle();
            }
        }

        private void UpdateFlee(Vector3 position, in AnimalSenses senses)
        {
            bool safe = !senses.ThreatDetected || senses.ThreatDistance >= _profile.SafeDistance;
            if ((StateSeconds >= _profile.FleeMinSeconds && safe) || StateSeconds >= _profile.FleeMaxSeconds)
            {
                _investigateCooldown = Math.Max(_investigateCooldown, _profile.InvestigateCooldown);
                _begCooldown = Math.Max(_begCooldown, _profile.BegCooldown * 0.5f);
                EnterRest();
                return;
            }

            // Reached the run target but the threat is still close: run further.
            if ((_arrived || _blocked) && !safe) { IssueFlee(position, senses); }
        }

        private void Enter(AnimalState next)
        {
            PreviousState = State;
            State = next;
            TransitionCount++;
            StateSeconds = 0f;
            HasDestination = false;
            HasFocus = false;
            _arrived = false;
            _blocked = false;
            _loseFoodSeconds = 0f;
            _holdSeconds = 0f;
        }

        private void EnterIdle()
        {
            Enter(AnimalState.Idle);
            _stateDuration = Range(_profile.IdleMin, _profile.IdleMax);
        }

        private void EnterRest()
        {
            Enter(AnimalState.Rest);
            _stateDuration = Range(_profile.RestMin, _profile.RestMax);
        }

        private void EnterWander(Vector3 position)
        {
            Vector3 target = position;
            bool found = false;
            for (int attempt = 0; attempt < 6 && !found; attempt++)
            {
                WanderArea.Pick(_home.x, _home.z, _profile.WanderRadius, _random.NextDouble(), _random.NextDouble(), out float x, out float z);
                target = new Vector3(x, position.y, z);
                found = Flat(position, target) >= _profile.MinWalkMetres;
            }

            if (!found)
            {
                EnterIdle();
                _stateDuration = 1f;
                return;
            }

            Enter(AnimalState.Wander);
            _stateDuration = WanderArea.WalkSeconds(Flat(position, target), _profile.WalkSpeed) * 2f + 2f;
            Go(target);
        }

        private void EnterInvestigate(Vector3 position, in AnimalSenses senses)
        {
            Enter(AnimalState.Investigate);
            _stateDuration = _profile.InvestigateMaxSeconds;
            Focus = senses.InterestPosition;
            HasFocus = true;
            Vector3 toward = senses.InterestPosition - position;
            toward.y = 0f;
            float distance = toward.magnitude;
            Vector3 target = distance <= _profile.InvestigateStopDistance
                ? position
                : senses.InterestPosition - toward / distance * _profile.InvestigateStopDistance;
            target.y = position.y;
            Go(target);
        }

        private void EnterBeg(Vector3 position, in AnimalSenses senses)
        {
            Enter(AnimalState.Beg);
            _stateDuration = _profile.BegMaxSeconds;
            Focus = senses.FoodPosition;
            HasFocus = true;
            Vector3 away = position - senses.FoodPosition;
            away.y = 0f;
            Vector3 direction = away.sqrMagnitude < 1e-6f ? new Vector3(1f, 0f, 0f) : away.normalized;
            Vector3 target = senses.FoodPosition + direction * _profile.BegStopDistance;
            target.y = position.y;
            Go(target);
        }

        private void EnterFlee(Vector3 position, in AnimalSenses senses)
        {
            Enter(AnimalState.Flee);
            _stateDuration = _profile.FleeMaxSeconds;
            IssueFlee(position, senses);
        }

        private void IssueFlee(Vector3 position, in AnimalSenses senses)
        {
            Vector3 away = position - senses.ThreatPosition;
            away.y = 0f;
            Vector3 direction;
            if (away.sqrMagnitude < 1e-6f || !senses.ThreatDetected)
            {
                double angle = _random.NextDouble() * 2d * Math.PI;
                direction = new Vector3((float)Math.Cos(angle), 0f, (float)Math.Sin(angle));
            }
            else
            {
                direction = away.normalized;
            }

            Vector3 target = position + direction * _profile.FleeTravel;
            Vector3 fromHome = target - _home;
            fromHome.y = 0f;
            if (fromHome.magnitude > _profile.FleeLeash)
            {
                target = _home + fromHome.normalized * _profile.FleeLeash;
            }

            target.y = position.y;
            _arrived = false;
            _blocked = false;
            Go(target);
        }

        private void Go(Vector3 target)
        {
            Destination = target;
            DestinationId++;
            HasDestination = true;
            _arrived = false;
        }

        private bool Roll(float chance)
        {
            return _random.NextDouble() < chance;
        }

        private float Range(float min, float max)
        {
            if (max <= min) { return Math.Max(0f, min); }
            return min + (float)_random.NextDouble() * (max - min);
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            double dx = (double)a.x - b.x;
            double dz = (double)a.z - b.z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
