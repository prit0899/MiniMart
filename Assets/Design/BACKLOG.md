# Mini Mart — Living Backlog
**Updated:** 2026-07-03 (batch 3 — production chain unblocked, save hardened)

## Batch 3 fixes (verify on next run)
- [ ] **Farmer round-robin** — was egg-starved (hens always ready → never harvested wheat/tomato → whole chain dry, Shelver 2 had nothing to do). Now rotates coop→wheat→tomato, skipping full storages.
- [ ] **Chef self-fetch** — SelfFetch methods existed but were never called; chef now harvests tomato/wheat from the farms when storage is empty, so ketchup/flour/bread production runs from a cold start.
- [ ] **Shelf stock badges** — floating "n/cap" chips over every shelf (reference style); buying now visibly decreases stock.
- [ ] **Buyers pay for what they took** — checkout used to quote the *remaining wish-list* (i.e., items they FAILED to find). Now charges the collected items; empty-handed buyers pay nothing. Buyers also carry a visible stack now.
- [ ] **Save actually persists** — two causes fixed: (a) `Configure()` ran after load and reset all worker levels to 1; (b) force-killing an app never fires OnApplicationQuit. Load order fixed + autosave every 30 s + save on focus loss. Console logs "Save loaded — cash $X" on boot.
**Companions:** [GDD.md](GDD.md) (what to build) · [TDD.md](TDD.md) (how) · [ROADMAP.md](ROADMAP.md) (full build order)

This is the working triage list: every known bug, requirement gap, and UI/UX issue, ordered by priority. Check items off as they're verified in a build. The ROADMAP is the long-term plan; this file is "what's broken or missing *right now*."

---

## P0 — Verify on next build (fixes applied, need confirmation)

- [x] **Player movement** — CONFIRMED working (2026-07-03). Root cause was the 2D sprite prefab in `PlayerPrefab`.
- [ ] **Movement smoothness** — SmoothDamp input smoothing + analog throttle + faster turn added; verify feel on device.
- [ ] **Buyer visuals** — bare yellow capsules replaced with full built characters in 7 palette colors.
- [ ] **Buyer flow** — spawn interval was 8–20 s; now 1.5–3 s (reference pace), capped at 12 concurrent.
- [ ] **Thieves actually spawn now** — `SpawnThief` used to no-op because `ThiefPrefab` was never assigned; runtime-built dark-outfit thief added.
- [x] **Debug status line removed** (movement confirmed).
- [ ] **UPGRADES panel restyled to reference** — green shell, Workers/Machines/Animals tabs, colored row bars, green cost / grey Maxed buttons.
- [ ] **PRICES panel** — green shell, spacing fixed, taller panel; full reference redesign still pending (P2).
- [ ] **Buyers appear on device** — CapsuleCollider stripping fixed via `Assets/link.xml`.
- [ ] **No shader errors on device** — Standard shader force-included (GraphicsSettings) + null-safe material helpers.
- [ ] **HUD text visible everywhere** — LegacyRuntime font replaces removed Arial (HUDBuilder, both panels).
- [ ] **Upgrade panel no longer max-upgrades machines for free** — display path is read-only now; spend-before-upgrade enforced.
- [ ] **Phone orders fulfillable** — quantities capped by storage capacity; payout = shown value; max 2 active; FULFIL greys out without stock; DISMISS actually declines.
- [ ] **Event pacing** — phone/thief timers restored to spec 4–5 min (`PriceCatalog.FastEventTimers = true` for quick testing).
- [ ] **Panels** — UPGRADES/PRICES mutually exclusive; slider fill bar no longer a giant green block.
- [ ] Once movement is confirmed: **remove the yellow debug status line** (HUDBuilder `RuntimeStatusText` + `ReportDebugStatus` in PlayerInputHandler).

## P1 — Requirement gaps vs GDD (core gameplay)

