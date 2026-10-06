using System;
using System.Collections;
using NUnit.Framework;
using TramChanh.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TramChanh.Tests.PlayMode.Core
{
    public sealed class CoreServicesSmokeTests
    {
        [UnityTest]
        public IEnumerator CX_002_UnityClock_TicksPausesAndResumesAcrossFrames()
        {
            var clock = new UnityGameClock();
            yield return null;
            clock.Tick();
            Assert.That(clock.Now, Is.GreaterThan(0));
            double beforePause = clock.Now;
            clock.Pause();
            yield return null;
            clock.Tick();
            Assert.That(clock.Now, Is.EqualTo(beforePause));
            Assert.That(clock.DeltaTime, Is.Zero);
            clock.Resume();
            yield return null;
            clock.Tick();
            Assert.That(clock.Now, Is.GreaterThan(beforePause));
            Assert.That(clock.Now - beforePause, Is.EqualTo(clock.DeltaTime).Within(0.000001));
        }

        [UnityTest]
        public IEnumerator CX_002_SceneUnload_DisposesOwnedServicesWithoutAffectingOtherScope()
        {
            Scene scene = SceneManager.CreateScene("CX_002_ServiceScope_Test");
            var owner = new GameObject("CoreServiceScopeProbe");
            SceneManager.MoveGameObjectToScene(owner, scene);
            var probe = owner.AddComponent<ServiceScopeProbe>();
            ServiceRegistry registry = probe.Registry;
            IEventBus bus = registry.Get<IEventBus>();
            using var otherRegistry = new ServiceRegistry();
            var otherBus = new EventBus();
            otherRegistry.Register<IEventBus>(otherBus);
            int calls = 0;
            using IDisposable subscription = otherBus.Subscribe<int>(value => calls += value);
            yield return SceneManager.UnloadSceneAsync(scene);
            Assert.Throws<ObjectDisposedException>(() => registry.Get<IEventBus>());
            Assert.Throws<ObjectDisposedException>(() => bus.Publish(1));
            otherBus.Publish(2);
            Assert.That(calls, Is.EqualTo(2));
        }
    }
}
