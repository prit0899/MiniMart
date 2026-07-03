# Mini Mart — Game Design Document (GDD)
**Version:** 2.0
**Status:** Authoritative design spec — supersedes v1.0
**Target Platforms:** Android, iOS (portrait-landscape auto, landscape primary)
**Companion documents:** [TDD.md](TDD.md) (technical implementation), [ROADMAP.md](ROADMAP.md) (build order)

---

## 1. Executive Summary

**Mini Mart** is a hybrid-casual / idle market simulation. The player runs a small supermarket with an attached micro-farm: harvest tomatoes, wheat, and eggs; process them into ketchup, flour, and bread with machines; stock the shelves; and check out a random flow of customers. Money funds upgrades for workers and machines until the store runs nearly hands-free — at which point deliberately thin margins, phone orders, and thief events keep the player engaged.

**Design pillars**
1. **Everything is cheap, volume is king.** Items are pocket-change cheap ($0.30–$1.50), and every basket rings up at a **$1.00 minimum** (bundle floor). Profit comes from throughput, not markup.
2. **Prices reward hard work.** The more production steps an item needs, the more it sells for (Bread > Ketchup > Flour > Wheat > Egg > Tomato).
3. **The player is always the fastest character** — automation never fully replaces them (thief catching, phone orders, bottleneck-clearing are player-only).
4. **Three content families:** Workers, Machines, Animals — each with its own upgrade track.

---

## 2. Core Game Loop

```
Harvest ──► Store ──► Process ──► Shelf ──► Customer ──► Money ──► Upgrade ──► Produce Faster ──► (repeat)
```

- **Harvest** — tomatoes, wheat, eggs from the farm zones.
- **Store** — deposit raw goods into storage racks (tomato storage, wheat storage, egg storage).
- **Process** — Blender → ketchup, Mill → flour, Oven → bread.
- **Shelf** — Shelvers move goods from storage to display shelves.
- **Customer** — buyers pick items and pay at a cash counter.
- **Money → Upgrade** — spend on worker/machine/animal levels; each level raises speed and stack size, closing the loop faster.

**Endgame:** at max levels the chain looks automated, but because item prices are cents, profit stays modest — the player still optimizes prices/offers, answers phone orders, and catches thieves to grow meaningfully.

---

## 3. The Three Families

### 3.1 Workers

| Worker | Count | Levels | Stack (min→max) | Role |
| :-- | :-: | :-: | :-: | :-- |
| **Player (Worker 1)** | 1 | 1–5 | 4 → 7 | Does everything; only one who can catch thieves; fastest character in the game at every level |
| **Shelver 1** | 1 | 1–5 | 3 → 5 | Stocks **Eggs, Tomatoes, Tomato Ketchup** onto display shelves |
| **Shelver 2** | 1 | 1–5 | 3 → 5 | Stocks **Wheat, Wheat Flour, Bread** onto display shelves |
| **Chef** | 1 | 1–5 | 3 → 6 | Makes **Ketchup** (fetches tomatoes from farm himself) and **Bread** (fetches wheat from farm, flour from Mill, eggs from farm himself) |
| **Farmer** | 1 | 1–5 | 3 → 6 | Manages hens (eggs), wheat farm, and tomato plants; moves harvest into storage |
| **Cashier 1** | 1 | 1–5 | — | Runs Cash Counter 1 (entrance). Unlocks at Player Level 2 |
| **Cashier 2** | 1 | 1–5 | — | Runs Cash Counter 2 (exit). Unlocks with the counter at Player Level 4 |

**Every worker upgrade increases movement/work speed.** The Player additionally gains stack size at levels 3/4/5.

#### Player level table
| Property | L1 | L2 | L3 | L4 | L5 (max) |
| :-- | :-: | :-: | :-: | :-: | :-: |
| Carry stack | 4 | 4 | 5 | 6 | 7 |
| Move speed (u/s) | 6.0 | 6.9 | 7.9 | 9.1 | 10.5 |

#### Shelver 1 & 2 level table
| Property | L1 | L2 | L3 | L4 | L5 |
| :-- | :-: | :-: | :-: | :-: | :-: |
| Carry stack | 3 | 3 | 4 | 4 | 5 |
| Move speed (u/s) | 3.6 | 4.1 | 4.7 | 5.4 | 6.2 |

