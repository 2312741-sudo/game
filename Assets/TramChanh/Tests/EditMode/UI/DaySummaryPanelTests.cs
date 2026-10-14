using NUnit.Framework;
using TramChanh.Core;
using TramChanh.UI.Menu;

namespace TramChanh.Tests.EditMode.UI
{
    public sealed class DaySummaryPanelTests
    {
        [Test]
        public void Describe_ShowsServedFailedAndPercent()
        {
            string text = DaySummaryPanel.Describe(new DaySummary(2, 3, 1), null);
            Assert.That(text, Does.Contain("Đã phục vụ: 3"));
            Assert.That(text, Does.Contain("Khách bỏ đi: 1"));
            Assert.That(text, Does.Contain("75%"));
            Assert.That(text, Does.Not.Contain("Tổng cộng"));
        }

        [Test]
        public void Describe_WithProgress_AddsLifetimeTotalsAndBestDay()
        {
            var progress = new GameProgress();
            progress.RecordServed(); progress.RecordServed(); progress.RecordFailed();
            DaySummary summary = progress.EndDay();
            string text = DaySummaryPanel.Describe(summary, progress);
            Assert.That(text, Does.Contain("Tổng cộng: 2 đơn"));
            Assert.That(text, Does.Contain("Ngày tốt nhất: 2 đơn"));
        }

        [Test]
        public void Describe_WithNoOrders_ShowsZeroPercentWithoutDividingByZero()
        {
            Assert.That(DaySummaryPanel.Describe(new DaySummary(1, 0, 0), null), Does.Contain("0%"));
        }
    }
}
