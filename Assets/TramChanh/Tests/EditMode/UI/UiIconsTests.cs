using System.IO;
using NUnit.Framework;
using TramChanh.Core;
using TramChanh.UI.Menu;

namespace TramChanh.Tests.EditMode.UI
{
    public sealed class UiIconsTests
    {
        private static string IconFile(string name)
        {
            string root = Directory.GetCurrentDirectory();
            return Path.Combine(root, "Assets", "TramChanh", "Resources", "UI", "Icons", name + ".png");
        }

        [Test]
        public void EveryNameResolvesToAnIconFile()
        {
            foreach (Ingredient i in StockBook.Ingredients) { Assert.That(File.Exists(IconFile(UiIcons.NameFor(i))), Is.True, UiIcons.NameFor(i)); }
            foreach (UpgradeInfo info in UpgradeBook.All) { Assert.That(File.Exists(IconFile(UiIcons.NameFor(info.Id))), Is.True, UiIcons.NameFor(info.Id)); }
            foreach (NightEvent n in new[] { NightEvent.Clear, NightEvent.Rain, NightEvent.HotNight, NightEvent.PowerCut, NightEvent.Festival, NightEvent.Inspector })
            { Assert.That(File.Exists(IconFile(UiIcons.NameFor(n))), Is.True, UiIcons.NameFor(n)); }
            foreach (CustomerKind k in new[] { CustomerKind.Student, CustomerKind.Family, CustomerKind.Office, CustomerKind.Regular })
            { Assert.That(File.Exists(IconFile(UiIcons.NameFor(k))), Is.True, UiIcons.NameFor(k)); }
            foreach (string fixedName in new[] { UiIcons.Money, UiIcons.StarFull, UiIcons.StarEmpty, UiIcons.Patience, UiIcons.Goal })
            { Assert.That(File.Exists(IconFile(fixedName)), Is.True, fixedName); }
        }

        [Test]
        public void UnknownOrEmptyName_GivesNoTexture()
        {
            Assert.That(UiIcons.Load(null) == null, Is.True);
            Assert.That(UiIcons.Load("") == null, Is.True);
        }
    }
}
