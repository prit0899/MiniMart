# Design — colour, theme, typography

> **The visual language.** All of it is generated in code — there is no art
> pipeline to sync with.
> Companion docs: [PRD](PRD.md) · [Architecture](Architecture.md) · [Rules](Rules.md) · [Phases](Phases.md) · [Memory](Memory.md)

---

## 1. Art direction

**Flat, bright, low-poly cartoon** — matching "My Mini Mart". Saturated, cheerful,
readable at a glance on a phone held at arm's length.

- **Everything is a primitive.** Meshes are built at runtime from cubes,
  cylinders and spheres by `Engine/PrimitiveFactory.cs`. There are no imported
  character or prop models (the Genies avatar is the one optional exception).
- **Isometric camera**, orthographic, fixed 35°/45° pitch-yaw, follows the player.
- **Shape + colour together** identify every item — colour alone was unreadable at
  isometric distance and useless for colourblind players. Each SKU has a distinct
  silhouette (`PrimitiveFactory.ItemMesh`).

## 2. Lighting

| Setting | Value |
|---|---|
| Sun (directional) | warm white `#FFF7E6` · intensity `0.85` · soft shadows, strength `0.3` |
| Sun rotation | `(52°, -40°, 0)` |
| Ambient | Flat, `RGB(0.42, 0.45, 0.50)`, intensity `1.0` |
| Camera background | grass green `RGB(0.48, 0.72, 0.35)` |

Gentle shadows on purpose — harsh shadows fight the flat cartoon look.

## 3. UI palette

Defined in `Engine/HUDBuilder.cs`.

| Token | RGBA | Use |
|---|---|---|
| `PanelColor` | `0.10, 0.12, 0.16, 0.92` | dark panel background |
| `GreenShell` | `0.45, 0.76, 0.32, 0.98` | level-up / positive popup shell |
| `PurpleShell` | `0.72, 0.60, 0.95, 0.98` | settings & phone-order shell |
| `AccentColor` | `0.20, 0.60, 0.95, 1.0` | primary buttons (blue) |
| `GreenColor` | `0.20, 0.70, 0.30, 1.0` | confirm / buy |
| `RedColor` | `0.85, 0.25, 0.25, 1.0` | destructive / close |
| `CashBgColor` | `0.15, 0.55, 0.15, 0.95` | cash pill |
| Cash pill face | `0.94, 0.96, 0.92, 0.95` | light pill, dark text |
| MENU button | `0.55, 0.35, 0.75, 0.90` | purple |
| Phone-order chip | `0.98, 0.87, 0.42, 0.95` | warning yellow |
| Pause overlay | `0, 0, 0, 0.65` | dim |

**Purchase pads:** bright green disc `0.55, 0.85, 0.45`, yellow arrow
`1.0, 0.85, 0.10`, green cost pill `0.20, 0.65, 0.28`.

## 4. Item colours

From `PrimitiveFactory.ItemColor()`. Keep new items visually distinct from these.

| Item | Colour | | Item | Colour |
|---|---|---|---|---|
| Egg | `1.00, 0.97, 0.88` cream | | Cheese | `1.00, 0.83, 0.25` gold |
| Tomato | `0.90, 0.15, 0.10` red | | Herb | `0.25, 0.62, 0.22` green |
| Tomato Ketchup | `0.82, 0.12, 0.10` deep red | | Herb Pack | `0.45, 0.85, 0.50` light green |
| Wheat | `0.92, 0.82, 0.45` straw | | Fried Egg | `1.00, 0.78, 0.15` yolk |
| Wheat Flour | `0.96, 0.93, 0.88` off-white | | Corn | `0.98, 0.78, 0.20` yellow |
| Bread | `0.82, 0.62, 0.32` crust | | Processed Corn | `0.85, 0.55, 0.15` amber |
| Milk | `0.95, 0.97, 1.00` white | | Coffee | `0.32, 0.20, 0.12` dark brown |

> Ketchup is **deep red, not green** — a real bug we already fixed once.

## 5. Typography

| | |
|---|---|
| **Font** | Unity built-in `LegacyRuntime.ttf`; falls back to OS **Helvetica** if unavailable (`HUDBuilder.UIFont`). |
| **Style** | Bold, high-contrast, generous size — readable on a phone in sunlight. |
| **In-world text** | `TextMesh` + `Billboard` component so labels always face the camera (pad names, cost pills, stock badges "n/cap"). |
| **HUD text** | uGUI `Text`. Dark text `0.15,0.15,0.15` on light pills; white on dark panels. |

Typical sizes: pad name `36`, cost `40`, popup title `40`, body `24`, stock badge
`42` (small `characterSize`, ~0.075).

## 6. UI layout principles

- **Portrait, thumb-first.** Primary actions bottom-right (DASH), menus top-left.
- **Safe-area aware** — respect the notch.
- **Rounded panels**, 9-sliced sprites from `Assets/Resources/UI/`
  (`popup_bg`, `btn_close`, `prg_bar_*`), with a procedural fallback so a missing
  sprite never breaks the UI.
- **Nothing blocks the shop.** Popups auto-dismiss (the level-up popup used to sit
  on screen for 30 minutes).
- **Show, don't tell.** Floating item icons above every production source; a
  bouncing arrow at each new unlock; carry stacks over every character's head.

## 7. Audio

Procedural + a small SFX pack in `Assets/Resources/SFX`. Coin, sale, purchase,
level-up, lose. One-shots are **throttled to one per clip per 80 ms** — an
unthrottled busy store exhausted Unity's virtual audio channels.
