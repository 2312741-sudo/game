using System;

namespace TramChanh.Core
{
    public enum CustomerKind { Student = 0, Family = 1, Office = 2, Regular = 3, Critic = 4 }

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
            new CustomerProfile(CustomerKind.Student, "Học sinh", 180d, 1.0f, 40),
            new CustomerProfile(CustomerKind.Family, "Gia đình", 270d, 1.0f, 25),
            new CustomerProfile(CustomerKind.Office, "Nhân viên văn phòng", 135d, 1.15f, 25),
            new CustomerProfile(CustomerKind.Regular, "Khách quen", 225d, 1.2f, 10),
            new CustomerProfile(CustomerKind.Critic, "Nhà phê bình", 150d, 1.0f, 5),
        });

        public int Count => _profiles.Length;
        public CustomerProfile At(int index) => _profiles[index];

        public CustomerProfile Profile(CustomerKind kind)
        {
            foreach (CustomerProfile p in _profiles) { if (p.Kind == kind) { return p; } }
            return _profiles[0];
        }

        /// <summary>Picks a kind by weight. <paramref name="roll"/> is a random number in [0, 1); other values are clamped. The critic can come.</summary>
        public CustomerProfile Pick(double roll) => Pick(roll, true);

        /// <summary>Same, but with <paramref name="allowCritic"/> false the critic is left out (the critic only comes at higher reputation).</summary>
        public CustomerProfile Pick(double roll, bool allowCritic)
        {
            if (double.IsNaN(roll)) { roll = 0d; }
            roll = Math.Min(0.999999d, Math.Max(0d, roll));
            int total = 0;
            foreach (CustomerProfile p in _profiles) { if (allowCritic || p.Kind != CustomerKind.Critic) { total += Math.Max(0, p.Weight); } }
            if (total <= 0) { return _profiles[0]; }
            double target = roll * total;
            double sum = 0d;
            CustomerProfile last = _profiles[0];
            foreach (CustomerProfile p in _profiles)
            {
                if (!allowCritic && p.Kind == CustomerKind.Critic) { continue; }
                sum += Math.Max(0, p.Weight);
                last = p;
                if (target < sum) { return p; }
            }
            return last;
        }
    }
}
