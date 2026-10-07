using TramChanh.Core;
using UnityEngine;

namespace TramChanh.Tests.PlayMode.Core
{
    /// <summary>Test-only owner exercising disposal through a real Unity scene unload.</summary>
    public sealed class ServiceScopeProbe : MonoBehaviour
    {
        public ServiceRegistry Registry { get; private set; }

        private void Awake()
        {
            Registry = new ServiceRegistry();
            Registry.Register<IEventBus>(new EventBus());
        }

        private void OnDestroy()
        {
            Registry.Dispose();
        }
    }
}
