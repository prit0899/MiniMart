# Mini Mart — Technical Design Document (TDD)
**Version:** 2.0
**Status:** Authoritative technical spec — supersedes v1.0
**Engine:** Unity 6000.5.1f1
**Companion documents:** [GDD.md](GDD.md) (design + numbers), [ROADMAP.md](ROADMAP.md) (build order)

All gameplay numbers (stats, prices, capacities) live in the GDD; this document defines *how* they are implemented. When the two disagree, the GDD wins.

---

## 1. Project Setup

| Setting | Value |
| :-- | :-- |
| Unity version | **6000.5.1f1** (locked; upgrade only at version boundaries) |
| Render pipeline | Built-in RP for MVP (current state). URP migration is a v1.2 task — tracked, not blocking. |
| Input | Legacy Input Manager wrapped by `PlayerInputHandler` (keyboard WASD + virtual joystick). All input reads go through this single class so an Input System migration touches one file. |
| Target FPS | 60 (`Application.targetFrameRate = 60` in `GameManager.Awake`) |
| Orientation | Landscape primary (auto-rotate landscape only) |
| Quality | 3 presets mapped to `QualityPreset` enum (LowPower / Balanced / High): shadows off/hard/soft, no post on LowPower |
| Physics | 3D physics, gravity default; `Physics.autoSyncTransforms = false` |

### Layers
| Layer | Used for |
| :-- | :-- |
| `Ground` | floor plane (tap-to-move raycast) |
| `Character` | player, workers, buyers, thief |
| `Interactable` | shelves, machines, storage, counters, upgrade pads |
| `Obstacle` | walls, bins, farm fences (pathfinding blockers) |
| `UI` | canvas |

### Tags
`Player`, `Buyer`, `Thief`, `Worker`, `Shelf`, `Storage`, `Machine`, `CashCounter`, `ExitDoor`, `TrolleyRack`, `Bin`.

---

## 2. Scene Flow

```
Splash ──► Loading ──► Main Menu ──► Gameplay ◄──► Pause ◄──► Settings
                                        │
                                        └──► (no Game Over — idle game; failure states are soft)
```

MVP ships **two scenes**: `Boot` (splash + load save + menu) and `Game`. Pause/Settings are canvas overlays inside `Game`, not scenes. `EditorBuildSettings` lists Boot at index 0, Game at index 1.

---

## 3. Scene Hierarchy (Game scene)

Generated procedurally by `SceneBootstrapper` at boot (primitives via `PrimitiveFactory`), so the hierarchy below is *runtime* truth, not a hand-authored scene:

```
Game
├── Main Camera            [CameraRig, CameraFollow]
├── Directional Light
├── Managers               [GameManager, EconomyManager, SaveSystem,
│                           PhoneOrderManager, TheftManager, BuyerSpawner]
├── NavigationGrid         [GridPathfinder, MapLayout]
├── Store_Structure        (walls, entrance door, exit door, 4 secondary exit doors)
├── AisleA                 (EggStorage, CashCounter_1, TomatoArea, HenCoop)
├── AisleGap1              (SecondaryExit, Bin_1, SecondaryExit)
├── AisleB                 (KetchupStorage+Blender, Mill+FlourStorage, WheatStorage, WheatFarm)
├── AisleGap2              (SecondaryExit, Bin_2, SecondaryExit)
├── AisleC                 (BreadStorage+Oven)
├── CashCounter_2          (locked until Player L4)
├── TrolleyRack            (pooled trolleys, entrance side)
├── Canvas                 [HUDBuilder output: HUDController, InventoryPanel,
│                           PricePanelController, UpgradeButton(s), PhonePopup, PausePanel]
└── Dynamic_Entities       (Player, Farmer, Chef, Shelver_1, Shelver_2,
                            Cashiers, pooled Buyers, Thief)
```

Anchor world-positions for every zone come from `MapLayout` (single source of truth for the GDD §10 map). All AI destinations reference `MapLayout` anchors — never hard-coded vectors.

---

## 4. Camera

