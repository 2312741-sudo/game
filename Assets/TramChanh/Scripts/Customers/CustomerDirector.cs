using System;
using System.Collections.Generic;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Customers
{
    /// <summary>
    /// Plain-C# dine-in arrival and table occupancy. Seats a customer at a random free table on a game-clock
    /// interval and places the request through the seat (Lobby order point).
    /// <para>Legacy mode (default, <see cref="CustomerDirectorSettings.CustomersWalk"/> false): the request is placed at
    /// spawn and the table is freed <see cref="CustomerDirectorSettings.TableClearSeconds"/> after its order stops being live.</para>
    /// <para>Walking mode: spawn only reserves the seat (<see cref="CustomerArriving"/>); the request is placed on
    /// <see cref="NotifyArrived"/> or the arrival timeout; after the order ends the customer eats
    /// (<see cref="CustomerEating"/>), leaves (<see cref="CustomerLeaving"/>) and the seat is freed on
    /// <see cref="NotifyDeparted"/> or the departure timeout (<see cref="CustomerLeft"/>). The seat counts as occupied
    /// from CustomerArriving until CustomerLeft.</para>
    /// All timers run on <see cref="Tick"/> deltas only (pause-safe). Publishes nothing on the game EventBus; visuals
    /// listen to the C# events. Handler exceptions are isolated and reported to <see cref="FaultSink"/>.
    /// </summary>
    public sealed class CustomerDirector
    {
        private readonly ICustomerSeat[] _seats;
        private readonly CustomerId[] _customers;
        private readonly CustomerPhase[] _phases;
        private readonly IReadOnlyList<ItemRequest>[] _menus;
        private readonly bool[] _clearing;
        private readonly double[] _clearRemaining;
        private readonly double[] _timers;
        private readonly CustomerDirectorSettings _settings;
        private readonly bool _walking;
        private readonly string _drinkItemId;
        private readonly string _cakeItemId;
        private readonly Random _random;
        private readonly List<int> _free = new List<int>();
        private int _nextCustomerId;
        private double _sinceArrival;

        public CustomerDirector(IReadOnlyList<ICustomerSeat> seats, CustomerDirectorSettings settings,
            string drinkItemId, string cakeItemId, int seed, int firstCustomerId = 100, Action<Exception> faultSink = null)
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
            _phases = new CustomerPhase[_seats.Length];
            _menus = new IReadOnlyList<ItemRequest>[_seats.Length];
            _clearing = new bool[_seats.Length];
            _clearRemaining = new double[_seats.Length];
            _timers = new double[_seats.Length];
            _settings = settings ?? new CustomerDirectorSettings();
            // The mode is fixed for the director's lifetime so an inspector toggle at runtime cannot strand a seat mid-phase.
            _walking = _settings.CustomersWalk;
            _drinkItemId = drinkItemId;
            _cakeItemId = cakeItemId;
            _random = new Random(seed);
            _nextCustomerId = firstCustomerId;
            FaultSink = faultSink;
        }

        public event Action<CustomerSeatedEvent> CustomerSeated;
        /// <summary>Seat index; raised when the table clears and becomes free again.</summary>
        public event Action<int> CustomerLeft;
        /// <summary>
        /// A seat refused the request. Legacy mode: the seat stays free. Walking mode: raised on arrival, followed by
        /// <see cref="CustomerLeaving"/> (the seat stays reserved until the customer departs).
        /// </summary>
        public event Action<CustomerRequestFailedEvent> CustomerRequestFailed;
        /// <summary>Walking mode: seat reserved and the customer starts walking to it (no order yet).</summary>
        public event Action<CustomerArrivingEvent> CustomerArriving;
        /// <summary>Walking mode, seat index: the order ended (completed or failed); the customer eats for EatSeconds.</summary>
        public event Action<int> CustomerEating;
        /// <summary>Walking mode, seat index: the customer stands up and walks away; the seat is still reserved.</summary>
        public event Action<int> CustomerLeaving;

        /// <summary>
        /// Receives exceptions thrown by handlers of this director's events. The director's state is already
        /// consistent when a handler runs, so a throwing handler never corrupts it. Null: UnityEngine.Debug.LogException.
        /// </summary>
        public Action<Exception> FaultSink { get; set; }

        /// <summary>True when this director runs in walking mode (fixed at construction from the settings).</summary>
        public bool CustomersWalk => _walking;

        public int SeatCount => _seats.Length;
        public int ActiveCustomers { get; private set; }
        /// <summary>Settings value clamped to 0..seat count.</summary>
        public int MaxActiveCustomers => Math.Min(_settings.MaxActiveCustomers, _seats.Length);
        public ICustomerSeat SeatAt(int seatIndex) => InRange(seatIndex) ? _seats[seatIndex] : null;
        public bool IsOccupied(int seatIndex) => InRange(seatIndex) && _customers[seatIndex].IsValid;
        public CustomerId CustomerAt(int seatIndex) => InRange(seatIndex) ? _customers[seatIndex] : default;
        /// <summary>Phase of the seat's customer; <see cref="CustomerPhase.Free"/> when empty or out of range.
        /// Legacy mode reports Seated for the whole occupancy, including the table clear delay.</summary>
        public CustomerPhase PhaseAt(int seatIndex) => InRange(seatIndex) ? _phases[seatIndex] : CustomerPhase.Free;

        /// <summary>Advances by a game-clock delta. Zero (paused), negative or NaN deltas are a no-op.</summary>
        public void Tick(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0d) { return; }
            double delta = seconds;
            if (_walking) { UpdateWalking(delta); } else { UpdateOccupancy(delta); }

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

        /// <summary>
        /// Tries one arrival immediately; returns the seat index or -1 (cap reached, no free table, or request refused).
        /// Walking mode: only reserves the seat and raises <see cref="CustomerArriving"/>; no request is placed yet.
        /// </summary>
        public int SpawnNow()
        {
            if (ActiveCustomers >= MaxActiveCustomers) { return -1; }
            CollectFreeSeats();
            if (_free.Count == 0) { return -1; }
            int seatIndex = _free[_random.Next(_free.Count)];
            IReadOnlyList<ItemRequest> items = PickMenu();
            var customer = new CustomerId(_nextCustomerId);
            if (_walking)
            {
                _nextCustomerId++;
                _customers[seatIndex] = customer;
                _menus[seatIndex] = items;
                _phases[seatIndex] = CustomerPhase.Arriving;
                _timers[seatIndex] = _settings.ArrivalTimeoutSeconds;
                ActiveCustomers++;
                Raise(CustomerArriving, new CustomerArrivingEvent(seatIndex, customer, items));
                return seatIndex;
            }
            Result<OrderId> result = _seats[seatIndex].Request(customer, items);
            if (!result.IsSuccess)
            {
                Raise(CustomerRequestFailed, new CustomerRequestFailedEvent(seatIndex, result.ReasonKey));
                return -1;
            }
            _nextCustomerId++;
            _customers[seatIndex] = customer;
            _phases[seatIndex] = CustomerPhase.Seated;
            _clearing[seatIndex] = false;
            _clearRemaining[seatIndex] = 0d;
            ActiveCustomers++;
            Raise(CustomerSeated, new CustomerSeatedEvent(seatIndex, customer, result.Value));
            return seatIndex;
        }

        /// <summary>
        /// Presenter: the customer reached the seat. Places the request through the seat (Lobby path): success raises
        /// <see cref="CustomerSeated"/>; failure raises <see cref="CustomerRequestFailed"/> then <see cref="CustomerLeaving"/>.
        /// Ignored (no throw, no state change) unless the seat is <see cref="CustomerPhase.Arriving"/>.
        /// </summary>
        public void NotifyArrived(int seatIndex)
        {
            if (!InRange(seatIndex) || _phases[seatIndex] != CustomerPhase.Arriving) { return; }
            Arrive(seatIndex);
        }

        /// <summary>
        /// Presenter: the customer left the scene. Frees the seat and raises <see cref="CustomerLeft"/>.
        /// Ignored (no throw, no state change) unless the seat is <see cref="CustomerPhase.Leaving"/>.
        /// </summary>
        public void NotifyDeparted(int seatIndex)
        {
            if (!InRange(seatIndex) || _phases[seatIndex] != CustomerPhase.Leaving) { return; }
            Depart(seatIndex);
        }

        private void UpdateWalking(double delta)
        {
            for (int i = 0; i < _seats.Length; i++)
            {
                switch (_phases[i])
                {
                    case CustomerPhase.Arriving:
                        _timers[i] -= delta;
                        if (_timers[i] <= 0d) { Arrive(i); }
                        break;
                    case CustomerPhase.Seated:
                        // The order ended this tick (Completed, or Failed once game failures exist); the eat timer
                        // starts now and is consumed by later ticks, like the legacy clear delay.
                        if (_seats[i].HasLiveOrder) { break; }
                        StartEating(i);
                        break;
                    case CustomerPhase.Eating:
                        _timers[i] -= delta;
                        if (_timers[i] <= 0d) { StartLeaving(i); }
                        break;
                    case CustomerPhase.Leaving:
                        _timers[i] -= delta;
                        if (_timers[i] <= 0d) { Depart(i); }
                        break;
                }
            }
        }

        private void Arrive(int seatIndex)
        {
            CustomerId customer = _customers[seatIndex];
            IReadOnlyList<ItemRequest> items = _menus[seatIndex];
            _menus[seatIndex] = null;
            Result<OrderId> result = _seats[seatIndex].Request(customer, items);
            if (!result.IsSuccess)
            {
                // Seat stays reserved while the customer walks away; it is freed on departure.
                _phases[seatIndex] = CustomerPhase.Leaving;
                _timers[seatIndex] = _settings.DepartureTimeoutSeconds;
                Raise(CustomerRequestFailed, new CustomerRequestFailedEvent(seatIndex, result.ReasonKey));
                if (IsStill(seatIndex, customer, CustomerPhase.Leaving)) { Raise(CustomerLeaving, seatIndex); }
                return;
            }
            _phases[seatIndex] = CustomerPhase.Seated;
            _timers[seatIndex] = 0d;
            Raise(CustomerSeated, new CustomerSeatedEvent(seatIndex, customer, result.Value));
        }

        private void StartEating(int seatIndex)
        {
            CustomerId customer = _customers[seatIndex];
            _phases[seatIndex] = CustomerPhase.Eating;
            _timers[seatIndex] = _settings.EatSeconds;
            Raise(CustomerEating, seatIndex);
            // A zero eat time leaves in the same tick.
            if (IsStill(seatIndex, customer, CustomerPhase.Eating) && _timers[seatIndex] <= 0d) { StartLeaving(seatIndex); }
        }

        private void StartLeaving(int seatIndex)
        {
            _phases[seatIndex] = CustomerPhase.Leaving;
            _timers[seatIndex] = _settings.DepartureTimeoutSeconds;
            Raise(CustomerLeaving, seatIndex);
        }

        private void Depart(int seatIndex)
        {
            _customers[seatIndex] = default;
            _phases[seatIndex] = CustomerPhase.Free;
            _menus[seatIndex] = null;
            _timers[seatIndex] = 0d;
            ActiveCustomers--;
            Raise(CustomerLeft, seatIndex);
        }

        // A handler may re-enter (e.g. NotifyDeparted from CustomerLeaving); only continue a chain if it did not.
        private bool IsStill(int seatIndex, CustomerId customer, CustomerPhase phase) =>
            _phases[seatIndex] == phase && _customers[seatIndex] == customer;

        private void Raise<T>(Action<T> handlers, T args)
        {
            if (handlers == null) { return; }
            // Each subscriber runs isolated, so one throwing handler neither skips the others nor aborts the director.
            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try { ((Action<T>)handler)(args); }
                catch (Exception exception) { ReportFault(exception); }
            }
        }

        private void ReportFault(Exception exception)
        {
            Action<Exception> sink = FaultSink;
            if (sink != null)
            {
                try { sink(exception); return; }
                catch (Exception sinkFault) { UnityEngine.Debug.LogException(sinkFault); }
            }
            UnityEngine.Debug.LogException(exception);
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
                _phases[i] = CustomerPhase.Free;
                _clearing[i] = false;
                _clearRemaining[i] = 0d;
                ActiveCustomers--;
                Raise(CustomerLeft, i);
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
