using UnityEngine;

namespace TramChanh.People
{
    /// <summary>Something an animal may be afraid of or curious about: the player, a motorbike. Plain data.</summary>
    public readonly struct AnimalThreat
    {
        public AnimalThreat(Vector3 position, float danger, bool isPlayer)
        {
            Position = position;
            Danger = danger > 0.05f ? danger : 0.05f;
            IsPlayer = isPlayer;
        }

        public Vector3 Position { get; }

        /// <summary>
        /// 1 for a walking person. A louder or faster thing has a larger value: it is heard from further away and
        /// scares the animal from further away (distances are divided by this number).
        /// </summary>
        public float Danger { get; }

        /// <summary>True for the player. Only the player can make an animal curious.</summary>
        public bool IsPlayer { get; }
    }
}
