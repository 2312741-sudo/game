# MAIN-103: Player experience (functional HUD)

Branch `feature/MAIN-103-player-ux`, based on `wave/MAIN-001-integration` at 248eac1. Owner: CLAUDE-03.
This change covers functional UI behaviour, data bindings and messages only. It adds no USS, UXML, fonts, sprites or panel art. Styling is a little inline code, written the same way as the existing views.

## What the player can now tell without a developer

| Question | Where it shows | Source |
|---|---|---|
| Which order am I making? | Order list (top left): origin, items, stage, ready/total count | `IOrderService.Active` and `Get`, refreshed by `OrderStatusChanged` / `OrderItemStatusChanged` |
| What am I holding? | Held panel (top right): name, state, next step, which order it belongs to | `IHeldItemSlot.Current`, `IPreparationFeedback`, `IPreparedItem` and the read-only order item |
| What do I do next? | "Tiếp theo: …" line (most urgent order), the held panel's "Tiếp: …" line, and per-order next steps | `ActiveOrderListModel.PrimaryNextStep`, `HeldItemFeedbackModel.NextAction` |
| Where do I go? | Next steps name the place: "Tới Xe 1 nhận đơn", "Lấy đơn ở quầy giao", "Mang đơn tới Xe 1" | Order origin, as `hud.origin.table` / `hud.origin.vehicle` |
| Is the order complete? | Toast: "Hoàn tất đơn Xe 1!"; the order leaves the list | `OrderStatusChanged(Completed)` |

Holding progress: the existing hold bar now shows a caption, "Đang giữ... 45%", and appears only while the action is available. Continuous actions (cake batter fill) show "Giữ phím, thả ra để dừng" instead of an empty bar.

## Files

- `Assets/TramChanh/Scripts/UI/Hud/HudText.cs`: a key plus arguments, rendered late. Nested arguments are localized, and a malformed template never makes it throw.
- `Assets/TramChanh/Scripts/UI/Hud/HudKeys.cs`: every HUD key, the origin text, and the status, item and held-type key builders.
- `Assets/TramChanh/Scripts/UI/Hud/ActiveOrderListModel.cs`: plain C# list of live orders (max 4 rows; the primary order is always listed).
- `Assets/TramChanh/Scripts/UI/Hud/HeldItemFeedbackModel.cs`: plain C# held-item state.
- `Assets/TramChanh/Scripts/UI/Hud/OrderToastModel.cs`: plain C# milestone toasts (sent, ready, completed, failed, wrong target), shown for 3 s.
- `Assets/TramChanh/Scripts/UI/Hud/HoldProgressModel.cs`: hold and continuous captions.
- `Assets/TramChanh/Scripts/UI/Hud/PlayerHudPresenter.cs`: builds three elements into the existing InteractionHUD UIDocument root. Model events only mark it dirty; it renders in `Tick()`.
- `Assets/TramChanh/Scripts/UI/Prompt/InteractionPromptView.cs`: adds an opt-in `Initialize(IEventBus, PlayerInteractor, IOrderService)` overload, an `Update()` that ticks the HUD, and the hold captions. The old two-argument overload behaves as before, so DrinkWave and the PlayerInteraction test scenes and their tables are unaffected.
- `Assets/TramChanh/Scripts/UI/Localization/PromptLocalizationTable.cs`: adds `Contains(key)`.
- `Assets/TramChanh/ScriptableObjects/UI/SO_PromptText_TramChanhMain.asset`: 131 to 203 entries. 21 existing texts were polished. `SO_PromptText_DrinkWave.asset` is untouched.
- `Assets/TramChanh/Tests/EditMode/UI/PromptTableFile.cs`: plain .NET reader for the Unity YAML table.
- `Assets/TramChanh/Tests/EditMode/UI/PromptTextCoverageTests.cs`: localization coverage (4 tests).
- `Assets/TramChanh/Tests/EditMode/UI/PlayerHudModelTests.cs`: model tests against the real OrderService, StallTicketQueue and ReadyShelf (12 tests).
- `Assets/TramChanh/Tests/EditMode/UI/PlayerHudPresenterTests.cs`: renders the presenter into a detached `VisualElement` (1 test; needs Unity).

