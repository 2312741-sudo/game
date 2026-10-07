using System;
using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class ServiceRegistryTests
    {
        private interface IService { }
        private sealed class Service : IService, IDisposable
        {
            private readonly Action _dispose;
            public Service(Action dispose) { _dispose = dispose; }
            public void Dispose() { _dispose(); }
        }

        [Test]
        public void CX_002_Registry_ResolvesExplicitContractAndReportsMissingType()
        {
            using var registry = new ServiceRegistry();
            var service = new Service(() => { });
            registry.Register<IService>(service);
            Assert.That(registry.Get<IService>(), Is.SameAs(service));
            var error = Assert.Throws<InvalidOperationException>(() => registry.Get<IEventBus>());
            Assert.That(error.Message, Does.Contain(nameof(IEventBus)));
            Assert.Throws<InvalidOperationException>(() => registry.Register<IService>(service));
            Assert.Throws<ArgumentNullException>(() => registry.Register<IEventBus>(null));
        }

        [Test]
        public void CX_002_Registry_DisposesOnceInReverseOrderAndRejectsReuse()
        {
            var order = new List<int>();
            var registry = new ServiceRegistry();
            var first = new Service(() => order.Add(1));
            registry.Register<IService>(first);
            registry.Register<Service>(first);
            registry.Register<IDisposable>(new Service(() => order.Add(2)));
            registry.Dispose();
            registry.Dispose();
            Assert.That(order, Is.EqualTo(new[] { 2, 1 }));
            Assert.Throws<ObjectDisposedException>(() => registry.Get<IService>());
            Assert.Throws<ObjectDisposedException>(() => registry.Register<IService>(first));
        }

        [Test]
        public void CX_002_Registry_DisposesRemainingServicesAfterFailure()
        {
            bool disposed = false;
            var registry = new ServiceRegistry();
            registry.Register<IService>(new Service(() => disposed = true));
            registry.Register<IDisposable>(new Service(() => throw new InvalidOperationException()));
            Assert.Throws<AggregateException>(() => registry.Dispose());
            Assert.That(disposed, Is.True);
            registry.Dispose();
        }
    }
}
