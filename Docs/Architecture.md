# Architecture — Mini Mart

> **How the app is put together.** Read this before touching code.
> Companion docs: [PRD](PRD.md) · [Rules](Rules.md) · [Phases](Phases.md) · [Design](Design.md) · [Memory](Memory.md)

---

## 1. Tech stack

| Layer | Choice | Notes |
|---|---|---|
| Engine | **Unity 6000.5.1f1** | Exact version — do not upgrade casually. |
| Render pipeline | **Built-in (BiRP)** | **URP is NOT installed.** Do not write URP/HDRP shaders or Shader Graph assets that assume it. |
| Language | C# | |
| Target | **iOS**, portrait, touch | Build target is already set to iOS. |
| UI | uGUI (`com.unity.ugui`) + TextMeshPro | Legacy `Text`/`Image`, built from code. |
| Avatar | Genies Avatar SDK | Optional; auto-skips if unavailable. |
| Save | `PlayerPrefs` (JSON blob) | Key: `MiniMart_Save_v1`. |
| Installed but unused | `com.unity.purchasing`, services.authentication | Don't build on these yet. |

## 2. THE most important architectural fact

> ### The scenes are **built entirely from code at runtime.**

Each scene contains essentially **one GameObject: `_Bootstrap`**. Everything else
— ground, walls, farms, machines, shelves, counters, doors, workers, the player,
the whole HUD — is created in C# when the scene starts.

| Scene | Bootstrapper class | Role |
|---|---|---|
| `Assets/Game.unity` | `MiniMart.SceneBootstrapper` | **Mart 1** (tomato, wheat, egg chains) |
| `Assets/MegaMart.unity` | `MiniMart.SceneBootstrapper2` | **Mart 2 / MegaMart** (milk, corn, herb, apple, cheese, coffee) |

**Consequences you must internalise:**
- **To change the map, you edit C#, not the scene.** Dragging objects in the
  Unity editor does nothing — they'll be wiped/ignored on play.
- Object positions are literal `Vector2/Vector3` coordinates in the bootstrapper.
- Both bootstrappers must be kept in sync conceptually (same core map plan).

## 3. World coordinate layout (both marts)

One **enclosed building**. Mart 1 spans `x[-20,10]`, MegaMart `x[-20,20]`; both
span `z[14,60]`.

```
            z=60  ┌──────── NORTH WALL (closed) ────────┐
                  │                                     │
   ENTRY doors    │          SHOP FLOOR                 │   EXIT doors
   (west wall,    │   shelves, cash counters,           │   (east wall,
    z 44-46 &     │   bread oven                        │    z 44-46 &
    z 54-56)      │                                     │    z 54-56)
            z=36  ├───── INTERIOR WALL ──[gap x 1..9]───┤   ← the only way between
                  │                                     │     shop and farm
                  │   FARM + PROCESSING ZONE            │
                  │   farm plots, machines, racks,      │
                  │   player upgrade pads               │
            z=14  └──────── SOUTH WALL (closed) ────────┘

   Outside the walls: grass, the road (z~70), buyer spawn/exit,
   the phone-order van, and the travel pad to the other mart.
```

- **Walls exist twice** and *must agree*: a **visual mesh** (`PrimitiveFactory.Wall`)
  and a **pathfinding grid block** (`BuildBoundaryWalls` → `GridPathfinder.SetWalkable`).
  If they disagree, players slide into invisible walls or walk through visible ones.
- Movement is blocked in `CharacterBase.MoveTowardsTarget`, which checks the grid
  every step (with axis-slide). **This is what enforces "no teleporting through walls."**

## 4. Folder & file structure

```
MiniMart/
├── Assets/
│   ├── Game.unity              # Mart 1 scene  (just _Bootstrap)
│   ├── MegaMart.unity          # Mart 2 scene  (just _Bootstrap)
│   ├── Design/                 # GDD, TDD, BACKLOG, QA reports, reference images
│   ├── Resources/              # UI sprites, SFX, music loaded by name at runtime
│   ├── Genies/                 # Avatar SDK  (GITIGNORED — contains auth artifact)
│   └── Scripts/
│       ├── Core/        (1)    # ItemType, RoleType — the shared enums
│       ├── Catalog/     (5)    # DATA: PriceCatalog, RoleCatalog, FarmCatalog,
│       │                       #       StorageCatalog, UpgradeCurve
│       ├── Runtime/     (1)    # StoreInventory (storage + overflow bins)
│       ├── Characters/ (11)    # CharacterBase, PlayerController, PlayerInteraction,
│       │                       #   Farmer, Chef, Shelver, Cashier, Worker, NetTool
│       ├── Production/  (9)    # Farms (Tomato/Wheat/Corn/Herb/Apple), HenCoop,
│       │                       #   CowPen, HayFeedTrough, Machine
│       ├── Economy/     (2)    # EconomyManager (prices/offers), CashCounter
│       ├── AI/          (5)    # Buyer, BuyerSpawner, Thief, TaskSystem, AssistantNode
│       ├── Engine/     (32)    # ← the big one, see below
│       ├── Map/         (3)    # GridPathfinder, MapLayout
│       ├── Save/        (1)    # SaveSystem (PlayerPrefs JSON)
│       └── Editor/      (3)    # editor-only tooling
├── Docs/                       # ← THESE SIX DOCS (start here)
├── Refer/                      # reference game links + map-plan-mart1.pdf
├── Logs/                       # Editor.log + TesterBot diary (gitignored)
└── plan.md, GDD.md, TDD.md ... # older long-form design docs
```

