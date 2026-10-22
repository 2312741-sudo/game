using UnityEngine;

namespace TramChanh.Props
{
    /// <summary>Keeps a thin cylinder (Unity default: 2 m tall, Y axis) stretched between two transforms, so a cord follows a swinging prop.</summary>
    public sealed class JointRopeVisual : MonoBehaviour
    {
        [SerializeField] private Transform _top;
        [SerializeField] private Transform _bottom;
        [SerializeField, Min(0.001f)] private float _thickness = 0.01f;

        public void Configure(Transform top, Transform bottom, float thickness)
        {
            _top = top;
            _bottom = bottom;
            _thickness = thickness;
        }

        private void LateUpdate()
        {
            if (_top == null || _bottom == null)
            {
                return;
            }
            Vector3 a = _top.position;
            Vector3 b = _bottom.position;
            Vector3 d = b - a;
            float length = d.magnitude;
            if (length < 1e-4f)
            {
                return;
            }
            transform.position = (a + b) * 0.5f;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, d / length);
            transform.localScale = new Vector3(_thickness, length * 0.5f, _thickness);
        }
    }
}
