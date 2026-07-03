# Mini Mart — Implementation Roadmap
**Version:** 1.0
**Companion documents:** [GDD.md](GDD.md) (what to build), [TDD.md](TDD.md) (how to build it)

This is the dependency-ordered task list. Work top-to-bottom; every phase ends with a **Gate** — do not start the next phase until the gate passes. This is what prevents "stuck on one screen with lots of errors": each task only depends on tasks above it.

### How to read current state
The repo already contains scripts for most systems (`CharacterBase`, `Chef`, `Buyer`, `GridPathfinder`, `SaveSystem`, `PhoneOrderManager`, …). **A script existing does not mean its phase is done** — the checkbox is only ticked when the task's *verify* step passes in Play Mode. Treat existing code as a head start, and use the verify tasks to find what's actually broken.

Legend: `V:` = how to verify the task is done.

---

## Phase 0 — Project Foundation (Tasks 1–20)

- [ ] 1. Confirm Unity 6000.5.1f1 opens the project with zero console errors.
- [ ] 2. Set `Application.targetFrameRate = 60` in `GameManager.Awake`. V: Stats panel shows ~60.
- [ ] 3. Configure landscape-only auto-rotation in Player Settings.
- [ ] 4. Create layers: Ground, Character, Interactable, Obstacle (TDD §1). V: layers exist.
- [ ] 5. Create tags: Player, Buyer, Thief, Worker, Shelf, Storage, Machine, CashCounter, ExitDoor, TrolleyRack, Bin.
- [ ] 6. Define 3 quality presets mapped to `QualityPreset` enum.
- [ ] 7. Verify `GameEnums.cs` matches GDD (ItemType ×6, RoleType ×8, CharacterState, BagType).
- [ ] 8. Add `Cashier2` handling to `RoleType` usage (two cashiers exist as separate save slots).
- [ ] 9. Migrate money to `long cents` end-to-end (TDD §13.5). V: buying 3 tomatoes = exactly $0.12.
- [ ] 10. Update `PriceCatalog` to GDD §6.1 cent prices + unlock levels. V: unit check sums one-of-each = $0.72.
- [ ] 11. Update `RoleCatalog` to GDD §3.1 level tables + §6.2 cost ladders. V: Chef L4→5 = $2,000.
- [ ] 12. Update `ProductionCatalog`: stacks 4/5/6/8, time multipliers 1.0/0.85/0.72/0.60.
- [ ] 13. Update `FarmCatalog`: tomato 2×3 ×3 @0.5 s; wheat 3×4 ×1 @3.0 s; coop 2 hens ×3 @0.5 s, buffer 4→8.
- [ ] 14. Enforce "player always fastest" clamp (worker speed ≤ player × 0.9) in `CharacterBase`.
- [ ] 15. Add `Boot` scene (splash → load save → menu → load Game). V: EditorBuildSettings has Boot(0), Game(1).
- [ ] 16. Wire scene flow Splash → Loading → Menu → Gameplay (TDD §2).
- [ ] 17. Create `MapLayout` anchors for the full GDD §10 map (3 aisles, 2 gaps, 6 doors, 2 counters, bins, trolley rack).
- [ ] 18. `SceneBootstrapper` builds the §10 layout from `MapLayout`. V: screenshot matches GDD map sketch.
- [ ] 19. Boundary colliders on walls/fences (Obstacle layer). V: player cannot leave the lot.
- [ ] 20. **GATE 0:** clean console, Boot→Game flow works, map visibly matches GDD §10.

## Phase 1 — Player On The Floor (21–45)

