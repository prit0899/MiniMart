# Reference Game Flow — Restructured Pipeline
**Sources:** `Game_Design_Plan.docx` (9-video gameplay analysis) + `merged_all.txt` (frame-by-frame video logs).
**Goal:** same-to-same game flow as the reference. This file is the authoritative flow spec;
[BACKLOG.md](BACKLOG.md) tracks the work items.

## The reference core loop (from the video logs, second by second)

```
walk over MONEY STACKS to collect coins
        ↓
walk over ARROW-MARKED PAD → buy plot / stand / counter / machine / worker (cost drains while standing)
        ↓
walk over PLOT → crop grows instantly → harvest into head-stack (15 → 44 carry, "MAX" badge)
        ↓
walk to STAND/SHELF → stock it (n/cap fraction shown)
        ↓
customers browse with thought bubbles (icon + 2/4 progress), fill carts, queue
        ↓
player (or hired cashier) serves → happy face + heart → MONEY STACK grows on the counter
        ↓
collect the stack → afford the next pad → the store physically grows
```

Key properties: **the world starts nearly empty**, everything is *walked into being*; money is
physical; there are no menus in the core loop at all.

## Flow parity status

| Reference mechanic | Status |
| :-- | :-- |
| World starts empty; expansion via walk-over purchase pads (arrow + draining cost) | ✅ Batch 7 — `PurchasePad`, 10 gated expansions (coop, wheat, 3 machines, 4 workers-hire, counter 2, shelves bundled) |
| Money stacks pile on the counter; collect by walking over (grants cash + XP) | ✅ Batch 7 — `MoneyStack`; checkout no longer auto-banks |
| Carry 15 → 44+, tall head-stack, "MAX" badge when full | ✅ Batch 7 — player curve {15,22,29,36,44}, 24-icon stack, MAX badge |
| Instant crop growth | ✅ already ~0.5 s/unit — reads as instant |
| Shelf fraction overlays (0/20) | ✅ shelf badges (earlier batch) |
| Buyers only want items whose shelf exists; incremental cart fill | ✅ wishlist checks active shelves; incremental take exists |
| Queue at counter, served by player or cashier NPC | ✅ queue slots + visible cashiers (earlier batch) |
| Purchases persist across sessions | ✅ `PurchasedPads` in save |
| Thought bubbles over customers (item icon + n/m progress, "$" when queueing) | ✅ Batch 8 — `ThoughtBubble` |
| Happy face + heart on successful sale; angry on walk-out; "?" on sold-out | ✅ Batch 8 — `Emote` |
| Cow pen + milk + dairy (cheese) chain | ✅ Batch 8 — CowPen, Dairy machine, 2 new shelves, 2 pads ($100 cow, $175 dairy), farmer milks, upgrade rows |
| Watering-can worker / plant care (observed once) | ⬜ optional, low priority |
| Herbs chain, fridge display visuals | ⬜ content phase — same pattern as dairy |
| Second location ("GO TO Cafe Mart" truck) | ⬜ far future |
| Rewarded ads (2× money, instant grow) | ⬜ monetization phase (AdsManager stub exists in plan) |

## Deliberate deviations (keep)

- **Thief + net, phone orders + delivery van** — not in the reference videos but explicitly in our GDD; they layer on top of the reference loop without changing it.
- **3D primitives instead of 2D sprites** — presentation only; flow is identical.
- **Store XP/level** — reference gates purely by coins; we additionally gate cashiers/thief by store level. Compatible: pads are the *spatial* gate, level the *pacing* gate.

## Pad ladder (current tuning)

Farmer $15 → Hen Coop+Egg stand $25 → Shelver A $40 → Wheat farm+stand $50 → Shelver B $60 →
Blender+Ketchup shelf $75 → Mill+Flour shelf $125 → Chef $150 → Oven+Bread shelf $200 → Counter 2 $300.
New game starts with $10 pocket money; first minutes are tomato-only, exactly like the reference video.
