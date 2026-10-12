# PEOPLE-001 — walking customers (director logic, phases D/E)

Branch: `feature/PEOPLE-001-customers` (base `origin/feature/PEOPLE-001` @ e9db7c4).
Owner: PEOPLE-P2. No Unity Editor was available, so Unity PASS is **not** claimed.
Plan: `Docs/Design/NPC_AND_MAP_EXPANSION.md` §2 (Customers row), §3 (seat reserved on walk, order on arrival), §7 (risks).

## Scope
Plain-C# logic only, in `Assets/TramChanh/Scripts/Customers` and `Assets/TramChanh/Tests/EditMode/Customers`.
No presenter, no scene, prefab, bootstrap, Lobby, Orders or People change. No reference to `TramChanh.People`.
No static state; nothing published on the game EventBus.

## Modes
- **Legacy (default, `CustomersWalk == false`)**: unchanged. The request is placed at spawn; the table is freed
  `TableClearSeconds` after its order stops being live. All 23 pre-existing Customers tests pass untouched.
- **Walking (`CustomersWalk == true`)**, fixed at director construction:

| Step | Trigger | Phase after | Event |
|---|---|---|---|
| Spawn (timer or `SpawnNow`) | free seat, below cap | Arriving | `CustomerArriving` (seat reserved, **no request**) |
| Arrival | `NotifyArrived` or `ArrivalTimeoutSeconds` of Tick time | Seated | request through the seat (Lobby) -> `CustomerSeated` |
| Arrival, request refused | same | Leaving | `CustomerRequestFailed`, then `CustomerLeaving` |
| Order ends (Completed or Failed) | detected on Tick | Eating | `CustomerEating` |
| Eat done | `EatSeconds` of later Tick time (0 = same tick) | Leaving | `CustomerLeaving` |
| Departure | `NotifyDeparted` or `DepartureTimeoutSeconds` of Tick time | Free | `CustomerLeft` |

The seat counts as occupied (`IsOccupied`, `CustomerAt`, `ActiveCustomers`, `MaxActiveCustomers`, never reassigned) from
`CustomerArriving` until `CustomerLeft`. `CustomerEating` is raised for any order end, including a Failed order; game
failures are not reachable today, so in practice it follows a Completed order. A refused request at arrival skips Eating.

## Public API (additive)
```csharp
// CustomerDirectorSettings: new [SerializeField, Tbd("DEC-010")] fields with field-initializer defaults
public bool  CustomersWalk { get; }             // false
public float EatSeconds { get; }                // 4   (negative/NaN -> 0)
public float ArrivalTimeoutSeconds { get; }     // 30  (negative/NaN -> 0)
public float DepartureTimeoutSeconds { get; }   // 20  (negative/NaN -> 0)
public CustomerDirectorSettings(int maxActiveCustomers, float arrivalIntervalSeconds = 6f, float tableClearSeconds = 2f,
    float drinkWeight = 1f, float cakeWeight = 1f, float mixedWeight = 1f,
    bool customersWalk = false, float eatSeconds = 4f, float arrivalTimeoutSeconds = 30f, float departureTimeoutSeconds = 20f);

public readonly struct CustomerArrivingEvent
{ public CustomerArrivingEvent(int seatIndex, CustomerId customer, IReadOnlyList<ItemRequest> items);
  public int SeatIndex { get; } public CustomerId Customer { get; } public IReadOnlyList<ItemRequest> Items { get; } }
public enum CustomerPhase { Free, Arriving, Seated, Eating, Leaving }

// CustomerDirector
public CustomerDirector(IReadOnlyList<ICustomerSeat> seats, CustomerDirectorSettings settings, string drinkItemId,
    string cakeItemId, int seed, int firstCustomerId = 100, Action<Exception> faultSink = null);
public event Action<CustomerArrivingEvent> CustomerArriving;
public event Action<int> CustomerEating;
public event Action<int> CustomerLeaving;
public CustomerPhase PhaseAt(int seatIndex);   // Free when empty/out of range; legacy: Seated for the whole occupancy
public void NotifyArrived(int seatIndex);      // ignored unless Arriving
public void NotifyDeparted(int seatIndex);     // ignored unless Leaving
public Action<Exception> FaultSink { get; set; } // null -> UnityEngine.Debug.LogException
public bool CustomersWalk { get; }             // extra: the mode this director runs in (read once at construction)
```

## Robustness
- Wrong-phase or out-of-range `Notify*` calls return without throwing or changing state.
- Timers advance only by positive, finite `Tick` deltas (0, negative, NaN, infinity are no-ops), so pause holds every timer.
  A phase entered during a tick starts its timer on the next tick (same rule as the legacy clear delay).
- Every event raise (old and new events, both modes) runs each subscriber in its own try/catch; faults go to `FaultSink`
  (a throwing sink falls back to `Debug.LogException`). State is updated before raising, so handlers see a consistent
  director and may re-enter it (e.g. `NotifyArrived` from `CustomerArriving`, `NotifyDeparted` from `CustomerLeaving`).
- **Behaviour change for throwing handlers only**: in legacy mode an exception from a `CustomerSeated`/`CustomerLeft`/
  `CustomerRequestFailed` handler used to escape `SpawnNow`/`Tick`; it is now reported to the sink / logged instead.

## Prompt / reason keys
None added. A refused arrival forwards the seat's reason key unchanged (e.g. `order.point.not_configured`,
`customers.seat.missing_point`, or the point's busy key).

## Tests (.NET 8 + NUnitLite harness, per-asmdef build, not Unity)
`Tests/EditMode/Customers/CustomerWalkingTests.cs`: 19 tests (16 in `CustomerWalkingTests` against `FakeCustomerSeat`,
3 in `CustomerWalkingIntegrationTests` against the real OrderService, StallTicketQueue, ReadyShelf, Lobby entry flow and
10 real `TableOrderPoint`s). EditMode total 259/259 (240 before; Customers 23 -> 42).
Full compile of all Scripts against the real UnityEngine DLLs (C# 9, with and without `UNITY_EDITOR`): 0 errors, 0 warnings.
Mutation checks (unwrapped raises, seat reuse while reserved, disabled arrival timeout) each fail the new tests.

## Not verified
- Unity 6000.6.0f1 compile, EditMode run, serialization of the new fields in an existing scene/bootstrap (the bootstrap
  serializes `new CustomerDirectorSettings()`; new fields take their initializer defaults, so walking stays off).
- The presenter (lead) against this API; PlayMode behaviour; the HUD/active-order expectations listed in plan §7 once
  walking is switched on.
