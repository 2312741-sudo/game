using UnityEngine;

namespace TramChanh.Interaction
{
    /// <summary>Explicit collider-to-contract reference; no scene-wide searches.</summary>
    [DisallowMultipleComponent]
    public sealed class InteractableRef : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _behaviour;
        public IInteractable Target => _behaviour != null && _behaviour.isActiveAndEnabled ? _behaviour as IInteractable : null;
    }
}
