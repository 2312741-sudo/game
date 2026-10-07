using System;

namespace TramChanh.Core
{
    public readonly struct ActorRef : IEquatable<ActorRef>
    {
        public int Value { get; }
        public ActorRef(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            Value = value;
        }
        public bool Equals(ActorRef other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ActorRef other && Equals(other);
        public override int GetHashCode() => Value;
    }
}
