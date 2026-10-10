using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Customers;
using TramChanh.Orders;

namespace TramChanh.Tests.EditMode.Customers
{
    /// <summary>Scriptable seat: records requests, can refuse, and its live order is toggled by the test.</summary>
    public sealed class FakeCustomerSeat : ICustomerSeat
    {
        private int _nextOrder;
        public FakeCustomerSeat(int tableNumber) { TableNumber = tableNumber; _nextOrder = tableNumber * 1000; }
        public int TableNumber { get; }
        public bool HasLiveOrder { get; set; }
        public string FailWith { get; set; }
        public List<(CustomerId Customer, IReadOnlyList<ItemRequest> Items)> Requests { get; } = new List<(CustomerId, IReadOnlyList<ItemRequest>)>();

        public Result<OrderId> Request(CustomerId customer, IReadOnlyList<ItemRequest> items)
        {
            Requests.Add((customer, items));
            if (FailWith != null) { return Result<OrderId>.Fail(FailWith); }
            HasLiveOrder = true;
            return Result<OrderId>.Success(new OrderId(++_nextOrder));
        }

        public static FakeCustomerSeat[] Tables(int count = 10)
        {
            var seats = new FakeCustomerSeat[count];
            for (int i = 0; i < count; i++) { seats[i] = new FakeCustomerSeat(i + 1); }
            return seats;
        }
    }
}
