using UnityEngine;

namespace TramChanh.People
{
    /// <summary>What an animal perceives right now. This is the only input of <see cref="AnimalBrain"/> besides time and position.</summary>
    public struct AnimalSenses
    {
        /// <summary>The nearest threat was seen (inside the view cone, not hidden) or heard.</summary>
        public bool ThreatDetected;

        public Vector3 ThreatPosition;

        /// <summary>Flat distance divided by the threat's danger. Compare it with the profile's flee distances.</summary>
        public float ThreatDistance;

        /// <summary>The player was seen at a distance that is not scary: worth a look.</summary>
        public bool InterestDetected;

        public Vector3 InterestPosition;

        /// <summary>Somebody is eating at a table within smelling range.</summary>
        public bool FoodDetected;

        public Vector3 FoodPosition;
    }
}