| Parameter | Value |
| :-- | :-- |
| Projection | Perspective, isometric-style |
| Pitch | 45° |
| Yaw | fixed (no rotation) |
| Height | 16 u above player |
| Follow | `CameraFollow` — smooth damp, follow speed 5, dead zone 0.5 u |
| Zoom | +15% distance as player carry stack grows (lerped) |
| Bounds | clamped to store rect + 4 u margin (`MapLayout.CameraBounds`) |
| Shake | 0.15 s, 0.2 amplitude on thief-caught and big-payout events |
| Safe area | HUD anchors respect `Screen.safeArea` (notch devices) |

---

## 5. Player Controller

- **Movement:** `CharacterController` component (capsule), speed from `RoleCatalog` (GDD §3.1 table). Acceleration 0→max in 0.1 s, instant stop. Character rotates toward move direction (slerp, 720°/s).
- **Input:** virtual joystick (floating, anchors to first touch in lower half of screen) + WASD in editor. Dead zone 0.15.
- **Interaction:** proximity triggers, radius 1.2 u — no button needed. Standing in a trigger auto-starts the interaction (harvest, load machine, take from storage, man the counter, upgrade pad).
- **Pickup/Drop:** automatic on trigger overlap, throttled to 1 item / 0.12 s for readable stack animation. Carry limit from player level (4→7).
- **Net throw:** when a thief is active and within 6 u, HUD shows a Throw button; net is a pooled parabolic projectile, capture check on arrival (see §14 Thief).

### Physics choices (per object type)
| Object | Setup |
| :-- | :-- |
| Player | `CharacterController` (capsule), no Rigidbody |
| Workers / Buyers / Thief | kinematic Rigidbody + CapsuleCollider (trigger interactions), moved by pathfinder |
| Carried items | no colliders — purely visual children of `CarryVisual` |
| Shelves / machines / storage | BoxCollider (solid) + child BoxCollider (trigger) for the interaction zone |
| Walls / bins / fences | BoxCollider on `Obstacle` layer |
| Coins | small Rigidbody burst on spawn, then kinematic magnet-to-player |

---

## 6. System Architecture

Data-driven, tick-based simulation. Rendering (wobble, carry visuals, camera) runs per-frame; game logic runs on a fixed **SimTick of 0.15 s** driven by `GameManager`.

```
                    ┌──────────────────────────┐
                    │       GameManager        │
                    │  (SimTick hub, 0.15 s)   │
                    └────────────┬─────────────┘
     ┌──────────┬────────────┬───┴────────┬────────────┬───────────┐
     ▼          ▼            ▼            ▼            ▼           ▼
EconomyMgr  TheftManager PhoneOrderMgr SaveSystem  BuyerSpawner MapLayout
     │
     └── CashCounter ×2, PricePanelController, offers
```

**Catalogs (static config, `Assets/Scripts/Catalog/`):**
- `PriceCatalog` — base prices in **dollars as `float` cents-precision** (Tomato 0.04 … Bread 0.30), unlock level per item, ±50% clamp, $0.01 floor.
- `RoleCatalog` — per-role level tables (speed, stack, upgrade cost ladder $50/$100/$200/$500/$1,000/$2,000; Chef offset so Chef L4→5 = $2,000 ≥ Shelver max $500).
- `ProductionCatalog` — recipes (Blender/Mill/Oven), base times, per-level stack 4/5/6/8 and time multipliers 1.0/0.85/0.72/0.60.
- `FarmCatalog` — tomato 2×3 grid ×3 fruit @0.5 s; wheat 3×4 ×1 @3.0 s; coop 2 hens ×3 eggs @0.5 s, buffer 4→8.
- `UpgradeCurve` — serializable step struct shared by all tracks.

> **Money representation:** store cash as `long cents` internally (`4` = $0.04) to avoid float drift; format to `$0.00` only in UI. `PriceCatalog` exposes cents.

---

## 7. Character FSM

Single enum `CharacterState` (Idle, Walking, Waiting, Carrying, Loading, Processing, Selling, Chasing, Fleeing, Resting) in `GameEnums.cs`; `CharacterBase` owns the state machine, subclasses own the task queues.

