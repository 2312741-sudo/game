using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class ServiceComboTests
    {
        private static Sale Good(int amount = 20000) => new Sale(amount, QualityTier.Good, 80);

        [Test]
        public void FirstGoodSale_HasNoBonus_ThenFivePercentPerStep()
        {
            var combo = new ServiceCombo();
            Assert.That(combo.OnSale(Good(), 0.9), Is.EqualTo(0));
            Assert.That(combo.OnSale(Good(), 0.9), Is.EqualTo(1000));
            Assert.That(combo.OnSale(Good(), 0.9), Is.EqualTo(2000));
            Assert.That(combo.Streak, Is.EqualTo(3));
        }

        [Test]
        public void Bonus_IsCappedAtTwentyFivePercent()
        {
            var combo = new ServiceCombo();
            int last = 0;
            for (int i = 0; i < 12; i++) { last = combo.OnSale(Good(), 1d); }
            Assert.That(last, Is.EqualTo(5000));
            Assert.That(combo.BestStreak, Is.EqualTo(12));
        }

        [Test]
        public void PoorSlowOrFailedCustomer_EndsTheStreak()
        {
            var combo = new ServiceCombo();
            combo.OnSale(Good(), 1d);
            combo.OnSale(Good(), 1d);
            Assert.That(combo.OnSale(new Sale(10000, QualityTier.Poor, 20), 1d), Is.EqualTo(0));
            Assert.That(combo.Streak, Is.EqualTo(0));
            combo.OnSale(Good(), 1d);
            Assert.That(combo.OnSale(Good(), 0.2), Is.EqualTo(0), "slow sale");
            Assert.That(combo.Streak, Is.EqualTo(0));
            combo.OnSale(Good(), 1d);
            combo.OnFailed();
            Assert.That(combo.Streak, Is.EqualTo(0));
            Assert.That(combo.BestStreak, Is.EqualTo(2));
        }

        [Test]
        public void NewNight_ClearsStreakAndBest()
        {
            var combo = new ServiceCombo();
            combo.OnSale(Good(), 1d);
            combo.OnSale(Good(), 1d);
            combo.NewNight();
            Assert.That(combo.Streak, Is.EqualTo(0));
            Assert.That(combo.BestStreak, Is.EqualTo(0));
        }
    }
}
