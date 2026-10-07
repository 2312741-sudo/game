using TramChanh.Core.Provisional;
using UnityEngine;

namespace TramChanh.Content
{
    [CreateAssetMenu(menuName = "Tram Chanh/Balance Config")]
    public sealed class BalanceConfig : ScriptableObject
    {
        [SerializeField, Tbd("DEC-007"), Min(0.01f)] private float _reachDistance;
        [SerializeField, Tbd("DEC-002", "Temporary first-person tuning"), Min(0.01f)] private float _moveSpeed;
        [SerializeField, Tbd("DEC-002"), Min(0.01f)] private float _lookSensitivity;
        [SerializeField, Tbd("DEC-002")] private float _gravity;
        [SerializeField, Tbd("DEC-002"), Range(1f, 89f)] private float _pitchLimit;
        [SerializeField, Tbd("DEC-015"), Min(0)] private int _teaRackCapacity;
        [SerializeField, Tbd("DEC-015"), Min(0)] private int _teaRackInitialStock;
        public int TeaRackCapacity => _teaRackCapacity;
        public int TeaRackInitialStock => _teaRackInitialStock;
        public float ReachDistance => _reachDistance;
        public float MoveSpeed => _moveSpeed;
        public float LookSensitivity => _lookSensitivity;
        public float Gravity => _gravity;
        public float PitchLimit => _pitchLimit;
    }
}
