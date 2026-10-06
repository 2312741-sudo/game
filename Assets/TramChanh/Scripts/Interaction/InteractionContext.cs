using System;
using TramChanh.Core;

namespace TramChanh.Interaction
{
    public sealed class InteractionContext
    {
        public ActorRef Actor { get; }
        public ActorRole Role { get; }
        public IHeldItemSlot Hands { get; }
        public IGameClock Clock { get; }
        public IEventBus Events { get; }

        public InteractionContext(ActorRef actor, ActorRole role, IHeldItemSlot hands, IGameClock clock, IEventBus events)
        {
            Actor = actor;
            Role = role;
            Hands = hands ?? throw new ArgumentNullException(nameof(hands));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Events = events ?? throw new ArgumentNullException(nameof(events));
        }
    }
}
