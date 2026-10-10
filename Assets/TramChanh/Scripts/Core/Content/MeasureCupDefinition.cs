using TramChanh.Core.Provisional;
using UnityEngine;

namespace TramChanh.Content
{
    [CreateAssetMenu(menuName = "Tram Chanh/Cakes/500 ml cup")]
    public sealed class MeasureCupDefinition : ScriptableObject
    {
        public const float NominalCapacityMl = 500f;
        [SerializeField, Tbd("DEC-007")] private float _fillRateMlPerSecond;
        [SerializeField, Tbd("DEC-007")] private AnimationCurve _levelCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField, TextArea] private string _provisionalNotes;
        public float CapacityMl => NominalCapacityMl;
        public float FillRateMlPerSecond => _fillRateMlPerSecond;
        public bool IsConfigured => CakeRecipe.Positive(_fillRateMlPerSecond) && _levelCurve != null;
        public float Level01(float ml) => Mathf.Clamp01(_levelCurve.Evaluate(Mathf.Clamp01(ml / CapacityMl)));
    }
}
