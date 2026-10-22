using TramChanh.Core.GroundTruth;
using UnityEngine;

namespace TramChanh.Props
{
    /// <summary>
    /// Trigger sphere around a hanging prop (child object of the prop's Rigidbody, so the trigger belongs to a dynamic body).
    /// OnTriggerEnter forwards the brush-past to <see cref="HangingSwayDriver"/>. Keep this object off the Interactable layer.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class HangingNudgeZone : MonoBehaviour
    {
        [SerializeField] private HangingSwayDriver _driver;
        [SerializeField] private LayerMask _actorLayers = (1 << TramChanhLayers.PlayerIndex) | (1 << TramChanhLayers.NpcIndex);

        public void Configure(HangingSwayDriver driver)
        {
            _driver = driver;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_driver == null || (_actorLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }
            float speed = 1f;
            if (other is CharacterController controller)
            {
                speed = controller.velocity.magnitude;
            }
            else if (other.attachedRigidbody != null && !other.attachedRigidbody.isKinematic)
            {
                speed = other.attachedRigidbody.linearVelocity.magnitude;
            }
            Vector3 away = _driver.transform.position - other.transform.position;
            _driver.Nudge(away, speed);
        }
    }
}
