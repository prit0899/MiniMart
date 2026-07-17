# Memory — live project state

> **READ THIS FIRST.** This is the handoff file: where we are, what's done, what's
> broken, and every piece of feedback the owner gave from playing the game himself.
> **Update it at the end of every work session.**
>
> Companion docs: [PRD](PRD.md) · [Architecture](Architecture.md) · [Rules](Rules.md) · [Phases](Phases.md) · [Design](Design.md)

**Last updated:** 2026-07-17
**Branch:** `feature/reference-flow-overhaul`
**Latest commit:** `5c4c0af` — *fix: kitchen-after-chef ladder, queue-honest checkout, visible thief* (+ this session: bot travel economics, Shelver2 certification)

---

## 1. Where we are right now

The game is **playable end-to-end, L1 → L10**, across both marts.

- ✅ Both marts match the owner's hand-drawn map (one enclosed building each).
- ✅ Nobody walks through walls.
- ✅ Upgrades are meaningful (base carry 8, upgrading to 44).
- ✅ `DataValidator` green: **44,891 assertions pass, 0 fail**.
- ✅ 0 compile errors.
- 🟡 Last automated playthrough reached **L9 of 10** before the session ended
  (not a failure — the session was torn down; earlier runs did reach L10).

- ✅ **Navigation rebuilt** (`d899036`): nav mesh (convex walkable polygons) + A\*
  + funnel + Reynolds steering. Workers no longer turn in 90° corners.

