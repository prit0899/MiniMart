# Logic Test Report — 2026-07-05
**Method:** no C# compiler is available on this machine and the open editor locks Unity batch mode,
so the game's rules were mirrored line-by-line into a Python simulation
(`scratchpad/minimart_sim.py`) and exercised by `test_minimart.py`. The mirror first reproduced
the C# behavior EXACTLY (including suspected bugs) to prove failures, then both sides were fixed.
Caveat: this validates game LOGIC and system design — not Unity-specific behavior
(rendering, physics, lifecycle, input). Those still need the on-device pass.

## Result
**30,116 test cases — 30,116 pass** (after fixes; 10 failed before, all traced to 3 real bugs).

## Coverage
| Area | Cases | What's checked |
| :-- | :-- | :-- |
| Upgrade curves (5 tracks × all levels) | ~90 | cost monotonic & positive, -1 at max, speed/stack non-decreasing, level clamping, chef≥shelver ceiling, player-fastest rule |
| Economy | ~60 | base prices, offer clamp 0–90%, elasticity boundaries (±20%), discount detection (≥10%), $1 basket floor, spend/deposit edge cases |
| Storage (8 items) | ~40 | caps respected, overflow routed to bins, bin load-balancing, overdraw refused |
| Machines (4 types × 4 levels) | ~40 | input clamp, throughput timing per level, collect-empty, output-full behavior |
| Farms (4 types) | ~30 | growth to cap, never exceeds, greedy harvest, drain, level speed effect |
| Store XP/level | ~50 | exact thresholds (100/200/300…), multi-level grants, negative ignored, full unlock truth table (5 levels × 8 items) |
| Phone orders | ~2,500 | 600 spawns × per-item checks: value ∈ [45,300], 1–3 types, only unlocked, qty ≤ storage cap; 2-active limit; fulfil pays shown value & withdraws exactly; van completion |
| Buyers | ~27,000 | 1,000 randomized buyers × invariants: wishes only unlocked+shelved items, trolley iff ≥5, shelf bounds, item conservation, pays quote(collected)+tip≤25%, empty pays nothing, patience per personality, angry-leave, counter open/unlock rules, 3× manual checkout |
| Money stacks / pads / offline | ~50 | merge, collect grants cash+XP, pad drain exact-cost & never-negative, ladder totals, offline formula grid incl. 40 h cap and full-storage no-dupe |
| Monte Carlo worlds | 12 seeds × 15 min | full loop (farms→machines→shelves→buyers→money→pads→phones) with per-tick invariants: cash ≥ 0, all bounds, economy flows, L2 reached, ketchup flows after blender, buyer cap |

## Bugs found → fixed in C#
1. **Machine destroyed product when its output tray was full** — `Machine.Update` consumed input
   and clamped `OutputReady`, losing one item per cycle. Now stalls until the tray is emptied
   ([Machine.cs](../Scripts/Production/Machine.cs)).
2. **Delivery-van money exploit** — loading the van drained `Order.Items` without paying, and the
   emptied order became vacuously "fulfillable" via the HUD FULFIL button for full value with zero
   stock. Now: a fully-loaded van pays out + closes the order (`CompleteByVan`), and drained orders
   are not HUD-fulfillable ([PhoneOrderManager.cs](../Scripts/Engine/PhoneOrderManager.cs),
   [DeliveryVan.cs](../Scripts/Engine/DeliveryVan.cs)).
3. **Manual prices unclamped** — `SetManualPrice` only floored at $0.05; the GDD ±50% band was
   enforced nowhere central, so saves/scripts could set absurd prices and break elasticity.
   Now clamped to [max(0.05, base×0.5), base×1.5] ([EconomyManager.cs](../Scripts/Economy/EconomyManager.cs)).

## Design quirks verified as intended (not bugs)
- `QuoteBasket({})` returns the $1 floor — every caller guards for empty collections first.
- A single cheap item checkout charges the $1 bundle floor — per the pricing rule.
- Phone-order payout is a premium ($45–300), deliberately decoupled from retail total.

## How to re-run
`python3 scratchpad/minimart_sim.py` is imported by `python3 scratchpad/test_minimart.py`
(scratchpad = the session scratch dir). Copies live outside the repo intentionally — they are
dev tools, not game assets. Ask Claude to re-run after logic changes.
