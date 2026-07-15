# MiniMart — Developer Onboarding & Troubleshooting Guide

Welcome to the **MiniMart** codebase! This document provides a complete map of the project architecture, explanation of key systems, and guidance on how to modify layouts (specifically, how to relocate the **Tomato Farm**) and fix common bugs.

---

## 1. Project Directory Map

The core codebase is organized under `Assets/Scripts/` with clean separation of concerns:

```
Assets/Scripts/
├── Core/
│   └── GameEnums.cs           — Global definitions (ItemType, RoleType, CharacterState, BagType)
│
├── Catalog/                   — Static, designer-tunable configuration (never mutated at runtime)
│   ├── FarmCatalog.cs         — Dimensions, yield limits, and regrowth rates (Tomato, Wheat, Hen, Cow, Herb, Corn, Apple)
│   ├── PriceCatalog.cs        — Item base prices, unlock levels, checkout price floor
│   ├── RoleCatalog.cs         — Player & NPC speed/carry upgrade curves and costs
│   └── UpgradeCurve.cs        — Generic data structures for upgrade progressions
│
├── Map/                       — Navigation, grid layouts, and visual mapping
│   ├── MapLayout.cs           — World-space zone anchors (single source of truth for coordinates)
│   ├── MapTileGenerator.cs    — Procedural Tilemap floor/wall grid generator
│   └── GridPathfinder.cs      — A* pathfinding on a 2D walkability grid
│
├── Production/                — Farms, coops, and processing machines
│   ├── TomatoFarm.cs          — Logic for tomato plant growth cycles (6 slots)
│   ├── WheatFarm.cs           — Logic for binary grid harvest tiles (3x4 tiles)
│   ├── HenCoop.cs             — Logic for egg laying (2 hens)
│   ├── CowPen.cs & HayFeedTrough.cs — Logic for milk production
│   └── Machine.cs             — Upgradable factory processors (Blender, Mill, Oven, etc.)
│
├── Characters/                — All actors sharing a common base state machine
│   ├── CharacterBase.cs       — Movement, waypoint navigation, and FSM state
│   ├── PlayerController.cs    — Player input, speed multipliers, and net-throwing
│   ├── PlayerInteraction.cs   — Automatic proximity harvesting and loading/depositing
│   ├── Farmer.cs              — NPC that collects raw crops and moves them to storage
│   ├── Chef.cs                — NPC that collects ingredients and runs processors (Mixer, Oven, Blender)
│   ├── Shelver.cs             — NPC that restocks store shelves from storage racks
│   └── Cashier.cs             — NPC that mans checkout counters
│
├── Economy/                   — Checkout flow, tips, and financial transaction management
│   ├── EconomyManager.cs      — Money handling, internal cents representation, manual overrides
│   └── CashCounter.cs         — Checkout queue slots and cashier actions
│
├── Engine/                    — Main game cycle managers, UI builders, and helpers
│   ├── GameManager.cs         — Central tick-driver (0.15s SimTick), level progression, and save loops
│   ├── SceneBootstrapper.cs   — Procedurally instantiates and wires the main scene (Game.unity)
│   ├── SceneBootstrapper2.cs  — Procedurally instantiates and wires the second scene (MegaMart.unity)
│   └── PrimitiveFactory.cs    — Spawns 3D mesh representations using basic Unity shapes (primitives)
│
├── Runtime/
│   └── StoreInventory.cs      — Central inventory caps and storage overflow dustbin routing
│
└── Save/
    └── SaveSystem.cs          — Dual-buffered JSON save system using PlayerPrefs
```

---

## 2. World Concept & Bootstrapping

This project uses **procedural scene bootstrapping**. 
* There are no pre-placed gameplay GameObjects in `Game.unity` or `MegaMart.unity`.
* At runtime, `SceneBootstrapper` (in **Game.unity**) or `SceneBootstrapper2` (in **MegaMart.unity**) instantiates every machine, shelf, farm, worker, and wall dynamically via code, using `PrimitiveFactory` to build 3D geometry from built-in primitives.

