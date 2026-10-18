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
- [ ] Serving a drink adds about 25.000đ, a cake about 20.000đ, a drink plus cake 40.500đ (10 percent combo discount) at Good quality.
- [ ] A wrongly prepared or ruined item pays less (Poor x0.6); a perfect one pays more (x1.3).
- [ ] The end-of-day card shows "Doanh thu", "Chất lượng trung bình", "Hoàn hảo" and "Tiền đang có".
- [ ] Stop and Play again: the cash in hand is still there.

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
