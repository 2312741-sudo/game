using TramChanh.Core;
using TramChanh.Core.Provisional;
using UnityEngine;

namespace TramChanh.Content
{
    public enum BatterOutOfTolerancePolicy { AllowWithPenalty, BlockPour }
    [CreateAssetMenu(menuName = "Tram Chanh/Cakes/Recipe (real values TBD)")]
    public sealed class CakeRecipe : ScriptableObject
    {
        [SerializeField] private ItemDefinition _itemDefinition;
        [SerializeField, Tbd("DEC-007")] private float _targetBatterMl;
        [SerializeField, Tbd("DEC-007")] private float _batterToleranceMl;
        [SerializeField, Tbd("DEC-007")] private float _cookedThreshold;
        [SerializeField, Tbd("DEC-007")] private float _burnThreshold;
        [SerializeField, Tbd("DEC-007")] private float _openLidHeatFactor;
        [SerializeField, Tbd("DEC-008")] private string _sauce;
        [SerializeField, Tbd("DEC-007")] private float _cutHoldSeconds;
        [SerializeField, Tbd("DEC-007")] private float _sauceHoldSeconds;
        [SerializeField, Tbd("DEC-007")] private float _rollHoldSeconds;
        [SerializeField, Tbd("DEC-019")] private BatterOutOfTolerancePolicy _outOfTolerancePolicy;
        [SerializeField, Tbd("DEC-019")] private float _deviationPenaltyPerMl;
        [SerializeField, TextArea] private string _provisionalNotes;
        public ItemDefinition ItemDefinition => _itemDefinition;
        public float TargetBatterMl => _targetBatterMl;
        public float BatterToleranceMl => _batterToleranceMl;
        public float CookedThreshold => _cookedThreshold;
        public float BurnThreshold => _burnThreshold;
        public float OpenLidHeatFactor => _openLidHeatFactor;
        public string Sauce => _sauce;
        public float CutHoldSeconds => _cutHoldSeconds;
        public float SauceHoldSeconds => _sauceHoldSeconds;
        public float RollHoldSeconds => _rollHoldSeconds;
        public BatterOutOfTolerancePolicy OutOfTolerancePolicy => _outOfTolerancePolicy;
        public float DeviationPenaltyPerMl => _deviationPenaltyPerMl;
        public bool IsConfigured => _itemDefinition != null && _itemDefinition.Kind == ItemKind.Cake
            && Positive(_targetBatterMl) && _targetBatterMl <= MeasureCupDefinition.NominalCapacityMl
            && Nonnegative(_batterToleranceMl) && Positive(_cookedThreshold)
            && Finite(_burnThreshold) && _burnThreshold > _cookedThreshold && Nonnegative(_openLidHeatFactor)
            && !string.IsNullOrWhiteSpace(_sauce) && Positive(_cutHoldSeconds) && Positive(_sauceHoldSeconds)
            && Positive(_rollHoldSeconds) && Nonnegative(_deviationPenaltyPerMl)
            && (_outOfTolerancePolicy == BatterOutOfTolerancePolicy.AllowWithPenalty || _outOfTolerancePolicy == BatterOutOfTolerancePolicy.BlockPour);
        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static bool Positive(float value) => Finite(value) && value > 0f;
        public static bool Nonnegative(float value) => Finite(value) && value >= 0f;
    }
}
