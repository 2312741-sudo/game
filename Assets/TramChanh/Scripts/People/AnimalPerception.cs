using System;
using System.Collections.Generic;
using UnityEngine;

namespace TramChanh.People
{
    /// <summary>
    /// Sight, hearing and smell of one animal.
    /// Sight needs the target inside the view cone (distance and angle) and no wall in between.
    /// Hearing needs only distance (divided by the threat's danger), so a motorbike behind a wall is still heard.
    /// Food is smelled by distance alone. The wall test is an interface, so tests need no physics.
    /// </summary>
    public sealed class AnimalPerception : IAnimalPerception
    {
        private const float EyeHeight = 0.25f;
        private const float TargetHeight = 1.0f;

        private readonly AnimalProfile _profile;
        private readonly IAnimalThreatSource _threats;
        private readonly IAnimalFoodSource _food;
        private readonly ILineOfSight _lineOfSight;
        private readonly List<AnimalThreat> _scratch = new List<AnimalThreat>(8);

        public AnimalPerception(AnimalProfile profile, IAnimalThreatSource threats, IAnimalFoodSource food, ILineOfSight lineOfSight)
        {
            if (profile == null) { throw new ArgumentNullException(nameof(profile)); }
            _profile = profile;
            _threats = threats;
            _food = food;
            _lineOfSight = lineOfSight;
        }

        public AnimalSenses Sense(Vector3 position, Vector3 forward)
        {
            var senses = new AnimalSenses();
            if (_threats != null)
            {
                _scratch.Clear();
                _threats.Collect(_scratch);
                float bestThreat = float.PositiveInfinity;
                float bestInterest = float.PositiveInfinity;
                for (int i = 0; i < _scratch.Count; i++)
                {
                    AnimalThreat t = _scratch[i];
                    float flat = Flat(position, t.Position);
                    float effective = flat / t.Danger;
                    bool heard = effective <= _profile.HearDistance;
                    bool inCone = VisionCone.Contains(position, forward, t.Position, _profile.ViewDistance, _profile.ViewHalfAngleDegrees);
                    bool seen = inCone && !Blocked(position, t.Position);
                    if (!heard && !seen) { continue; }
                    if (effective < bestThreat)
                    {
                        bestThreat = effective;
                        senses.ThreatDetected = true;
                        senses.ThreatPosition = t.Position;
                        senses.ThreatDistance = effective;
                    }

                    if (seen && t.IsPlayer && flat < bestInterest)
                    {
                        bestInterest = flat;
                        senses.InterestDetected = true;
                        senses.InterestPosition = t.Position;
                    }
                }
            }

            if (_food != null && _profile.CanBeg && _food.TryFindFood(position, _profile.SmellRange, out Vector3 food))
            {
                senses.FoodDetected = true;
                senses.FoodPosition = food;
            }

            return senses;
        }

        private bool Blocked(Vector3 from, Vector3 to)
        {
            return _lineOfSight != null
                && _lineOfSight.IsBlocked(from + new Vector3(0f, EyeHeight, 0f), to + new Vector3(0f, TargetHeight, 0f));
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            double dx = (double)a.x - b.x;
            double dz = (double)a.z - b.z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