#### Chef / Farmer level table
| Property | L1 | L2 | L3 | L4 | L5 |
| :-- | :-: | :-: | :-: | :-: | :-: |
| Carry stack | 3 | 4 | 4 | 5 | 6 |
| Move speed (u/s) | 3.8 | 4.4 | 5.0 | 5.8 | 6.6 |

> Rule check: worker speeds cap at 6.6 u/s — always below the Player's 6.0-at-L1 only briefly; by design the Player upgrade pace is gated so the Player is always the fastest character on the floor. If the Player is L1 while a worker reaches L4+, worker speed is clamped to `PlayerSpeed × 0.9`.

### 3.2 Machines

All machines: **levels 1–4, input/output stack 4 → 8**, each level also shortens processing time.

| Machine | Recipe | Base process time | L1 | L2 | L3 | L4 |
| :-- | :-- | :-: | :-: | :-: | :-: | :-: |
| **Blender** | 1 Tomato → 1 Ketchup | 2.5 s | stack 4, ×1.00 | stack 5, ×0.85 | stack 6, ×0.72 | stack 8, ×0.60 |
| **Mill** | 1 Wheat → 1 Wheat Flour | 3.0 s | stack 4, ×1.00 | stack 5, ×0.85 | stack 6, ×0.72 | stack 8, ×0.60 |
| **Oven** | 1 Flour + 1 Egg → 1 Bread | 4.0 s | stack 4, ×1.00 | stack 5, ×0.85 | stack 6, ×0.72 | stack 8, ×0.60 |

(×N = multiplier on process time; L4 Blender = 1.5 s per ketchup.)

### 3.3 Animals

| Property | Value |
| :-- | :-- |
| Hens | 2 |
| Eggs per hen (uncollected max) | 3 |
| Lay rate | 1 egg / 0.5 s until full (same rate as tomato) |
| Coop egg-buffer stack | 4 → 8 across Coop levels 1–4 |
| Coop upgrades | Levels 1–4; each level increases lay speed and buffer stack (4, 5, 6, 8) |

---

## 4. Farms & Production Sources

| Farm | Layout | Per-unit capacity | Growth rate |
| :-- | :-- | :-- | :-- |
| **Tomato plants** | 2 columns × 3 rows = **6 plants** | 3 tomatoes per plant max | 1 tomato / 0.5 s per plant |
| **Wheat farm** | 3 × 4 grid = **12 tiles** | 1 wheat per tile | regrow 3.0 s per tile (tunable) |
| **Hen coop** | 2 hens | 3 eggs per hen | 1 egg / 0.5 s per hen |

Growth pauses when a plant/hen/tile is at its max until harvested.

---

## 5. Storage & Shelves

| Storage | Capacity |
| :-- | :-: |
| Tomato storage | **15** |
| Egg storage | 15 |
| Wheat storage | **15** |
| Wheat flour storage | **12** |
| Ketchup storage | **15** |
| Bread storage | **12** |

Display shelves draw from these storages (Shelvers do the moving). If a deposit would overflow a storage, the carrier routes the excess to the nearest **bin** (two bins sit in the aisles — see map).

---

## 6. Economy

### 6.1 Item prices (base)
Priced by production effort. Rule (as implemented in `PriceCatalog`): **no basket/bundle ever totals under $1.00** — cheap per-item prices with a $1 checkout floor.

| Item | Effort chain | Base price |
| :-- | :-- | :-: |
| Wheat | grow (slow) | **$0.30** |
| Tomato | grow | **$0.35** |
| Egg | hen | **$0.40** |
| Wheat Flour | wheat → mill | **$0.80** |
| Tomato Ketchup | tomato → blender | **$1.20** |
| Bread | wheat → flour + egg → oven | **$1.50** |

Player may adjust any shelf price ±50% from base. High prices raise rejection chance; discounts raise spawn rate and basket size (see 8.3).

### 6.2 Upgrade costs
Global ladder: **$50 → $100 → $200 → $500 → $1,000 → $2,000 …** (×2, with the 200→500 step ×2.5). Each track starts at a different rung so that **Chef's max upgrade is never cheaper than the Shelvers'**:

| Track | L1→2 | L2→3 | L3→4 | L4→5 |
| :-- | :-: | :-: | :-: | :-: |
| Player | $50 | $100 | $200 | $500 |
| Shelver 1 / Shelver 2 | $50 | $100 | $200 | $500 |
| Farmer | $100 | $200 | $500 | $1,000 |
| **Chef** | $200 | $500 | $1,000 | $2,000 |
| Cashiers | $50 | $100 | $200 | $500 |

