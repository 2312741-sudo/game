using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class PatienceBookTests
    {
        private static readonly OrderId A = new OrderId(1);
        private static readonly OrderId B = new OrderId(2);

        [Test]
        public void LeftFraction_FallsFromOneToZero()
        {
            var book = new PatienceBook();
            book.Add(A, CustomerKind.Student, 10d, 100d);
            Assert.That(book.LeftFraction(A, 10d), Is.EqualTo(1d).Within(1e-9));
            Assert.That(book.LeftFraction(A, 60d), Is.EqualTo(0.5d).Within(1e-9));
            Assert.That(book.LeftFraction(A, 500d), Is.EqualTo(0d));
            Assert.That(book.LeftFraction(B, 10d), Is.EqualTo(0d), "Unknown order.");
        }

        [Test]
        public void CollectExpired_ReturnsAndRemovesOnlyTheOnesWhoseTimeIsUp()
        {
            var book = new PatienceBook();
            book.Add(A, CustomerKind.Office, 0d, 50d);
            book.Add(B, CustomerKind.Family, 0d, 200d);
            var gone = new List<OrderId>();
            book.CollectExpired(49.9d, gone);
            Assert.That(gone, Is.Empty);
            book.CollectExpired(50d, gone);
            Assert.That(gone, Is.EqualTo(new[] { A }));
            Assert.That(book.Has(A), Is.False);
            Assert.That(book.Has(B), Is.True);
            gone.Clear();
            book.CollectExpired(60d, gone);
            Assert.That(gone, Is.Empty, "An order is reported only once.");
        }

        [Test]
        public void PausedClock_NeverMakesAnyoneLeave()
        {
            var book = new PatienceBook();
            book.Add(A, CustomerKind.Student, 5d, 30d);
            var gone = new List<OrderId>();
            for (int i = 0; i < 100; i++) { book.CollectExpired(5d, gone); }
            Assert.That(gone, Is.Empty);
        }

        [Test]
        public void Freeze_StopsTheCountdownAndRemembersWhatWasLeft()
        {
            var book = new PatienceBook();
            book.Add(A, CustomerKind.Regular, 0d, 100d);
            book.Freeze(A, 25d);
            Assert.That(book.LeftFraction(A, 1000d), Is.EqualTo(0.75d).Within(1e-9));
            var gone = new List<OrderId>();
            book.CollectExpired(1000d, gone);
            Assert.That(gone, Is.Empty);
            book.Freeze(A, 90d);
            Assert.That(book.LeftFraction(A, 1000d), Is.EqualTo(0.75d).Within(1e-9), "Second freeze changes nothing.");
        }

        [Test]
        public void Add_IgnoresInvalidIdAndFixesBadPatience()
        {
            var book = new PatienceBook();
            book.Add(default, CustomerKind.Student, 0d, 10d);
            Assert.That(book.Count, Is.EqualTo(0));
            book.Add(A, CustomerKind.Student, 0d, double.NaN);
            var gone = new List<OrderId>();
            book.CollectExpired(1d, gone);
            Assert.That(gone, Is.EqualTo(new[] { A }));
        }

        [Test]
        public void TryGetKind_AndRemove()
        {
            var book = new PatienceBook();
            book.Add(A, CustomerKind.Office, 0d, 10d);
            Assert.That(book.TryGetKind(A, out CustomerKind kind), Is.True);
            Assert.That(kind, Is.EqualTo(CustomerKind.Office));
            Assert.That(book.Remove(A), Is.True);
            Assert.That(book.TryGetKind(A, out _), Is.False);
        }
    }
}
