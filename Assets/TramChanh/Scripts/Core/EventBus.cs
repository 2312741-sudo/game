using System;
using System.Collections.Generic;

namespace TramChanh.Core
{
    /// <summary>
    /// Dispatches in subscription order without boxing or copying event payloads to objects.
    /// Removed listeners are skipped immediately; added listeners wait for the next publish.
    /// Nested publishes see current subscriptions. Listener exceptions propagate to the caller.
    /// </summary>
    public sealed class EventBus : IEventBus, IDisposable
    {
        private readonly Dictionary<Type, IDisposable> _channels = new Dictionary<Type, IDisposable>();
        private bool _disposed;

        public IDisposable Subscribe<TEvent>(Action<TEvent> listener) where TEvent : struct
        {
            ThrowIfDisposed();
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }
            if (!_channels.TryGetValue(typeof(TEvent), out IDisposable channel))
            {
                channel = new Channel<TEvent>();
                _channels.Add(typeof(TEvent), channel);
            }
            return ((Channel<TEvent>)channel).Subscribe(listener);
        }

        public void Publish<TEvent>(TEvent message) where TEvent : struct
        {
            ThrowIfDisposed();
            if (_channels.TryGetValue(typeof(TEvent), out IDisposable channel))
            {
                ((Channel<TEvent>)channel).Publish(message);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            foreach (IDisposable channel in _channels.Values)
            {
                channel.Dispose();
            }
            _channels.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EventBus));
            }
        }

        private sealed class Channel<TEvent> : IDisposable where TEvent : struct
        {
            private readonly List<Subscription> _subscriptions = new List<Subscription>();
            private int _publishDepth;

            public IDisposable Subscribe(Action<TEvent> listener)
            {
                var subscription = new Subscription(this, listener);
                _subscriptions.Add(subscription);
                return subscription;
            }

            public void Publish(TEvent message)
            {
                int count = _subscriptions.Count;
                _publishDepth++;
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        _subscriptions[i].Listener?.Invoke(message);
                    }
                }
                finally
                {
                    _publishDepth--;
                    Compact();
                }
            }

            public void Dispose()
            {
                for (int i = 0; i < _subscriptions.Count; i++)
                {
                    _subscriptions[i].Deactivate();
                }
                Compact();
            }

            private void Compact()
            {
                // Keep indices stable until the outermost dispatch finishes.
                if (_publishDepth != 0)
                {
                    return;
                }
                for (int i = _subscriptions.Count - 1; i >= 0; i--)
                {
                    if (_subscriptions[i].Listener == null)
                    {
                        _subscriptions.RemoveAt(i);
                    }
                }
            }

            private sealed class Subscription : IDisposable
            {
                private Channel<TEvent> _owner;
                public Action<TEvent> Listener { get; private set; }

                public Subscription(Channel<TEvent> owner, Action<TEvent> listener)
                {
                    _owner = owner;
                    Listener = listener;
                }

                public void Dispose()
                {
                    Channel<TEvent> owner = _owner;
                    Deactivate();
                    owner?.Compact();
                }

                public void Deactivate()
                {
                    Listener = null;
                    _owner = null;
                }
            }
        }
    }
}
