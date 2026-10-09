# TABLES-01 — dine-in customer arrival and table occupancy

Branch: `feature/TABLES-01-customers` (base `feature/TABLES-10-integration` @ 390f9e8).
Owner: CLAUDE-ENV-01. No Unity Editor was available, so Unity PASS is **not** claimed.

## Scope
Plain-C# logic in `Assets/TramChanh/Scripts/Customers` (asmdef `TramChanh.Customers`: Core, Orders, Lobby).
No MonoBehaviour, no static state, nothing published on the game EventBus. The takeaway vehicle is not handled here.

## Public API (as implemented)
```csharp
namespace TramChanh.Customers
{
    public interface ICustomerSeat
    {
        int TableNumber { get; }
        bool HasLiveOrder { get; }
        Result<OrderId> Request(CustomerId customer, IReadOnlyList<ItemRequest> items); // TramChanh.Core.Result<T>, TramChanh.Orders.ItemRequest
    }
    public sealed class OrderPointSeat : ICustomerSeat
    {
        public const string MissingPointReasonKey = "customers.seat.missing_point";
        public OrderPointSeat(OrderPoint point, int tableNumber); // throws on null point, tableNumber <= 0, or TableOrderPoint.TableId != tableNumber
        public OrderPoint Point { get; }
    }
    [Serializable] public sealed class CustomerDirectorSettings
    {
        public CustomerDirectorSettings();   // field initializers: 3, 6, 2, 1, 1, 1 — all [SerializeField, Tbd("DEC-010")]
        public CustomerDirectorSettings(int maxActiveCustomers, float arrivalIntervalSeconds = 6f, float tableClearSeconds = 2f,
                                        float drinkWeight = 1f, float cakeWeight = 1f, float mixedWeight = 1f);
        public int MaxActiveCustomers { get; }       // >= 0 (director clamps to seat count)
        public float ArrivalIntervalSeconds { get; } // negative/NaN -> 0
        public float TableClearSeconds { get; }
        public float DrinkWeight { get; } public float CakeWeight { get; } public float MixedWeight { get; }
    }
    public readonly struct CustomerSeatedEvent { int SeatIndex; CustomerId Customer; OrderId Order; }       // get-only props + ctor
    public readonly struct CustomerRequestFailedEvent { int SeatIndex; string ReasonKey; }                  // get-only props + ctor
    public sealed class CustomerDirector
    {
        public CustomerDirector(IReadOnlyList<ICustomerSeat> seats, CustomerDirectorSettings settings,
                                string drinkItemId, string cakeItemId, int seed, int firstCustomerId = 100);
        public event Action<CustomerSeatedEvent> CustomerSeated;
        public event Action<int> CustomerLeft;
        public event Action<CustomerRequestFailedEvent> CustomerRequestFailed;
        public int SeatCount { get; }
        public int ActiveCustomers { get; }
        public int MaxActiveCustomers { get; }      // settings value clamped to 0..SeatCount
        public ICustomerSeat SeatAt(int seatIndex);
        public bool IsOccupied(int seatIndex);
        public CustomerId CustomerAt(int seatIndex);
        public void Tick(double seconds);
        public int SpawnNow();
    }
}
```
Item ids are `string` (`ItemRequest(string itemDefinitionId, int quantity)`); `CustomerId`, `OrderId`, `Result<T>` are in `TramChanh.Core`.

