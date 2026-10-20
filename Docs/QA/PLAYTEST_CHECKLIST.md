# Playtest checklist (Mac, project folder game-MP)

Setup: close Unity, then `git stash` (only if Git complains about local changes), `git checkout main`, `git pull origin main`, `git lfs pull`, open the project, wait for the import, open `Assets/TramChanh/Scenes/Gameplay/SCN_TramChanh_Main.unity`.
Report anything that fails with a screenshot and the Console text.

## Start and menus
- [ ] Pressing Play shows the title screen "TRẠM CHANH" and the game is frozen behind it.
- [ ] The title screen shows "Ngày N | Tổng cộng: X đơn".
- [ ] "Chơi" starts the game and hides the cursor.
- [ ] Esc opens "TẠM DỪNG"; the clock stops; Esc again or "Tiếp tục" resumes.
- [ ] Volume slider changes the volume. Look-speed slider changes mouse turning speed.
- [ ] "Mặc định" resets both sliders. Stop and Play again: the sliders keep their last values.
- [ ] "Ghi công" (or F1) shows 10 CC-BY entries on top of the menu; it closes with the menu.

## Clock and day
- [ ] Top centre reads "Ngày 1  18:00  Mở cửa" and the minutes run.
- [ ] At 24:00 the game stops and "Hết ngày 1" appears with served, failed, percent, totals.
- [ ] "Ngày mới" continues; the label shows "Đóng cửa (tua nhanh)" and time runs fast, no customers arrive.
- [ ] At 18:00 it reads "Ngày 2  Mở cửa" and customers arrive again.
- [ ] Sky and light change between day and night.

## Money (G1, new)
- [ ] Top centre reads "Ngày 1  18:00  Mở cửa  |  0đ".
- [ ] Serving the basic drink (Trà Olong - Chanh tươi) adds 20.000đ, a cake 20.000đ, a drink plus cake 40.000đ (no combo discount, as on the real menu), a flavour drink 30.000đ, all at Good quality.
- [ ] A wrongly prepared or ruined item pays less (Poor x0.6); a perfect one pays more (x1.3).
- [ ] The end-of-day card shows "Doanh thu", "Chất lượng trung bình", "Hoàn hảo" and "Tiền đang có".
- [ ] Stop and Play again: the cash in hand is still there.

## Customers: patience and rush (G2, new)
- [ ] 18:00 to 19:00 customers arrive slowly; around 19:30 to 21:30 they come faster; after 23:00 it calms down.
- [ ] A customer you ignore leaves by themselves after roughly 1.5 to 3 minutes (Student 120 s, Family 180 s, Office 90 s, Regular 150 s). The failed buzz plays and the order disappears.
- [ ] Every order in the top-left list has a thin bar under it: green above half, orange below half, red below a quarter; it drains while you wait.
- [ ] While the pause menu or the order window is open nobody leaves.
- [ ] Once you pick the order up to carry it to the customer, they stop losing patience.
- [ ] Serve fast (while at least two thirds of the patience is left): the order pays 15 percent more.
- [ ] After three failed orders in a row, new customers come noticeably slower for a while.
- [ ] Day 2 and later are a little busier than day 1.

## Stock and supplier (G3, new)
- [ ] Under the clock a second line reads "Kho: Đá 30  Thạch 30  Bột bánh 20  Giấy gói 30" on day 1.
- [ ] Each drink sent to the stall takes 1 ice and 1 jelly; each cake takes 1 batter and 1 wrap. The numbers go down.
- [ ] When something runs out, the next order that needs it makes the customer leave at once (failed buzz).
- [ ] At the end of the day the summary card is followed by "Nhà cung cấp": cash, stock per ingredient, a "Mua 10" button each, and "Mở hàng".
- [ ] Buying takes the money and adds 10 units; the button is disabled when you lack money or the stock would overflow.
- [ ] Ice is 0 after the night (it melted); jelly, batter and wrap stay.
- [ ] Stop and Play again: stock and cash are kept.

## Reputation, upgrades, nights, goal, difficulty (G4-G6, new)
- [ ] Under the stock line a third line reads e.g. "Trời quang  |  ★☆☆☆☆  |  Mục tiêu 0/6" and the goal count goes up as you serve.
- [ ] End-of-day card ends with the goal result ("Đạt mục tiêu! Thưởng ..." or "Chưa đạt mục tiêu") and "Uy tín: ★..." with the point change.
- [ ] The shop shows stars, five upgrades (price, or "cần N sao", or "đã có"), a difficulty button, and tomorrow's night plus its goal.
- [ ] Buying an upgrade takes the money: bigger ice bin raises the ice capacity by 20, pantry raises the others by 20, wholesale deal makes packs 10 percent cheaper, golden board makes orders pay 10 percent more, string lights make customers wait 15 percent longer.
- [ ] Difficulty button cycles Thư thả / Bình thường / Cao điểm; the choice is kept after restarting.
- [ ] Day 1 and 2 are "Trời quang". Later nights vary: Rain (fewer customers), Hot night (more customers, 2 ice per drink), Power cut 21:00 to 21:20 (no customers arrive), Festival 20:00 to 21:00 (a rush), Inspector from day 4 (reward or fine at the end of the day).
- [ ] More stars bring customers a little faster.

