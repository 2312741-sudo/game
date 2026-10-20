using System.Linq;
using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class MenuBookTests
    {
        [Test]
        public void BaseDrinkAlwaysAvailable_FlavoursUnlockByStars()
        {
            Assert.That(MenuBook.UnlockedDrinkIds("drink.slice", 1), Is.EqualTo(new[] { "drink.slice" }));
            Assert.That(MenuBook.UnlockedDrinkIds("drink.slice", 2), Is.EqualTo(new[] { "drink.slice", MenuBook.PassionfruitId }));
            Assert.That(MenuBook.UnlockedDrinkIds("drink.slice", 3), Is.EqualTo(new[] { "drink.slice", MenuBook.PassionfruitId, MenuBook.MangoId }));
            Assert.That(MenuBook.UnlockedDrinkIds("drink.slice", 4).Count, Is.EqualTo(4));
            Assert.That(MenuBook.UnlockedDrinkIds("drink.slice", 5).Count, Is.EqualTo(6));
        }

        [Test]
        public void ExtraIds_AreUniqueAndDistinctFromTheBaseDrink()
        {
            string[] ids = MenuBook.ExtraIds().ToArray();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
            Assert.That(ids, Does.Not.Contain("drink.slice"));
            Assert.That(MenuBook.Extra.Select(e => e.DisplayName), Is.EqualTo(new[]
            {
                "Trà Olong - Chanh dây", "Trà Olong - Xoài", "Trà Olong - Đào", "Trà Olong - Quả mọng", "Trà Olong - Mãng cầu",
            }));
        }

        [Test]
        public void EveryFlavourCosts30k_LikeTheRealMenu()
        {
            foreach (MenuEntry entry in MenuBook.Extra) { Assert.That(entry.Price, Is.EqualTo(30000), entry.DisplayName); }
            Assert.That(MenuBook.BaseDrinkPrice, Is.EqualTo(20000));
            Assert.That(MenuBook.BaseCakePrice, Is.EqualTo(20000));
        }

        [Test]
        public void UnlockedAt_ListsTheFlavoursOfExactlyThatStarCount()
        {
            Assert.That(MenuBook.UnlockedAt(2).Select(e => e.Id), Is.EqualTo(new[] { MenuBook.PassionfruitId }));
            Assert.That(MenuBook.UnlockedAt(5).Count, Is.EqualTo(2));
            Assert.That(MenuBook.UnlockedAt(1).Count, Is.EqualTo(0));
        }

        [Test]
        public void MissingBaseId_IsSkipped()
        {
            Assert.That(MenuBook.UnlockedDrinkIds(null, 2), Is.EqualTo(new[] { MenuBook.PassionfruitId }));
        }
    }
}