- [ ] 21. Player uses `CharacterController` capsule (TDD §5).
- [ ] 22. WASD movement with accel 0.1 s, instant stop, 720°/s turn.
- [ ] 23. Floating virtual joystick in `PlayerInputHandler` (lower-half touch, dead zone 0.15).
- [ ] 24. Joystick tested on device/simulator. V: smooth 8-direction run on touch.
- [ ] 25. `CameraRig`/`CameraFollow`: 45° pitch, height 16, smooth-damp follow, dead zone 0.5.
- [ ] 26. Camera bounds clamp from `MapLayout.CameraBounds`.
- [ ] 27. Carry-stack zoom-out (+15% lerped).
- [ ] 28. `WobbleAnimator` on player (idle breathe, run lean/bounce).
- [ ] 29. `CarryVisual` stacking with lag/bend on turns.
- [ ] 30. Proximity interaction triggers (radius 1.2 u, auto-start, no button).
- [ ] 31. Pickup throttle 1 item / 0.12 s.
- [ ] 32. Carry cap reads player level (4→7). V: at L1 the 5th pickup is refused.
- [ ] 33. Walk-dust particles rate ∝ speed.
- [ ] 34. Pickup/drop SFX (pitch-randomized).
- [ ] 35. `Screen.safeArea` HUD anchoring.
- [ ] 36–44. Reserved: player feel polish passes (turn feel, stack wobble tune, camera damp tune, joystick sensitivity, footstep timing, idle emote, drop animation, stack cap feedback flash, device FPS check).
- [ ] 45. **GATE 1:** player runs the whole map at 60 FPS on device, picks up/drops with correct caps.

## Phase 2 — Farms & Growth (46–70)

- [ ] 46. Tomato plot: 2×3 plants, per-plant fruit counter (max 3).
- [ ] 47. Tomato SimTick growth 0.5 s/fruit, paused at max. V: empty plant is full in 1.5 s.
- [ ] 48. Tomato harvest on player proximity (one fruit per throttle tick).
- [ ] 49. Wheat plot: 3×4 tiles, 1 wheat each, regrow 3.0 s.
- [ ] 50. Wheat harvest + tile regrow cycle. V: harvested tile regrows alone.
- [ ] 51. Hen coop: 2 hens, 3-egg per-hen cap, 0.5 s lay rate.
- [ ] 52. Coop buffer stack (4 at L1) collects laid eggs; hens pause when buffer full.
- [ ] 53. Coop levels 1–4: buffer 4/5/6/8 + lay-speed factor.
- [ ] 54. Harvest pop VFX on all three farms.
- [ ] 55. Growth visuals (fruit scale-in, wheat sway, egg appear).
- [ ] 56–68. Reserved: farm polish (per-plant stagger, full-farm indicator, harvest SFX variants, fence visuals, soil materials, hen idle wander, egg roll animation, plot upgrade pads placement, growth-rate save/load hooks, editor gizmos for plots, farm zone no-walk cells, farmer access slots, balancing pass on rates).
- [ ] 69. All farm state ticks from `GameManager` SimTick (0.15 s), not `Update`.
- [ ] 70. **GATE 2:** all three farms grow/harvest correctly with caps; profiler shows zero GC alloc per tick.

## Phase 3 — Storage, Shelves & Overflow (71–90)

- [ ] 71. `StoreInventory` per-item storages with GDD §5 caps (15/15/15/12/15/12).
- [ ] 72. Storage racks are world objects at `MapLayout` anchors (egg, tomato, wheat, flour, ketchup, bread).
- [ ] 73. Player deposit/withdraw at racks via proximity.
- [ ] 74. Display shelves (one per sellable item) with own caps, drawing visually stacked items.
- [ ] 75. Overflow → nearest Bin routing (GDD §5). V: deposit into a full rack sends excess to bin.
- [ ] 76. Bin object: consumes items, puff VFX, counter for analytics.
- [ ] 77. `InventoryPanel` HUD quick-view (6 items, n/cap).
- [ ] 78–88. Reserved: shelf visuals per item, storage fill meters, shelf tap = price panel target, restock feedback flash, rack collider tuning, shelf item icons atlas, storage save/load, low-stock HUD hint, bin full warning, aisle signage, verify caps vs GDD table.
- [ ] 89. Storage counts persist across save/load. V: quit/reopen keeps counts.
- [ ] 90. **GATE 3:** harvest → store → shelf flow works manually end-to-end for tomato + egg.

## Phase 4 — Machines (91–115)