## Icons and new drinks (new)
- [ ] The shop between days shows an icon on every ingredient row and every upgrade row, a money icon beside the cash, five star icons (full and empty) under "Uy tín", and tomorrow's night icon above the forecast text.
- [ ] The end-of-day card shows five star icons above the "Ngày mới" button.
- [ ] Every order in the top-left list has a small customer icon (student, family, office, regular) left of its patience bar.
- [ ] Customers also ask for the real menu flavours: Chanh dây at 2 stars, Xoài at 3, Đào at 4, Quả mọng and Mãng cầu at 5 (all "Trà Olong - ..." at 30.000đ). The shop forecast lists "Món thêm đang bán" and "Món kế tiếp". Any of the drinks is made the same way at the station (the flavour is only the order label for now); the order list and the held-item panel show the flavour name.

## Night light (new)
- [ ] Rain night: the street is noticeably darker and greyer all night, lamps unchanged.
- [ ] Power cut night, 21:00 to 21:20: the lamps go almost out and the light drops; no customers arrive; at 21:20 the light comes back smoothly.
- [ ] Festival night, 20:00 to 21:00: the lamps are brighter.
- [ ] Clear, hot and inspector nights look the same as before.

## Critic, helper worker, notices, real names (new)
- [ ] The first order list shows "Trà Olong - Chanh tươi" and "Lăn Truyền Thống" instead of the old placeholder names.
- [ ] From 3 stars a customer can be "Nhà phê bình" (rare): a notice says so. Serve him Perfect and fast: double pay and +25 reputation. Poor or slow: half pay and -20. If he leaves unserved: -15.
- [ ] The shop sells the upgrade "Nhân viên phụ" (3 stars, 350.000đ). With it, an order waiting at a table for about 8 seconds is taken and sent to the stall by itself (a notice shows). It costs 30.000đ at the end of every night (shown on the summary card). It has no icon yet.
- [ ] Short notices appear under the clock: ingredient ran out and the customer left, goal reached, a new star and the drinks it opens, helper took an order.

## Sound
- [ ] A soft bell when a drink is ready, a bright ding when a customer is served, a low buzz when a customer leaves.

## Stall (look from the seller side, then the customer side)
- [ ] Left wing: grill and cake tools. Middle hole: ice bin. Right hole: topping tray, long side running front to back.
- [ ] Nothing floats over a hole, nothing is buried in the counter.
- [ ] The red tea rack stands on the floor behind the counter and you can reach it from the player spawn.
- [ ] Run the menu "Check Assembled Stall Overlaps" and paste the result.

## Street
- [ ] You can walk off the sidewalk onto the road.
- [ ] Crates are open, upright, with tea bags standing inside.
- [ ] Truck has colour and detail (known issue until ART-FIX-014).
- [ ] Cat, chicken and dog stand in a row on the west porch.

## Tests
- [ ] Window > General > Test Runner: run EditMode and PlayMode. Click the Unity window first so it has focus. For every red test paste the exact failure message.
- Last verified on the Mac (2026-10-15, main at 25ff788): EditMode 732/732 green, including all `ART_STALL_001_*` and `GameplayBlockoutTests` 7/7. PlayMode 75/76: `DrinkWaveFlowTests.TC_ORDER_UI_MousePipelineActivatesEnterSendAndCloseWithoutRecapturingGameplay` fails with "Input System mouse press must reach the runtime button as a pointer event. Expected: 1 But was: 0" when the Unity window is not focused; re-run it with the window focused to confirm.
- Known budget miss: at x = 0 m there are 12 real-time lights within 30 m (budget 8). Tracked as a lighting task.

## Corner shop (emergency restock)
- [ ] While open, with the scarcest ingredient at 0, press R: 5 units arrive, cost is 2x unit price, notice appears.
- [ ] Press R again within 30 s: refused with a notice. Not enough cash: refused, no stock added.
- [ ] R does nothing in a menu, pause or title screen.

## Flavour colours
- [ ] On the active-order list, a drink order shows a round swatch in its liquid colour (basic drink amber; chanh dây yellow; xoài; đào; quả mọng dark red; mãng cầu pale green). Cake-only orders show no swatch.

## Service combo
- [ ] Serve 3 customers in a row, each not Poor and with at least a third of their patience left: the 2nd shows "Chuỗi phục vụ x2 +1.000đ" style notice, the 3rd gets more (5% per step, up to 25%).
- [ ] A Poor drink, a slow sale or a customer who leaves resets the streak. A new night starts at zero.
