using TramChanh.Core;
using TramChanh.Core.Provisional;
using UnityEngine;

namespace TramChanh.Content
{
    [CreateAssetMenu(menuName = "Tram Chanh/Content/Drink Recipe")]
    public sealed class DrinkRecipe : ScriptableObject
    {
        [SerializeField] private ItemDefinition _itemDefinition;
        [SerializeField] private string _teaBagType;
        [SerializeField, Tbd("DEC-007")] private float _coconutJellyPortion;
        [SerializeField, Tbd("DEC-007")] private float _lemonJellyPortion;
        [SerializeField, Tbd("DEC-007")] private float _icePortion;
        [SerializeField, Tbd("DEC-007")] private float _shakeHoldSeconds;
        [SerializeField, Tbd("DEC-007")] private float _wipeHoldSeconds;
        public ItemDefinition ItemDefinition => _itemDefinition;
        public string TeaBagType => _teaBagType;
        public float CoconutJellyPortion => _coconutJellyPortion;
        public float LemonJellyPortion => _lemonJellyPortion;
        public float IcePortion => _icePortion;
        public float ShakeHoldSeconds => _shakeHoldSeconds;
        public float WipeHoldSeconds => _wipeHoldSeconds;
        public bool HasValidDurations => PositiveFinite(_shakeHoldSeconds) && PositiveFinite(_wipeHoldSeconds);
        public bool IsConfigured => _itemDefinition != null && _itemDefinition.Kind == ItemKind.Drink
            && HasValidDurations && NonnegativeFinite(_coconutJellyPortion)
            && NonnegativeFinite(_lemonJellyPortion) && NonnegativeFinite(_icePortion);

        private static bool PositiveFinite(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool NonnegativeFinite(float value) => value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

