using System;
using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class CoreValueTests
    {
        [Test]
        public void CX_002_Ids_AreDeterministicTypedAndRejectInvalidValues()
        {
            IIdGenerator ids = new SequentialIdGenerator(10);
            var first = new OrderId(ids.Next());
            var second = new OrderId(ids.Next());
            Assert.That(first.Value, Is.EqualTo(11));
            Assert.That(second.Value, Is.EqualTo(12));
            Assert.That(first, Is.EqualTo(new OrderId(11)));
            Assert.That(first.GetHashCode(), Is.EqualTo(new OrderId(11).GetHashCode()));
            Assert.That(new HashSet<OrderId> { first, new OrderId(11) }.Count, Is.EqualTo(1));
            Assert.That(first == new OrderId(11), Is.True);
            Assert.That(first != second, Is.True);
            Assert.That(default(OrderId).IsValid, Is.False);
            Assert.That(new CustomerId(11).Equals((object)first), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => new OrderId(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SequentialIdGenerator(-1));
            Assert.Throws<OverflowException>(() => new SequentialIdGenerator(int.MaxValue).Next());
        }

        [TestCase(typeof(OrderId))]
        [TestCase(typeof(OrderItemId))]
        [TestCase(typeof(CustomerId))]
        [TestCase(typeof(TableId))]
        [TestCase(typeof(VehicleId))]
        [TestCase(typeof(InteractableId))]
        [TestCase(typeof(GrillId))]
        [TestCase(typeof(PreparationId))]
        public void CX_002_Ids_ContractIsConsistentAcrossTypes(Type idType)
        {
            object first = Activator.CreateInstance(idType, 1);
            object equal = Activator.CreateInstance(idType, 1);
            object other = Activator.CreateInstance(idType, 2);
            Assert.That(first, Is.EqualTo(equal));
            Assert.That(first, Is.Not.EqualTo(other));
            Assert.That(first.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
            Assert.That(idType.GetProperty("IsValid").GetValue(first), Is.EqualTo(true));
        }

        [Test]
        public void CX_002_Result_PreservesFailureReasonAndProtectsMissingValue()
        {
            Assert.That(Result.Success().IsSuccess, Is.True);
            Assert.That(Result.Success().ReasonKey, Is.Null);
            Assert.That(Result.Fail("test.blocked").ReasonKey, Is.EqualTo("test.blocked"));
            Assert.That(default(Result).IsSuccess, Is.False);
            Assert.That(default(Result).ReasonKey, Is.Not.Null.And.Not.Empty);
            Result<int> success = Result<int>.Success(42);
            Assert.That(success.Value, Is.EqualTo(42));
            Assert.That(success.IsSuccess, Is.True);
            Result<int> failure = Result<int>.Fail("test.blocked");
            Assert.That(failure.IsSuccess, Is.False);
            Assert.That(failure.ReasonKey, Is.EqualTo("test.blocked"));
            Assert.Throws<InvalidOperationException>(() => { _ = failure.Value; });
            Assert.Throws<InvalidOperationException>(() => { _ = default(Result<int>).Value; });
            Assert.Throws<ArgumentException>(() => Result.Fail(" "));
            Assert.Throws<ArgumentException>(() => Result<int>.Fail(null));
        }

        [Test]
        public void CX_002_Availability_SeparatesAvailableBlockedAndHidden()
        {
            Assert.That(Availability.Available.IsAvailable, Is.True);
            Assert.That(Availability.Available.Status, Is.EqualTo(AvailabilityStatus.Available));
            Assert.That(Availability.Hidden.Status, Is.EqualTo(AvailabilityStatus.Hidden));
            Assert.That(default(Availability).Status, Is.EqualTo(AvailabilityStatus.Hidden));
            Availability blocked = Availability.Blocked("test.blocked");
            Assert.That(blocked.IsAvailable, Is.False);
            Assert.That(blocked.Status, Is.EqualTo(AvailabilityStatus.Blocked));
            Assert.That(blocked.ReasonKey, Is.EqualTo("test.blocked"));
            Assert.Throws<ArgumentException>(() => Availability.Blocked(""));
        }
    }
}
