using TramChanh.Core;
using UnityEngine;

namespace TramChanh.Interaction.Player
{
    [RequireComponent(typeof(CharacterController))]
    [DefaultExecutionOrder(-200)]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        private CharacterController _body;
        private PlayerInputReader _input;
        private IGameClock _clock;
        private float _moveSpeed;
        private float _lookSensitivity;
        private float _gravity;
        private float _pitchLimit;
        private float _pitch;
        private float _verticalSpeed;
        public Camera ViewCamera => _camera;

        public void Initialize(PlayerInputReader input, IGameClock clock, float moveSpeed, float lookSensitivity, float gravity, float pitchLimit)
        {
            _body = GetComponent<CharacterController>();
            _input = input;
            _clock = clock;
            _moveSpeed = moveSpeed;
            _lookSensitivity = lookSensitivity;
            _gravity = gravity;
            _pitchLimit = pitchLimit;
            _pitch = Mathf.DeltaAngle(0f, _camera.transform.localEulerAngles.x);
        }
        private void Update()
        {
            if (_clock != null && !_clock.IsPaused && _input.IsCaptured)
            {
                Simulate(_input.Move, _input.Look, (float)_clock.DeltaTime);
            }
        }
        public void Simulate(Vector2 move, Vector2 look, float seconds)
        {
            if (_clock == null || _clock.IsPaused || seconds <= 0f)
            {
                return;
            }
            transform.Rotate(Vector3.up, look.x * _lookSensitivity, Space.World);
            _pitch = Mathf.Clamp(_pitch - look.y * _lookSensitivity, -_pitchLimit, _pitchLimit);
            _camera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            Vector2 direction = Vector2.ClampMagnitude(move, 1f);
            Vector3 horizontal = (transform.right * direction.x + transform.forward * direction.y) * _moveSpeed;
            if (_body.isGrounded && _verticalSpeed < 0f)
            {
                _verticalSpeed = 0f;
            }
            _verticalSpeed += _gravity * seconds;
            _body.Move((horizontal + Vector3.up * _verticalSpeed) * seconds);
        }
    }
}
