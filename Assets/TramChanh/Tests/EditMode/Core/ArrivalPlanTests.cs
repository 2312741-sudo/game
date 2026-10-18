using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class ArrivalPlanTests
    {
        [Test]
        public void Evening_IsSlowThenBusyThenQuiet()
        {
            Assert.That(ArrivalPlan.HourFactor(18.0), Is.EqualTo(1.6).Within(1e-9));
            Assert.That(ArrivalPlan.HourFactor(19.0), Is.EqualTo(1.6).Within(1e-9));
            Assert.That(ArrivalPlan.HourFactor(20.0), Is.EqualTo(0.6).Within(1e-9));
            Assert.That(ArrivalPlan.HourFactor(21.5), Is.EqualTo(0.6).Within(1e-9));
            Assert.That(ArrivalPlan.HourFactor(23.0), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(ArrivalPlan.HourFactor(24.0), Is.EqualTo(1.4).Within(1e-9));
            Assert.That(ArrivalPlan.HourFactor(19.25), Is.EqualTo(1.1).Within(1e-9), "Halfway down the ramp.");
        }

        [Test]
        public void Hours_OutsideTheEveningAndNaN_AreSafe()
        {
            Assert.That(ArrivalPlan.HourFactor(3.0), Is.EqualTo(1.6).Within(1e-9));
            Assert.That(ArrivalPlan.HourFactor(30.0), Is.EqualTo(1.4).Within(1e-9));
            Assert.That(ArrivalPlan.HourFactor(double.NaN), Is.EqualTo(1.0).Within(1e-9));
        }

        [Test]
        public void LaterDays_AreBusierButNotBeyondTheFloor()
        {
            Assert.That(ArrivalPlan.DayFactor(1), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(ArrivalPlan.DayFactor(0), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(ArrivalPlan.DayFactor(6), Is.LessThan(ArrivalPlan.DayFactor(2)));
            Assert.That(ArrivalPlan.DayFactor(1000), Is.EqualTo(0.6).Within(1e-9));
        }

        [Test]
        public void Assist_KicksInAfterThreeFailuresInARow()
        {
            Assert.That(ArrivalPlan.Assist(2), Is.EqualTo(1.0));
            Assert.That(ArrivalPlan.Assist(3), Is.EqualTo(1.5));
        }

        [Test]
        public void IntervalScale_StaysInsideTheLimits()
        {
            for (double hour = 18d; hour <= 24d; hour += 0.25)
            {
                foreach (int day in new[] { 1, 5, 100 })
                {
                    double scale = ArrivalPlan.IntervalScale(hour, day, 5);
                    Assert.That(scale, Is.InRange(ArrivalPlan.MinScale, ArrivalPlan.MaxScale));
                }
            }
            Assert.That(ArrivalPlan.IntervalScale(20.0, 1, 0), Is.EqualTo(0.6).Within(1e-9));
        }
    }
}
