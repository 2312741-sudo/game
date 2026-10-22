using TramChanh.Core.GroundTruth;
using UnityEngine;

namespace TramChanh.Props
{
    /// <summary>Shared numbers for the discarded-prop physics. The same values are written into PM_Debris by PhysicsPropsBuilder.</summary>
    public static class DebrisPhysicsDefaults
    {
        public const float DynamicFriction = 0.5f;
        public const float StaticFriction = 0.6f;
        public const float Bounciness = 0.35f;
        public const float Mass = 0.15f;
        public const float LinearDamping = 0.1f;
        public const float AngularDamping = 0.6f;

        /// <summary>Layers the debris must never collide with: the player's CharacterController, NPCs, interactables and held items.</summary>
        public static int ExcludedLayerMask =>
            (1 << TramChanhLayers.PlayerIndex)
            | (1 << TramChanhLayers.NpcIndex)
            | (1 << TramChanhLayers.InteractableIndex);

        /// <summary>Runtime fallback used only when no PM_Debris asset is assigned.</summary>
        public static PhysicsMaterial CreateMaterial()
        {
            return new PhysicsMaterial("PM_Debris_Runtime")
            {
                dynamicFriction = DynamicFriction,
                staticFriction = StaticFriction,
                bounciness = Bounciness,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Maximum,
            };
        }
    }
}