- [ ] 91. Base `Machine` FSM: Idle → Processing → Completed (TDD v1 §3.3 carry-over).
- [ ] 92. Blender: 1 Tomato → 1 Ketchup, 2.5 s base.
- [ ] 93. Mill: 1 Wheat → 1 Flour, 3.0 s base.
- [ ] 94. Oven: 1 Flour + 1 Egg → 1 Bread, 4.0 s base. V: refuses to run with only one ingredient.
- [ ] 95. Input/output hoppers per machine, stack from level (4/5/6/8).
- [ ] 96. Machine levels 1–4 with time multipliers (1.0/0.85/0.72/0.60).
- [ ] 97. Player manual load/collect via proximity.
- [ ] 98. Progress bar + full/empty tray indicators.
- [ ] 99. Machine processing loop SFX + smoke VFX.
- [ ] 100–113. Reserved: machine meshes (blender/mill/oven primitives), hopper visuals, output tray stacking, machine upgrade pads, machine save/load, completion ding, machine placement per §10 map (blender in ketchup zone, mill in flour zone, oven in bread zone), balance pass, machine idle hum off, load animation, dual-ingredient UI hint for oven, error feedback when hopper full, machine level icons, unit test oven recipe.
- [ ] 114. Machine levels persist in save.
- [ ] 115. **GATE 4:** full manual production chain: wheat → flour → (+egg) → bread on the shelf.

## Phase 5 — Pathfinding & Worker AI (116–150)

- [ ] 116. `GridPathfinder` bakes walkability grid from `MapLayout` (cell 0.5 u).
- [ ] 117. **Fix pathfinding bypass** (TDD §13.1): `CharacterBase.NavigateTo` requests A* path; waypoint follower. V: no character ever crosses a wall.
- [ ] 118. Path request budget: ≤6/SimTick round-robin.
- [ ] 119. Farmer AI loop (harvest → deposit, emptiest-storage priority).
- [ ] 120. Farmer stack/speed from `RoleCatalog` L1–5.
- [ ] 121. Shelver 1 AI: Egg/Tomato/Ketchup shelves <40% → restock from storage.
- [ ] 122. Shelver 2 AI: Wheat/Flour/Bread same loop.
- [ ] 123. Shelver stack 3→5 + speed per level.
- [ ] 124. Chef AI — ketchup chain: tomatoes **from farm directly** → Blender → collect → ketchup storage.
- [ ] 125. Chef AI — bread chain: wheat from farm → Mill → flour; egg from farm; → Oven → bread storage (closes TDD §13.2).
- [ ] 126. Chef stack 3→6 + speed per level; cost ladder $200/$500/$1,000/$2,000.
- [ ] 127. Deadlock guard: re-validate on arrival, 1 s idle cooldown on failure. V: Chef doesn't spin with empty farms.
- [ ] 128. Worker color coding + `WobbleAnimator` on all workers.
- [ ] 129–147. Reserved: worker hire pads, worker upgrade pads, per-worker save/load, worker pause/resume with game pause, farmer multi-farm priority tune, shelver threshold tune, chef chain priority (bread over ketchup when both low), worker idle behavior, path smoothing, worker collision avoidance (soft), 5-workers concurrent perf test, worker level-up VFX, task debug overlay, worker SFX, navigation zone masks (buyers vs workers), stress test 10-min unattended run, log cleanup, worker speed clamp vs player verify, worker stat HUD tooltip.
- [ ] 148. All workers tick from SimTick, movement per-frame.
- [ ] 149. 10-minute unattended run: production keeps flowing, no deadlock, no wall clipping.
- [ ] 150. **GATE 5:** with all workers hired, the store runs the full chain hands-free (slow but correct).

## Phase 6 — Buyers, Trolleys & Checkout (151–185)