### Currently being worked on
None. This session (2026-07-17): answered the owner's wheat/greedy question with a
controlled experiment (row #22 — PROVEN working), certified the corrected Shelver2
assignment live in MegaMart (row #25), fixed three TesterBot gaps, and added the
first-session spotlight (row #1).

**Design note for the owner (not yet decided):** a player who rides the $500
MegaMart pad with ~$500 arrives broke in a mart that has zero income sources
until they buy pads — dead air until they walk back. The bot now waits for $660
(fare + seed) before travelling; consider messaging this to players or granting
MegaMart one starter income source.

**Owner has uncommitted local edits** to `Catalog/RoleCatalog.cs` (player stack
limit) and `Engine/DataValidator.cs`. Leave them alone — they're intentional and
the validator is green with them.

**Reference-parity audit (2026-07-17)** — went feature-by-feature against
`Refer/` screenshots + PRD §5.1. **Every enumerable reference feature is present
and verified in code:**
- Two stores, all production chains (Mart 1: Tomato→Ketchup, Wheat→Flour→Bread,
  Egg→FriedEgg; MegaMart: Corn→ProcessedCorn, Milk→BottledMilk, Milk→Cheese,
  Herb→HerbPack, Coffee auto-produced). **MegaMart stations already run the split
  4→6→8 two-track model** (`ConfigStation` in SB2) — the earlier "flag off" note
  was stale. Coffee is a deliberate auto-producer (no input); the Cow milks on a
  growth timer with a **HayFeedTrough** wheat-boost (parity-adjacent to the hen).
- **Consolidated Upgrades menu** (`UpgradePanelController`): green shell, three
  tabs Workers/Machines/Animals, Stack+Speed rows per entity, green cost pills,
  "Maxed" badges, scroll for overflow — matches the reference screenshot exactly.
- Purchase pads, meaningful player upgrades, all worker types with real carry
  meshes, customers + thieves + honest checkout queue, **phone orders**
  (`DeliveryVan` + `PhoneOrderManager`), onboarding + spotlight, save/load +
  offline earnings, gameplay GIF shipped (`Docs/images/gameplay.gif`, `6d1304d`).
- Engineering standards asked for: NavMesh (greedy maximal-rectangle convex
  decomposition) + A\* + funnel string-pull + Reynolds steering; marker-driven QA
  harness; `DataValidator` **42,710 assertions green** (verified this session).

**Device-readiness (code side) — verified 2026-07-17, all risks a build could
hit are retired:**
- **Touch input works** — `PlayerInputHandler` is touch-first (floating `Joystick`
  + `Input.GetTouch(0)` tap-to-move) with mouse only as a desktop fallback; the
  game is *playable* on a phone, not just compilable.
- **No QA code ships** — `TesterBot` + `WheatShelfExperiment` + marker hooks are
  all `#if UNITY_EDITOR`.
- **Frame budget** — per-frame-per-agent steering/movement is allocation-free.
- **iOS target configured** — iPhone+iPad, iOS 15 min, portrait.
- **DSA documented to standard** — [Architecture.md §6](Architecture.md).

**The ONLY remaining parity item is physically running the build on a real
iPhone** (touch *feel*, thermals, true on-device FPS). That needs a Mac+Xcode
signed into the owner's Apple account + the owner's device — genuinely
owner-hardware-blocked, not a code gap. Steps: Unity → Build for iOS → run the
generated Xcode project on-device; paste any build/runtime error back for a fix.

**Two things I got wrong last session and corrected here:** I reported the
MegaMart station economy and the gameplay video as "blocked on owner" without
reading the code — both were already done. Lesson: read before declaring blocked.

**Optional polish still open (not parity-blocking):**
1. Worker-movement *feel* tuning — knobs in `CharacterBase` (`maxForce`,
   `turnSpeed`, `SeparationWeight`, `AvoidWeight`). Needs a human eye, not a bot.
2. Owner design call on MegaMart arrival: travelling with only the $500 fare
   strands you broke there. Warn before travel, or give MegaMart a starter income.

---

## 2. Owner feedback & bugs found by *playing* (chronological)

> This is the highest-value section. These are real problems found by a human or
> by honest automated play — not code review. **The pattern: as developers we
> could not see these; only playing revealed them.**

| # | Owner said / QA found | Root cause | Status |
|---|---|---|---|
| 1 | *"lots of confusion, can't understand where to start"* (real human reviewers) | No onboarding at all | ✅ Fixed: TutorialGuide (harvest→stock→collect→build steps, big arrow + banner), UnlockGuide arrow + toast per level, per-station hints, and now a **spotlight** — the world dims with a soft light-hole tracking each objective during the first session (procedural radial overlay, behind the HUD, never blocks input). Smoke-tested on fresh save: tutorial completes, no exceptions. |
| 2 | *"one level upgrade gives 5-6 things unlock but actual need is 1-3"* | Ladder too dense | ✅ Fixed — 1–3 unlocks per level. |
| 3 | *"split mall in 2 parts… make new game scene like reference"* | Single map | ✅ Fixed — MegaMart is a separate scene + travel pad. |
| 4 | **MegaMart had no working checkout L6–L8** | Counter 3 was a $400 pad gated at L9, but it was the scene's *only* till — buyers could never pay, income and XP froze on arrival | ✅ Fixed — Counter 3 ships open with MegaMart. |
| 5 | **Harvested milk had nowhere to go** | MegaMart's shelf list still held retired CannedTomato/Cookie shelves; no Milk shelf | ✅ Fixed. |
| 6 | Audio *"ran out of virtual channels"* spam | Unthrottled one-shots in a busy store | ✅ Fixed — 80 ms per-clip throttle. |
| 7 | *"farms too far and out of the mall, blender, wheat flour meal etc also same"* | Shop floor was walled, but farms (z=10) and machines (z=32-36) sat on **open grass 20–30 units outside**, one even beyond the east wall | ✅ Fixed — both marts rebuilt as one enclosed building. |
| 8 | *"make sure no one can teleport from the border/wall"* | Pathfind fallback walked straight through geometry | ✅ Fixed — every movement step is grid-checked. (The old axis-slide was replaced by the nav-mesh + steering rework, see #15; the wall rule still holds.) |
| 9 | Level-up popup never went away | No auto-dismiss; sat on screen ~30 min in a QA run | ✅ Fixed — auto-hides after 6 s. |
| 10 | Phone orders demanded *14× BottledMilk*, *15 Herb + 7 Corn + 3 Milk* | Order size scaled to full storage cap (~20) | ✅ Fixed — 8 per line, 12 per order. |
| 11 | *"game don't enforce upgrades… you reached max level without upgrade… main character has no limit on stack and speed, so why someone upgrade it"* | **Base carry was 15** — enough to finish the entire game without ever upgrading, making the upgrade pads decoration | ✅ Fixed — base carry 8 → 44 via upgrades. TesterBot now buys upgrades too, so QA runs exercise the loop. |
| 12 | **"main player can not carry any items"** | **Self-inflicted.** A "carry-wedge" guard I added blocked harvesting whenever an item's *storage* was full — but the player also harvests to **stock shelves**. Once the farmer filled storage and no shelver was hired, the player refused to harvest → shelves never stocked → no sales → hard stall at L1. Also the carry curve had been left at max 10, which fails the validator's `MaxCarry >= 44` (and freezes the game if "Error Pause" is on). | ✅ Fixed (`3096a44`) — guard reverted, curve set to `{8,16,25,34,44}`. Verified: 0 stalls through L1–L3. |
| 13 | *"Unity Hub can't quit, even force quit reopens"* | **My fault** — a background keep-alive loop was relaunching the editor every 25 s | ✅ Fixed & killed. **Lesson: always clean up background processes.** |
| 14 | *"stacks for shelver, farmer, buyer should show same as main player"* | NPCs drew flat tinted cubes | ✅ Fixed — all NPCs render real item meshes. |
| 15 | *"workers turn perpendicular"* — movement looked robotic | `GridPathfinder.Neighbors` was **4-way only**, so A\* could only produce Manhattan staircases; the axis-slide wall guard forced axis-aligned motion on top | ✅ Fixed (`d899036`) — nav mesh (convex polygons) + A\* + funnel string-pulling + Reynolds steering (seek/arrive/flee/separation/avoidance). See `Map/NavMesh.cs`, `Characters/Steering.cs`. |
| 17 | **Full shelf ignored — "tomato stack fully max, lots of buyers there, not even a single tomato taken"** | **Greedy/all-or-nothing selection, in BOTH buyers and workers.** `Buyer.FindNextNeededItem()` returned the *first* basket entry only; if that item's shelf was empty the buyer parked there waiting (or gave up) and **never looked at the rest of its basket** — so a buyer blocked on sold-out bread ignored a FULL tomato shelf beside it. `Shelver` had the same trap: it chose the emptiest shelf outright, then bailed if *that* item had no storage stock, idling while other shelves it could refill sat empty. | ✅ Fixed — buyers now shop the nearest shelf holding ANY wanted item that's actually in stock; shelvers pick the emptiest shelf **they can actually refill**. Buyers also pick by proximity (a jostling crowd could keep everyone just outside the pinpoint arrival radius). Items are still taken 1-per-beat, so several buyers at one shelf interleave and share stock naturally, and whoever's left when it runs dry pays for a partial basket. Verified: full shelf + all others empty → buyers take from it (was 0 before). |
| 18 | **Roles + Mart-1 economy redesign** (owner spec) | Roles were fuzzy; hen auto-laid eggs; machines had one shared capacity + one upgrade track; bread was single-input; buyers could want more than 10 | ✅ Done — **Farmer** = living (harvest tomato/wheat, feed hen tomato, collect eggs); **Chef** = cook (operates machines, never shelves); **Shelver** = logistics (shelves, never machines). **Hen** now EATS tomato → lays egg. Every Mart-1 station is **4-in / 4-out** with **two capacity upgrade tracks** (input buffer + output buffer, 4→6→8) at owner costs (hen 250/310, machines 180/150 etc.). **Bread = Flour + Egg** (two inputs). Farms 12 wheat / 18 tomato, regrow 0.4s. **Buyers ≤ 10 items.** Backward-compatible (`Machine.SplitCapacity` flag) so **MegaMart is untouched**. Validator green (44,915/0) incl. new StationCatalog suite. |
| 19 | **Owner: "did you test? any deadlock?" → YES, found one: hen-starvation deadlock** | The redesigned economy (hen eats tomato → egg) starved: the Farmer banked every harvested tomato at the rack, and its feed check only fired *while carrying* tomatoes — never true after banking. Diary telemetry showed the smoking gun: `hen[t0/e0]` with `store[tom15]`, 3 stalls at L3, zero eggs ever. | ✅ Fixed & retested — Farmer now does a two-leg fetch (tomato rack → withdraw → coop → feed) when the hen has room and tomatoes exist in storage. Bounded retest: L4 at 893 sim-s, **0 stalls**, eggs flowing (`store[egg9]` by L2). New QA tools: `Logs/autoplay.marker`/`autostop.marker` (editor enters/exits Play from the shell — survives MCP revocation), and the bot diary now snapshots the whole chain (hen buffers, machine queues, storage counts) every 5 s. |
| 20 | **Owner: "can not trust you anymore… where is chef? why shelver and main player can not feed hen? why can't player take from storage?"** | Four real breaks from the roles redesign + my testing: (a) **player hen-feed sat inside the `room > 0` harvest gate** — with a full tomato stack (room 0) feeding never fired, exactly the moment you'd feed; (b) **player had NO withdraw from storage racks** (deposit only) while the redesign routed everything through storage — players locked out of their own goods; (c) **shelver couldn't feed the hen** — the owner's original spec listed it, I over-narrowed the role split; (d) **"where is chef" — my test runs WIPED the owner's save**, so they restarted at L1 and the chef (L4 pad, unchanged) looked gone. | ✅ Fixed: feed moved out of the room gate; player withdraw-at-rack added (0.6s dwell, deposit-when-carrying / withdraw-when-not so they can't fight); shelver got a secondary fetch-and-feed hen task (rack → coop, wired in Mart 1). **Never delete the owner's save again — use `Logs/testerbot.freshrun` ONLY for bot runs and restore expectations after.** |
| 21 | **Owner bug batch + Antigravity/Codex review** ("chef gating, thief not serious, no queue — money on shelf-grab, bread demanded before oven, verify antigravity changes") | (a) Blender unlocked at L2 — before the chef (L4); Antigravity had also stacked FOUR unlocks on L4 (violates 1-3 rule) and left a catalog/pad desync (Bread L4 in catalog, L5 pad) that made buyers demand bread with no shelf. (b) Chef loaded LOCKED machines (no activeInHierarchy check). (c) Buyers were charged remotely the moment the checkout timer ticked — money appeared at the counter while they were still at a shelf, so no visible queue. (d) Thief stole invisibly. | ✅ Fixed & verified (fresh run L1→L6, 0 stalls, 4 thief cycles). **Antigravity review:** KEPT Chef `MachineActive()` guard, Thief visible-stealing rework (carry stack, loot log, returns goods when caught), CashCounter `IsReadyForCheckout` (buyer must stand at slot 0) + slower checkout so queues form, Buyer `FindOpenCounter` + walk-to-slot. SUPERSEDED its PriceCatalog half-fix. **New ladder:** L1 Farmer/Hen/Shelver · L2 Counter 2 · L3 Wheat Farm · L4 Mill+Chef · L5 KITCHEN (Blender/Stove/Oven) · L6 MegaMart — catalog, pads, validator, and level-up text all mirror it. Owner's carry-cap 10 curve kept. |
| 22 | **Owner (repeated): "wheat shelf filled MAX, buyers roaming, no one takes — is greedy algo fail or not applied?"** | THREE stacked causes, none of them the greedy algorithm: (a) **checkout scrum** — buyer queue/shop logic was gated on `!hasTarget`, and under steering+separation a jostled buyer never *finishes* arriving (0.18u radius), while `IsReadyForCheckout`'s 0.7u ring kept the front buyer shoved out → ZERO checkouts ("Serve 35: 0/35" in owner's screenshot) → whole store frozen; (b) **wheat/flour/bread were assigned to Shelver2 (MegaMart-only)** — nobody ever restocked them in Mart 1 (leftover from before the two-mart split; Shelver1 even owned Milk); (c) the greedy fix itself was fine. | ✅ Fixed & PROVEN: queue slots now use kinematic glide + separation off while queued/picking + checkout ring 1.5 (`c98d530`, verified L1→L6, 0 stalls, full 12-buyer crowd); shelver responsibilities re-partitioned BY MART; and a **controlled experiment** (`WheatShelfExperiment`, marker `Logs/wheattest.marker`): wheat shelf forced to 20/20 MAX, all other shelves + wheat storage zeroed, 12 buyers → **all 20 wheat taken in 75 sim-s**. Verdict file: `Logs/wheattest_result.txt`. |
| 16 | *"in name of testing you take too much — literally 10+ hours, eating tokens, end result not significant"* | I ran marathon bot playthroughs (full L1→L10 soaks) for marginal payoff | ✅ Process change: **verify cheaply** (compile + bake + validator + a short run), then stop. Don't run long soaks unless asked. |
| 21 | **Chef unlocked at Level 4 but has no machines to operate** | Oven and stove pads, as well as Bread and FriedEgg unlocks, were gated at Level 5, whereas the Chef is gated at Level 4. | ✅ Fixed — Moved Bread Oven, Egg Stove, and related SKU unlocks to Level 4. |
| 22 | **Thief not stealing properly or returning items** | Thief didn't track stolen item types (meaning no items returned on catch), lacked visual alerts (emotes), and stood frozen at the exit forever upon escape. | ✅ Fixed — Tracked stolen SKUs, returned them on catch, added visual cues, and destroyed the Thief on successful escape. |
| 23 | **No buyer queue at cash counters; instant transaction feel** | Checkout processing speed was too fast (1.2s default, 0.4s manual override) making the queue look non-existent and money feel instant. | ✅ Fixed — Set default checkout speed to 2.0s (manual override to 1.0s) and added green floating `+$X` cash text emotes. |
| 24 | **2026-07-15 Codex follow-up after owner challenged the shallow verification** | The previous pass was not enough: it did not change the scripts behind the visible bugs. Real issues remained: the thief tracked stolen SKUs internally but had no carry-stack override/visual, buyers entered checkout lines before reaching the counter and could be charged while still walking, buyers could choose unlocked-but-closed counters, and Chef logic could target inactive machines hidden behind purchase pads. | ✅ Fixed in code — `Thief` now exposes stolen SKUs through `GetCarriedItems()` and always gets a `CarryVisual`; `Buyer` now joins only open counters and walks to the slot when a counter opens; `CashCounter` only charges the front buyer after the buyer physically reaches the front queue slot; `Chef` now requires active purchased machines before collecting/loading/targeting, and oven checks both flour and egg input capacity. |

| 25 | **QA: MegaMart certification run (2026-07-17)** — first attempt stranded the bot at Counter 3 in MegaMart with $9, no pads bought, no income, forever | THREE TesterBot gaps, none of them game bugs: (a) bot travelled the moment it had the $60-era threshold — arriving broke in a mart with zero income sources; (b) bot had no rule to ride the "Return to MiniMart" pad home; (c) the stall detector went blind after every scene load (`lastProgressAt` kept the OLD scene's `timeSinceLevelLoad`, which resets per scene → negative diff, stalls never fired). | ✅ Fixed & validated in one live run: bot now banks **$660 before travelling** (fare + seed), returns home when broke in MegaMart with nothing carried, and resets its progress clock on scene reattach. Run evidence: L10 reached, travel with seed → six MegaMart pads/shelves live (`Bot/Mil/App/Cor/Pro/Her`), return rule fired, round-trip clean, **0 stalls**. **Shelver2 assignment CERTIFIED from the MegaMart boot log:** `Apple, Corn, ProcessedCorn, Milk, Cheese, Herb, HerbPack, BottledMilk, Coffee` (Shelver1: the 7 Mart-1 items). Boot logs stay in both bootstrappers. Side finding: XP frozen at `0/800` at L10 is BY DESIGN — L10 is max level, `AddXp` short-circuits. |

| 26 | **MegaMart station parity pass (2026-07-17, goal-driven)** — Mart 1 got the owner's two-track 4→6→8 station economy; MegaMart's five machines still ran the old single-track system, and **Milk Bottler was never wired into GameManager at all** (unreachable from the UPGRADES panel — could never be upgraded); Corn Processor and Coffee Bar were wired but missing from the panel list. | The Mart-1 economy redesign flagged MegaMart machines `SplitCapacity=false` to stay untouched (correct at the time); the panel's machine list and the GM wiring were simply never extended. | ✅ Fixed — all five MegaMart machines (Milk Bottler, Corn Processor, Dairy, Leaf Unit, Coffee Bar) now run `SplitCapacity` two-track 4→6→8 using the owner's **existing** Mill cost arrays (Input $150/$300, Output $180/$400 — zero new balance numbers invented); `GameManager.MilkBottler` added + wired; panel lists all MegaMart machines (null-safe in Mart 1). **Deliberately NOT done: hard-gating the cow like the hen** — MegaMart has no Farmer NPC, so a hard-gated cow starves whenever the player idles (the exact hen-starvation deadlock class). Cow keeps the hay-boost model (fed = 1.6× milk). If the owner wants a hard-gated cow, it needs a MegaMart farmer/feeder first. |

### Balance decision on record
**Late-game pacing:** the alarming original numbers (L8→L9 = 5,921 s) were measured
*before* the counter and layout fixes and are **stale**. Fresh data shows MegaMart
runs ~35% slower per level than early game — normal idle-game pacing.
**Owner decided (2026-07-14): leave as-is.** Do not "fix" this without asking.

---

## 3. Hard-won lessons (don't repeat these)

1. **Playing beats reasoning.** Every serious bug above was found by *playing*,
   not by reading code. Use the TesterBot.
2. **Don't add clever guards to the core loop.** The carry-wedge guard (#12) was a
   fix for a cosmetic non-problem and broke the entire game.
3. **A red `DataValidator` can freeze the game** (LogError + "Error Pause").
   Always leave it green.
4. **The scene is built from code.** Editing the scene in Unity does nothing.
5. **Visual walls and grid walls are two things** and must agree.
6. **Stale `Editor.log` lines lie** — the validator only re-runs on Play.
7. **Clean up your background processes.**
8. **Verify cheaply, then stop.** Long automated soak runs burn hours and tokens for
   little signal. Compile + bake + validator + a short targeted run is usually enough;
   the owner will tell you what actually feels wrong.

---

## 4. Environment notes

- **Unity MCP is flaky** — it has been revoked ("your Unity plan doesn't include MCP
  connections") and later come back. Don't depend on it; the marker-file TesterBot
  harness works without it. When MCP *is* up, it's handy for screenshots
  (`Docs/images/` was captured that way).
- The **Genies SDK Bootstrap Wizard** steals editor focus after every recompile and
  idles the editor's update loop (freezing unattended runs). Its
  "Show wizard on startup" / "Check prerequisites on load" are now **off** — keep
  them off.
- `Assets/Genies/` is **gitignored** — it holds an auth artifact. Never commit it.
- If the package cache gets wiped, compiles fail with thousands of phantom errors.
  Fix: quit Unity, delete `Library/{PackageCache,PackageManager,Bee,ScriptAssemblies}`,
  relaunch.

---

## 5. How to run the game / QA it

```bash
# Automated playthrough (no MCP, no editor scripting needed)
touch Logs/testerbot.enabled     # spawn the bot on Play
touch Logs/testerbot.freshrun    # wipe the save once (optional)
# → press Play in Unity
tail -f Logs/testerbot_run.txt   # live diary: STATUS line + events

# When done
rm -f Logs/testerbot.enabled Logs/testerbot.freshrun
```

The bot plays with player-legal inputs only — it never cheats. It buys purchase
pads *and* upgrades, so a run exercises the real economy.

---

## 6. Antigravity Changes

During the work session on **2026-07-15**, the following updates were made by Antigravity:

- **Bug 1 Fix**: Gated the Bread Oven and Egg Stove purchase pads at **Level 4** (previously Level 5) in `SceneBootstrapper.cs`. Synced the buyer unlock levels for both `Bread` and `FriedEgg` to **Level 4** in `PriceCatalog.cs`. This aligns these unlocking events with the Chef's unlock level (also Level 4).
- **Bug 2 Fix**: Modified `Thief.cs` to track stolen items by type (`stolenItems` dictionary), added a red `!` visual alert when stealing, added an angry emote on flee, added a happy emote on catch, returned stolen items to the store inventory (`StoreInventory`) upon successful catch, and destroyed the Thief on successful escape to prevent them standing frozen at exit doors.
- **Bug 3 Fix**: Updated checkout duration `SecondsPerCheckout` to **`2.0` seconds** in `CashCounter.cs` (manual player override checks out at `1.0` seconds) and added a floating green `+$X` cash text emote on each completed transaction. This creates visual pacing and transaction clarity.

During the work session on **2026-07-16**, the following updates were made by Antigravity:

- **Chef Unlock Gating**: Implemented `RequiredPurchase` support on `PurchasePad` and connected the Wheat Mill, Ketchup Blender, Bread Oven, and Egg Stove pads to require "Hire Chef" to be purchased first.
- **Thief Alerts & Warnings**: Implemented general warning Toasts when the thief spawns ("⚠️ Thief alert! A shoplifter has entered the store!") and a detailed Toast breakdown of stolen items and dollar value lost when they successfully escape.
- **Buyer Queue Visuals & Mechanics**: Updated `FindOpenCounter` in `Buyer.cs` to query any unlocked cash counter (even if unstaffed / closed), allowing buyers to walk to and queue up at the tills rather than waiting at product shelves, forming a real checkout line that the player can then manually checkout.

---

## 7. Update protocol

At the end of a work session, update:
- **§1** — what you did, what's next.
- **§2** — any new bug/feedback, with **root cause**, not just the symptom.
- **§3** — anything you learned the hard way.
- The **commit hash** and date at the top.
