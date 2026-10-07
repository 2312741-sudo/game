using NUnit.Framework;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Orders
{
    /// <summary>Every preparation module runs this fixture with a finished, not-yet-Ready item.</summary>
    public static class PreparedItemContractAssertions
    {
        public static void AssertReadyTransition(IPreparedItem item)
        {
            Assert.That(item.IsFinished, Is.True);
            Assert.That(item.MarkReady().IsSuccess, Is.True);
            Assert.That(item.IsFinished, Is.True, "Final-step completion remains true after Ready.");
            Assert.That(item.MarkReady().ReasonKey, Is.EqualTo("ready.already_ready"));
            Assert.That(item.IsFinished, Is.True);
        }
    }
}
