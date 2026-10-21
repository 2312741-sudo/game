using System;
using System.Collections.Generic;
using TramChanh.Cakes;
using TramChanh.Core;
using TramChanh.Drinks.Domain;
using TramChanh.Interaction;
using TramChanh.Orders;

namespace TramChanh.Audio
{
    /// <summary>Where the router sends cues. AudioManager implements it; tests use a recorder.</summary>
    public interface IAudioCueSink
    {
        void Play(AudioCue cue);
        void Stop(AudioCue cue);
    }

    /// <summary>
    /// Pure C# translation of existing game events into cues. Subscribes on construction, unsubscribes on Dispose.
    /// Events that do not exist yet (day start/end, UI click, ambient) are triggered by calling the sink directly.
    /// </summary>
    public sealed class AudioEventRouter : IDisposable
    {
        private readonly IAudioCueSink _sink;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly HashSet<PreparationId> _cooking = new HashSet<PreparationId>();

        public AudioEventRouter(IEventBus events, IAudioCueSink sink)
        {
            if (events == null) { throw new ArgumentNullException(nameof(events)); }
            if (sink == null) { throw new ArgumentNullException(nameof(sink)); }
            _sink = sink;
            _subscriptions.Add(events.Subscribe<OrderStatusChanged>(OnOrderStatus));
            _subscriptions.Add(events.Subscribe<ActionBlocked>(OnActionBlocked));
            _subscriptions.Add(events.Subscribe<DrinkStepCompleted>(OnDrinkStep));
            _subscriptions.Add(events.Subscribe<CakeStepCompleted>(OnCakeStep));
        }

        public void Dispose()
        {
            for (int i = 0; i < _subscriptions.Count; i++) { _subscriptions[i].Dispose(); }
            _subscriptions.Clear();
            _cooking.Clear();
        }

        private void OnOrderStatus(OrderStatusChanged change)
        {
            switch (change.Status)
            {
                case OrderStatus.SentToStall: _sink.Play(AudioCue.OrderReceived); break;
                case OrderStatus.Ready: _sink.Play(AudioCue.TicketBell); break;
                case OrderStatus.Completed: _sink.Play(AudioCue.OrderServed); break;
                case OrderStatus.Failed: _sink.Play(AudioCue.OrderFailed); break;
            }
        }

        private void OnActionBlocked(ActionBlocked blocked)
        {
            _sink.Play(AudioCue.WrongAction);
        }

        private void OnDrinkStep(DrinkStepCompleted step)
        {
            switch (step.State)
            {
                case TeaBagState.CoconutJellyAdded:
                case TeaBagState.LemonJellyAdded: _sink.Play(AudioCue.Pour); break;
                case TeaBagState.IceAdded: _sink.Play(AudioCue.Ice); break;
            }
        }

        private void OnCakeStep(CakeStepCompleted step)
        {
            switch (step.State)
            {
                case CakeState.BatterPoured: _sink.Play(AudioCue.Pour); break;
                case CakeState.Flipped: _sink.Play(AudioCue.CakeFlip); break;
            }
            bool wasCooking = _cooking.Count > 0;
            if (step.State == CakeState.Cooking) { _cooking.Add(step.PreparationId); }
            else if (step.State != CakeState.Flipped) { _cooking.Remove(step.PreparationId); }
            bool isCooking = _cooking.Count > 0;
            if (!wasCooking && isCooking) { _sink.Play(AudioCue.GrillSizzleLoop); }
            else if (wasCooking && !isCooking) { _sink.Stop(AudioCue.GrillSizzleLoop); }
        }
    }
}
