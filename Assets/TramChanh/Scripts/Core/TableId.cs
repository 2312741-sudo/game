using System;

namespace TramChanh.Core
{
    /// <summary>Positive identity; default is invalid and cannot identify a runtime entity.</summary>
    public readonly struct TableId : IEquatable<TableId>
    {
        public int Value { get; }
        public bool IsValid => Value > 0;

        public TableId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Identity must be positive.");
            }
            Value = value;
        }

        public bool Equals(TableId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is TableId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(TableId left, TableId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TableId left, TableId right)
        {
            return !left.Equals(right);
        }
    }
}
