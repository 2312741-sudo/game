using UnityEngine;

namespace TramChanh.Effects
{
    /// <summary>
    /// A hanging wire drawn by a LineRenderer. The Positions of the line are computed once from two end transforms and a sag
    /// (<see cref="CatenaryCurve"/>), so it costs one draw call and nothing per frame. Call <see cref="Rebuild"/> after
    /// moving an end.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class SagWire : MonoBehaviour
    {
        [SerializeField] private Transform _start;
        [SerializeField] private Transform _end;
        [SerializeField, Min(0f), Tooltip("Metres the lowest point hangs below the straight line between the ends.")]
        private float _sag = 0.5f;
        [SerializeField, Range(2, 64), Tooltip("Number of straight pieces; the line has this many plus one positions.")]
        private int _segments = 24;

        private LineRenderer _line;
        private Vector3[] _points;

        public float Sag => _sag;
        public int Segments => _segments;

        private void Start()
        {
            Rebuild();
        }

        public void Configure(Transform start, Transform end, float sag, int segments)
        {
            _start = start;
            _end = end;
            _sag = Mathf.Max(0f, sag);
            _segments = Mathf.Clamp(segments, 2, 64);
        }

        [ContextMenu("Rebuild wire")]
        public void Rebuild()
        {
            if (_start == null || _end == null)
            {
                return;
            }
            if (_line == null)
            {
                _line = GetComponent<LineRenderer>();
            }
            int count = _segments + 1;
            if (_points == null || _points.Length != count)
            {
                _points = new Vector3[count];
            }
            // Positions are kept in the local space of this object so the wire follows it when the stall moves.
            _line.useWorldSpace = false;
            Vector3 a = transform.InverseTransformPoint(_start.position);
            Vector3 b = transform.InverseTransformPoint(_end.position);
            CatenaryCurve.Fill(a, b, _sag, _points);
            _line.positionCount = count;
            _line.SetPositions(_points);
        }
    }
}