- [ ] 151. `BuyerSpawner`: random interval 8–14 s (L1) tightening to 3–6 s (L5).
- [ ] 152. Shopping list generator: **unlocked items only**, weighted to cheap items. V: bread locked ⇒ never requested.
- [ ] 153. Buyer FSM: enter → list → (trolley?) → shop shelves → queue → pay → exit.
- [ ] 154. Hand-carry rule: <5 items in hands; **≥5 items requires trolley** (max 12).
- [ ] 155. `TrolleyController` + trolley rack at entrance; pooled trolleys, returned on checkout.
- [ ] 156. Buyers take items off shelf visuals (shelf count decrements).
- [ ] 157. Queue system per counter; buyers pick shortest queue.
- [ ] 158. Cash Counter 1 (Aisle A): at Player L1 **the player must stand at the counter** to check out.
- [ ] 159. Checkout tick: 1 item / 0.4 s, coins burst, cash credited in cents.
- [ ] 160. Cashier 1 unlock at Player L2 — replaces the player at Counter 1.
- [ ] 161. Cash Counter 2 + Cashier 2 unlock at Player L4 (exit side).
- [ ] 162. Cashier speed levels 1–5.
- [ ] 163. Player-at-counter override is 3× cashier speed (any time, any level).
- [ ] 164. XP + player-level progression (level milestones drive unlocks per GDD §7).
- [ ] 165–182. Reserved: buyer visual variety, buyer emotes, queue spacing, angry-leave on 6+ min wait, coin magnet to player, register SFX, buyer exit through correct door, trolley push animation, buyer pooling (≤12 concurrent), spawn-rate save of level, elasticity hooks (phase 8 dependency), buyer counting analytics, checkout balancing pass, trolley collision off, buyer avoid-worker steering, entrance/exit door animations, crowd stress test, buyer list debug overlay.
- [ ] 183. Full loop test: money grows from buyer sales alone.
- [ ] 184. Buyer/economy state survives save/load mid-session.
- [ ] 185. **GATE 6:** GDD §2 loop closes — harvest→…→money→upgrade all in-game, no editor intervention.

## Phase 7 — Upgrades & Economy Controls (186–210)

- [ ] 186. `EconomyManager` single source of cash (cents), spend/credit API.
- [ ] 187. Upgrade pads per worker/machine/coop with `UpgradeButton` popups (next-level delta + cost).
- [ ] 188. All cost ladders per GDD §6.2. V: Chef max ≥ Shelver max.
- [ ] 189. `PricePanelController`: per-shelf ±50% steppers, $0.01 floor, base-price reference.
- [ ] 190. Manual stock manipulation: player can move items storage↔shelf by hand at any time.
- [ ] 191. Offer system: pick item + discount% + duration; banner over shelf.
- [ ] 192. Price elasticity: >+20% ⇒ 35% item-skip w/ angry emote; ≥10% discount ⇒ +50% spawn, double-qty chance.
- [ ] 193. Pause / Stop (soft-pause AI) / Resume controls; timescale + SimTick suspension.
- [ ] 194–207. Reserved: upgrade glow VFX, upgrade fanfare, price color coding (green/red/white), offer expiry cleanup, offer save/load, price save/load, insufficient-funds feedback, upgrade pad unlock gating by player level, cost display formatting ($0.00), balancing pass GDD §13 targets, elasticity telemetry, sell-price tooltip, HUD cash count-up animation, economy unit tests (cents math).
- [ ] 208. Prices/offers/levels all persist.
- [ ] 209. 30-min balance playtest against GDD §13 table (first upgrade ≤4 min, L2 in 10–15 min).
- [ ] 210. **GATE 7:** player can pause, reprice, create offers, and buy every upgrade; economy numbers match GDD.

## Phase 8 — Phone Orders (211–230)

- [ ] 211. `PhoneOrderManager`: call every 240–300 s of unpaused play, max 2 active.
- [ ] 212. Order generator: 2–4 unlocked item types, payout $45–$300 by effort value.
- [ ] 213. Incoming-call popup: contents, payout, Accept/Decline + ringtone loop.
- [ ] 214. Accepted order: HUD card with 10-min countdown + progress per item.
- [ ] 215. Delivery: consume storage stock at drop point; partial progress allowed.
- [ ] 216. Success: payout + money-fly VFX + jingle; Expiry: buzzer + card dismiss.
- [ ] 217–227. Reserved: order queue when 2nd call arrives, decline cooldown, order save/load (timers survive), order size scaling with level, payout clamp tests, HUD card stacking, phone icon flash, analytics events, reward XP bonus, drop point visual, order balancing pass (60–70% of income mid-game).
- [ ] 228. Orders persist across save/load with correct remaining time.
- [ ] 229. Verify frequency + limit: soak test 30 min ⇒ 6–7 calls.
- [ ] 230. **GATE 8:** phone orders are the dominant income source by mid-game, per GDD §13.

## Phase 9 — Thief & Net (231–255)

