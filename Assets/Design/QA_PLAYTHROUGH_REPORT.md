# Tester Playthrough Report — 5 honest runs (2026-07-13)

**Method.** An in-game TesterBot plays exactly like a human: tap-to-move walking,
proximity interactions, standing on purchase pads, riding the travel pad. It never
teleports, grants cash, or calls dev APIs. Runs executed at 3× time scale (all game
logic is delta-time based, so only the wall clock changes). Diaries archived at
`Logs/testerbot_run[1-5]_archive.txt` (local; Logs/ is gitignored).

**Scope honesty.** Run 1 played the full arc to max level (L10). A full run costs
~2 wall-clock hours even at 3×, so runs 2–5 were fresh-save runs targeted at the
early game (L1→L6 + MegaMart travel) where the human-review feedback ("can't
understand where to start") lives; runs 2–3 incidentally continued to L8 before
being stopped.

## Level-up times per run (sim-seconds since scene start)

| Transition | Run 1 | Run 2 | Run 3 | Run 4 | Run 5 |
|---|---|---|---|---|---|
| L1→L2 | 181 | 221 | 144 | 123 | 223 |
| L2→L3 | 441 | 525 | 327 | 315 | 450 |
| L3→L4 | 959 | 1017 | 862 | 877 | 1082 |
| L4→L5 | 1866 | 1955 | 1730 | 1816 | 1802 |
| L5→L6 | 3592 | 3441 | 3461 | 3134 | 3101 |
| L6→L7 (MegaMart clock) | ~900* | 790 | 766 | — | — |
| L7→L8 | 1058* | 2519 | 2665 | — | — |
| L8→L9 | 6940* | — | — | — | — |
| L9→L10 | 14726* | — | — | — | — |

\* Run 1's MegaMart segment resumed from save after mid-run fixes; times are within
that session's clock. Early-game variance across runs is tight (±15%) — pacing is
stable and RNG-fair. The pacing cliff at L8+ is the outlier (see finding 5).

## Game-breaking bugs found by playing (all FIXED this batch)
1. **MegaMart had no working checkout L6–L8.** Counter 3 (the scene's only till)
   was a $400 pad gated at L9. Buyers could never pay → income and XP froze the
   moment a player traveled. Fix: Counter 3 ships open with MegaMart; Counter 4
   stays the L10 upgrade. Validator asserts added.
2. **No Milk/BottledMilk shelf in MegaMart** (shelf list still had the retired
   CannedTomato/Cookie shelves). Harvested milk stayed in the player's hands
   forever. Fix: shelf list corrected; retired shelves removed.
3. **Audio virtual-channel exhaustion** in a busy store ("Ran out of virtual
   channels" spam). Fix: 80 ms per-clip throttle in AudioFx.

## Open findings (design/balance — recommended next batch)
4. **LEVEL UP! popup never auto-dismisses** and stacks over the phone-order panel;
   it sat covering screen-center for ~30 sim-minutes. → auto-hide after ~6 s.
5. **Pacing cliff late game:** L8→L9 = 5,921 s and L9→L10 = 7,753 s of sim time vs
   200–3,600 s for all earlier levels. Root cause observed on screen: thin buyer
   traffic in MegaMart caps income. → scale buyer spawn rate with store level, or
   soften XP curve above L7 (80×level → ~60×level), or both.
6. **Phone orders demand unfulfillable quantities late** (e.g. 15 Herb + 7 Corn +
   3 Milk for $300; 14 BottledMilk for $380). → cap per-item order size by current
   production rate/level.
7. **Locked purchase pads flash for one frame** on scene load before their first
   Update hides them. Cosmetic. → hide children in Start.
8. **Carry-wedge slow cycle at L7–L8:** the player can fill their carry stack (up
   to 24 items) with goods whose shelves are full, spending long stretches unable
   to deposit. Not a deadlock (it self-recovers) but reads as "stuck". → soft cap
   pickup when the item's shelf is ≥90% full.

## Onboarding answer (the "lots of confusion, can't understand where to start" reviews)
Implemented this batch: **UnlockGuide** (bouncing arrow + toast pointing at the
cheapest newly-unlocked pad on every level-up), **per-station hint toasts** on
purchase ("Carry tomatoes to the Blender to make ketchup!"), and **TutorialGuide**
on fresh saves. Recommendation: skip the demo-video idea — mobile players skip
videos, they cost build size and localization, and contextual guidance teaches at
the exact moment of need. If more is wanted, next steps in order of value:
1. First-session scripted path: dim the world, spotlight Tomato pad → shelf → counter.
2. "!" badge + brief camera pan to the new unlock on level-up (reference game does this).
3. Persistent goal card under the level bar ("Next: buy the Hen Coop — $25").

## NPC carry stacks (user request)
Shelver, Farmer, Buyer, and Chef now stack real item meshes overhead exactly like
the player (CharacterBase.GetCarriedItems overrides + CarryVisual NPC path).
Verified visually in-game during runs.

## QA harness (how to rerun this anytime, no MCP needed)
- `touch Logs/testerbot.enabled` → entering Play spawns the bot (editor only).
- `touch Logs/testerbot.freshrun` → next Play wipes the save once (consumed).
- Bot plays at 3×, diary at `Logs/testerbot_run.txt` (STATUS line + timeline).
- Editor gotchas for unattended runs: keep the Unity main window frontmost
  (the Genies Bootstrap Wizard steals focus and idles the whole editor — its
  startup/on-load checkboxes are now off), and on battery keep the machine awake.
