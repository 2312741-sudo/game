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
            Assert.That(MenuBook.UnlockedDrinkIds("drink.slice", 0), Is.EqualTo(new[] { "drink.slice" }));
            Assert.That(MenuBook.UnlockedDrinkIds("drink.slice", 1), Is.EqualTo(new[] { "drink.slice" }));
            Assert.That(MenuBook.UnlockedDrinkIds("drink.slice", 2), Is.EqualTo(new[] { "drink.slice", MenuBook.PassionfruitId }));
            Assert.That(MenuBook.UnlockedDrinkIds("drink.slice", 3), Is.EqualTo(new[] { "drink.slice", MenuBook.PassionfruitId, MenuBook.OolongLemonId }));
            Assert.That(MenuBook.UnlockedDrinkIds("drink.slice", 5).Count, Is.EqualTo(3));
        }

        [Test]
        public void ExtraIds_AreUniqueAndDistinctFromTheBaseDrink()
        {
            string[] ids = MenuBook.ExtraIds().ToArray();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
            Assert.That(ids, Does.Not.Contain("drink.slice"));
            Assert.That(MenuBook.Extra.Select(e => e.DisplayName), Is.EqualTo(new[] { "Trà chanh dây", "Trà ô long chanh tươi" }));
        }

        [Test]
        public void UnlockedAt_FindsTheFlavourForExactlyThatStarCount()
        {
            Assert.That(MenuBook.UnlockedAt(2).Value.Id, Is.EqualTo(MenuBook.PassionfruitId));
            Assert.That(MenuBook.UnlockedAt(3).Value.Id, Is.EqualTo(MenuBook.OolongLemonId));
            Assert.That(MenuBook.UnlockedAt(4).HasValue, Is.False);
        }

        [Test]
        public void MissingBaseId_IsSkipped()
        {
            Assert.That(MenuBook.UnlockedDrinkIds(null, 3), Is.EqualTo(new[] { MenuBook.PassionfruitId, MenuBook.OolongLemonId }));
        }
    }
}
