# Mini Mart — Living Backlog
**Updated:** 2026-07-04 (batch 4 — Farm-Market spec cherry-pick: store XP/level, tips, offline earnings)

## Batch 10 (verify on next run) — playtest integration fixes
- [ ] **Per-source storage racks** — the single depot is gone. Egg rack by the coop, tomato+ketchup racks by the plants, wheat+flour racks by the wheat farm, milk+cheese racks by the cow, bread rack by the oven. Each shows its own live n/cap badge; racks appear with their purchase pads. Player deposits per-item at the matching rack; farmer walks to the dominant item's rack; chef withdraws/deposits at the right racks.
- [ ] **Shelvers now physically fetch** — two-leg trip (rack → shelf) instead of withdrawing from thin air; with visible colored carry stacks.
- [ ] **Buyers pick items one per beat** — thought bubble now visibly ticks 1/4 → 2/4 → …; buyers/chef/farmer/shelvers all have carry-stack visuals.
- [ ] **Van no longer parks in the doorway** — pickup spot moved outside onto the grass below the entrance ((2.5, 4.5) instead of (2, 7)).
- [ ] **Bins work** — stand at a dustbin to throw away everything you carry (grey "x" emote).
- [ ] Note: "buyer paying without buying" — buyers only pay when they collected ≥1 item; a single cheap item rings up the $1.00 minimum-basket floor by design (GDD 6.1). If that floor reads as a bug in playtests, remove it from buyer checkout.

## Batch 9 — systematic logic test pass (see [TEST-REPORT.md](TEST-REPORT.md))
- [x] **30,116 simulated test cases, all passing** — curves, economy, storage, machines, farms, XP/unlocks, phone orders, 1,000 randomized buyers, pads, offline earnings, 12 Monte-Carlo full-loop worlds with per-tick invariants.
- [x] **Bug: machines destroyed product when output tray full** — now stall (Machine.cs).
- [x] **Bug: delivery-van money exploit** — van now pays + closes the order when fully loaded; drained orders can't be HUD-fulfilled (PhoneOrderManager/DeliveryVan).
- [x] **Bug: manual prices unclamped** — SetManualPrice now enforces the GDD ±50% band centrally (EconomyManager).