### The Two Scenes (Two-Mart Split)
1. **`Game.unity`** (Mini Mart): Features Tomato, Egg, and Wheat production chains. Wired by [SceneBootstrapper.cs](file:///Users/prit/Documents/PRIT/untitled%20folder%202/MiniMart/Assets/Scripts/Engine/SceneBootstrapper.cs).
2. **`MegaMart.unity`** (Mega Mart): Features Milk, Corn, Herb, Coffee, and Apple chains. Wired by [SceneBootstrapper2.cs](file:///Users/prit/Documents/PRIT/untitled%20folder%202/MiniMart/Assets/Scripts/Engine/SceneBootstrapper2.cs).

---

## 3. How to Change the Tomato Farm Location

Right now, the Tomato Farm's location is defined entirely in code inside the scene bootstrapper. Because characters and locators fetch the position dynamically from the GameObject's transform, changing the position is extremely clean:

### Step 1: Change the Coordinates in the Bootstrapper
For the main scene (`Game.unity`), open [SceneBootstrapper.cs](file:///Users/prit/Documents/PRIT/untitled%20folder%202/MiniMart/Assets/Scripts/Engine/SceneBootstrapper.cs) and locate line **183**:

```csharp
// Current Code:
var tomatoFarmGO = CreateAt("TomatoFarm", new Vector2(16f, 12f));
```

* **Vector2 Parameter:** The `new Vector2(Xf, Zf)` maps directly to 3D world coordinates `Vector3(Xf, 0f, Zf)`.
* **Modification:** Simply update `16f` (X-axis position) and `12f` (Z-axis position) to your desired coordinates. For example, to move it 4 units to the right and 3 units up:
  ```csharp
  var tomatoFarmGO = CreateAt("TomatoFarm", new Vector2(20f, 15f));
  ```

---

### Step 2: Understand the Interplay with Other Systems
If you relocate the Tomato Farm, keep the following dependencies in mind:

#### A. Pathfinder & Boundary Obstacles
* Obstacles like walls and boundaries are baked dynamically at startup in `BuildBoundaryWalls(pf)` (inside [SceneBootstrapper.cs](file:///Users/prit/Documents/PRIT/untitled%20folder%202/MiniMart/Assets/Scripts/Engine/SceneBootstrapper.cs)).
* **Rule:** Do not place the Tomato Farm on top of coordinate lines that are marked blocked (e.g., Z=40, Z=60, X=-20, X=30) or outside the grid bounds `(-50 to 50 on X, 0 to 82 on Z)`.

#### B. Visual Floor Tiles (MapTileGenerator)
* [MapTileGenerator.cs](file:///Users/prit/Documents/PRIT/untitled%20folder%202/MiniMart/Assets/Scripts/Map/MapTileGenerator.cs) stamps floor textures based on rectangular boundaries.
* The base map is green grass (`FarmTile`).
* **Rule:** If you move the Tomato Farm, ensure it remains in a grass zone. Avoid placing it inside the store bounds (`Z >= 14`), the processing road strip (`Z ∈ [8, 12]`), or the road (`Z >= 23`), otherwise the dirt patch will sit awkwardly on tiles meant for cashiers or machinery.

#### C. Character Navigation is Fully Dynamic
* Workers (Farmer, Chef) navigate to the farm via:
  ```csharp
  SetTarget(tomatoFarm.transform.position);
  ```
* Because they request A* paths to the live `transform.position` of the farm, they will automatically find the new location and walk to it without any pathing adjustments needed!

#### D. Locator Icons are Fully Dynamic
* The floating locator icon (visual item prompt) is parented to the farm:
  ```csharp
  Locator(tomatoFarmGO, Core.ItemType.Tomato);
  ```
* This means the floating Tomato icon will automatically move to the new location alongside the farm.

---

## 4. Developer Bug-Fixing & Troubleshooting Checklist

Use this checklist to diagnose and resolve bugs in the codebase:

### 1. Characters Stuck or Walking Through Walls
* **Invisible Barriers:** Check `BuildBoundaryWalls` in [SceneBootstrapper.cs](file:///Users/prit/Documents/PRIT/untitled%20folder%202/MiniMart/Assets/Scripts/Engine/SceneBootstrapper.cs). Ensure no grid cells are blocked where actors need to walk.
* **Walking Through Obstacles:** The character controller must follow waypoints. Check [CharacterBase.cs](file:///Users/prit/Documents/PRIT/untitled%20folder%202/MiniMart/Assets/Scripts/Characters/CharacterBase.cs)'s navigation method. Direct linear movements using `MoveTowards` bypassing A* are forbidden.

### 2. Machines Refusing Input (Production Deadlocks)
* **Chef Stalling:** If the Chef is spinning in place, check `Chef.cs`'s FSM logic. Ensure that the chef can find all required ingredients. For example, if a recipe requires Flour and Eggs, but the Mill is not unlocked/built, the chef's fetch validation will fail.
* **Storage Capacity:** Check [StoreInventory.cs](file:///Users/prit/Documents/PRIT/untitled%20folder%202/MiniMart/Assets/Runtime/StoreInventory.cs). If a storage rack is full, farmers will route overflow to the dustbins. If the dustbins are also full, production stops.

### 3. Save File / UI Inconsistencies
* **Cent Float Rounding:** The game represents cash internally as a `long` in cents (`100 = $1.00`) to prevent float precision drift. Ensure all new financial calculations use internal cents and only format to dollars in the UI text elements.
* **Save/Load Crashes:** If adding a new item type to [GameEnums.cs](file:///Users/prit/Documents/PRIT/untitled%20folder%202/MiniMart/Assets/Scripts/Core/GameEnums.cs), make sure to add its default storage limits to [FarmCatalog.cs](file:///Users/prit/Documents/PRIT/untitled%20folder%202/MiniMart/Assets/Scripts/Catalog/FarmCatalog.cs) and update the JSON serialization mappings in [SaveSystem.cs](file:///Users/prit/Documents/PRIT/untitled%20folder%202/MiniMart/Assets/Scripts/Save/SaveSystem.cs) to avoid schema discrepancies.
