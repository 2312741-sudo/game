using System;

namespace TramChanh.Core
{
    public readonly struct Availability
    {
        public AvailabilityStatus Status { get; }
        public string ReasonKey { get; }
        public bool IsAvailable => Status == AvailabilityStatus.Available;
        public static Availability Available => new Availability(AvailabilityStatus.Available, null);
        public static Availability Hidden => default;

        private Availability(AvailabilityStatus status, string reasonKey)
        {
            Status = status;
            ReasonKey = reasonKey;
        }

        public static Availability Blocked(string reasonKey)
        {
            if (string.IsNullOrWhiteSpace(reasonKey))
            {
                throw new ArgumentException("A blocked action requires a reason key.", nameof(reasonKey));
            }
            return new Availability(AvailabilityStatus.Blocked, reasonKey);
        }
    }
}