## Batch 8 (verify on next run) — bubbles, emotes, dairy chain
- [ ] **Thought bubbles** — buyers show a white bubble with a colored item chip + "have/want" fraction while shopping, "$" chip while queueing (ThoughtBubble.cs).
- [ ] **Emotes** — ":)" on every sale, "<3" when tipped, ">:(" on queue walk-out, "?" when an item was sold out (Emote.cs).
- [ ] **Dairy chain** — Cow Pen ($100 pad, includes Milk shelf) → Dairy machine ($175 pad, includes Cheese shelf). Milk $0.50 (unlock L2), Cheese $1.35 (L3). Farmer milks the cow in his rotation; Shelver A stocks milk, Shelver B cheese; player loads/collects the Dairy; levels save; upgrade panel has Dairy + Cow Pen rows.
- [ ] Note: only the player runs the Dairy machine for now (chef's chains remain ketchup + bread) — chef dairy support queued.

## Batch 7 (verify on next run) — REFERENCE FLOW RESTRUCTURE (see [REFERENCE-FLOW.md](REFERENCE-FLOW.md))
- [ ] **World starts nearly empty** — only tomato farm, tomato stand, Counter 1, depot. Coop, wheat farm, all 3 machines, their shelves, all 4 workers, and Counter 2 are bought via walk-over **purchase pads** (arrow + cost that drains while you stand on it). $10 starting cash.
- [ ] **Money stacks** — checkout piles physical cash beside the till; walk over it to collect (grants cash + store XP). No more auto-banking.
- [ ] **Carry scale 15→44** with taller stack visual and a red **MAX** badge when full; faster harvest cadence to match.
- [ ] **Purchases persist** in the save (`PurchasedPads`); bought pads restore for free on load.
- [ ] Buyers only wish for items whose shelf is actually purchased; thief/shelvers/player ignore unpurchased shelves.
- [ ] Next for parity: customer thought bubbles (icon + n/m), happy-face+heart on sale, milk/cheese content chain.

## Batch 6 (verify on next run) — playtest bug sweep
- [ ] **Visible cashiers** — an orange-uniformed cashier NPC now spawns behind the till when hired (store Lv 2 / counter 2 at Lv 4) and despawns if staffing changes.
- [ ] **Buyer spawn drought fixed (again)** — buyers who finished shopping with no open counter used to freeze forever and silently fill the 12-buyer cap. They now wait near the tills with patience, join the queue the moment a counter opens (or the player arrives), and walk out if nobody shows.
- [ ] **Queues form as a line** — spaced queue slots per counter; buyers shuffle forward as the line advances instead of stacking on one point.
- [ ] **Player-at-till radius** widened to 3.0 so manning the counter reliably collects cash.
- [ ] **Storage/machine showcase** — StorageBadge lists inventory over the depot; MachineBadge shows "in n/cap out n" over Blender/Mill/Oven; shelf badges already live.
- [ ] **Carry stack colors** — carried cubes tint to the item color. Limitation: a mixed stack shows the last item's color; per-cube coloring is queued below.
- [ ] **Delivery vans for phone orders** — van drives in with a WANTED list; stand near it with goods to load; leaves when filled or expired.

### Queued from UX/edge-case review doc
- [ ] "Inventory Full / MAX" blocked-action feedback (flash + floating text) instead of silent no-op.
- [ ] Sold-out U-turn: thought bubble + sad emote when a buyer's item runs out mid-walk.
- [ ] Coin-fly-to-HUD + "ka-ching" on checkout (juice pass, with harvest pops and restock slides).
- [ ] Tutorial arrows for the first 60 seconds (harvest → shelf → register → upgrade).
- [ ] Per-cube carry-stack coloring (track item type per slot in CarryVisual).
- [ ] Interactable highlight ring when the player is in range of a usable object.

## Batch 5 (verify on next run) — player agency + liveliness
- [ ] **Player proximity interactions (NEW — was completely missing!)** — stand at a farm to harvest, at the storage depot (new pallet-and-crates object at the worker drop point) to deposit, at a machine to load/collect, at a shelf to stock it. The "player does everything" pillar now exists (`PlayerInteraction.cs`).
- [ ] **Player-at-counter 3× checkout** — checkout now takes 1.2 s per buyer (0.4 s when the player mans the till). GDD 4 satisfied.
- [ ] **Customer personalities** — Normal / Impatient (fast, 12 s queue patience) / Bargain (small baskets) / Rich (big baskets, trolleys). Impatient buyers abandon long queues and walk out.
- [ ] **Store level-up popup** — "LEVEL UP!" panel with per-level unlock text (cashier at 2, thief warning at 3, bread + counter 2 at 4).

## Batch 4 (verify on next run) — see [FARM-SPEC-NOTES.md](FARM-SPEC-NOTES.md)
- [ ] **Store XP & level** — revenue earns XP ($1 = 1 XP, phone orders +25); store level now drives ALL unlocks (cashier, counter 2, items, thief) instead of the player's personal upgrade level. HUD shows `Lv N  x/y XP`.
- [ ] **Customer tips** — buyers who found everything tip 10–25% (20% chance).
- [ ] **Offline earnings** — away ≥2 min (cap 4 h): storage trickles eggs/tomato/wheat + offline sales coins; "WELCOME BACK!" panel with COLLECT on launch.
- [ ] From the spec, queued: customer personalities, patience/angry-leave, daily quests, level-up popup, daily rewards, more crops. Rejected: 2D pivot, watering/wither, Firebase, gems (reasons in FARM-SPEC-NOTES).

## Batch 3 fixes (verify on next run)
- [ ] **Farmer round-robin** — was egg-starved (hens always ready → never harvested wheat/tomato → whole chain dry, Shelver 2 had nothing to do). Now rotates coop→wheat→tomato, skipping full storages.
- [ ] **Chef self-fetch** — SelfFetch methods existed but were never called; chef now harvests tomato/wheat from the farms when storage is empty, so ketchup/flour/bread production runs from a cold start.
- [ ] **Shelf stock badges** — floating "n/cap" chips over every shelf (reference style); buying now visibly decreases stock.
- [ ] **Buyers pay for what they took** — checkout used to quote the *remaining wish-list* (i.e., items they FAILED to find). Now charges the collected items; empty-handed buyers pay nothing. Buyers also carry a visible stack now.
- [ ] **Save actually persists** — two causes fixed: (a) `Configure()` ran after load and reset all worker levels to 1; (b) force-killing an app never fires OnApplicationQuit. Load order fixed + autosave every 30 s + save on focus loss. Console logs "Save loaded — cash $X" on boot.
**Companions:** [GDD.md](GDD.md) (what to build) · [TDD.md](TDD.md) (how) · [ROADMAP.md](ROADMAP.md) (full build order)

This is the working triage list: every known bug, requirement gap, and UI/UX issue, ordered by priority. Check items off as they're verified in a build. The ROADMAP is the long-term plan; this file is "what's broken or missing *right now*."

---

## P0 — Verify on next build (fixes applied, need confirmation)

- [x] **Player movement** — CONFIRMED working (2026-07-03). Root cause was the 2D sprite prefab in `PlayerPrefab`.
- [ ] **Movement smoothness** — SmoothDamp input smoothing + analog throttle + faster turn added; verify feel on device.
- [ ] **Buyer visuals** — bare yellow capsules replaced with full built characters in 7 palette colors.
- [ ] **Buyer flow** — spawn interval was 8–20 s; now 1.5–3 s (reference pace), capped at 12 concurrent.
- [ ] **Thieves actually spawn now** — `SpawnThief` used to no-op because `ThiefPrefab` was never assigned; runtime-built dark-outfit thief added.
- [x] **Debug status line removed** (movement confirmed).
- [ ] **UPGRADES panel restyled to reference** — green shell, Workers/Machines/Animals tabs, colored row bars, green cost / grey Maxed buttons.
- [ ] **PRICES panel** — green shell, spacing fixed, taller panel; full reference redesign still pending (P2).
- [ ] **Buyers appear on device** — CapsuleCollider stripping fixed via `Assets/link.xml`.
- [ ] **No shader errors on device** — Standard shader force-included (GraphicsSettings) + null-safe material helpers.
- [ ] **HUD text visible everywhere** — LegacyRuntime font replaces removed Arial (HUDBuilder, both panels).
- [ ] **Upgrade panel no longer max-upgrades machines for free** — display path is read-only now; spend-before-upgrade enforced.
- [ ] **Phone orders fulfillable** — quantities capped by storage capacity; payout = shown value; max 2 active; FULFIL greys out without stock; DISMISS actually declines.
- [ ] **Event pacing** — phone/thief timers restored to spec 4–5 min (`PriceCatalog.FastEventTimers = true` for quick testing).
- [ ] **Panels** — UPGRADES/PRICES mutually exclusive; slider fill bar no longer a giant green block.
- [ ] Once movement is confirmed: **remove the yellow debug status line** (HUDBuilder `RuntimeStatusText` + `ReportDebugStatus` in PlayerInputHandler).

## P1 — Requirement gaps vs GDD (core gameplay)

- [x] **Buyers never left after checkout** (found 2026-07-03) — they froze at the counter forever; with the 12-buyer cap that eventually blocked ALL spawns. Now they walk to the exit door and despawn.
- [x] **Player-at-counter override** — `ManualOverride` was never set anywhere, so Counter 1 was NEVER open at L1 and no buyer could check out. GameManager now opens a cashierless counter while the player stands within 2.2 u. (3× speed bonus still TODO.)
- [x] **Trolley visual** — buyers with 5+ items now push a runtime-built cart (basket, handle, 4 wheels). Pooled trolley rack still TODO (polish).
- [x] **Price elasticity** — items >+20% over base get skipped 35% of the time at basket-build; any ≥10% discount boosts spawn rate +50%. Angry emote still TODO (polish).
- [x] **Thief gating** — TheftManager now requires Player L3+, synced on load and level-up.
- [x] **Save coverage** — worker/machine/coop levels + manual shelf prices now save and load (GameManager fills the existing GameSaveData dictionaries).
- [x] **Unlock levels aligned** — Bread moved to L4 per GDD §7.
- [ ] **Map layout doesn't match GDD §10** — no aisle structure, no 4 secondary exit doors, counters not at entrance/exit positions. `MapLayout` should be the single source of anchors (TDD §3).
- [ ] **Player level vs upgrade level are conflated** — unlocks key off the player's *upgrade* level. Decide: keep conflation (simpler) or add XP.
- [ ] **Player-at-counter 3× checkout speed** when manning a counter (GDD §4).
- [ ] **Offers** — no duration/expiry and no shelf banner yet.
- [ ] **Manual stock manipulation** — verify player can move items storage↔shelf by hand.
- [ ] **Bin overflow routing** — deposits over storage cap should route to bins (GDD §5). Verify.
- [ ] **Split Stack/Speed upgrade tracks** per entity like the reference (needs two curves per role + save keys).

## P2 — UI/UX issues

- [ ] **Phone-order card can't show 2 concurrent orders** — HUD shows only the latest; second active order is invisible. Stack cards or add a queue badge.
- [ ] **Phone card lacks live countdown** — shows static "Time: 10 min"; should tick down (GDD §11).
- [ ] **Price rows: slider has no handle/thumb** — value is settable only by tapping the bar; add a draggable knob or −/+ steppers (GDD wireframe uses steppers).
- [ ] **Upgrade rows: no player-cash context** — buttons grey out correctly, but show why (cash vs cost) — e.g. red cost text when unaffordable.
- [ ] **Storage readout is a raw text column** — replace with icon grid quick-look (GDD §11); at minimum align counts (shelf vs storage split).
- [ ] **MENU button doubles as pause with no visual state** — pressing it pauses with full-screen overlay; label it PAUSE or add a settings sheet (sound toggles, save & quit — GDD §11).
- [ ] **NET button always visible** — GDD: appears only when a thief is active and in range.
- [ ] **Joystick floating behavior** — verify the new hide-until-touch floating joystick feels right on device; dead zone 0.15.
- [ ] **Level display "Lv 1" has no XP/progress context** — depends on the P1 level-system decision.
- [ ] **No tutorial** — first-session task banner (harvest → shelf → sell → upgrade), flagged in save (ROADMAP task 259).

## P3 — Polish gap vs the reference (My Mini Mart)

- [ ] Character models: replace capsule+sphere blobs with proper low-poly characters (or paid asset pack) — biggest visual delta.
- [ ] Carried-item stack: per-item colors/shapes instead of white cubes; wobble/lag on turns.
- [ ] Coin burst at checkout + magnet-to-HUD money fly.
- [ ] Harvest pop, machine smoke, upgrade glow particles (VFXManager, pooled).
- [ ] Audio: BGM + pickup/coin/upgrade/machine/thief/phone SFX (AudioManager) — none exist yet.
- [ ] Camera: dynamic zoom with carry stack; screen shake on thief catch.
- [ ] Upgrade pads in-world (stand-on-pad to upgrade) like the reference, instead of only the menu.

## Decisions needed (design conflicts to resolve — pick one, then align code+GDD)

1. **Pricing rule**: GDD v2 says one-of-everything totals **under $1** (prices $0.04–$0.30). Code implements the opposite reading: a **$1 floor** per basket with prices $0.30–$1.50. Screenshots show the code prices in use. → Current recommendation: keep code prices (they're live and balance the $50+ upgrade ladder better); update GDD §6.1 to match.
2. **Store level source**: separate XP-based store level vs player upgrade level (see P1).

## Housekeeping

- [ ] Delete stale `MiniMart.app` (July 1 build of an empty scene) from project root.
- [ ] Delete `Assets/MiniMartgame.unity` (old empty scene — already caused one lost debugging session).
- [ ] Remove unused packages: `com.unity.purchasing` (ships in iOS build!), `com.unity.ai.assistant`, `com.unity.ai.inference`, `com.unity.multiplayer.center`.
- [ ] Remove `Assets/Jovial Games`, `Assets/30 2D Game Kit`, `Assets/Layer Lab` asset packs if unused (source of missing-script warnings; one already broke the player).
- [ ] The editor console's repeating `LastBuild.buildreport` FileNotFoundException is a harmless Unity Project Auditor bug — ignore.
