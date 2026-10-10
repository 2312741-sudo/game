using System;
using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Customers
{
    /// <summary>
    /// Plain-C# dine-in arrival and table occupancy. Seats a customer at a random free table on a game-clock
    /// interval, places the request through the seat (Lobby order point), and frees the table
    /// <see cref="CustomerDirectorSettings.TableClearSeconds"/> after its order stops being live.
    /// Publishes nothing on the game EventBus; visuals listen to the C# events.
    /// </summary>
    public sealed class CustomerDirector
    {
        private readonly ICustomerSeat[] _seats;
        private readonly CustomerId[] _customers;
        private readonly bool[] _clearing;
        private readonly double[] _clearRemaining;
        private readonly CustomerDirectorSettings _settings;
        private readonly string _drinkItemId;
        private readonly string _cakeItemId;
        private readonly Random _random;
        private readonly List<int> _free = new List<int>();
        private int _nextCustomerId;
        private double _sinceArrival;

        public CustomerDirector(IReadOnlyList<ICustomerSeat> seats, CustomerDirectorSettings settings,
            string drinkItemId, string cakeItemId, int seed, int firstCustomerId = 100)
        {
            if (seats == null) { throw new ArgumentNullException(nameof(seats)); }
            if (string.IsNullOrWhiteSpace(drinkItemId)) { throw new ArgumentException("Drink item id is required.", nameof(drinkItemId)); }
            if (string.IsNullOrWhiteSpace(cakeItemId)) { throw new ArgumentException("Cake item id is required.", nameof(cakeItemId)); }
            if (firstCustomerId <= 0) { throw new ArgumentOutOfRangeException(nameof(firstCustomerId), "Customer ids must be positive."); }
            _seats = new ICustomerSeat[seats.Count];
            var tables = new HashSet<int>();
            for (int i = 0; i < seats.Count; i++)
            {
                ICustomerSeat seat = seats[i] ?? throw new ArgumentException("Seat " + i + " is null.", nameof(seats));
                if (!tables.Add(seat.TableNumber)) { throw new ArgumentException("Duplicate table number " + seat.TableNumber + ".", nameof(seats)); }
                _seats[i] = seat;
            }
            _customers = new CustomerId[_seats.Length];
            _clearing = new bool[_seats.Length];
            _clearRemaining = new double[_seats.Length];
            _settings = settings ?? new CustomerDirectorSettings();
            _drinkItemId = drinkItemId;
            _cakeItemId = cakeItemId;
            _random = new Random(seed);
            _nextCustomerId = firstCustomerId;
        }

        public event Action<CustomerSeatedEvent> CustomerSeated;
        /// <summary>Seat index; raised when the table clears and becomes free again.</summary>
        public event Action<int> CustomerLeft;
        /// <summary>A chosen seat refused the request; the seat stays free.</summary>
        public event Action<CustomerRequestFailedEvent> CustomerRequestFailed;

        public int SeatCount => _seats.Length;
        public int ActiveCustomers { get; private set; }
        /// <summary>Settings value clamped to 0..seat count.</summary>
        public int MaxActiveCustomers => Math.Min(_settings.MaxActiveCustomers, _seats.Length);
        public ICustomerSeat SeatAt(int seatIndex) => InRange(seatIndex) ? _seats[seatIndex] : null;
        public bool IsOccupied(int seatIndex) => InRange(seatIndex) && _customers[seatIndex].IsValid;
        public CustomerId CustomerAt(int seatIndex) => InRange(seatIndex) ? _customers[seatIndex] : default;

        /// <summary>Advances by a game-clock delta. Zero (paused), negative or NaN deltas are a no-op.</summary>
        public void Tick(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0d) { return; }
            double delta = seconds;
            UpdateOccupancy(delta);

            double interval = _settings.ArrivalIntervalSeconds;
            _sinceArrival += delta;
            if (_sinceArrival < interval) { return; }
            if (ActiveCustomers >= MaxActiveCustomers || !HasFreeSeat())
            {
                // Hold the timer at the threshold: the next arrival happens as soon as a table can take it.
                _sinceArrival = interval;
                return;
            }
            // At most one arrival per tick, so a long frame never seats a burst of customers at once.
            _sinceArrival = interval > 0d ? Math.Min(_sinceArrival - interval, interval) : 0d;
            SpawnNow();
        }

        /// <summary>Tries one arrival immediately; returns the seat index or -1 (cap reached, no free table, or request refused).</summary>
        public int SpawnNow()
        {
            if (ActiveCustomers >= MaxActiveCustomers) { return -1; }
            CollectFreeSeats();
            if (_free.Count == 0) { return -1; }
            int seatIndex = _free[_random.Next(_free.Count)];
            IReadOnlyList<ItemRequest> items = PickMenu();
            var customer = new CustomerId(_nextCustomerId);
            Result<OrderId> result = _seats[seatIndex].Request(customer, items);
            if (!result.IsSuccess)
            {
                CustomerRequestFailed?.Invoke(new CustomerRequestFailedEvent(seatIndex, result.ReasonKey));
                return -1;
            }
            _nextCustomerId++;
            _customers[seatIndex] = customer;
            _clearing[seatIndex] = false;
            _clearRemaining[seatIndex] = 0d;
            ActiveCustomers++;
            CustomerSeated?.Invoke(new CustomerSeatedEvent(seatIndex, customer, result.Value));
            return seatIndex;
        }

        private void UpdateOccupancy(double delta)
        {
            for (int i = 0; i < _seats.Length; i++)
            {
                if (!_customers[i].IsValid) { continue; }
                if (!_clearing[i])
                {
                    if (_seats[i].HasLiveOrder) { continue; }
                    // The order ended this tick; the clear delay starts now and is consumed by later ticks.
                    _clearing[i] = true;
                    _clearRemaining[i] = _settings.TableClearSeconds;
                }
                else
                {
                    _clearRemaining[i] -= delta;
                }
                if (_clearRemaining[i] > 0d) { continue; }
                _customers[i] = default;
                _clearing[i] = false;
                _clearRemaining[i] = 0d;
                ActiveCustomers--;
                CustomerLeft?.Invoke(i);
            }
        }

        private bool HasFreeSeat()
        {
            CollectFreeSeats();
            return _free.Count > 0;
        }

        private void CollectFreeSeats()
        {
            _free.Clear();
            for (int i = 0; i < _seats.Length; i++)
            {
                if (!_customers[i].IsValid && !_seats[i].HasLiveOrder) { _free.Add(i); }
            }
        }

        private IReadOnlyList<ItemRequest> PickMenu()
        {
            double drink = _settings.DrinkWeight;
            double cake = _settings.CakeWeight;
            double mixed = _settings.MixedWeight;
            double total = drink + cake + mixed;
            if (total <= 0d) { return Mixed(); }
            double roll = _random.NextDouble() * total;
            if (roll < drink) { return new[] { new ItemRequest(_drinkItemId, 1) }; }
            if (roll < drink + cake || mixed <= 0d) { return new[] { new ItemRequest(_cakeItemId, 1) }; }
            return Mixed();
        }

        private ItemRequest[] Mixed() => new[] { new ItemRequest(_drinkItemId, 1), new ItemRequest(_cakeItemId, 1) };

        private bool InRange(int seatIndex) => seatIndex >= 0 && seatIndex < _seats.Length;
    }
}
