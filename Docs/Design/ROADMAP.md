# Roadmap (product owner decision)

Order of work: finish the whole look (UI + art) and every function first. Rich gameplay (depth, balance, progression) comes after that.

## Phase 1: Finish the look
- Stall: counter, stations and props match the real stall photos (layout agreed: cake/grill on -x wing, ice bin centre, topping tray on +x, tea rack on the floor).
- Street: believable models for people, vehicles, animals, props; 500 m walkable street; day/night.
- UI art: HUD, menus, sign and menu board in the brand style.

## Phase 2: Finish every function (no depth yet)
- Flow: main menu, start, pause, settings, credits (F1 exists), end of day.
- Stall loop works end to end: order, drink, cake, serve, pay.
- Clock and opening hours 18:00-24:00 with fast-forward while closed (done).
- Save and load, audio, localisation, input rebinding.
- Stable: EditMode and PlayMode green, no Console errors, runs on the Mac build.

## Phase 3: Gameplay depth (after 1 and 2 are done)
- Economy and progression, customer variety, difficulty, events, goals.

Rule: no Phase 3 work starts until Phase 1 and 2 acceptance is signed off by the product owner.

## Remaining look and UI list (final sweep, one or two rounds)
Round A (art, Anti; lead reviews): ART-FIX-021 (stool under 1,500 triangles, drop VIP_Real), table rows, bar mat and cake props verified in Play; tea rack model when the owner supplies the file.
Round B (UI, lead; Mac session verifies): HUD and menu in brand style (fonts, colours, icons), menu board and shop sign, order toast style, day-summary art, title-screen background (stall at night), cursor and prompt icons, lighting budget at x = 0 (12 lights vs 8).
After Round B: owner plays `PLAYTEST_CHECKLIST.md`, signs off Phases 1-2, then `GAMEPLAY_DESIGN.md` is approved and steps G1-G6 start.

## Status on 2026-10-20 (lead)
Done in code, waiting for the owner's play test in Unity: G1 money and quality, G2 patience and customer kinds, G3 stock and shop, G4 stars, upgrades and drink flavours, G5 night events (numbers plus light), G6 daily goal and difficulty, UI icons.
Open, by owner of the work:
- Owner: real prices of the drinks and the cake (board photo `IMG_4932 2`); decide if each flavour needs its own tea bag and cup colour; play 3 nights and report balance.
- Anti: push `fix/ART-FIX-022` (seated pose, rider head, puddle light, street clearing); look of the two new drinks on the counter; the tea rack model when the owner supplies the file.
- Mac session: Play Mode checks (street ends after the gate fix, black lake, building in front of the stall, tree/person gone) once Unity MCP is connected.
- Lead (needs a Unity run first): PlayMode full-night smoke test, rain particles (needs the particle system module decision), corner shop restock, flavour-specific tea bags and cups.
