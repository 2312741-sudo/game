using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class CreditsTextTests
    {
        [Test]
        public void Build_NamesEveryAuthorAndLicense()
        {
            string text = CreditsText.Build();
            foreach (CreditsText.Entry e in CreditsText.Entries)
            {
                Assert.That(text, Does.Contain(e.Title));
                Assert.That(text, Does.Contain(e.Author));
                Assert.That(text, Does.Contain(e.License));
                Assert.That(text, Does.Contain(e.Source));
            }
        }

        [Test]
        public void Entries_AreAllCcBy()
        {
            foreach (CreditsText.Entry e in CreditsText.Entries)
            {
                Assert.That(e.License, Does.StartWith("CC-BY"));
            }
        }

        [Test]
        public void Entries_CoverEveryCcByRowOfTheLicenseRegistry()
        {
            string registry = System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Docs/Reference/ASSET_LICENSES.md"));
            int rows = 0;
            foreach (string line in registry.Split('\n'))
            {
                if (line.StartsWith("| **") && line.Contains("| CC-BY ")) { rows++; }
            }
            Assert.That(CreditsText.Entries.Length, Is.EqualTo(rows));
        }
    }
}
