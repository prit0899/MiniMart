# Mini Mart — External Playtest & QA Report

**Date:** 2026-07-27 · **Build:** `v1.0.0-mvp` · **Scene:** `Game.unity` (Mart 1)
**Method:** ~15 min live session in the Unity Editor, iOS Simulator (Notch Device Large, landscape), player-legal input only. Existing owner save: **Lv 10 MAX, $2,826**. Evidence = on-screen observation + Editor console.

---

## 1. Overall impressions

The simulation underneath this game is solid. Across the whole session the console logged **0 errors and 0 warnings**. Steering reads well — workers curve around each other and ease into targets rather than snapping to 90° headings. Systems fired correctly and on cue: offline earnings ("away 4h 0m, offline sales $720"), auto-save, a phone order (`PhoneOrderManager` — "Order_1 arrived — value $300.00"), a thief alert with a contextual NET button, and buyer restock-waiting.

What is not solid is the presentation layer and the endgame economy. As a player, my session read as: a well-engineered store with nothing left to sell me, viewed through a camera mostly pointed at empty grass. (Facts below are stated plainly; judgements are prefixed "in my view".)

---

## 2. Bugs and technical issues

| # | Sev | Issue & evidence | Recommended fix |
|---|---|---|---|
| B1 | **High** | **No camera clamp.** Driving to a map edge leaves the store in a corner — at the SE corner ~60% of the frame was empty grass/road. The player can also walk out onto the road, where only asphalt is visible. Repro: joystick into any wall, hold 10 s. | Clamp the camera target to a bounds rect inset by half the frustum footprint; add a follow dead-zone. |
| B2 | **High** | **Store starves at max level; customers walk out.** Most shelves read `0/20`, only two read `MAX`. `Buyer.cs:530 LeaveWithoutPaying()` fired repeatedly — collapsed counts: Normal ×5, Bargain ×4, Impatient ×2, Rich ×1. Repro: load the L10 save, idle 10 min. | Audit L10 throughput (shelver count/carry/speed vs. buyer spawn rate). Scale shelver capacity with store level, or add an auto-restock unlock. |
| B3 | Med | **Panels are not mutually exclusive.** Opening MENU over PRICES draws Settings *on top of* Pricing; the two interleave and a thief toast rendered between them. | One panel manager: close-others-on-open, plus a modal scrim. |
| B4 | Med | **PRICING overflows the screen.** Rows run past the bottom device edge, the last is clipped mid-height, and the list does not scroll (drag and wheel both no-op). | Constrain to the safe area; wrap the list in a ScrollRect. |
| B5 | Med | **Blank buttons — missing icon sprites.** Upgrades and Settings close buttons are empty grey squares (no ✕); every Pricing row has an unlabeled dark square; the Settings volume control has an unlabeled blue square beside its track. | Assign the glyphs; audit all icon-only controls. |
| B6 | Med | **Oversized floating egg.** A white egg ~6× the coop eggs hovers ~2 units above the yard floor and casts no shadow, unlike every other prop. | Check parenting/scale on the Egg prefab — likely un-parented or unpooled. |
| B7 | Low | **Pricing panel is translucent** — the perimeter wall shows through behind the Milk/Dough rows. | Make the panel image opaque. |
| B8 | Low | **Chimney smoke renders as opaque hard-edged white squares**, reading as artifacts rather than smoke. | Fade alpha and size over lifetime. |
| B9 | Low | **NET button persists** after the thief event resolved. | Bind visibility to an active-thief count. |

**Balance finding (most consequential).** At **Lv 10 MAX with $2,842 banked**, every Worker and Machine upgrade was still **Lvl.1**, priced $50–$310; only the PLAYER row was Maxed. The active quest, *"Serve 85 customers → $245"*, is worth 8.6% of the bank. In my view this is the same defect class as Memory row #11 — fixed for the player, never for workers or machines — so the store can be maxed without touching either track. Re-tune those curves so L5–L10 requires them, and add a late-game sink. Offline yield also looks mis-scaled: 4 hours away gave **+3 tomatoes, +3 wheat** but **$720**.

---

## 3. UI/UX observations

**Legibility.** World badges use two styles — dark chips (`MAX`, `12/12`) and plain white text (`0/20`, `0`); the white variant on the cream floor is very hard to read. *Standardise on the dark chip.* Shelf badges show a count but **no item icon**, so an empty shelf gives no clue what it sells — extend the farm badges' colour chip to shelves. The floating joystick base is near-white at low opacity on cream and was barely visible while I was using it. The hen badge (`Tom 0  Egg 3`) is abbreviated and ~10 px; the phone-order timer overlaps its own icon.

**Panels.** Settings shows Vibration as a green ON pill but Graphics LOW/MED/HIGH with **no selected state at all**. The Animals tab has two rows in a fixed-height card, leaving ~70% empty. Lists clip at the panel edge with no fade or scrollbar, so nothing signals more content below. Prices read `$3.00`; genre convention is `$3`. Naming drifts: **"Chickens"** (Upgrades) vs **"Hen"** (world), **"Shelver A"** vs *Shelver1* (logs), `PLAYER` in caps against Title Case peers.

**Working well.** Hit areas are generous and thumb-reachable; DASH/NET sit in the right-thumb zone; tab colour-coding parses fast; toast copy is plain-English.

---

## 4. Comparative insights

Against the reference titles in `Refer/refer games.rtf`:

**The endgame problem is the genre's known killer, and Mini Mart has it.** A representative *My Mini Mall* review (4.6★, 2.2K ratings): *"I finished one mall in about 24 hours… you finish [the second] in like 30 minutes. After that, there's nothing to do so I'm basically just walking around."* That is almost exactly my session — max level, cash surplus, cheap unbought upgrades, empty shelves. Prestige loops, scaling daily goals, or a third venue are the standard answers.

**The differentiator is real.** *My Mini Mall* is department-based; Mini Mart runs an actual supply chain (Tomato→Ketchup, Wheat+Egg→Bread) — a deeper hook, but one that only pays off if the chain visibly keeps up with demand, which is what B2 breaks.

**Monetization contrast.** The loudest complaints on the reference titles are ad frequency ("ads every 30 seconds"; ads persisting after paying to remove them). Mini Mart ships no ads today; that review thread maps what players will not tolerate.

---

## 5. Priority

**B2** (starved shelves — kills the core loop and the endgame at once) → **B1** (camera clamp — affects every second of play) → worker/machine curves + a late-game sink → **B3–B5** → **B6–B9** and the badge/contrast pass.

---

*Scope note: this session used the existing Lv 10 save, so first-session onboarding (TutorialGuide, UnlockGuide, spotlight) was **not** exercised and is not assessed. A fresh-save pass on a backed-up copy is the recommended next test.*
