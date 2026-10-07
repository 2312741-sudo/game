using TramChanh.App;
using TramChanh.Core;
using TramChanh.Drinks.Runtime;
using UnityEngine;

namespace TramChanh.DevTools.Drinks
{
    /// <summary>Only serialized into the original DRINK-001 pickup fixture.</summary>
    [DefaultExecutionOrder(-900)]
    public sealed class TeaRackPickupTestSetup : MonoBehaviour
    {
        [SerializeField] private PlayerInteractionTestBootstrap _bootstrap;
        [SerializeField] private TeaRackController _rack;
        private void Awake()
        {
            if (!Debug.isDebugBuild) { return; }
            _rack.Initialize(new DevelopmentPickupTicketQueue(), new SequentialIdGenerator(), _bootstrap.Interactor.Context.Events);
        }
    }
}
