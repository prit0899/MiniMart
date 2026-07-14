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

## 6. QA harness (no MCP / no editor scripting needed)

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

## 7. Key invariants (break these and the game breaks)

1. **Visual walls == grid walls.** Always update both.
2. **`PriceCatalog.UnlockLevel` must mirror the purchase-pad ladder exactly.**
3. **XP comes only from picking cash up off the floor** (`MoneyStack`), not from
   direct `Economy.Deposit`.
4. **`DataValidator` must stay green** — it enforces the curves, ladder, and
   pricing. A red validator can freeze the game if "Error Pause" is on.
5. The player must remain **the fastest character** in the scene.