| Machines / Coop (L1–4) | L1→2 | L2→3 | L3→4 |
| :-- | :-: | :-: | :-: |
| Blender / Mill / Oven / Hen Coop | $100 | $200 | $500 |

### 6.3 Income sources
1. **Shelf sales** — cents per item, high volume.
2. **Phone orders** — $45–$300 per fulfilled order (the big money; see 9.1).
3. **Thief bounty** — catching a thief recovers the goods plus a small cash bonus.

---

## 7. Store Levels & Unlocks

| Player Level | Unlocks |
| :-- | :-- |
| **1** | Tomatoes + Eggs sellable. Player must personally stand at Cash Counter 1 to check out buyers. |
| **2** | **Cashier 1 hired** — player no longer needs to man the counter. Blender + Ketchup unlock. |
| **3** | Mill + Wheat Flour unlock. Thief events begin. |
| **4** | Oven + Bread unlock. **Cash Counter 2 (exit) + Cashier 2** unlock. |
| **5** | All upgrade tracks open to max. "Automation plateau" begins. |

**Buyers only ever ask for unlocked items.** If Bread is locked, no buyer ever wants bread.

---

## 8. Customers (Buyers)

### 8.1 Spawning
- Random flow: spawn interval drawn from a range that tightens with player level (e.g., 8–14 s at L1 → 3–6 s at L5).
- Shopping list = random multiset of currently unlocked items, weighted toward cheap items.

### 8.2 Hands vs. Trolley
- A buyer arrives **empty-handed** or grabs a **trolley** at the entrance rack:
  - **< 5 items** total (same or different) → hand-carry.
  - **≥ 5 items** → must take a trolley (trolley max 12 items).
- Trolleys are pooled objects; returned to the rack on checkout.

### 8.3 Price reactions
- Item priced **> +20% over base** → 35% chance the buyer skips that item (angry emote).
- Item **discounted ≥ 10%** → +50% buyer spawn rate for that period and a chance to double the quantity of that item in lists.
- **Offers** (player-created promotions, e.g. "Ketchup −30%") behave as discounts with a banner icon over the shelf.

### 8.4 Checkout
- Buyers queue at the nearest open counter (Counter 1 at entrance side, Counter 2 at exit side after L4).
- Payment = sum of shelf prices; coins burst onto the counter; cash auto-credits (or player collects at L1).

---

## 9. Special Events

### 9.1 Phone Orders
| Rule | Value |
| :-- | :-- |
| Frequency | one call every **4–5 minutes** |
| Time limit | **10 minutes** to fulfill, then it expires |
| Concurrency | max 2 active orders (one may arrive while another is live) |
| Contents | random group of unlocked items, e.g. *12 Bread + 20 Eggs + 15 Ketchup* |
| Payout | **$45 – $300**, scaled to order size and item effort |

The player can **accept or decline** a call. Accepted orders show on the HUD with a progress bar; delivering is done from storage stock at the phone-order drop point.

### 9.2 Thief
| Rule | Value |
| :-- | :-- |
| Trigger | random timer, one thief every **4–5 minutes** (from Player Level 3) |
| Behavior | enters like a buyer, sprints to the fullest/most valuable shelf, grabs up to 5 items, runs for the nearest exit (main or secondary exits) |
| Capture | **only the Player** can catch him, using the **Net** tool (throw within range) |
| Escape | once the thief crosses any exit door to outside, he **cannot be caught** — items permanently lost |
| Caught | stolen items return to storage + cash bounty ($5–$15, tuned to cent-economy) |

### 9.3 Player controls over the sim
The player can at any time:
- **Pause / resume** the game.
- **Stop** (soft-pause AI while keeping UI open).
- **Manipulate prices** per shelf (±50%).
- **Manipulate stock** (move items between storage/shelves manually).
- **Create offers** (timed discounts) to spike demand.

---

## 10. Map Layout

Top-down store layout (entrance at bottom, exit at top). Three vertical product aisles separated by two service aisles that each contain **secondary exit doors and a bin**:

```
                 ┌──────────────────────── EXIT DOOR ───────────────────────┐
                 │                    CASH COUNTER 2  (unlocks L4)           │
                 │                                                          │
    AISLE A      │   AISLE GAP 1     AISLE B        AISLE GAP 2   AISLE C   │
 ┌────────────┐  │ ┌─────────────┐ ┌────────────┐ ┌─────────────┐ ┌───────┐ │
 │ Hen Coop   │  │ │ 2nd Exit    │ │ Wheat Farm │ │ 2nd Exit    │ │ Bread │ │
 │ (2 hens)   │  │ │  Door       │ │ (3×4)      │ │  Door       │ │Storage│ │
 ├────────────┤  │ ├─────────────┤ ├────────────┤ ├─────────────┤ │  +    │ │
 │ Tomato     │  │ │    BIN      │ │ Wheat      │ │    BIN      │ │ OVEN  │ │
 │ (plants +  │  │ ├─────────────┤ │ Storage    │ ├─────────────┤ └───────┘ │
 │  shelf)    │  │ │ 2nd Exit    │ ├────────────┤ │ 2nd Exit    │           │
 ├────────────┤  │ │  Door       │ │ Flour      │ │  Door       │           │
 │ CASH       │  │ └─────────────┘ │ (MILL +    │ └─────────────┘           │
 │ COUNTER 1  │  │                 │  storage)  │                           │
 ├────────────┤  │                 ├────────────┤                           │
 │ Egg        │  │                 │ Ketchup    │                           │
 │ Storage    │  │                 │ Storage    │                           │
 └────────────┘  │                 │ (+BLENDER) │                           │
                 │                 └────────────┘                           │
                 │                                                          │
                 └──────────────────────── OPEN (ENTRANCE) DOOR ────────────┘
                          [ Trolley rack beside entrance ]
```

- **Aisle A (vertical):** Egg storage → Cash Counter 1 → Tomato area → Hen coop.
- **Gap 1:** secondary exit door, bin, secondary exit door.
- **Aisle B (vertical):** Ketchup storage (+Blender) → Flour/Mill → Wheat storage → Wheat farm.
- **Gap 2:** secondary exit door, bin, secondary exit door.
- **Aisle C:** Bread storage with the Oven.
- **Cash Counter 2 + exit door** at the top; **entrance door** at the bottom.
- Thieves may flee through **any** exit (main, exit, or the four secondary doors) — this is why the net and player speed matter.

---

## 11. UI

| Screen / widget | Contents |
| :-- | :-- |
| **HUD** | Cash, player level + XP bar, active phone-order card(s) with timers, inventory quick-view, joystick |
| **Inventory panel** | All 6 items: storage count / cap, shelf count / cap |
| **Upgrade popup** | Per-target (worker/machine/coop): current level, next-level stats, cost button |
| **Phone popup** | Incoming call: order contents, payout, Accept / Decline, 10-min countdown once accepted |
| **Price panel** | Per shelf: −/+ price steppers, base-price reference, offer toggle |
| **Offer popup** | Pick item, discount %, duration |
| **Pause / Settings** | Resume, sound/music toggles, save & quit |
| **Tutorial** | Contextual arrows + task banner for first 10 minutes (harvest → shelf → sell → upgrade) |

---

## 12. Audio & VFX (summary — full list in TDD §8–9)

**Audio:** BGM loop, pickup/drop, coin, upgrade, button, customer chatter, machine hums (3 distinct), thief sting, phone ring, order success/failure.
**Particles:** coin burst, walk dust, harvest pop, machine smoke, upgrade glow, money-fly-to-HUD, net capture puff.

---

## 13. Balancing Targets

| Metric | Target |
| :-- | :-- |
| Session length | 6–10 min average |
| Time to first upgrade ($50) | ≤ 4 min of play |
| Time to L2 (Cashier 1) | ~10–15 min |
| Time to L5 max | 4–6 hours of cumulative play |
| Shelf-sale income at L5 | ~$1.5–3 / minute (intentionally thin) |
| Phone-order share of income | 60–70% mid/late game |
| Thief loss if ignored | ~$0.50–1.50 of goods per event |

---

## 14. Monetization & Analytics (post-MVP)

**Monetization:** interstitial ads between sessions, rewarded ads (2× phone-order payout, instant machine finish), Remove Ads IAP, starter pack, daily reward, offline earnings.
**Analytics events:** tutorial_finished, upgrade_bought (track+level), phone_order_accepted / _completed / _failed, thief_spawned / _caught / _escaped, offer_created, session_length, level_reached.

---

## 15. Release Phasing

- **v1.0 (MVP):** full loop, all workers, all machines, phone orders, both counters, save/load, tutorial.
- **v1.1:** thief + net polish, trolleys, offers, price elasticity.
- **v1.2:** monetization, analytics, daily rewards, offline earnings, expansion area.