- [x] **Buyers never left after checkout** (found 2026-07-03) — they froze at the counter forever; with the 12-buyer cap that eventually blocked ALL spawns. Now they walk to the exit door and despawn.
- [x] **Player-at-counter override** — `ManualOverride` was never set anywhere, so Counter 1 was NEVER open at L1 and no buyer could check out. GameManager now opens a cashierless counter while the player stands within 2.2 u. (3× speed bonus still TODO.)
- [x] **Trolley visual** — buyers with 5+ items now push a runtime-built cart (basket, handle, 4 wheels). Pooled trolley rack still TODO (polish).
- [x] **Price elasticity** — items >+20% over base get skipped 35% of the time at basket-build; any ≥10% discount boosts spawn rate +50%. Angry emote still TODO (polish).
- [x] **Thief gating** — TheftManager now requires Player L3+, synced on load and level-up.
- [x] **Save coverage** — worker/machine/coop levels + manual shelf prices now save and load (GameManager fills the existing GameSaveData dictionaries).
- [x] **Unlock levels aligned** — Bread moved to L4 per GDD §7.
- [ ] **Map layout doesn't match GDD §10** — no aisle structure, no 4 secondary exit doors, counters not at entrance/exit positions. `MapLayout` should be the single source of anchors (TDD §3).
- [ ] **Player level vs upgrade level are conflated** — unlocks key off the player's *upgrade* level. Decide: keep conflation (simpler) or add XP.
- [ ] **Player-at-counter 3× checkout speed** when manning a counter (GDD §4).
- [ ] **Offers** — no duration/expiry and no shelf banner yet.
- [ ] **Manual stock manipulation** — verify player can move items storage↔shelf by hand.
- [ ] **Bin overflow routing** — deposits over storage cap should route to bins (GDD §5). Verify.
- [ ] **Split Stack/Speed upgrade tracks** per entity like the reference (needs two curves per role + save keys).

## P2 — UI/UX issues

- [ ] **Phone-order card can't show 2 concurrent orders** — HUD shows only the latest; second active order is invisible. Stack cards or add a queue badge.
- [ ] **Phone card lacks live countdown** — shows static "Time: 10 min"; should tick down (GDD §11).
- [ ] **Price rows: slider has no handle/thumb** — value is settable only by tapping the bar; add a draggable knob or −/+ steppers (GDD wireframe uses steppers).
- [ ] **Upgrade rows: no player-cash context** — buttons grey out correctly, but show why (cash vs cost) — e.g. red cost text when unaffordable.
- [ ] **Storage readout is a raw text column** — replace with icon grid quick-look (GDD §11); at minimum align counts (shelf vs storage split).
- [ ] **MENU button doubles as pause with no visual state** — pressing it pauses with full-screen overlay; label it PAUSE or add a settings sheet (sound toggles, save & quit — GDD §11).
- [ ] **NET button always visible** — GDD: appears only when a thief is active and in range.
- [ ] **Joystick floating behavior** — verify the new hide-until-touch floating joystick feels right on device; dead zone 0.15.
- [ ] **Level display "Lv 1" has no XP/progress context** — depends on the P1 level-system decision.
- [ ] **No tutorial** — first-session task banner (harvest → shelf → sell → upgrade), flagged in save (ROADMAP task 259).

## P3 — Polish gap vs the reference (My Mini Mart)

- [ ] Character models: replace capsule+sphere blobs with proper low-poly characters (or paid asset pack) — biggest visual delta.
- [ ] Carried-item stack: per-item colors/shapes instead of white cubes; wobble/lag on turns.
- [ ] Coin burst at checkout + magnet-to-HUD money fly.
- [ ] Harvest pop, machine smoke, upgrade glow particles (VFXManager, pooled).
- [ ] Audio: BGM + pickup/coin/upgrade/machine/thief/phone SFX (AudioManager) — none exist yet.
- [ ] Camera: dynamic zoom with carry stack; screen shake on thief catch.
- [ ] Upgrade pads in-world (stand-on-pad to upgrade) like the reference, instead of only the menu.

## Decisions needed (design conflicts to resolve — pick one, then align code+GDD)

1. **Pricing rule**: GDD v2 says one-of-everything totals **under $1** (prices $0.04–$0.30). Code implements the opposite reading: a **$1 floor** per basket with prices $0.30–$1.50. Screenshots show the code prices in use. → Current recommendation: keep code prices (they're live and balance the $50+ upgrade ladder better); update GDD §6.1 to match.
2. **Store level source**: separate XP-based store level vs player upgrade level (see P1).

## Housekeeping

- [ ] Delete stale `MiniMart.app` (July 1 build of an empty scene) from project root.
- [ ] Delete `Assets/MiniMartgame.unity` (old empty scene — already caused one lost debugging session).
- [ ] Remove unused packages: `com.unity.purchasing` (ships in iOS build!), `com.unity.ai.assistant`, `com.unity.ai.inference`, `com.unity.multiplayer.center`.
- [ ] Remove `Assets/Jovial Games`, `Assets/30 2D Game Kit`, `Assets/Layer Lab` asset packs if unused (source of missing-script warnings; one already broke the player).
- [ ] The editor console's repeating `LastBuild.buildreport` FileNotFoundException is a harmless Unity Project Auditor bug — ignore.
