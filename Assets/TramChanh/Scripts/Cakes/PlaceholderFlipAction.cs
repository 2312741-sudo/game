using TramChanh.Core;
using UnityEngine;
namespace TramChanh.Cakes
{
    public sealed class PlaceholderFlipAction : MonoBehaviour, IFlipAction
    {
        [SerializeField] private Transform _rollArea;
        [SerializeField] private Transform _spatula;
        public Transform Destination => _rollArea;
        public Availability CanFlip(GrillModel grill, CakePreparation cake) => _rollArea == null
            ? Availability.Blocked("cake.flip_unconfigured") : Availability.Available;
        public void Present(CakeItem item)
        {
            item.transform.SetParent(_rollArea, false); item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            if (_spatula != null) { _spatula.localRotation = Quaternion.Euler(0f, 0f, -15f); }
        }
    }
}
