using UnityEngine;

namespace TramChanh.Effects
{
    /// <summary>
    /// Decides when a tail-light trail must be cleared: a pooled vehicle that jumps to a new place would otherwise draw a
    /// long red line across the street between the old and the new position.
    /// </summary>
    public static class TrailResetRule
    {
        public static bool ShouldClear(Vector3 previous, Vector3 current, float maxStepMeters)
        {
            return (current - previous).sqrMagnitude > maxStepMeters * maxStepMeters;
        }
    }
}
