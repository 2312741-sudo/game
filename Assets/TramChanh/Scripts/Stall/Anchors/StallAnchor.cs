using UnityEngine;

namespace TramChanh.Stall.Anchors
{
    /// <summary>
    /// A station anchor on the stall. Its transform is the only source of the
    /// station position: positions are edited in the prefab or scene and are never
    /// hard-coded in scripts. Until the real stall reference is measured (DEC-011)
    /// every anchor stays <see cref="PositionConfirmed"/> == false.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StallAnchor : MonoBehaviour
    {
        [SerializeField] private StallAnchorId _id;

        [SerializeField, Tooltip("Set only when the position has been taken from the real stall reference (DEC-011).")]
        private bool _positionConfirmed;

        [SerializeField, TextArea, Tooltip("Where the position comes from (photo id, measurement, or 'provisional').")]
        private string _positionSource = "provisional — awaiting real stall reference (DEC-011)";

        public StallAnchorId Id => _id;
        public bool PositionConfirmed => _positionConfirmed;
        public string PositionSource => _positionSource;

        public void Configure(StallAnchorId id, bool positionConfirmed, string positionSource)
        {
            _id = id;
            _positionConfirmed = positionConfirmed;
            _positionSource = positionSource;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = _positionConfirmed ? Color.green : new Color(1f, 0.6f, 0f);
            Gizmos.DrawWireCube(transform.position, new Vector3(0.08f, 0.02f, 0.08f));
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.1f);
        }
#endif
    }
}
