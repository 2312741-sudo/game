using TramChanh.Core.GroundTruth;
using TramChanh.People;
using UnityEngine;

namespace TramChanh.App.People
{
    /// <summary>
    /// Wall test for animal sight and movement: a raycast against the Environment layer. Colliders that belong to a street animal are ignored,
    /// so a neighbour never counts as a wall. Owns its hit buffer, so it does not allocate per call.
    /// </summary>
    public sealed class PhysicsLineOfSight : ILineOfSight
    {
        private const int BufferSize = 8;
        private static readonly int Mask = 1 << TramChanhLayers.EnvironmentIndex;
        private readonly RaycastHit[] _hits = new RaycastHit[BufferSize];

        public bool IsBlocked(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float distance = delta.magnitude;
            if (distance < 1e-3f) { return false; }
            int count = Physics.RaycastNonAlloc(from, delta / distance, _hits, distance, Mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i].collider;
                if (hit != null && hit.GetComponentInParent<StreetAnimalWander>() == null) { return true; }
            }

            return false;
        }
    }
}