## Behaviour
- Free seat = not occupied by the director **and** `HasLiveOrder == false`. Seat chosen with `System.Random(seed)` among free seats.
- `SpawnNow()` returns -1 when `ActiveCustomers >= MaxActiveCustomers`, no seat is free, or the request fails. Works before the first `Tick`.
- On failure the seat stays free, `CustomerRequestFailed` carries the Lobby reason key, and the customer id is not consumed. Nothing throws.
- `CustomerSeated` fires only after `Request` succeeded; ids start at `firstCustomerId` and increase by one per seated customer.
- Menu: drink only / cake only / drink + cake, chosen by weight (all weights 0 -> mixed).
- `Tick(seconds)`: `<= 0`, NaN or infinity is a full no-op (pause). Occupied seats whose order stopped being live start the clear delay on the tick that sees it; later ticks consume it; at `<= 0` the seat is freed and `CustomerLeft(seatIndex)` fires. A 0 clear delay frees on detection.
- Arrival timer accumulates game time; at most **one** arrival per tick (a long frame does not seat a burst). While full or capped the timer holds at the threshold, so the next arrival comes as soon as a table frees.
- Constructor throws (config errors only) on null seats list/entries, duplicate table numbers, empty item ids, or `firstCustomerId <= 0`. Null settings -> defaults.

## Tests (Tests/EditMode/Customers, 23 cases)
`CustomerDirectorTests` (fake seats): Settings_DefaultsMatchProvisionalValues, Settings_DefaultsAreFieldInitializers_SoADeserializedInstanceKeepsThem,
SpawnNow_WorksBeforeTheFirstTick_AndSeatedFiresOnlyAfterASuccessfulRequest, Constructor_RejectsDuplicateTableNumbersAndNullSeats,
SpawnNow_AssignsOnlyFreeSeats_AndNeverAnOccupiedOne, SpawnNow_SkipsSeatsWhosePointStillHasALiveOrder,
MaxActive_DefaultsToThree_IsConfigurable_AndClampedToSeatCount, MaxActive_AlsoLimitsTimedArrivals, Tick_ArrivesOnInterval_AndZeroDeltaIsAPause,
Tick_LongFrameSeatsAtMostOneCustomer, Menu_ProducesDrinkCakeAndMixedOrders_WithMatchingItems, Menu_FollowsWeights (x3),
TableReleases_AfterOrderEndsPlusClearDelay_AndIsReusedByANewCustomer, ArrivalTimerHoldsWhileFull_AndSeatsAsSoonAsATableClears,
FailedRequest_LeavesSeatFree_ReportsReason_AndDoesNotThrow, CustomerIds_AreUniqueAndIncreasing, SeatChoice_IsDeterministicForASeed_AndVariesAcrossSeeds.

`CustomerTableIntegrationTests` (real OrderService + StallTicketQueue + ReadyShelf + LobbyOrderController + OrderEntryModel + 10 TableOrderPoints):
TenTableSeats_HaveUniqueTableNumbersOneToTen_MatchingTheirPoints, SeatedOrder_GoesThroughLobbyWithTheSeatTableAsOrigin_AndOccupiedTablesAreSkipped
(WaitingForLobby, origin TableId == seat table number, no stall ticket before Lobby),
CompletedOrder_ReleasesTableAfterClearDelay_AndANewCustomerReusesIt (full Lobby -> stall -> Ready -> pickup -> delivery -> Completed, then clear delay and reuse with a new id),
UnconfiguredPoint_FailsRequest_AndTheSeatStaysFree.

## Verification (.NET 8 harnesses with UnityEngine stand-ins, not Unity)
- Multi-asmdef NUnitLite harness: 232/232 (209 existing + 23 new). Harness change: added the Customers project, `Core/Provisional/TbdAttribute.cs` and a `PropertyAttribute` stand-in.
- All runtime scripts compile against real UnityEngine 2021.3.33 DLLs (C# 9): 0 errors, 0 warnings.
- Trial merge with `feature/TABLES-10-integration` @ c7a5de6 (bootstrap using this API): clean merge, runtime compile 0 errors, 232/232.

## Not verified
- Unity 6 Editor compile, Test Runner, and import of the new `.meta` files.
- Unity's bundled NUnit version against the assertions used (kept to long-standing constraints; `Is.AnyOf` avoided).
- Scene/bootstrap behaviour in Play Mode (pacing feel, visuals on CustomerSeated/CustomerLeft).
