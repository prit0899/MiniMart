# Rules — working on Mini Mart (human or AI)

> **Read before writing a single line.** These are the guardrails.
> Companion docs: [PRD](PRD.md) · [Architecture](Architecture.md) · [Phases](Phases.md) · [Design](Design.md) · [Memory](Memory.md)

---

## 1. Non-negotiables (owner rules)

Breaking one of these means the work is wrong, however clean the code is.

1. **The hand-drawn map is law.** `Refer/map-plan-mart1.pdf` beats the reference
   video, the screenshots, and your own judgement. Both marts are ONE enclosed
   building — shop floor above, farms + machines in the lower half of the *same*
   building. **Nothing lives outside on open grass.**
2. **Nobody walks through walls.** Player, shelver, chef, farmer, buyers. Doors
   and the interior gap are the only openings.
3. **1–3 unlocks per store level.** Never 5–6.
4. **Upgrades must be worth buying.** If a player can max the game without ever
   upgrading, the balance is broken.
5. **The phone-order van stays outside the mall**, where it is.
6. **Mart 2, 3, … reuse the same core map plan** with their own items.
7. **Same-to-same with the reference game** for UI/UX and flow. Don't invent
   features that aren't in "My Mini Mart".

## 2. What to do

- **Read `Docs/Memory.md` first** to see where the project actually is.
- **Edit C#, not the scene.** The world is built from code
  (`SceneBootstrapper.cs` / `SceneBootstrapper2.cs`). Moving objects in the Unity
  editor accomplishes nothing.
- **Change visual walls AND grid walls together** — they are two representations
  of the same wall and must agree cell-for-cell.
- **Keep `DataValidator` green.** It runs ~45k assertions on boot. If you change
  a curve or the unlock ladder, update its assertions in the same commit.
- **Verify by playing, not by reasoning.** Use the TesterBot harness
  (`touch Logs/testerbot.enabled`) and watch `Logs/testerbot_run.txt`.
  "It compiles" is not "it works".
- **Grep before you edit.** Files get changed between your reads.
- One feature at a time; self-review before finishing.

## 3. What to avoid

| Don't | Why |
|---|---|
| Don't add **URP / HDRP / Shader Graph** assets | The project is **Built-in Render Pipeline**. URP is not installed. |
| Don't commit `Assets/Genies/` | It's gitignored and holds an **auth artifact** (`GeniesAuthSettings.bytes`). Never commit it. |
| Don't commit `.DS_Store` or `.claude/settings.local.json` | Noise. |
| Don't "fix" the carry/harvest loop with clever guards | A guard that blocked harvesting when *storage* was full starved the *shelves* and stalled the whole game. See Memory.md. |
| Don't hard-gate progression on player upgrades | Make them *desirable*, like the reference — not mandatory. |
| Don't change the unlock ladder or XP curve unilaterally | That's the owner's design call. Ask. |
| Don't leave background keep-alive loops running | One kept relaunching Unity and blocked the owner from quitting Unity Hub. Always clean up. |
| Don't trust a stale `Editor.log` line | The validator only re-runs on Play. |

## 4. Libraries & dependencies

- **Prefer zero new dependencies.** Everything (meshes, UI, SFX) is generated in
  code via `PrimitiveFactory` / `HUDBuilder` / `AudioFx`.
- Allowed & already present: uGUI, TextMeshPro, Genies Avatar SDK.
- Installed but **unused — do not build on**: `com.unity.purchasing`,
  `com.unity.services.*`.
- If you genuinely need a package, **ask first** — it affects build size and iOS
  review.

## 5. Error handling & robustness

- **Fail loud in the editor, never in the player's face.** Use `Debug.LogError`
  for developer mistakes (e.g. a prefab of the wrong type) and degrade gracefully
  at runtime.
- **Every external/optional system must no-op safely if missing** — the Genies
  avatar, a missing sprite, a missing audio clip. The game must still run.
- Null-guard `GameManager.Instance`, `Economy`, `Inventory` — bootstrap order
  means they can be briefly absent.
- Never let a save/load exception break the boot.
- Audio: throttle one-shots (a busy store exhausted Unity's virtual channels).

## 6. Quality gates

- 0 compile errors.
- `DataValidator` ✅ ALL PASS on boot.
- 60 FPS target on device; mobile-first.
- Save/Load works, including offline earnings.
- A full playthrough (L1→L10) completes with no stalls or dead-ends.

## 7. Boundaries for AI agents

**You may, without asking:** fix bugs, refactor within a system, add tests, write
docs, adjust code the owner has already asked you to change.

**You must ASK before:**
- Changing the **unlock ladder**, **XP curve**, or **prices** (owner's design).
- Adding a **new dependency** or changing the **render pipeline**.
- Restructuring folders or renaming public systems.
- Deleting content the owner created (their map, their docs, their edits).
- Anything the owner has explicitly decided (see the rules in §1).

**Always:**
- State honestly what you ran and what you did **not** verify. Do not claim a
  playthrough you didn't do.
- Report failures with the actual output.
- Clean up background processes you start.
- Update [Memory.md](Memory.md) at the end of a work session.
