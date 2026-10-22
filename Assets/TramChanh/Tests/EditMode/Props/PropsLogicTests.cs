using NUnit.Framework;
using TramChanh.Props;

namespace TramChanh.Tests.EditMode.Props
{
    /// <summary>Pure logic of the physics props. NUnit 3.5 only (no Is.AnyOf).</summary>
    [TestFixture]
    public class PropsLogicTests
    {
        // ------------------------------------------------------------ DebrisBudget

        [Test]
        public void PHYS_001_Register_UnderCapacity_EvictsNothing()
        {
            var budget = new DebrisBudget(3);
            Assert.AreEqual(-1, budget.Register(1, 0d, 6d));
            Assert.AreEqual(-1, budget.Register(2, 0.1d, 6d));
            Assert.AreEqual(2, budget.Count);
        }

        [Test]
        public void PHYS_002_Register_AtCapacity_EvictsOldest()
        {
            var budget = new DebrisBudget(2);
            budget.Register(10, 0d, 6d);
            budget.Register(11, 1d, 6d);
            int evicted = budget.Register(12, 2d, 6d);
            Assert.AreEqual(10, evicted);
            Assert.AreEqual(2, budget.Count);
            Assert.IsFalse(budget.Contains(10));
            Assert.IsTrue(budget.Contains(12));
        }

        [Test]
        public void PHYS_003_TryTakeExpired_ReturnsOnlyExpired_AndRemovesThem()
        {
            var budget = new DebrisBudget(8);
            budget.Register(1, 0d, 5d);
            budget.Register(2, 1d, 10d);
            Assert.IsFalse(budget.TryTakeExpired(4.9d, out _));
            Assert.IsTrue(budget.TryTakeExpired(5d, out int id));
            Assert.AreEqual(1, id);
            Assert.IsFalse(budget.TryTakeExpired(5d, out _));
            Assert.AreEqual(1, budget.Count);
        }

        [Test]
        public void PHYS_004_Remaining_CountsDown_AndClampsAtZero()
        {
            var budget = new DebrisBudget(4);
            budget.Register(7, 10d, 6d);
            Assert.AreEqual(6d, budget.Remaining(7, 10d), 1e-9);
            Assert.AreEqual(1d, budget.Remaining(7, 15d), 1e-9);
            Assert.AreEqual(0d, budget.Remaining(7, 99d), 1e-9);
            Assert.AreEqual(0d, budget.Remaining(404, 10d), 1e-9);
        }

        [Test]
        public void PHYS_005_Release_ForgetsProp_AndCapacityIsAtLeastOne()
        {
            var budget = new DebrisBudget(0);
            Assert.AreEqual(1, budget.Capacity);
            budget.Register(1, 0d, 1d);
            Assert.IsTrue(budget.Release(1));
            Assert.IsFalse(budget.Release(1));
            Assert.AreEqual(0, budget.Count);
        }

        // ------------------------------------------------------------ ImpactCuePolicy

        [Test]
        public void PHYS_010_Impact_BelowMinSpeed_IsSilent()
        {
            var policy = new ImpactCuePolicy(0.6f, 4f, 0.1f);
            Assert.IsFalse(policy.ShouldPlay(1d, 0.3f, out float intensity));
            Assert.AreEqual(0f, intensity);
        }

        [Test]
        public void PHYS_011_Impact_IntensityScalesAndClamps()
        {
            var policy = new ImpactCuePolicy(1f, 3f, 0f);
            Assert.IsTrue(policy.ShouldPlay(1d, 2f, out float mid));
            Assert.AreEqual(0.5f, mid, 1e-5f);
            Assert.IsTrue(policy.ShouldPlay(2d, 50f, out float loud));
            Assert.AreEqual(1f, loud, 1e-5f);
        }

        [Test]
        public void PHYS_012_Impact_CooldownBlocksRapidRepeats()
        {
            var policy = new ImpactCuePolicy(0.5f, 4f, 0.5f);
            Assert.IsTrue(policy.ShouldPlay(1d, 2f, out _));
            Assert.IsFalse(policy.ShouldPlay(1.2d, 2f, out _));
            Assert.IsTrue(policy.ShouldPlay(1.5d, 2f, out _));
        }

        // ------------------------------------------------------------ PuddleSplashPolicy

        [Test]
        public void PHYS_020_Puddle_StandingStill_DoesNotSplash()
        {
            var policy = new PuddleSplashPolicy(0.5f, 0.35f);
            Assert.IsFalse(policy.ShouldSplash(1, 0d, 0.1f, out _));
        }

        [Test]
        public void PHYS_021_Puddle_MovingSplashes_ThenCooldownPerActor()
        {
            var policy = new PuddleSplashPolicy(0.5f, 0.35f);
            Assert.IsTrue(policy.ShouldSplash(1, 0d, 2f, out float intensity));
            Assert.Greater(intensity, 0f);
            Assert.IsFalse(policy.ShouldSplash(1, 0.2d, 2f, out _));
            Assert.IsTrue(policy.ShouldSplash(2, 0.2d, 2f, out _), "another actor has its own cooldown");
            Assert.IsTrue(policy.ShouldSplash(1, 0.36d, 2f, out _));
        }

        [Test]
        public void PHYS_022_Puddle_Exit_ForgetsActor_SoReentrySplashesAtOnce()
        {
            var policy = new PuddleSplashPolicy(0.5f, 10f);
            Assert.IsTrue(policy.ShouldSplash(1, 0d, 2f, out _));
            Assert.IsFalse(policy.ShouldSplash(1, 1d, 2f, out _));
            policy.Exit(1);
            Assert.AreEqual(0, policy.TrackedActors);
            Assert.IsTrue(policy.ShouldSplash(1, 1.1d, 2f, out _));
        }

        [Test]
        public void PHYS_023_Puddle_IntensityClampedToOne()
        {
            var policy = new PuddleSplashPolicy(0.5f, 0.1f, 6f);
            Assert.IsTrue(policy.ShouldSplash(1, 0d, 100f, out float intensity));
            Assert.AreEqual(1f, intensity, 1e-5f);
        }

        // ------------------------------------------------------------ SwayModel

        [Test]
        public void PHYS_030_Gust_IsBoundedByStrength()
        {
            for (double t = 0d; t < 60d; t += 0.05d)
            {
                float g = SwayModel.Gust(t, 0.4f, 1.3f);
                Assert.LessOrEqual(System.Math.Abs(g), 0.4f + 1e-5f);
            }
        }

        [Test]
        public void PHYS_031_Gust_ZeroStrength_IsZero()
        {
            Assert.AreEqual(0f, SwayModel.Gust(3.3d, 0f));
        }

        [Test]
        public void PHYS_032_NudgeImpulse_ProportionalThenCapped()
        {
            Assert.AreEqual(0.24f, SwayModel.NudgeImpulse(2f, 0.12f, 1f), 1e-5f);
            Assert.AreEqual(0.25f, SwayModel.NudgeImpulse(20f, 0.12f, 0.25f), 1e-5f);
            Assert.AreEqual(0f, SwayModel.NudgeImpulse(-1f, 0.12f, 0.25f));
        }
    }
}
