using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class GameClockTextTests
    {
        [TestCase(18d, true, "18:00  Mở cửa")]
        [TestCase(18.7001d, true, "18:42  Mở cửa")]
        [TestCase(23.999d, true, "23:59  Mở cửa")]
        [TestCase(0d, false, "00:00  Đóng cửa (tua nhanh)")]
        [TestCase(3.1666d, false, "03:09  Đóng cửa (tua nhanh)")]
        public void Format_ShowsHourMinuteAndState(double hour, bool open, string expected)
        {
            Assert.That(GameClockText.Format(hour, open), Is.EqualTo(expected));
        }

        [TestCase(24d, "00:00")]
        [TestCase(25.5d, "01:30")]
        [TestCase(-0.5d, "23:30")]
        [TestCase(double.NaN, "00:00")]
        public void Format_KeepsTheHourInsideTheDay(double hour, string expectedPrefix)
        {
            Assert.That(GameClockText.Format(hour, true), Does.StartWith(expectedPrefix));
        }

        [Test]
        public void MinuteOfDay_ChangesOnlyOnWholeMinutes()
        {
            Assert.That(GameClockText.MinuteOfDay(18d), Is.EqualTo(1080));
            Assert.That(GameClockText.MinuteOfDay(18.0166d), Is.EqualTo(1080));
            Assert.That(GameClockText.MinuteOfDay(18.0167d), Is.EqualTo(1081));
        }
    }
}
