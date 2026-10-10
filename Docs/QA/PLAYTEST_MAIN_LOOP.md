# Manual playtest — SCN_TramChanh_Main main loop

Use this for the **hands-on** part of the PR #22 Unity gate. Every box needs a person or an input-driving agent
playing in the Game view with mouse and keyboard. Tests that call methods in code are not a substitute.
Mark a box **only after you did it**. If you could not do something, write `NOT RUN` and the reason.

- **Project:** the isolated worktree `Lưu trữ/game-QA-22`. Never the canonical `Lưu trữ/game`, where Antigravity works.
- **Commit:** `git -C "…/game-QA-22" rev-parse --short HEAD`. Write it in the report.
- **Unity:** 6000.6.0f1. Scene: `Assets/TramChanh/Scenes/Gameplay/SCN_TramChanh_Main.unity`.
  Open it with **Tram Chanh ▸ Scenes ▸ Open Main Game**.
- Click once inside the **Game view** after pressing Play, so the cursor is captured.

## Controls

| Key | Action |
|---|---|
| WASD | Move |
| Mouse | Look |
| **E** / left mouse | Use the station or customer you are looking at |
| **F** / right mouse | Use the item in your hand |
| Hold E or F | Hold actions (a progress bar is shown) |
| **Esc** | Pause / release the cursor |

The HUD has three parts:
- **Top left:** the order list, with a "Tiếp theo: …" line saying where to go next.
- **Top right:** the item in your hand, its state, and the next step.
- **Centre:** the prompt for what you are looking at, with the reason when an action is blocked.

## A. Start (no input) and camera-drift probe

- [ ] A1. You see the night street, the green stall, and three customers: Bàn 1 (drink), Bàn 2 (cake), Xe 1 (takeaway, drink + cake).
- [ ] A2. The HUD is in Vietnamese with correct diacritics. No "□" boxes and no raw keys like `cake.fill` or `order.status.x`.
- [ ] A3. **Drift probe.** After the cursor is captured, take your hand off the mouse and keyboard for 30 s. The view must not move or rotate.
  - If it moves, record whether the **position** changed (you are now somewhere else) or only the **view direction**.
  - Record whether the mouse or trackpad was touched, and whether the Game view had focus.
- [ ] A4. Press Esc: the cursor appears and the game pauses. Click the Game view: play resumes, and the view does not jump.

## B. Drink-only order (Bàn 1)

- [ ] B1. Look at Bàn 1 and press E. The order window opens and the game pauses. Press **Nhập đơn**, then **Gửi tới quầy**. The window closes and a toast confirms the send.
- [ ] B2. Walk behind the stall. Look at the **red tea rack** and press E. A pre-portioned bag is in your hand. The HUD shows "Cho Bàn 1" and "Tiếp: Mở túi trà".
- [ ] B3. Press F to open the bag. Look at the coconut jelly bin and press E, then the lemon jelly bin and press E (**both topping bins can be targeted**). Then the ice bin, and press E.
- [ ] B4. Hold F to shake (progress bar), then hold E at the wipe area (progress bar).
- [ ] B5. **Wrong order:** trying lemon before coconut, or wiping before shaking, is refused with a readable reason. Nothing breaks.
- [ ] B6. Look at the Ready counter and press E. The bag leaves your hand and sits on the counter. The order shows as ready.
- [ ] B7. Look at the pickup point and press E. You carry the order tray. The HUD says to take it to Bàn 1.
- [ ] B8. **Wrong customer:** pressing E at Bàn 2 or Xe 1 is refused with a red toast. The tray stays in your hand.
- [ ] B9. Press E at Bàn 1. A green "Hoàn tất đơn Bàn 1!" toast appears and your hand is empty.
- [ ] B10. A few seconds later a **new customer** appears at Bàn 1 (Chờ nhận đơn).

## C. Cake-only order (Bàn 2) — all five cake points + cup

- [ ] C1. Take the order at Bàn 2 and send it.
- [ ] C2. Look at the **500 ml cup** and press E: the cup is in your hand.
- [ ] C3. **BatterSource:** hold E to measure. The cup level rises; release to stop.
- [ ] C4. **Grill:** press E to open the lid → E to pour → E to close. Wait for cooking, then E to open → E to **flip**. The cake moves to the roll area.
- [ ] C5. **RollArea:** hold E to cut.
- [ ] C6. **Sauce:** hold E to add sauce.
- [ ] C7. **RollArea** again: hold E to roll vertically.
- [ ] C8. **WrappingArea:** press E to wrap. The cake is in your hand.
- [ ] C9. Place it on the Ready counter, pick up the order, deliver it to Bàn 2. It completes and a new customer arrives later.
- [ ] C10. **Recovery:** pick up the empty cup when there is no cake ticket, then press F. The cup goes back (`Đặt ca đong về chỗ cũ`), and you can take orders again.

## D. Mixed takeaway order (Xe 1)

- [ ] D1. **The vehicle can be targeted** from the sidewalk. Take the order and send it. Both the drink and the cake have tickets.
- [ ] D2. Make the drink and place it on Ready. The order shows 1 of 2 items done and **cannot** be picked up yet.
- [ ] D3. Make the cake and place it on Ready. Now the order can be picked up. Deliver it to Xe 1, it completes, and a new customer arrives later.

## E. Capacity, restock, recovery

- [ ] E1. **Ready full.** Leave a finished drink on Ready that is waiting for its cake, with another drink order sent. The tea rack refuses with "slot full" until the Ready order is picked up. It works again afterwards.
- [ ] E2. **Rack restock.** Serve at least **3 drink orders** in a row. The rack always has a bag (it holds 2) and never stays empty.
- [ ] E3. **Hands full.** While holding an item, pressing E on another rack, bin or customer is refused with a reason. Nothing is lost.
- [ ] E4. Over the whole session the player never ends up stuck with an item that cannot be used, placed or discarded.

## F. End

- [ ] F1. Stop Play. Copy every red Console entry (Clear before you start).
- [ ] F2. Take screenshots of the Game view (`Cmd+Shift+4`, then Space, then click the Unity window) at A1, B7, C4, D3 and E1. Save them to `~/TramChanh-validation/manual-<date>/`.
- [ ] F3. In `game-QA-22`, `git status` shows no changed tracked files. If any did change, list them; Unity sometimes rewrites `.mat` colour fields.

## Report template (post on PR #22 and issue #20)

```
### Manual playtest SCN_TramChanh_Main — <commit> — Unity 6000.6.0f1
Tester: <name / agent>   Input: real mouse+keyboard | agent-driven | NOT RUN
A: A1 ☐ A2 ☐ A3 ☐ (drift: none | position | rotation) A4 ☐
B: B1–B10 …   C: C1–C10 …   D: D1–D3 …   E: E1–E4 …
Console errors: <none | list>
Screenshots: <attached>
Bugs found: <list with steps>
```
