using NUnit.Framework;
using TramChanh.Core;
using TramChanh.UI.Menu;

namespace TramChanh.Tests.EditMode.UI
{
    public sealed class SupplierPanelTests
    {
        [Test]
        public void DescribeRow_ShowsNameLevelCapacityAndPackPrice()
        {
            var stock = new StockBook();
            stock.Add(Ingredient.Ice, 12);
            string text = SupplierPanel.DescribeRow(Ingredient.Ice, stock, new Supplier());
            Assert.That(text, Does.Contain("Đá"));
            Assert.That(text, Does.Contain("12/40"));
            Assert.That(text, Does.Contain("gói 10 = 5.000đ"));
        }

        [Test]
        public void DescribeCash_UsesTheMoneyFormat()
        {
            var wallet = new GameProgress();
            wallet.RecordSale(new Sale(40500, QualityTier.Good, 80));
            Assert.That(SupplierPanel.DescribeCash(wallet), Is.EqualTo("Tiền đang có: 40.500đ"));
        }
    }
}