### `Scripts/Engine/` — the important ones

| File | Responsibility |
|---|---|
| `SceneBootstrapper.cs` | **Builds all of Mart 1.** Map coords live here. |
| `SceneBootstrapper2.cs` | **Builds all of MegaMart.** |
| `GameManager.cs` | Singleton. Store level, XP, save/load, syncs level to systems. |
| `PrimitiveFactory.cs` | Every mesh/visual in the game (walls, farms, characters, items). |
| `HUDBuilder.cs` / `HUDController.cs` | Builds & drives the entire UI from code. |
| `PurchasePad.cs` | Level-locked "stand here to buy" pads. |
| `UpgradePad.cs` | Player Carry / Speed / Crop Speed upgrades. |
| `SceneTransition.cs` | Dwell pad that travels between the two marts. |
| `PhoneOrderManager.cs` | Bulk van orders. |
| `DataValidator.cs` | **~45,000 assertions run on every boot.** Your safety net. |
| `TesterBot.cs` | Automated QA player (see §6). |
| `Retention.cs` | Daily bonus, goals, UnlockGuide arrow. |
| `SaveSystem` (in `Save/`) | Autosave, offline earnings. |

## 5. Runtime flow

```
Scene loads
   └─ _Bootstrap (SceneBootstrapper*.Awake)
        ├─ camera + lighting
        └─ Start() → Bootstrap()
             ├─ ground, floors, walls  (visual)
             ├─ GridPathfinder + BuildBoundaryWalls  (collision truth)
             ├─ farms, machines, storage racks
             ├─ shelves, cash counters, doors
             ├─ player, workers, buyers spawner
             ├─ GameManager (+ DataValidator runs ~45k asserts)
             ├─ HUDBuilder.Build()
             └─ Gate(...) calls → creates level-locked PurchasePads
```

`GameManager.SyncLevelDependents()` pushes the current store level into
`BuyerSpawner`, `PhoneOrderManager`, `TheftManager`, and the cash counters
whenever the level changes.

## 6. Algorithms & data structures (the engineering core)

The systems below are where the "same-to-same engineering standards / DSA" work
lives. Each is documented with its data structure, the algorithm, and its cost so
another developer (or a portfolio reviewer) can reason about it without reading
every line.

### 6.1 Navigation pipeline — `Map/NavMesh.cs`, `Map/GridPathfinder.cs`

