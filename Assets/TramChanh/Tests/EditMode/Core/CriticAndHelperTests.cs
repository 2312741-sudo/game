using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class CriticAndHelperTests
    {
        private static readonly OrderId A = new OrderId(1);
        private static readonly OrderId B = new OrderId(2);

        [Test]
        public void Critic_PerfectAndQuick_GetsABigTipAndAGoodReview()
        {
            CriticVerdict v = CriticReview.Judge(QualityTier.Perfect, 0.8);
            Assert.That(v.MoneyMultiplier, Is.EqualTo(2d));
            Assert.That(v.ReputationPoints, Is.EqualTo(25));
            Assert.That(v.IsGood, Is.True);
        }

        [Test]
        public void Critic_PoorOrKeptWaiting_PaysHalfAndWritesABadReview()
        {
            Assert.That(CriticReview.Judge(QualityTier.Poor, 0.9).ReputationPoints, Is.EqualTo(-20));
            Assert.That(CriticReview.Judge(QualityTier.Perfect, 0.2).MoneyMultiplier, Is.EqualTo(0.5d));
            Assert.That(CriticReview.Judge(QualityTier.Good, 0.1).IsGood, Is.False);
        }

        [Test]
        public void Critic_InBetween_IsNormalWithASmallPlus()
        {
            CriticVerdict v = CriticReview.Judge(QualityTier.Good, 0.5);
            Assert.That(v.MoneyMultiplier, Is.EqualTo(1d));
            Assert.That(v.ReputationPoints, Is.EqualTo(5));
            Assert.That(CriticReview.Judge(QualityTier.Perfect, 0.5).ReputationPoints, Is.EqualTo(5), "Perfect but not quick enough for the big tip.");
        }

        [Test]
        public void Critic_Walkout_IsAlwaysBad()
        {
            Assert.That(CriticReview.Walkout().ReputationPoints, Is.EqualTo(-15));
            Assert.That(CriticReview.Walkout().MoneyMultiplier, Is.EqualTo(0d));
        }

        [Test]
        public void CriticKind_OnlyComesWhenAllowed()
        {
            CustomerKinds kinds = CustomerKinds.Default;
            int total = 0;
            for (int i = 0; i < kinds.Count; i++) { total += kinds.At(i).Weight; }
            Assert.That(kinds.Pick(0.999999, true).Kind, Is.EqualTo(CustomerKind.Critic), "The last slice of the roll is the critic.");
            for (double roll = 0d; roll < 1d; roll += 0.01)
            {
                Assert.That(kinds.Pick(roll, false).Kind, Is.Not.EqualTo(CustomerKind.Critic));
            }
            Assert.That(kinds.Profile(CustomerKind.Critic).PatienceSeconds, Is.EqualTo(150d)); // owner asked for longer waits (x1.5 on 2026-10-22)
        }

        [Test]
        public void Helper_TakesTheOldestOrderAfterTheDelay_AndRestsBetweenOrders()
        {
            var helper = new HelperWorker(8d, 4d);
            helper.Observe(A, 10d);
            helper.Observe(B, 12d);
            Assert.That(helper.TryNext(17.9, out _), Is.False, "Not waited long enough.");
            Assert.That(helper.TryNext(18d, out OrderId first), Is.True);
            Assert.That(first, Is.EqualTo(A));
            Assert.That(helper.TryNext(20.5, out _), Is.False, "Resting, and B has waited only 8.5 s... but the pause is not over.");
            Assert.That(helper.TryNext(22d, out OrderId second), Is.True);
            Assert.That(second, Is.EqualTo(B));
            Assert.That(helper.Waiting, Is.EqualTo(0));
        }

        [Test]
        public void Helper_ForgetsOrdersTheyNoLongerNeed()
        {
            var helper = new HelperWorker(1d, 0d);
            helper.Observe(A, 0d);
            helper.Forget(A);
            Assert.That(helper.TryNext(100d, out _), Is.False);
            helper.Observe(default, 0d);
            Assert.That(helper.Waiting, Is.EqualTo(0));
        }

        [Test]
        public void Helper_SeeingTheSameOrderTwice_KeepsTheFirstTime()
        {
            var helper = new HelperWorker(5d, 0d);
            helper.Observe(A, 0d);
            helper.Observe(A, 4d);
            Assert.That(helper.TryNext(5d, out OrderId id), Is.True);
            Assert.That(id, Is.EqualTo(A));
        }

        [Test]
        public void HelperUpgrade_ExistsWithAWageAndAStarRequirement()
        {
            UpgradeInfo info = UpgradeBook.Info(UpgradeId.HelperWorker);
            Assert.That(info.RequiredStars, Is.EqualTo(3));
            Assert.That(HelperWorker.WagePerNight, Is.EqualTo(30000));
            var book = new UpgradeBook();
            Assert.That(book.HasHelper, Is.False);
            book.Grant(UpgradeId.HelperWorker);
            Assert.That(book.HasHelper, Is.True);
        }
    }
}
