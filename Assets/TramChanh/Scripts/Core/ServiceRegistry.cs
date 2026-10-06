using System;
using System.Collections.Generic;

namespace TramChanh.Core
{
    /// <summary>
    /// Explicit scene-owned service contracts. The composition root owns this registry
    /// and disposes it on scene unload. Each distinct instance is disposed once,
    /// in reverse registration order, even when registered through multiple contracts.
    /// </summary>
    public sealed class ServiceRegistry : IDisposable
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly List<IDisposable> _ownedServices = new List<IDisposable>();
        private bool _disposed;

        public void Register<TService>(TService service) where TService : class
        {
            ThrowIfDisposed();
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }
            if (_services.ContainsKey(typeof(TService)))
            {
                throw new InvalidOperationException($"Service '{typeof(TService).FullName}' is already registered.");
            }
            _services.Add(typeof(TService), service);
            if (service is IDisposable disposable)
            {
                for (int i = 0; i < _ownedServices.Count; i++)
                {
                    if (ReferenceEquals(_ownedServices[i], disposable))
                    {
                        return;
                    }
                }
                _ownedServices.Add(disposable);
            }
        }

        public TService Get<TService>() where TService : class
        {
            ThrowIfDisposed();
            if (!_services.TryGetValue(typeof(TService), out object service))
            {
                throw new InvalidOperationException($"Service '{typeof(TService).FullName}' is not registered in this scene scope.");
            }
            return (TService)service;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            List<Exception> errors = null;
            for (int i = _ownedServices.Count - 1; i >= 0; i--)
            {
                try
                {
                    _ownedServices[i].Dispose();
                }
                catch (Exception error)
                {
                    errors ??= new List<Exception>();
                    errors.Add(error);
                }
            }
            _ownedServices.Clear();
            _services.Clear();
            if (errors != null)
            {
                throw new AggregateException("Scene service disposal failed.", errors);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ServiceRegistry));
            }
        }
    }
}
