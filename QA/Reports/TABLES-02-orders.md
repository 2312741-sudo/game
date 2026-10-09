# TABLES-02 — Lobby orders and correct-table delivery for 10 tables

Branch `feature/TABLES-02-orders` (from `feature/TABLES-10-integration` @ 390f9e8).
Scope: Orders, Lobby, UI (HUD), prompt table, EditMode tests. No Unity Editor was available.
Nothing here is a Unity PASS. All results come from .NET 8 harnesses.

## Defects found and fixed

| # | Defect | Fix | Regression test |
|---|---|---|---|
| D1 | With many tables, an order left in `TakingOrder`/`Entered` (the entry panel was closed, or the Lobby walked to another table) gave the primary HUD hint "Nhập đơn rồi gửi tới quầy" / "Gửi đơn tới quầy" without saying which table. Entry only reopens at that order's own point. | `ActiveOrderListModel.NextStep` passes the origin to `hud.next.enter_order` / `hud.next.send_order`. The prompt texts become "Nhập đơn {0} rồi gửi tới quầy" / "Gửi đơn {0} tới quầy" (en: "Enter the order for {0}, then send it" / "Send the order for {0} to the stall"). Key names are unchanged and no C# API changed. | `TenTableHudTests.TABLES02_EveryStageHintOfATwoDigitTableNamesThatTable` |
| D2 (lead request) | `customers.seat.missing_point` (Customers lane, `OrderPointSeat.cs`) was missing from the prompt table, so `PromptTextCoverageTests` failed on the merged integration branch. | Added the key. en: "This table has no order point configured". vi: "Bàn này chưa được cấu hình điểm gọi món". | The existing `PromptTextCoverageTests` fail without the key and pass with it, on integration plus this branch. |

No defects were found in OrderService, StallTicketQueue, ReadyShelf, OrderPoint/TableOrderPoint/VehicleOrderPoint, LobbyOrderController, ReadyOrderPickupPoint or ServedOrder for ten tables.

## Tests (EditMode, real services and real point components)

`Tests/EditMode/Lobby/TenTableOrderFlowTests.cs` uses 10 TableOrderPoints (TableId 1..10, ids 31..40), VehicleOrderPoint 1, ReadyOrderPickupPoint, the real OrderEntryModel, OrderService, StallTicketQueue and a 1+1 ReadyShelf:
1. `TenTablePointsHaveUniqueIdsAndEveryRequestKeepsItsTableAndWaitsForTheLobby`: unique ids and origins. Every order starts at WaitingForLobby. Stray UI events and stall claims cannot bypass the Lobby. An occupied table rejects a second order.
2. `FourTablesAndTheVehicleAreServedFifoPerKindInSendOrder`: request order differs from send order. Tickets and drink/cake claims follow send order and identify their table or vehicle.
3. `BundleIsRejectedAtEveryWrongTableAndTheVehicleAndCompletesOnlyAtItsOwnTable` and `TableSevenOrderIsNeverDeliverableAtTableOneWithTheSameMixAndCustomerNumber`: 9 wrong tables plus the vehicle are rejected with `order.delivery.wrong_target`. The bundle stays in hand and the attempt count rises each time. The right table completes. A freed table reports `no_customer`.
4. `MixedOrdersAcrossTablesRespectReadyCapacityAndBundlesHoldOnlyTheirOwnItems`: `ready.slot_full` applies while another table's drink waits. One table's drink and another's cake sit on the shelf together, and only the complete order is picked up. Each ServedOrder holds exactly its own item definitions.
5. `CompletionFreesOnlyItsTableWhichThenAcceptsANewCustomerAndRejectsASecondCompletion`
6. `VehicleFlowIsUnchangedWhileTenTablesAreLive`: exact T1..T9 status sequence.
8. `OrderEntryAtTableTenStaysBoundToTableTenWhenTheLobbyVisitsTableNineMidway`

`Tests/EditMode/UI/TenTableHudTests.cs` (item 7, plus item 8 label formatting):
- `ElevenLiveOrdersAreLabelledBan1ToBan10AndXe1WithTheirOwnDestination`: "Bàn 1".."Bàn 10" and "Xe 1". With 11 orders: 4 rows, "+7 đơn khác", "Đơn hàng (11)".
- `EveryStageHintOfATwoDigitTableNamesThatTable`: regression test for D1.
- `CarriedOrderAmongElevenPointsTheHintAtItsOwnTableNotAnotherWithTheSameItems`

The HUD already caps the list at `MaxRows = 4` and adds a "+N đơn khác" line. The primary order is always listed. No new cap was needed.

## Harness results
- Per-asmdef build (`multi`): 217/217 (209 before, plus 8 new). Same result on `origin/feature/TABLES-10-integration` with these commits cherry-picked.
- HUD/prompt coverage (`uxh`): 30/30 (27 before, plus 3 new). Same result on the merged integration tree, which includes Scripts/Customers.
- Full runtime compile against real UnityEngine DLLs: 0 errors, 0 warnings.

## Observations / lead requests
- **Ticket tie-break**: tickets are FIFO by SentAt, and equal SentAt falls back to the lower OrderId (request order). The canonical test `TC_ORDER_003_EqualSendTimeUsesOrderIdTieBreak` pins this. In play the clock advances every frame, so two sends never share a reading, and I left it unchanged. If strict send order is wanted for ties, it is a one-line change in `OrderService.SendToStall`, but it needs a contract decision and changes TC-ORDER-003.
- **Ready shelf deadlock with many tables**: none is reachable. Drinks and cakes are each prepared one at a time (one held bag, one cake Current, Ready gates). Together with FIFO claims, this keeps the 1+1 shelf deadlock-free, as `ReadySlotDeadlockModelTests` already shows. If concurrent same-kind preparation is ever added (two players, a bag set-down surface), revisit this.
- The order-entry panel shows no table label because `OrderEntryRequested` carries no origin. The entry always opens at the point the Lobby is standing at, so I added nothing. Showing "Bàn N" there would need an additive event field or an order lookup in the UI.
- `QA/Reports/MAIN-103-player-ux.md` quotes the old hint strings. That file is a historical report and was left as is.

## Not verified
- Anything that needs the Unity Editor: scene wiring of the 10 points and ids 31..40, HUD layout and overflow on screen, PlayMode tests.
