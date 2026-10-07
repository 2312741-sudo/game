using System;

namespace TramChanh.Core
{
    /// <summary>Main-thread synchronous notifications. Events should be readonly structs.</summary>
    public interface IEventBus
    {
        IDisposable Subscribe<TEvent>(Action<TEvent> listener) where TEvent : struct;
        void Publish<TEvent>(TEvent message) where TEvent : struct;
    }
}