### Worker task loops
| Role | Loop |
| :-- | :-- |
| **Farmer** | scan farms (tomato → coop → wheat priority by emptiest storage) → harvest to stack → deposit into matching storage → repeat |
| **Shelver 1** | watch Egg/Tomato/Ketchup shelves; any < 40% → fetch from storage → restock |
| **Shelver 2** | same for Wheat/Flour/Bread |
| **Chef** | if ketchup storage low: fetch tomatoes **from farm directly** → load Blender → collect → store. If bread storage low: fetch wheat from farm → load Mill → collect flour → fetch egg from farm → load Oven → collect bread → store |
| **Cashier** | stand at counter; tick checkout: 1 item / 0.4 s (faster per level); spawn coins; release buyer |
| **Buyer** | enter → build list (unlocked items only) → if ≥5 items take trolley → visit shelves → queue at nearest counter → pay → exit |
| **Thief** | enter → sprint (1.5× buyer speed, always < player speed) to most valuable stocked shelf → grab ≤5 → flee to nearest of the 6 doors → outside = despawn (loss) |

Deadlock guards: every fetch task re-validates availability on arrival; failed validation → Idle with 1 s cooldown (prevents Chef spinning when storage is empty).

---

## 8. Audio

`AudioManager` (on Managers) with two mixer groups (Music / SFX), settings-persisted volume toggles. One-shot SFX pooled (8 `AudioSource`s).

| Event | Clip |
| :-- | :-- |
| BGM | light market loop, crossfade on pause |
| Pickup / drop | soft pop (pitch-randomized ±10%) |
| Coin collect | coin clink |
| Upgrade bought | fanfare short |
| UI button | click |
| Buyer checkout | register ding |
| Machines | per-machine loop while Processing (blender whirr, mill grind, oven hum) |
| Thief spawn / caught / escaped | sting / net whoosh + cheer / fail slide |
| Phone | ringtone (loops until answered/declined) |
| Order success / failure | jingle / buzzer |

---

## 9. Particles & Feedback

| Event | Effect |
| :-- | :-- |
| Coin payment | coin burst at counter + magnet to player/HUD |
| Walking | dust puffs (rate ∝ speed) |
| Harvest | green pop at plant |
| Machine processing | smoke/steam from vent |
| Upgrade | radial glow + scale bounce on target |
| Phone order complete | money-fly to HUD cash |
| Net capture | white puff + star ring |

All via one pooled `VFXManager`; effects are primitive-particle based (no textures required for MVP).

---

## 10. Navigation

- **`GridPathfinder`** — A* over a walkability grid baked from `MapLayout` at boot (cell 0.5 u). Obstacle layer cells are blocked. No Unity NavMesh in MVP (procedural scene favors the custom grid; NavMesh is a v1.2 option).
- Zones (from `MapLayout`): BuyerArea (aisles + counters), WorkerArea (everything inside walls), NoWalk (behind counters except cashier slot, inside farm beds), SpawnArea (outside entrance), ExitAreas (all 6 doors).
- **All characters must move along `GridPathfinder` paths** — `CharacterBase.NavigateTo(target)` requests a path and walks waypoints (§13 Gap 2 fix). Direct `MoveTowards` to a far target is forbidden.
- Path requests budgeted: max 6 per SimTick, round-robin queue.

---

## 11. UI Architecture

`HUDBuilder` constructs the canvas at runtime (TextMeshPro 3.2.0). One canvas, screen-space overlay, reference resolution 1920×1080, match height.

| Element | Class | Notes |
| :-- | :-- | :-- |
| Cash + level | `HUDController` | cash animates (count-up), XP bar |
| Inventory quick-view | `InventoryPanel` | 6 items, storage `n/cap` |
| Phone order card(s) | `PhoneOrderManager` UI | timer ring, contents, deliver button |
| Price panel | `PricePanelController` | opens on shelf tap; −/+ steppers, offer toggle |
| Upgrade popups | `UpgradeButton` | one per pad; shows next-level delta + cost |
| Pause/Settings | `PausePanel` | timescale 0, sim suspended, autosave on open |
| Tutorial | `TutorialController` (new) | step list driven by save flag |

---

## 12. Save System

JSON in `PlayerPrefs`, double-buffered (`save` + `save.bak`), autosave every 30 s + on pause/quit/focus-loss.

