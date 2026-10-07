using System;

namespace TramChanh.Core
{
    /// <summary>Positive identity; default is invalid and cannot identify a runtime entity.</summary>
    public readonly struct CustomerId : IEquatable<CustomerId>
    {
        public int Value { get; }
        public bool IsValid => Value > 0;

        public CustomerId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Identity must be positive.");
            }
            Value = value;
        }

        public bool Equals(CustomerId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is CustomerId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(CustomerId left, CustomerId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CustomerId left, CustomerId right)
        {
            return !left.Equals(right);
        }
    }
}
