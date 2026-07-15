# Phases — Mini Mart

> **The project broken into meaningful phases**, with where we actually are.
> Companion docs: [PRD](PRD.md) · [Architecture](Architecture.md) · [Rules](Rules.md) · [Design](Design.md) · [Memory](Memory.md)

Status legend: ✅ done · 🟡 in progress · ⬜ not started

---

## Phase 0 — Foundations ✅
Engine, project settings, and the skeleton everything else hangs off.
- ✅ Unity 6000.5.1f1, Built-in RP, iOS target, portrait.
- ✅ Code-built scene architecture (`SceneBootstrapper`), `PrimitiveFactory` for
  all meshes — no art dependency to get moving.
- ✅ `Core` enums (`ItemType`, `RoleType`) + `Catalog` data layer.
- ✅ `GridPathfinder` and grid-based movement.

## Phase 1 — Core loop ✅
The thing that makes it a game.
- ✅ Player movement, tap-to-move, carry stack.
- ✅ Farms (tomato, wheat, hen) → harvest.
- ✅ Machines (blender, mill, oven, stove) → process.
- ✅ Shelves + storage racks → stock.
- ✅ Buyers → shop, queue, pay at counter → cash on the floor → XP.

## Phase 2 — Economy & progression ✅
- ✅ `EconomyManager` (prices, offers), `CashCounter`.
- ✅ Store levels 1–10, XP from collected cash.
- ✅ `PurchasePad` ladder, level-gated, **1–3 unlocks per level** (owner rule).
- ✅ `UpgradePad`: player Carry / Speed / Crop Speed.
- ✅ Save/Load + offline earnings.
- ✅ `DataValidator` (~45k assertions) as the balance safety net.

## Phase 3 — Workers & life ✅
- ✅ Farmer, Chef, Shelver ×2, Cashiers — automate parts of the loop.
- ✅ Buyers with personalities, queues, patience; Thieves.
- ✅ Phone orders + delivery van.
- ✅ NPC carry stacks show **real item meshes**, same as the player.

## Phase 4 — Two-store split ✅
- ✅ MegaMart as a **separate scene** (`MegaMart.unity` + `SceneBootstrapper2`),
  reached by a travel pad — matching the reference's "GO TO Cafe Mart".
- ✅ MegaMart chains: milk, corn, herb, apple, cheese, coffee.
- ✅ Level ladder spans both marts (L1–L5 Mart 1, L6–L10 MegaMart).

## Phase 5 — Map fidelity ✅
Making the world match the owner's hand-drawn plan.
- ✅ Both marts rebuilt as **one enclosed building**: shop floor above, farm +
  processing zone in the lower half of the same building, interior wall with one
  gap. Entries west, exits east.
- ✅ Farms and machines moved **inside** the walls (they were stranded on open
  grass 20–30 units away).
- ✅ **No teleporting through walls** — every movement step checks the grid.
- ✅ Phone-order van left outside, as specified.

## Phase 6 — Onboarding & clarity 🟡
Driven by real reviewer feedback: *"can't understand where to start."*
- ✅ `TutorialGuide` on a fresh save.
- ✅ `UnlockGuide`: bouncing arrow + toast pointing at each newly unlocked pad.
- ✅ Per-station hint toast on purchase ("Carry tomatoes to the Blender!").
- ✅ Level-up popup auto-dismisses (it used to sit on screen forever).
- ⬜ **First-session spotlight path** (dim world, highlight tomato → shelf → counter).
- ⬜ "!" objective badge + camera pan to the new unlock.
- ⬜ Persistent goal card under the level bar.

## Phase 7 — Balance & feel 🟡
- ✅ Phone orders capped so they're fulfillable.
- ✅ Late-game pacing reviewed (accepted as-is by owner).
- ✅ **Upgrades made meaningful**: base carry 8 → 44 (was 15, which let you max
  the game without ever upgrading).
- 🟡 Ongoing tuning from owner playtests.

## Phase 8 — QA & hardening 🟡
- ✅ `TesterBot` automated player + marker-file harness.
- ✅ Full L1→L10 playthroughs; five-run comparison report
  (`Assets/Design/QA_PLAYTHROUGH_REPORT.md`).
- 🟡 Continued playtesting after each balance change.
- ⬜ Device (real iPhone) testing pass.
- ⬜ Performance profiling / 60 FPS verification on device.

## Phase 9 — Ship ⬜
- ⬜ Art pass / final polish.
- ⬜ App icon, splash, store listing.
- ⬜ iOS build, TestFlight, App Store review.
- ⬜ (Optional, later) monetisation — the purchasing package is installed but
  deliberately unused.

---

## Right now

We are in **Phase 6 / 7 / 8 concurrently**: the game is fully playable end-to-end
(L1→L10), the map matches the plan, and we are iterating on onboarding clarity and
balance from the owner's own playtests. See [Memory.md](Memory.md) for the live
state and the current bug list.
