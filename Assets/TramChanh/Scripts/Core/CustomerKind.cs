using System;

namespace TramChanh.Core
{
    public enum CustomerKind { Student = 0, Family = 1, Office = 2, Regular = 3 }

    /// <summary>How one kind of customer behaves: how long they wait and how much they pay.</summary>
    public readonly struct CustomerProfile
    {
        public CustomerProfile(CustomerKind kind, string displayName, double patienceSeconds, float payMultiplier, int weight)
        {
            Kind = kind;
            DisplayName = displayName;
            PatienceSeconds = patienceSeconds;
            PayMultiplier = payMultiplier;
            Weight = weight;
        }

        public CustomerKind Kind { get; }
        public string DisplayName { get; }
        /// <summary>Game-clock seconds the customer waits before leaving.</summary>
        public double PatienceSeconds { get; }
        /// <summary>Multiplies what the order pays (tips of regulars, rush pay of office workers).</summary>
        public float PayMultiplier { get; }
        /// <summary>Relative chance of this kind arriving.</summary>
        public int Weight { get; }
    }

    /// <summary>Table of the customer kinds. Numbers are first guesses (GAMEPLAY_DESIGN.md, section 3); tune them here.</summary>
    public sealed class CustomerKinds
    {
        private readonly CustomerProfile[] _profiles;
        private readonly int _totalWeight;

        public CustomerKinds(CustomerProfile[] profiles)
        {
            if (profiles == null || profiles.Length == 0) { throw new ArgumentException("Need at least one profile.", nameof(profiles)); }
            _profiles = (CustomerProfile[])profiles.Clone();
            foreach (CustomerProfile p in _profiles) { _totalWeight += Math.Max(0, p.Weight); }
            if (_totalWeight <= 0) { throw new ArgumentException("At least one profile needs a positive weight.", nameof(profiles)); }
        }

        public static CustomerKinds Default { get; } = new CustomerKinds(new[]
        {
            new CustomerProfile(CustomerKind.Student, "Học sinh", 120d, 1.0f, 40),
            new CustomerProfile(CustomerKind.Family, "Gia đình", 180d, 1.0f, 25),
            new CustomerProfile(CustomerKind.Office, "Nhân viên văn phòng", 90d, 1.15f, 25),
            new CustomerProfile(CustomerKind.Regular, "Khách quen", 150d, 1.2f, 10),
        });

        public int Count => _profiles.Length;
        public CustomerProfile At(int index) => _profiles[index];

        public CustomerProfile Profile(CustomerKind kind)
        {
            foreach (CustomerProfile p in _profiles) { if (p.Kind == kind) { return p; } }
            return _profiles[0];
        }

        /// <summary>Picks a kind by weight. <paramref name="roll"/> is a random number in [0, 1); other values are clamped.</summary>
        public CustomerProfile Pick(double roll)
        {
            if (double.IsNaN(roll)) { roll = 0d; }
            roll = Math.Min(0.999999d, Math.Max(0d, roll));
            double target = roll * _totalWeight;
            double sum = 0d;
            foreach (CustomerProfile p in _profiles)
            {
                sum += Math.Max(0, p.Weight);
                if (target < sum) { return p; }
            }
            return _profiles[_profiles.Length - 1];
        }
    }
}
