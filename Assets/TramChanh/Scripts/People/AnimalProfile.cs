using UnityEngine;

namespace TramChanh.People
{
    /// <summary>
    /// All numbers that make a species behave differently: senses, speeds, how easily it is scared, how often it begs.
    /// Getters are public; setters are internal (tests tweak them) so game code cannot change a profile by accident.
    /// Values are behaviour tuning only; none of them is a product, price or ground-truth measurement.
    /// </summary>
    public sealed class AnimalProfile
    {
        public AnimalSpecies Species { get; internal set; }

        // Senses
        public float ViewDistance { get; internal set; }
        public float ViewHalfAngleDegrees { get; internal set; }
        public float HearDistance { get; internal set; }
        public float SmellRange { get; internal set; }

        // Movement
        public float WanderRadius { get; internal set; }
        public float WalkSpeed { get; internal set; }
        public float RunMultiplier { get; internal set; }
        public float MinWalkMetres { get; internal set; }
        public float SeparationRadius { get; internal set; }

        // Idle / Rest
        public float IdleMin { get; internal set; }
        public float IdleMax { get; internal set; }
        public float RestMin { get; internal set; }
        public float RestMax { get; internal set; }
        public float RestChance { get; internal set; }

        // Flee
        public float FleeDistance { get; internal set; }
        public float SafeDistance { get; internal set; }
        public float FleeMinSeconds { get; internal set; }
        public float FleeMaxSeconds { get; internal set; }
        public float FleeTravel { get; internal set; }
        public float FleeLeash { get; internal set; }

        // Beg
        public bool CanBeg { get; internal set; }
        public float BegChance { get; internal set; }
        public float BegCooldown { get; internal set; }
        public float BegMaxSeconds { get; internal set; }
        public float BegLoseSeconds { get; internal set; }
        public float BegStopDistance { get; internal set; }

        // Investigate
        public bool Curious { get; internal set; }
        public float InvestigateChance { get; internal set; }
        public float InvestigateCooldown { get; internal set; }
        public float InvestigateMaxSeconds { get; internal set; }
        public float InvestigateLookSeconds { get; internal set; }
        public float InvestigateStopDistance { get; internal set; }

        /// <summary>Chances (begging, investigating) are rolled at most once per this many seconds, so they do not depend on the frame rate.</summary>
        public float DecisionInterval { get; internal set; }

        /// <summary>Profile for a species. <paramref name="wanderRadius"/> and <paramref name="walkSpeed"/> come from the scene setup.</summary>
        public static AnimalProfile For(AnimalSpecies species, float wanderRadius, float walkSpeed)
        {
            var p = new AnimalProfile
            {
                Species = species,
                WanderRadius = Mathf.Max(0f, wanderRadius),
                WalkSpeed = Mathf.Max(0.05f, walkSpeed),
                RunMultiplier = 4f,
                MinWalkMetres = 0.25f,
                SeparationRadius = 0.8f,
                IdleMin = 1.5f,
                IdleMax = 5.5f,
                RestMin = 6f,
                RestMax = 14f,
                RestChance = 0.3f,
                ViewDistance = 8f,
                ViewHalfAngleDegrees = 65f,
                HearDistance = 6f,
                SmellRange = 7f,
                FleeDistance = 2.2f,
                SafeDistance = 4f,
                FleeMinSeconds = 1.5f,
                FleeMaxSeconds = 6f,
                FleeTravel = 3f,
                FleeLeash = 4.5f,
                CanBeg = true,
                BegChance = 0.4f,
                BegCooldown = 40f,
                BegMaxSeconds = 25f,
                BegLoseSeconds = 3f,
                BegStopDistance = 1.1f,
                Curious = true,
                InvestigateChance = 0.3f,
                InvestigateCooldown = 25f,
                InvestigateMaxSeconds = 12f,
                InvestigateLookSeconds = 2.5f,
                InvestigateStopDistance = 2.4f,
                DecisionInterval = 2f,
            };
            switch (species)
            {
                case AnimalSpecies.Dog:
                    p.ViewDistance = 9f; p.ViewHalfAngleDegrees = 70f; p.HearDistance = 8f; p.SmellRange = 9f;
                    p.FleeDistance = 1.5f; p.SafeDistance = 3f; p.BegChance = 0.6f; p.InvestigateChance = 0.5f;
                    break;
                case AnimalSpecies.Cat:
                    p.ViewDistance = 8f; p.ViewHalfAngleDegrees = 60f; p.HearDistance = 8f; p.SmellRange = 7f;
                    p.FleeDistance = 2.8f; p.SafeDistance = 4.5f; p.RunMultiplier = 5f; p.RestChance = 0.5f;
                    p.BegChance = 0.35f; p.InvestigateChance = 0.25f;
                    break;
                case AnimalSpecies.Chicken:
                    p.ViewDistance = 6f; p.ViewHalfAngleDegrees = 100f; p.HearDistance = 5f; p.SmellRange = 5f;
                    p.FleeDistance = 2.2f; p.SafeDistance = 3.5f; p.BegChance = 0.3f; p.InvestigateChance = 0.15f;
                    break;
                default:
                    p.ViewDistance = 7f; p.ViewHalfAngleDegrees = 110f; p.HearDistance = 6f; p.SmellRange = 5f;
                    p.FleeDistance = 2.6f; p.SafeDistance = 4f; p.RunMultiplier = 5f; p.BegChance = 0.3f;
                    p.Curious = false; p.InvestigateChance = 0f;
                    break;
            }

            return p;
        }
    }
}