A four-stage pipeline turns a raw walkability grid into smooth, natural worker
paths. It replaced a naive 4-neighbour grid A\* that could only emit Manhattan
staircases (workers turning in hard 90° corners — the owner's "not like the
recent game" complaint).

| Stage | Data structure | Algorithm | Cost |
|---|---|---|---|
| **1. Bake** | `bool[,]` walkability → `List<Poly>` (each an axis-aligned rect: `xMin/xMax/zMin/zMax`) + `int[,] cellToPoly` | **Greedy maximal-rectangle decomposition**: scan for the first free cell, grow right while free, then grow up while the whole row span stays free; stamp those cells to one poly; repeat. Collapses a ~100×82 = 8,200-cell grid into a few dozen convex polygons. | `O(cells)` amortised — each cell is claimed once. Runs once at boot. |
| **2. Link** | Per-poly `Links` (neighbour ids) + `Portals` (world-space shared-edge segments) | Two polys sharing a cell edge get a **portal** = the overlapping span of that edge, **inset by `AgentRadius` (0.35u)** at both ends so a body-radius agent rounds the corner without clipping. | `O(polys²)` over a few dozen nodes — negligible, once at boot. |
| **3. A\*** | `float[] gScore`, `int[] cameFrom`, `List<int>` open set | **A\*** over the *polygon graph* (a few dozen nodes, not 8,200 cells). Heuristic = straight-line distance between poly centres (admissible → optimal). Open set is a linear min-scan `List` — deliberately not a binary heap, because at this node count the heap's constant factor loses; documented as a conscious trade-off. | `O(V²)` worst case with V ≈ dozens — microseconds. On-demand per retarget, **not per frame**. |
| **4. Funnel** | Portal corridor → `List<Vector3>` corner points | **Simple Stupid Funnel Algorithm** (string-pulling): walk the portal corridor keeping a left/right "funnel", emit a corner only when the funnel would invert. Produces the true shortest path *inside* the corridor — natural diagonals that hug corners instead of axis-aligned zig-zags. | `O(portals)` linear. |

`GridPathfinder` remains as an **8-neighbour A\* fallback** (and the collision
"source of truth" — `IsWalkableCell`) for the rare case the nav mesh has no route;
below that, a straight-line grid-checked move guarantees an agent never teleports
through geometry (invariant #1).

### 6.2 Steering — `Characters/Steering.cs`, integrated in `CharacterBase`

Path corner points are *followed* by **classic Reynolds steering** on the XZ
plane. Every behaviour returns a **force** (desired-velocity minus current), the
caller sums the ones it wants, clamps to `maxForce`, and integrates into velocity
— that accumulation is what makes agents bank into turns and ease out instead of
snapping headings.

- **Seek / Arrive** — head to the next corner; Arrive ramps speed down inside
  `slowRadius` so agents don't overshoot-and-jitter at the goal.
- **Separation** — inverse-distance push from nearby agents (`(1 − d/radius)`,
  closer ⇒ stronger) so a crowd flows around itself instead of grinding through.
- **Flee** — scatter from a threat (thief / over-crowded till).
- **AvoidWalls** — three look-ahead "feelers" (centre + two whiskers); a feeler on
  an unwalkable cell steers the agent away, so it curves around furniture rather
  than bumping and right-angle-sliding.

This layer is **allocation-free per frame** — it runs for the whole crowd every
frame with zero GC pressure, which is the system-design property that keeps the
frame budget safe on a phone.

### 6.3 Greedy item selection — the "wheat shelf" answer

Both buyers (`AI/Buyer.cs`) and shelvers (`Characters/Shelver.cs`) originally used
an **all-or-nothing greedy** that picked the *first* basket item / *emptiest*
shelf and then blocked on it — so a buyer waiting on sold-out bread would ignore a
FULL tomato shelf beside it (the owner's exact bug report). The corrected model:

- **Buyer** → `FindBestStockedShelf()`: the **nearest shelf holding ANY still-wanted
  item that is actually in stock**. Items leave the shelf **one-per-beat**, so
  several buyers at one shelf interleave and *share* stock naturally; whoever is
  left when it runs dry pays for a partial basket (≤10 items/order).
- **Shelver** → picks the **emptiest shelf it can actually refill** (has storage
  stock for), not the emptiest outright — so it never idles next to an
  un-restockable shelf while others sit empty.

Proven by a controlled experiment (`WheatShelfExperiment`): wheat shelf forced to
20/20 MAX, every other shelf + wheat storage zeroed, 12 buyers → **all 20 taken in
75 sim-s**. The algorithm was never the failure; missing restock + checkout scrum
were (see [Memory.md](Memory.md) row #22).

### 6.4 Other notable structures

- **`GrowthSlot[]`** (farms, hen, cow) — each slot is an independent fill timer;
  regrowth and capacity ceilings are data (`FarmCatalog`), not code.
- **`HashSet<string>` of purchased pads** (`GameManager`) — O(1) gate checks that
  also drive `PurchasePad.RequiredPurchase` dependency locks (e.g. kitchen pads
  need "Hire Chef" first).
- **`Machine.SplitCapacity`** — one flag swaps a station between a single buffer
  (MegaMart legacy) and independent 4→6→8 input/output buffers with two upgrade
  tracks (Mart-1 spec), so both economies share one class.
- **Marker-file QA harness (§7)** — a deliberately dependency-free IPC: the shell
  drops a file in `Logs/`, the editor polls and obeys. Chosen because Unity MCP is
  entitlement-gated and UI automation is fragile; a filesystem poll never breaks.

## 7. QA harness (no MCP / no editor scripting needed)

Marker files in `Logs/` drive an automated player:

| Marker | Effect |
|---|---|
| `Logs/testerbot.enabled` | Entering Play spawns the **TesterBot** |
| `Logs/testerbot.freshrun` | Wipes the save once (consumed) |

The bot plays with **player-legal inputs only** (tap-to-move, pad dwell,
proximity) — it never teleports or grants itself cash. It writes a live diary to
`Logs/testerbot_run.txt` (a STATUS line + timestamped events), which you can
`tail` from a normal shell. It buys purchase pads *and* player upgrades, so runs
exercise the real economy.

## 8. Key invariants (break these and the game breaks)

1. **Visual walls == grid walls.** Always update both.
2. **`PriceCatalog.UnlockLevel` must mirror the purchase-pad ladder exactly.**
3. **XP comes only from picking cash up off the floor** (`MoneyStack`), not from
   direct `Economy.Deposit`.
4. **`DataValidator` must stay green** — it enforces the curves, ladder, and
   pricing. A red validator can freeze the game if "Error Pause" is on.
5. The player must remain **the fastest character** in the scene.
