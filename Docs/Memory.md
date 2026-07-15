# Memory — live project state

> **READ THIS FIRST.** This is the handoff file: where we are, what's done, what's
> broken, and every piece of feedback the owner gave from playing the game himself.
> **Update it at the end of every work session.**
>
> Companion docs: [PRD](PRD.md) · [Architecture](Architecture.md) · [Rules](Rules.md) · [Phases](Phases.md) · [Design](Design.md)

**Last updated:** 2026-07-14
**Branch:** `feature/reference-flow-overhaul`
**Latest commit:** `d899036` — *feat: nav mesh + A\* + steering — workers move naturally*

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
Nothing mid-flight. Everything is committed and pushed.

**Owner has uncommitted local edits** to `Catalog/RoleCatalog.cs` (player stack
limit) and `Engine/DataValidator.cs`. Leave them alone — they're intentional and
the validator is green with them.

**Next most valuable work** (in order):
1. Play the game and check the new worker movement *feels* right. Tuning knobs are
   all in `CharacterBase`: `maxForce` (momentum), `turnSpeed`, `SeparationWeight`,
   `AvoidWeight`.
2. Phase 6 onboarding: first-session spotlight path (dim world → highlight tomato
   farm → shelf → counter). Top open item from real reviewer feedback.
3. Device (real iPhone) test pass — never done yet.
4. Gameplay capture for the README (a video/GIF; stills are already in `Docs/images/`).

---

## 2. Owner feedback & bugs found by *playing* (chronological)

> This is the highest-value section. These are real problems found by a human or
> by honest automated play — not code review. **The pattern: as developers we
> could not see these; only playing revealed them.**

| # | Owner said / QA found | Root cause | Status |
|---|---|---|---|
| 1 | *"lots of confusion, can't understand where to start"* (real human reviewers) | No onboarding at all | 🟡 Partly fixed: TutorialGuide, UnlockGuide arrow + toast, per-station hints. Spotlight path still open. |
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
| 16 | *"in name of testing you take too much — literally 10+ hours, eating tokens, end result not significant"* | I ran marathon bot playthroughs (full L1→L10 soaks) for marginal payoff | ✅ Process change: **verify cheaply** (compile + bake + validator + a short run), then stop. Don't run long soaks unless asked. |

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

## 6. Update protocol

At the end of a work session, update:
- **§1** — what you did, what's next.
- **§2** — any new bug/feedback, with **root cause**, not just the symptom.
- **§3** — anything you learned the hard way.
- The **commit hash** and date at the top.