The UI assembly still references only Core, Interaction, Orders and Content. No Drinks, Cakes, Lobby or Stall type is used. A test asserts this.

## Wiring request (lead: `TramChanhMainBootstrap.cs`)

Change one line in `Awake()`. It must run after `_interactor.Initialize(...)`, which is already the case:

```csharp
// before
_prompt.Initialize(_events, _interactor);
// after
_prompt.Initialize(_events, _interactor, Orders);
```

No new scene GameObject, component or serialized field is needed. The HUD is created in code inside the existing `InteractionHUD` UIDocument, which already has `_table` set to `SO_PromptText_TramChanhMain` and `_language` set to `vi`. The existing `_prompt.Disconnect()` in `OnDestroy` disposes the HUD's subscriptions.

## What the player sees: a mixed order (drink + cake) from Vehicle 1

1. A customer arrives. Order list: "Đơn hàng (1)", "Tiếp theo: Tới Xe 1 nhận đơn", then "Xe 1 · Chờ nhận đơn", then "Đồ uống (tên tạm) x1, Bánh thử nghiệm x1". Held panel: "Tay không" and "Nhìn vào quầy hoặc khách rồi nhấn E".
2. At the vehicle, "Nhận đơn của khách". The row changes to "Đang nhận đơn" with "Nhập đơn rồi gửi tới quầy", then "Đã nhập đơn" with "Gửi đơn tới quầy".
3. After Send: toast "Đã gửi đơn Xe 1 tới quầy". The row shows "Xe 1 · Đã gửi quầy (0/2 món xong)", the items "Đồ uống (tên tạm): Chờ làm, Bánh thử nghiệm: Chờ làm", and the next step "Lấy túi trà ở kệ".
4. Tea bag taken from the rack. Held panel: "Đồ uống (tên tạm)", "Trạng thái: Đang làm", "Cho Xe 1", and "Tiếp: Mở túi trà" (the bag's own available action) or the drink step guide. The order shows "Đang làm (0/2 món xong)" and "Làm xong Đồ uống (tên tạm) rồi đặt lên quầy giao". While shaking or wiping, the hold bar reads "Đang giữ... 45%".
5. Drink placed. The order shows "(1/2 món xong)" and "Làm bánh ở bếp nướng". With the batter cup in hand: "Ca đong bột", "Trạng thái: Chờ đong bột", "Tiếp: Giữ để đong bột". The fill prompt shows "Giữ phím, thả ra để dừng". As the cake advances, the cup or cake panel follows `cake.state.*` and the cake's next action ("Đang nướng" / "Chờ", "Đã chín" / "Lật bánh" … "Đã gói" / "Đặt lên quầy giao").
6. Both items are on the counter. Toast: "Đơn Xe 1 đã xong, tới quầy giao lấy". The row shows "Sẵn sàng" and "Lấy đơn ở quầy giao". It becomes the primary order ("Tiếp theo: Lấy đơn ở quầy giao").
7. Order picked up. Held panel: "Khay đơn", "Trạng thái: Đang mang đi", "Tiếp: Mang đơn tới Xe 1". The row shows the same next step.
8. At the wrong point, delivery is rejected with the toast "Sai chỗ, đơn này của Xe 1" (red).
9. At Vehicle 1, "Giao món cho khách". Toast: "Hoàn tất đơn Xe 1!" (green). The row disappears and the held panel returns to "Tay không".

## Localization coverage

- 202 keys are used by runtime code: 163 string literals in `Scripts/**` (Editor excluded) plus 41 composed keys: `cake.state.*` ×13, `drink.state.*` ×9, `order.status.*` ×10, `order.item.status.*` ×3, `held.<IHoldable type>` ×4, `item.<ItemDefinition id>` ×2.
- Baseline: 130 resolved and 72 missing. 7 of the missing keys were already shown by existing code: `core.result.uninitialized`, `drink.bag.not_stored`, `drink.need_pickup`, `interaction.held_item_changed`, `hud.next.delivery`, `order.status.PickedUpByLobby`, and `item.cake.dev.tbd` (OrderEntryUI showed it raw). The rest are new HUD keys or the `drink.state.*` contract keys.
- Now: 202 of 202 resolve in both languages, with no duplicate keys, matching `{n}` placeholders, Vietnamese at most 60 characters (except `preview.controls` and `order.transition.invalid`), and no Vietnamese text that looks like a raw key.
- The test scans source literals, reflects over `CakeState`, `TeaBagState`, the order/item status enums and the `IHoldable` implementers, and reads every serialized `ItemDefinition` id. A new key in code fails the test until the table has it.
- Text polish: "order" changed to "đơn" throughout, plus shorter "Lấy đơn đã xong", "Tay đang cầm đồ", "Đặt lên quầy giao", "Món chưa làm xong" and "Sai khách của đơn này". The long `order.transition.invalid` hint is kept on purpose, because it tells the player to enter the order before sending.

## Verification (.NET 8, no Unity Editor)

- Runtime compile: all `Scripts/**` against the real UnityEngine 2021.3.33 reference DLLs, UIElementsModule included. 0 errors, 0 warnings.
- Test compile: the four new test files together with all runtime scripts against the same Unity DLLs plus NUnit 3.14. 0 errors.
- Harness run: Core, Orders, Interaction, Drinks, Cakes, Lobby and the UI models with a UnityEngine stand-in, using NUnitLite. 27 of 27 pass: the 12 HUD model tests, the 4 localization coverage tests, and the 11 existing `OrderEntryModelTests`. The presenter test needs native UIElements and was compiled only.
- Negative check: removing `cake.state.cut`, `held.ServedOrder` and `drink.need_pickup` from the table made both coverage tests fail with those keys named.

No Unity EditMode or PlayMode PASS is claimed.

## Lead requests for other owners

1. Drinks (TeaBagItem): implement `IPreparationFeedback` as the contract describes. All of these keys already exist in the table.
   ```csharp
   public string PreparationStateKey => "drink.state." + State.ToString().ToLowerInvariant();
   public string NextActionKey => State switch
   {
       TeaBagState.Stored => "drink.rack.take_bag", TeaBagState.PickedUp => "drink.bag.open",
       TeaBagState.Opened => "drink.add_coconut", TeaBagState.CoconutJellyAdded => "drink.add_lemon",
       TeaBagState.LemonJellyAdded => "drink.add_ice", TeaBagState.IceAdded => "drink.bag.shake",
       TeaBagState.Shaken => "drink.wipe", _ => "ready.place"
   };
   ```
   Until this lands, the HUD shows the order item status, the bag's own available held action, or the drink step guide ("Thêm thạch dừa, thạch chanh, đá; rồi lắc và lau").
2. Interaction, for continuous progress. `InteractionActionDriver` reports `Progress` only for `Hold`. A minimal additive API:
   - `InteractionActionDriver`: `public InteractionKind ActiveKind => IsRunning ? _query.Kind : default;` and `public float ElapsedSeconds => IsRunning ? (float)(_context.Clock.Now - _startedAt) : 0f;`
   - `InteractionPromptChanged`: an optional trailing constructor argument `float continuousSeconds = 0f`, included in `Equals` and `GetHashCode`, filled by `PlayerInteractor.PublishPrompt` from `_driver.ElapsedSeconds` when `ActiveKind == Continuous`.
   - With that in place, `HoldProgressModel.Caption` can show elapsed time for the batter fill. Showing measured millilitres would need the cake feedback to expose a measurement key.
3. Orders (optional): a carrier contract such as `public interface IOrderCarrier { OrderId CarriedOrderId { get; } }`, implemented by `Lobby.ServedOrder`. Today the HUD finds the carried order as the single order that is `PickedUpByLobby`. That is correct with one hand slot, but it is inferred.
4. Antigravity (visual): the HUD uses the PanelSettings default font. Vietnamese diacritics and "·" (U+00B7) need glyph coverage. Layout positions are placeholders: orders top-left under the controls legend, held item top-right, toast at 18 % from the top.

## Not verified

- Nothing ran in Unity: the HUD layout and overlap, font glyphs, Play Mode behaviour, `PlayerHudPresenterTests`, and the Unity-hosted runs of the coverage and model tests (they use `Directory.GetCurrentDirectory()`, which is the project root in the Editor).
- Unity re-serialization of the edited table asset. The YAML is hand-generated in Unity's escape style and checked by PyYAML and the test reader.
- Reason keys from the parallel reliability agent are not included yet. The coverage test will flag them once they land in code.
