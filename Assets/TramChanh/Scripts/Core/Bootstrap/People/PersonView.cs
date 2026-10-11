using System;
using System.Collections.Generic;
using TramChanh.People;
using UnityEngine;

namespace TramChanh.App.People
{
    /// <summary>
    /// Thin presentation of one simulated person or vehicle: follows a precomputed path with the game clock
    /// and drives the optional Antigravity animator (AC_Person: Speed, Sitting, Eating, Wave).
    /// No colliders: people never block the player or the interaction ray.
    /// </summary>
    public sealed class PersonView : MonoBehaviour
    {
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int SittingParameter = Animator.StringToHash("Sitting");
        private static readonly int EatingParameter = Animator.StringToHash("Eating");

        private PathFollower _follower;
        private Action<PersonView> _onArrived;
        private Animator _animator;
        private bool _hasSpeed, _hasSitting, _hasEating;

        public bool IsWalking => _follower != null && !_follower.Arrived;

        public void Prepare()
        {
            // Contract: visuals only. Disable any collider an art prefab may bring.
            foreach (Collider collider in GetComponentsInChildren<Collider>(true)) { collider.enabled = false; }
            _animator = GetComponentInChildren<Animator>(true);
            if (_animator != null)
            {
                // The path follower owns the root; animation must never move it.
                _animator.applyRootMotion = false;
                _animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }
            _hasSpeed = _hasSitting = _hasEating = false;
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                foreach (AnimatorControllerParameter parameter in _animator.parameters)
                {
                    if (parameter.nameHash == SpeedParameter) { _hasSpeed = true; }
                    else if (parameter.nameHash == SittingParameter) { _hasSitting = true; }
                    else if (parameter.nameHash == EatingParameter) { _hasEating = true; }
                }
            }
        }

        public void Walk(IReadOnlyList<Vector3> path, float speed, Action<PersonView> onArrived)
        {
            _onArrived = onArrived;
            _follower = new PathFollower(path, speed);
            SetSitting(false);
            SetEating(false);
            Apply();
            if (_follower.Arrived) { Finish(); }
        }

        /// <summary>Keeps walking the current path and replaces the arrival callback (e.g. hand-over to a leaver pool).</summary>
        public void ContinueThen(Action<PersonView> onArrived)
        {
            if (IsWalking) { _onArrived = onArrived; }
            else { onArrived?.Invoke(this); }
        }

        public void Place(Vector3 position, Quaternion rotation)
        {
            _follower = null;
            _onArrived = null;
            transform.SetPositionAndRotation(position, rotation);
            SetSpeed(0f);
        }

        public void SetSitting(bool sitting) { if (_hasSitting) { _animator.SetBool(SittingParameter, sitting); } }
        public void SetEating(bool eating) { if (_hasEating) { _animator.SetBool(EatingParameter, eating); } }
        public void SetAnimatorSpeed(float speed) { if (_animator != null) { _animator.speed = speed; } }

        public void Step(double seconds)
        {
            if (_follower == null) { return; }
            _follower.Step(seconds);
            Apply();
            if (_follower.Arrived) { Finish(); }
        }

        private void Finish()
        {
            _follower = null;
            SetSpeed(0f);
            Action<PersonView> callback = _onArrived;
            _onArrived = null;
            callback?.Invoke(this);
        }

        private void Apply()
        {
            transform.position = _follower.Position;
            Vector3 forward = _follower.Forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f) { transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up); }
            SetSpeed(_follower.CurrentSpeed);
        }

        private void SetSpeed(float speed) { if (_hasSpeed) { _animator.SetFloat(SpeedParameter, speed); } }
    }
}
