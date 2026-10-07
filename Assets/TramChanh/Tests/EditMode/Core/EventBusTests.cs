using System;
using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class EventBusTests
    {
        private readonly struct Message
        {
            public Message(int value) { Value = value; }
            public int Value { get; }
        }

        [Test]
        public void CX_002_EventBus_DispatchesSynchronouslyInSubscriptionOrder()
        {
            using var bus = new EventBus();
            int result = 0;
            bus.Subscribe<Message>(message => result = message.Value);
            bus.Subscribe<Message>(message => result *= 2);
            bus.Publish(new Message(3));
            Assert.That(result, Is.EqualTo(6));
        }

        [Test]
        public void CX_002_EventBus_UnsubscribeDuringPublishSkipsRemovedListener()
        {
            using var bus = new EventBus();
            int calls = 0;
            IDisposable removed = null;
            bus.Subscribe<Message>(_ => removed.Dispose());
            removed = bus.Subscribe<Message>(_ => calls++);
            bus.Publish(new Message(1));
            bus.Publish(new Message(2));
            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void CX_002_EventBus_NewSubscriptionsWaitUntilNextPublish()
        {
            using var bus = new EventBus();
            int calls = 0;
            IDisposable first = null;
            first = bus.Subscribe<Message>(_ =>
            {
                first.Dispose();
                bus.Subscribe<Message>(message => calls += message.Value);
            });
            bus.Publish(new Message(1));
            Assert.That(calls, Is.Zero);
            bus.Publish(new Message(2));
            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void CX_002_EventBus_ReentrantPublishKeepsRemainingListeners()
        {
            using var bus = new EventBus();
            int total = 0;
            IDisposable first = null;
            first = bus.Subscribe<Message>(message =>
            {
                first.Dispose();
                bus.Publish(new Message(2));
            });
            bus.Subscribe<Message>(message => total += message.Value);
            bus.Publish(new Message(1));
            Assert.That(total, Is.EqualTo(3));
        }

        [Test]
        public void CX_002_EventBus_DisposeDuringPublishStopsDispatch()
        {
            var bus = new EventBus();
            int calls = 0;
            bus.Subscribe<Message>(_ => bus.Dispose());
            IDisposable token = bus.Subscribe<Message>(_ => calls++);
            bus.Publish(new Message(1));
            token.Dispose();
            bus.Dispose();
            Assert.That(calls, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => bus.Publish(new Message(2)));
            Assert.Throws<ObjectDisposedException>(() => bus.Subscribe<Message>(_ => { }));
        }

        [Test]
        public void CX_002_EventBus_ExceptionDoesNotCorruptSubscriptions()
        {
            using var bus = new EventBus();
            IDisposable broken = bus.Subscribe<Message>(_ => throw new InvalidOperationException());
            int calls = 0;
            bus.Subscribe<Message>(_ => calls++);
            Assert.Throws<InvalidOperationException>(() => bus.Publish(new Message(1)));
            broken.Dispose();
            bus.Publish(new Message(1));
            Assert.That(calls, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(8)]
        public void CX_002_EventBus_StructPublishAllocatesZeroBytes(int subscribers)
        {
            using var bus = new EventBus();
            Action<Message> listener = _ => { };
            for (int i = 0; i < subscribers; i++)
            {
                bus.Subscribe(listener);
            }
            var message = new Message(1);
            bus.Publish(message);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                bus.Publish(message);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }

        [Test]
        public void CX_002_EventBus_EventTypesAreIsolatedAndNullListenerRejected()
        {
            using var bus = new EventBus();
            int calls = 0;
            bus.Subscribe<Message>(_ => calls++);
            bus.Publish(1);
            Assert.That(calls, Is.Zero);
            Assert.Throws<ArgumentNullException>(() => bus.Subscribe<Message>(null));
        }
    }
}
