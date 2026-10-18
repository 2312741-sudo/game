using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class CustomerKindTests
    {
        [Test]
        public void Pick_FollowsTheWeights()
        {
            var kinds = new CustomerKinds(new[]
            {
                new CustomerProfile(CustomerKind.Student, "a", 10d, 1f, 1),
                new CustomerProfile(CustomerKind.Office, "b", 10d, 1f, 3),
            });
            Assert.That(kinds.Pick(0.0).Kind, Is.EqualTo(CustomerKind.Student));
            Assert.That(kinds.Pick(0.24).Kind, Is.EqualTo(CustomerKind.Student));
            Assert.That(kinds.Pick(0.26).Kind, Is.EqualTo(CustomerKind.Office));
            Assert.That(kinds.Pick(0.999).Kind, Is.EqualTo(CustomerKind.Office));
        }

        [Test]
        public void Pick_ClampsOutOfRangeAndNaNRolls()
        {
            CustomerKinds kinds = CustomerKinds.Default;
            Assert.That(kinds.Pick(-5d).Kind, Is.EqualTo(kinds.At(0).Kind));
            Assert.That(kinds.Pick(7d).Kind, Is.EqualTo(kinds.At(kinds.Count - 1).Kind));
            Assert.That(kinds.Pick(double.NaN).Kind, Is.EqualTo(kinds.At(0).Kind));
        }

        [Test]
        public void Default_HasAllFourKindsWithPositivePatience()
        {
            CustomerKinds kinds = CustomerKinds.Default;
            foreach (CustomerKind kind in new[] { CustomerKind.Student, CustomerKind.Family, CustomerKind.Office, CustomerKind.Regular })
            {
                CustomerProfile p = kinds.Profile(kind);
                Assert.That(p.Kind, Is.EqualTo(kind));
                Assert.That(p.PatienceSeconds, Is.GreaterThan(0d));
                Assert.That(p.PayMultiplier, Is.GreaterThan(0f));
            }
        }

        [Test]
        public void Table_RefusesEmptyOrZeroWeight()
        {
            Assert.Throws<System.ArgumentException>(() => new CustomerKinds(new CustomerProfile[0]));
            Assert.Throws<System.ArgumentException>(() => new CustomerKinds(new[] { new CustomerProfile(CustomerKind.Student, "a", 1d, 1f, 0) }));
        }
    }
}
