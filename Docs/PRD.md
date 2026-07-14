# PRD — Mini Mart

> **What we are building, who for, and what "done" means.**
> Companion docs: [Architecture](Architecture.md) · [Rules](Rules.md) · [Phases](Phases.md) · [Design](Design.md) · [Memory](Memory.md)

---

## 1. One-line pitch

A mobile **idle / tycoon store-management game**: grow crops, process them into
goods, stock your shelves, serve customers, and reinvest the profit to grow from
a tiny mini mart into a two-store empire.

## 2. The reference

This game is an intentional **same-to-same clone of "My Mini Mart" (Kiseki Games)**
in flow, feel, and UI/UX — not a loose inspiration.

- Reference links: `Refer/refer games.rtf`
- Reference screenshots: `Refer/` and `Assets/Design/refer images/`
- **The authoritative map layout is the owner's hand-drawn plan:**
  `Refer/map-plan-mart1.pdf` — this overrides any inference from the reference
  video or screenshots. When they conflict, the hand-drawn map wins.

## 3. Target player

| | |
|---|---|
| **Who** | Casual mobile players. Phone-first, plays in short bursts. |
| **Not** | Not technical, not a genre expert, will not read a tutorial wall. |
| **Key insight** | Real human reviewers said: *"game is good but lots of confusion, can't understand where to start."* Onboarding clarity is a **first-class requirement**, not polish. |
| **Platform** | iOS (primary). Built in Unity, portrait, touch-only. |

## 4. Core loop

```
Harvest crop  →  Carry to machine  →  Process into goods  →  Stock shelf
     ↑                                                            ↓
  Reinvest  ←  Collect cash  ←  Customer pays at counter  ←  Customer buys
```

Everything the player builds accelerates this loop. Workers (farmer, chef,
shelver, cashier) automate parts of it so the player can focus on the next
bottleneck.

## 5. Features (what must exist)

### 5.1 Shipped / required
- **Two stores.** Mart 1 (`Game.unity`) and MegaMart (`MegaMart.unity`) — two
  **separate scenes**, travelled between by a dwell pad, exactly like the
  reference's "GO TO Cafe Mart". Not a shared map.
- **Production chains.** Tomato→Ketchup, Wheat→Flour→Bread, Egg→Fried Egg;
  MegaMart adds Milk→Bottled Milk, Corn→Processed Corn, Herb→Herb Pack, Cheese,
  Apple, Coffee.
- **Store levels 1–10.** XP is earned by *collecting cash off the floor*.
  Each level reveals **1–3 new things** — never more (owner rule; see §6).
- **Purchase pads.** Stand on a pad to buy a station/worker/upgrade. Level-locked
  pads stay invisible until their level.
- **Player upgrade pads.** Carry, Speed, Crop Speed. These must be **worth
  buying** — the base carry is deliberately small (8, upgrading to 44).
- **Workers.** Farmer, Chef, Shelver ×2, Cashiers. All show a real item carry
  stack over their head, same as the player.
- **Customers.** Spawn on the road, enter, shop, queue, pay, leave. Thieves too.
- **Phone orders.** A van arrives with a bulk order; fulfil for a cash bonus.
- **Onboarding.** Tutorial guide, per-level unlock arrow + toast, per-station
  hint on purchase.
- **Save/Load.** Autosave + offline earnings on return.

### 5.2 Explicit non-goals (for now)
- Multiplayer / cloud save.
- Monetisation & IAP (the package is installed but unused).
- Any content not in the reference game.

## 6. Owner design rules (hard constraints)

These came directly from the owner and are **not negotiable without asking**:

1. **1–3 unlocks per level.** Not 5–6. Progression must feel calm, not rushed.
2. **The map follows the hand-drawn plan.** Both marts are ONE enclosed
   building: shop floor on top, farm + processing zone in the lower half of the
   *same* building, split by an interior wall with one middle gap. Entries on the
   **west** wall, exits on the **east**. Nothing (farms, machines) sits outside
   on open grass.
3. **Nobody teleports through walls** — not the player, shelver, chef, farmer, or
   buyers. Doors and the interior gap are the only ways through.
4. **The phone-order van stays outside** the mall, where it is today.
5. **Upgrades must matter.** If the player can reach max level without ever
   upgrading, the upgrade is useless and the design is wrong.
6. **Mart 2, 3, … reuse the same core map plan**, with their own items.

## 7. Success criteria

- [x] A fresh player can reach max level (L10) by honest play.
- [x] Zero compile errors; `DataValidator` runs ~45k assertions green on boot.
- [x] Every farm, machine, shelf, and counter is reachable in both marts.
- [ ] A non-technical player knows what to do in the first 60 seconds.
- [ ] No visible stalls / dead-ends across a full playthrough.

## 8. Known open items

Tracked live in [Memory.md](Memory.md) — that is the file to read for "where are
we right now".
