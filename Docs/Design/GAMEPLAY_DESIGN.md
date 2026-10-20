# Gameplay design: Trạm Chanh (Phase 3 draft)

Status: DESIGN ONLY. No code starts until the product owner signs off Phases 1-2 (see ROADMAP.md) and approves this document.
Numbers are first guesses, marked TBD, and live in `BalanceConfig` / ScriptableObjects, never in code.

## 1. Fantasy and pillars
You run a night lemon-tea and roll-cake stall on a 500 m street, 18:00-24:00. One person, two hands, a queue that grows.
1. **Rhythm over speed.** A good night feels like a kitchen rush you can read and plan, not random button mashing.
2. **Your hands are the game.** Every step (tea bag, jelly, ice, shake, wipe, batter, roll, wrap) stays physical. Progression changes what you can make and how forgiving it is, never removes the steps.
3. **Small decisions every night.** What to stock, what to price, which upgrade, which customer to prioritise.
4. **The street is alive.** Weather, events and regulars change each night, so night 8 differs from night 2.

## 2. Core loop (what the player does in one night)
Prep (closed, fast-forward) -> Open 18:00 -> take orders (tables + takeaway vehicles) -> make drinks and cakes -> put on the ready shelf -> serve -> collect money -> 24:00 summary -> spend money -> next night.

Already built and kept: ordering UI, ready shelf, 10 tables, takeaway vehicles, tea rack stock, day clock, end-of-day panel, saved totals.

## 3. Orders and customers
| Type | Behaviour | Patience (TBD) | Pay |
|---|---|---|---|
| Student (dine-in) | Single drink, orders fast | 90 s | base |
| Family (dine-in) | 2-4 items, mixes cake + drink | 150 s | base x items |
| Office worker | Takeaway vehicle, impatient | 60 s | base + rush bonus |
| Regular | Same order each visit, remembers you | 120 s | base + tip, builds loyalty |
| Food critic (rare) | Quality judged hard, no waiting tolerance | 100 s | big tip or review penalty |
- Patience drops while waiting; at 0 the customer leaves (`FailureReason.CustomerLeft`, already in code).
- Wrong item delivered: customer keeps waiting but pays 50 percent (TBD), never silent fail.
- Arrival rate follows a curve: slow 18:00, peak 19:30-21:30, tail off after 23:00. Day number scales the peak.

## 4. Quality and money
- Each item gets a quality 0-100 from the steps: correct jelly and ice portion (inside tolerance), shake time, wiped cup, cake not burnt, wrapped neatly.
- Price = base x quality tier (Poor 0.6, Good 1.0, Perfect 1.3). Perfect streaks add a small combo tip.
- Speed bonus: served in the first third of patience = +15 percent.
- Prices are the owner's real menu (VND): basic drink Trà Olong - Chanh tươi 20k, other Trà Olong flavours 30k, roll cake (Lăn Truyền Thống) 20k, no combo discount.
- Costs: tea bags, jelly, ice, batter, sauce, wrap. Running out is a real problem (tea rack already has capacity and stock).

## 5. Stock and restock (the planning layer)
- Tea rack capacity and stock already exist. Add stock for ice, jelly, batter, wrap, sauce.
- Between nights (and during closed fast-forward) a Supplier screen sells stock. Buying more than you can store is not allowed; ice melts overnight.
- Mid-night shortage: leave the stall to the corner shop 40 m away (walk time = cost) or turn the customer away.

## 6. Progression (stars, upgrades, unlocks)
- **Reputation 0-5 stars**, moves with served rate, quality and complaints. Higher stars raise arrival rate and unlock regulars and the critic.
- **Upgrades** (bought with money, shown in the supplier screen):
  - Second ice scoop and bigger ice bin (less restocking).
  - Larger grill (two cakes at once) and a timer beep on the grill.
  - Better shaker (shorter hold time).
  - Extra table sign, string lights, shade umbrella (raise tip and patience).
  - Helper worker (late game): automates one fixed step, never the whole order.
- **Menu unlocks** by stars: new tea flavour, new cake filling, seasonal drink.
- No permanent loss: bad nights cost money and stars slowly, never wipe the save.

## 7. Variety: weather and events (one per night, announced at 18:00)
- Rain: fewer walk-ins, more vehicles, wet-floor slows the player slightly.
- Hot night: ice melts faster, drinks sell more.
- Power cut at 21:00-21:20: lights off, grill off, ordering still works.
- Street festival: huge rush at 20:00 with simple orders.
- Health inspector (rare): rewards a clean stall (wiped counter, no stray items), penalises mess.
- Tet / holiday weeks: special menu goal.

## 8. Goals and meta
- Daily goal shown at 18:00 (example: serve 25 customers, earn 500k, 3 perfect drinks). Beating it gives a bonus and a star fraction.
- Weekly milestones: reach 3 stars, own the larger grill, serve 5 regulars.
- Day summary extends the current panel: served, failed, quality average, tips, costs, profit, new unlock, tomorrow's weather forecast.
- Endless play with soft difficulty curve. No game over. Optional 30-night "season" with a score.

## 9. Difficulty
- Difficulty = arrival peak x patience scale x stock cost. Three presets (Chill 0.8, Normal 1.0, Rush 1.3).
- Dynamic ease: after 3 failed customers in a row, the next arrival waits longer (silent assist).

## 10. Controls and feel (no change to what exists)
Keep E / F / hold actions. Add: audio cues per step, ticket pile that grows physically on the order rail, number of waiting customers visible on the street.

## 11. Build plan (when approved)
| Step | Content | Code area | Tests |
|---|---|---|---|
| G1 (done, awaiting owner playtest) | Money, price, quality score, day revenue in summary | Core + Bootstrap + UI Menu | EditMode: pricing, quality tiers, progress money |
| G2 (done, awaiting owner playtest) | Patience, customer kinds, arrival curve, speed bonus, failure assist | Core + Customers + Bootstrap | EditMode: patience book, kinds, arrival plan, price bonus, director scale |
| G3 (done, awaiting owner playtest) | Stock for ice, jelly, batter, wrap + supplier screen + save (tea bags stay on the rack; corner-shop walk not built) | Core + Bootstrap + UI Menu | EditMode: stock book, supplier, panel text |
| G4 (done, awaiting owner playtest) | Reputation stars + five upgrades gated by stars (menu unlocks with new recipes not built: they need art) | Core + UI Menu | EditMode: reputation, upgrade book |
| G5 (done, awaiting owner playtest) | Rain, hot night, power cut, festival, inspector (light only: rain darker, power cut dims the lamps, festival brightens them; no rain particles yet) | Core + Bootstrap | EditMode: night plan |
| G6 (goal and difficulty done; balance pass needs the owner playtest; PlayMode full-night smoke not written) | Daily goal + three difficulties | Core + UI Menu | EditMode: goal, difficulty |
Each step ships on its own branch, EditMode/PlayMode green, and the owner plays it before the next step.

## 12. Open questions for the owner
1. Money unit and price list (VND values above are guesses).
2. Is a helper worker wanted, or must the player always do every step?
3. Should the critic and inspector exist in v1, or later?
4. Season length (30 nights) or endless only?
5. Does the shop corner restock (walking away) feel fun, or only a menu screen?