```json
{
  "SaveVersion": 2,
  "LastSavedUnix": 1780000000,
  "CashCents": 12450,
  "PlayerLevel": 3, "PlayerXp": 240,
  "RoleLevels":    { "Player": 2, "Shelver1": 1, "Shelver2": 2, "Chef": 1, "Farmer": 2, "Cashier1": 1, "Cashier2": 0 },
  "MachineLevels": { "Blender": 2, "Mill": 1, "Oven": 1, "HenCoop": 2 },
  "Storage":       { "Tomato": 9, "Egg": 4, "Wheat": 12, "WheatFlour": 3, "TomatoKetchup": 7, "Bread": 2 },
  "PriceCentsOverride": { "Bread": 36 },
  "ActiveOffers":  [ { "Item": "TomatoKetchup", "DiscountPct": 30, "EndsUnix": 1780000600 } ],
  "UnlockedItems": [ "Tomato", "Egg", "TomatoKetchup", "WheatFlour" ],
  "TutorialDone": true,
  "Settings": { "Music": true, "Sfx": true, "Quality": "Balanced" }
}
```

Load-time validation: clamp levels to legal ranges, clamp prices to ±50%/floor, drop unknown items (forward-compat via `SaveVersion` migration switch).

---

## 13. Known Gaps & Required Fixes (carry-over audit)

1. **Pathfinding bypass** — `CharacterBase.MoveTowardsTarget` moves linearly through walls. Fix: route all movement through `GridPathfinder` paths (waypoint follower in `CharacterBase`). *Blocking for v1.0.*
2. **Chef Mill coverage** — Chef must run the Mill (wheat→flour) as part of the bread chain per GDD; verify `Chef.cs` task list includes ProcessFlour.
3. **Virtual joystick** — mobile input is stubbed; implement floating joystick in `PlayerInputHandler`.
4. **Net visual** — capture is logic-only; add pooled parabolic `NetProjectile`.
5. **Cent-based money** — current code paths using `float` dollars must migrate to `long cents` (GDD v2 prices are $0.04–$0.30; float rounding will visibly corrupt totals).

---

## 14. Event Systems

### PhoneOrderManager
- Timer: next call at `t + Random(240, 300)` s of unpaused play; suppressed if 2 orders already active.
- Order generation: pick 2–4 unlocked item types, quantities weighted by effort; payout = `sum(itemEffortValue × qty) × premiumMultiplier`, clamped to **$45–$300**.
- Accept → 600 s countdown; deliver by consuming storage stock at the counter drop point; expire → failure buzzer + analytics event.

### TheftManager
- Active from Player L3. Next thief at `t + Random(240, 300)` s; only one thief at a time; never while paused.
- Target selection: shelf with highest `stock × price`. Steal up to 5, flee to **nearest door by path length** (any of the 6).
- `Thief.IsOutside` flips when crossing a door trigger → uncatchable, despawn 2 s later.
- Net capture: projectile arrival within 0.8 u of thief → Caught: items to storage, bounty credited, thief despawns in "caught" pose.

---

## 15. Optimization Budget

| Concern | Approach |
| :-- | :-- |
| Buyers/coins/trolleys/VFX/net | object pools (no runtime `Instantiate` after boot warm-up) |
| Draw calls | shared material palette (one material, vertex-color primitives); target < 120 |
| GC | zero per-frame allocations in SimTick paths; reuse `List` buffers; string-free HUD updates (TMP `SetText` with cached values, dirty-flag) |
| Pathfinding | budgeted queue (§10); path smoothing to cut waypoint count |
| Sim scale cap | ≤ 12 concurrent buyers, ≤ 24 pooled coins visible |
| Sprite/UI | single sprite atlas for icons |
| LOD / Addressables / Occlusion | **not needed** at this scene scale — explicitly out of scope for MVP |

---

## 16. Monetization & Analytics Hooks (v1.2)

- `AdsManager` facade (interface now, SDK later): `ShowInterstitial()`, `ShowRewarded(callback)` — call sites: session end, 2× phone payout button, instant-finish machine button.
- `AnalyticsManager` facade: `Track(string eventName, Dictionary params)` — call sites listed in GDD §14. Both are no-op stubs in MVP so call sites can be wired now.
