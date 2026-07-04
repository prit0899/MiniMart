# Mini Farm Market Spec — Adoption Map
**Source:** external "Mini Farm Market" clone spec provided 2026-07-04 (full text lives in the chat;
`MiniMart 2/` folder is an old copy of this project's scripts, not that game).
**Decision:** cherry-pick into the current 3D MiniMart — no pivot.

## Adopted (implemented 2026-07-04)

| Feature | Where |
| :-- | :-- |
| Store XP + level (revenue → XP, level drives all unlocks; decoupled from player upgrade level) | `GameManager` (StoreLevel/StoreXp/AddStoreXp), HUD label |
| XP sources: $1 = 1 XP at checkout, +25 per phone order, +25 per player upgrade | `GameManager.SimTick`, `PhoneOrderManager.TryFulfil` |
| Customer tips (20% chance, +10–25%, only when the buyer found everything) | `CashCounter.ProcessFront` |
| Offline earnings (capped 4 h; trickle of eggs/tomato/wheat into storage + offline sales coins; Welcome Back panel with COLLECT) | `GameManager.ComputeOfflineEarnings`, `HUDBuilder`/`HUDController` |

## Adopt later (added to BACKLOG)

- **Customer personalities** (impatient/patient/rich/bargain/loyal) — budget, patience, walk speed, queue-abandon, tip modifiers.
- **Patience meter + angry leave** with reputation counter.
- **Daily quests + achievements** ("serve 20 customers", "sell 15 bread") with coin/XP rewards.
- **Level-up popup** with unlock reveal (confetti, claim button).
- **Daily reward calendar.**
- **"Watch ad for 2x" on offline earnings** (needs AdsManager; monetization phase).
- **More crops** (carrot, corn, potato, strawberry-style regrow) — fits FarmCatalog pattern.
- **Staff/auto-helpers beyond current workers** (their staff system ≈ our workers; skip unless design calls for hiring more).

## Rejected (conflicts with our design)

- 2D grid presentation, till/water/wither plot states, sprinklers/greenhouses/fertilizer — different genre core; our farms are automated growth + proximity harvest.
- Organic certification & dual seed economy — depth without fit for the mart loop; revisit for v1.2 only if retention needs it.
- Firebase backend / Cloud Functions / leaderboards — this game is offline-first with PlayerPrefs; no server budget.
- Drag-and-drop shelf stocking — ours is proximity/AI-driven per My Mini Mart.
- Gems second currency — single-currency economy per GDD.