- [ ] 231. `TheftManager`: from Player L3, one thief per 240–300 s, never during pause, one at a time.
- [ ] 232. Thief FSM: enter → sprint (1.5× buyer, < player) → richest shelf (`stock × price`) → grab ≤5 → flee.
- [ ] 233. Flee target = nearest of the **6 doors** by path length (main, exit, 4 secondary).
- [ ] 234. Outside = uncatchable (`IsOutside` on door trigger), despawn 2 s, items lost.
- [ ] 235. `NetTool`: Throw button appears when thief within 6 u; pooled parabolic `NetProjectile` (closes TDD §13.4).
- [ ] 236. Capture on arrival ≤0.8 u: items → storage, bounty $5–$15, caught pose + despawn.
- [ ] 237. Thief sting SFX on spawn; whoosh/cheer on catch; fail slide on escape.
- [ ] 238–252. Reserved: thief visual (distinct outfit), stolen-items visual on thief, shelf theft animation, camera shake on catch, net cooldown, thief pathing through crowd, escape analytics, catch analytics, thief spawn save (timer), balancing loss/event vs GDD §13, thief warning icon on HUD, secondary-door ambush strategy test, thief vs pause interaction, net aim assist, tutorial hint on first thief.
- [ ] 253. Verify: player at max speed can intercept a thief that spawns at the far aisle.
- [ ] 254. Soak test: thief events for 30 min, zero stuck thieves.
- [ ] 255. **GATE 9:** thief loop is fair — catchable with effort, punishing when ignored.

## Phase 10 — Save/Load Hardening, Tutorial & Polish (256–285)

- [ ] 256. Save schema v2 (TDD §12) with `.bak` double-buffer + load-time validation/clamps.
- [ ] 257. Autosave: 30 s interval + pause + quit + focus-loss.
- [ ] 258. Save-migration switch on `SaveVersion` (v1 saves either migrate or reset cleanly).
- [ ] 259. Tutorial: contextual task banner for first loop (harvest → shelf → man counter → sell → upgrade), flagged in save.
- [ ] 260. Main Menu: Continue / New Game / Settings.
- [ ] 261. Settings: music/SFX toggles, quality preset, persist in save.
- [ ] 262. `AudioManager` with full GDD §12 clip table.
- [ ] 263. `VFXManager` pooled effects, full GDD §12 table.
- [ ] 264–281. Reserved: coin pool cap 24, buyer pool cap 12, sprite atlas for UI icons, draw-call audit <120, GC audit (zero alloc/tick), device test matrix (low-end Android), loading screen, app icon + splash, store-level-up celebration, edge-case pass (kill app mid-save, airplane mode, 3-day-old save), long-session soak (2 h), memory snapshot audit, TMP font fallback, localization-ready strings pass (keys, English only), colorblind-safe price colors, screenshot set for store listing, version stamping.
- [ ] 282. Full-loop regression checklist run (every gate re-verified on device).
- [ ] 283. Crash-free 2-hour soak on device.
- [ ] 284. Code freeze: zero warnings, dead code removed.
- [ ] 285. **GATE 10 = v1.0 MVP RELEASE CANDIDATE.**

## Phase 11 — v1.2: Monetization & Analytics (286–300)

- [ ] 286. `AdsManager` facade wired at call sites (stub in MVP).
- [ ] 287. `AnalyticsManager` facade + full GDD §14 event list.
- [ ] 288. Rewarded ads: 2× phone payout, instant machine finish.
- [ ] 289. Interstitial policy (session boundaries only).
- [ ] 290. Remove Ads IAP + starter pack.
- [ ] 291. Daily reward + offline earnings (capped, from `LastSavedUnix`).
- [ ] 292–298. Reserved: ad SDK integration, IAP validation, consent/GDPR flow, A/B hooks for prices, retention dashboards, store metadata, release build pipeline.
- [ ] 299. Analytics verified end-to-end in dashboard.
- [ ] 300. **GATE 11 = v1.2 live-ops ready.**

---

## Dependency summary (why this order)

```
Foundation ─► Player ─► Farms ─► Storage ─► Machines ─► Workers ─► Buyers ─► Upgrades ─► Phone ─► Thief ─► Polish ─► Monetization
```

- Workers come **after** machines/storage because their AI targets those objects.
- Buyers come **after** workers so shelves actually restock under load.
- Phone orders and thief come **after** the economy exists (they modify income, they don't create the loop).
- Monetization is last — it decorates a game that already works.
