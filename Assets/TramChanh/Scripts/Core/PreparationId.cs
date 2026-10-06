using System;

namespace TramChanh.Core
{
    /// <summary>Positive identity; default is invalid and cannot identify a runtime entity.</summary>
    public readonly struct PreparationId : IEquatable<PreparationId>
    {
        public int Value { get; }
        public bool IsValid => Value > 0;

        public PreparationId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Identity must be positive.");
            }
            Value = value;
        }

        public bool Equals(PreparationId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is PreparationId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(PreparationId left, PreparationId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PreparationId left, PreparationId right)
        {
            return !left.Equals(right);
        }
    }
}
