# MiniMart — Unity C# Script Package
## Setup

### Requirements
- Unity 2022.3 LTS or newer
- TextMeshPro (Window → Package Manager → install)
- No other third-party packages required

### Quick Start
1. Create a new **2D (URP)** Unity project.
2. Copy the `Assets/Scripts/` folder into your project's `Assets/` folder.
3. Create a new empty scene.
4. Add an empty GameObject named `_Bootstrap`, attach **SceneBootstrapper**.
5. Press Play — the full game scene is wired at runtime automatically.
6. Replace the placeholder colour sprites with your actual art assets.

---

## File Map

```
Assets/Scripts/
├── Core/
│   └── GameEnums.cs           — ItemType, RoleType, CharacterState, BagType, QualityPreset
│
├── Catalog/                   ← Static / designer-tunable data (never mutated at runtime)
│   ├── UpgradeCurve.cs        — UpgradeStep struct + UpgradeCurve class + cost-progression builder
│   ├── RoleCatalog.cs         — Player/Shelver1/Shelver2/Chef/Farmer upgrade ladders
│   ├── ProductionCatalog.cs   — Blender/Oven/Mill/HenCoop shared lvl1-4 curves
│   ├── FarmCatalog.cs         — Tomato (2×3), Wheat (3×4), Hen, growth-rate constants
│   └── PriceCatalog.cs        — Base prices, unlock levels, $1 floor, phone/theft timers
│
├── Runtime/
│   └── StoreInventory.cs      — Per-item storage (capped), 2-dustbin overflow routing
│
├── Characters/                ← All actors: share CharacterBase
│   ├── CharacterBase.cs       — id/role/level/speed/carry/state/target — base MonoBehaviour
│   ├── PlayerController.cs    — Fastest worker; pause/resume/price/stock/offer/catch-thief
│   ├── NetTool.cs             — Ready → Thrown → Cooldown FSM for thief capture
│   ├── Shelver.cs             — Shelver + ShopShelf; greedy restock of responsible item set
│   ├── Chef.cs                — Self-fetches from farms; drives Blender + Oven
│   ├── Farmer.cs              — Ferries eggs/tomato/wheat from farms to storage
│   ├── Cashier.cs             — Stands at assigned CashCounter; state driven by queue size
│   └── TrolleyController.cs   — Swaps hand-carry ↔ trolley sprite based on BagType
│
├── Production/
│   ├── Machine.cs             — Blender/Oven/Mill: queue input → process → output, upgradeable
│   ├── TomatoFarm.cs          — 6 GrowthSlots, 1 tomato / 0.5 s, max 3 per plant
│   ├── HenCoop.cs             — 2 hens, same growth cycle, lvl1-4 lay-rate upgrade
│   └── WheatFarm.cs           — 12 binary boxes (3×4), 1 wheat per box
│
├── AI/
│   ├── Buyer.cs               — Random unlock-gated basket; hand-carry < 5 items else trolley
│   ├── BuyerSpawner.cs        — Level-scaled spawn timer; spawns Buyer prefabs at entry door
│   └── Thief.cs               — TheftManager (4-5 min timer) + Thief FSM (enter→steal→flee)
│
├── Economy/
│   ├── EconomyManager.cs      — Prices, manual overrides, 20% offer, $1 basket floor, cash
│   └── CashCounter.cs         — Queue + checkout; staffing unlock at player lvl 2 / 4
│
├── Map/
│   ├── MapLayout.cs           — ZoneId enum + anchor references for every named room
│   ├── MapTileGenerator.cs    — Stamps floor/farm/storage/door/wall tiles from RectInt zones
│   └── GridPathfinder.cs      — A* on a 30×20 boolean walkability grid
│
├── Engine/
│   ├── GameManager.cs         — Central tick driver; wires all systems; save on quit
│   ├── PhoneOrderManager.cs   — 4-5 min call interval; $45-$300 bundle; 10-min window
│   ├── PlayerInputHandler.cs  — WASD + tap-to-move + net-throw + pause
│   ├── HUDController.cs       — Cash/level bar, pause overlay, phone-order popup
│   ├── PricePanelController.cs— Per-item price sliders + offer toggles
│   ├── UpgradeButton.cs       — Generic upgrade row for characters, machines, hen coop
│   ├── InventoryPanel.cs      — Per-item fill bars updated every frame
│   ├── CarryVisual.cs         — Floating item icons above any character's head
│   └── SceneBootstrapper.cs   — Builds entire scene hierarchy at runtime; no manual wiring
│
└── Save/
    └── SaveSystem.cs          — JSON serialize/deserialize via PlayerPrefs; swap for file IO
```

---

## Key Design Rules (from spec)

| Rule | Where enforced |
|---|---|
| Player always faster than workers | `RoleCatalog.PlayerCurve` speedMultiplier always > NPC curves |
| Chef ceiling ≥ Shelver ceiling | `RoleCatalog.ValidateChefVsShelverCeiling()` — call in tests |
| Basket/bundle never < $1 | `PriceCatalog.ApplyBundleFloor()` called in every checkout + phone-order |
| No buyer requests unlocked item | `Buyer.GenerateBasket()` filters by `PriceCatalog.IsUnlocked()` |
| Thief only catchable inside store | `Thief.HasLeftStore` zone flag checked in `PlayerController.TryCatchThief` |
| Counter 1 cashier from lvl 2 | `PriceCatalog.Cashier1AssignableLevel = 2` |
| Counter 2 unlocks at lvl 4 | `PriceCatalog.CashCounter2UnlockLevel = 4` |
| Upgrade cost 50→100→200→500→… | `UpgradeCurve.BuildCostProgression()` |
| Storage overflow → dustbin | `StoreInventory.Deposit()` routes surplus to least-loaded dustbin |
| Views never own gameplay truth | UI reads from `GameManager` systems; buttons dispatch commands only |

---

## Art Placeholder
`SceneBootstrapper.EnsureSprite()` loads `Resources/Sprites/Placeholder_Circle`.  
Create a white circle sprite, save it at that path, and the game will run immediately with
coloured circles representing each character type until real art is dropped in.

---

## Next Steps (Phase 2)
- Animator controllers per character role (idle / walk / carry / celebrate)
- SpriteAtlas for all items and characters
- Particle feedback for level-up, sale, theft catch
- Audio: SFX bus for ambience, tills, farm, machines
- Cloud save via Unity Gaming Services
- Performance profiling pass against Architecture Spec caps (20-30 visible animated chars)
