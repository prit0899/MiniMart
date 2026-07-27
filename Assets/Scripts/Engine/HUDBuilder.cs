using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MiniMart.UI;

namespace MiniMart.Engine
{
    /// <summary>
    /// Builds the entire in-game dashboard/HUD at runtime (no prefabs required) and wires it into
    /// HUDController + PlayerInputHandler. Uses Unity built-in UI.Text (no TMP dependency).
    /// Matches GDD Section 11 (UI wireframes): top-right cash/level, settings button,
    /// bottom-center virtual joystick, bottom-right net button, plus pause overlay and panels.
    /// </summary>
    public static class HUDBuilder
    {
        // Unity 6 removed the built-in Arial; LegacyRuntime.ttf is the supported built-in font.
        // OS fonts (CreateDynamicFontFromOSFont) are unavailable on WebGL and unreliable on mobile,
        // so they are only a fallback. One cached font instance for the whole HUD.
        private static Font uiFont;
        public static Font UIFont
        {
            get
            {
                if (uiFont == null)
                {
                    try { uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
                    catch { /* fall through to OS font */ }
                    if (uiFont == null)
                        uiFont = Font.CreateDynamicFontFromOSFont("Helvetica", 24);
                }
                return uiFont;
            }
        }

        // Runtime-generated sprites so the primitive HUD gets real rounded corners /
        // circles like the reference game (no art assets required).
        private static Sprite roundedSprite;
        public static Sprite RoundedSprite
        {
            get
            {
                if (roundedSprite == null)
                    roundedSprite = BuildRoundedSprite(64, 20);
                return roundedSprite;
            }
        }

        // ── Layer Lab UI pack (Assets/Resources/UI) ─────────────────────────
        // Real panel/button art when present; every caller falls back to the
        // procedural sprites so a missing import never breaks the HUD.
        private static Sprite PackSprite(string name) => Resources.Load<Sprite>("UI/" + name);

        /// <summary>Swap a panel to pack art (popup_bg) when available.</summary>
        private static void ApplyPanelArt(Image panel)
        {
            var art = PackSprite("popup_bg");
            if (art == null) return;
            panel.sprite = art;
            panel.type = Image.Type.Sliced;
            panel.color = Color.white; // art carries its own palette
        }

        /// <summary>Swap a close button to pack art (btn_close) when available.</summary>
        private static void ApplyCloseArt(Button btn, Text label)
        {
            var art = PackSprite("btn_close");
            if (art != null)
            {
                var img = btn.GetComponent<Image>();
                img.sprite = art;
                img.type = Image.Type.Simple;
                img.color = Color.white;
            }
            // B5: the btn_close art carries no visible glyph, so clearing the label
            // left a blank square. Always keep a bold white "X" drawn on top.
            if (label != null)
            {
                label.text = "X";
                label.color = Color.white;
                label.fontStyle = FontStyle.Bold;
                label.fontSize = 30;
            }
        }

        private static Sprite circleSprite;
        public static Sprite CircleSprite
        {
            get
            {
                if (circleSprite == null)
                    circleSprite = BuildCircleSprite(64);
                return circleSprite;
            }
        }

        private static Sprite BuildRoundedSprite(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distance outside the rounded-rect core (0 inside).
                    float dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    float dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(radius - d + 0.5f); // 1px anti-alias edge
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            // 9-slice border sized to the corner radius so stretching keeps corners crisp.
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
        }

        private static Sprite BuildCircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[size * size];
            float r = size * 0.5f - 1f;
            float cx = (size - 1) * 0.5f, cy = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static readonly Color PanelColor  = new Color(0.10f, 0.12f, 0.16f, 0.92f);
        // Reference-style bright green shell for the big center panels (My Mini Mart look).
        // B7: fully opaque modal shells — at 0.98 the perimeter wall showed through
        // behind the Pricing rows. Modal panels should never be see-through.
        private static readonly Color GreenShell  = new Color(0.45f, 0.76f, 0.32f, 1f);
        private static readonly Color PurpleShell = new Color(0.72f, 0.60f, 0.95f, 1f); // reference settings shell
        private static readonly Color AccentColor = new Color(0.20f, 0.60f, 0.95f, 1f);
        private static readonly Color GreenColor  = new Color(0.20f, 0.70f, 0.30f, 1f);
        private static readonly Color RedColor    = new Color(0.85f, 0.25f, 0.25f, 1f);
        private static readonly Color CashBgColor = new Color(0.15f, 0.55f, 0.15f, 0.95f);

        // Game version stamp shown in the Settings panel (reference has "v1.18.41-0").
        public const string GameVersion = "v1.0.0-mvp";

        public static HUDController Build(PlayerInputHandler input)
        {
            EnsureEventSystem();

            // ── Canvas ─────────────────────────────────────────────────────
            var canvasGO = new GameObject("HUD_Canvas", typeof(RectTransform));
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // All HUD elements live inside a safe-area container so nothing hides
            // under a notch / rounded corner on phones.
            var safeGO = NewUI("SafeArea", canvasGO.transform);
            var safeRT = safeGO.GetComponent<RectTransform>();
            Stretch(safeRT);
            safeGO.AddComponent<SafeAreaFitter>();

            Transform root = safeGO.transform;

            // ── Top-right: Cash display (green pill badge, like the reference) ──
            // Reference uses a small stacked-bill icon (not a dollar sign) as the
            // pill's leading glyph. We approximate with a rounded green rectangle
            // that reads as a "bill" thumbnail on-screen.
            var cashPanel = NewPanel("CashPanel", root, new Color(0.94f, 0.96f, 0.92f, 0.95f));
            SetAnchored(cashPanel.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-24, -24), new Vector2(200, 56));
            RoundCorners(cashPanel, 30f);

            // Bill icon — small green rounded rect inside the pill.
            var cashIconImg = NewPanel("CashBillIcon", cashPanel.transform, new Color(0.28f, 0.75f, 0.32f));
            SetAnchored(cashIconImg.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(10, 0), new Vector2(36, 24));
            cashIconImg.sprite = RoundedSprite;
            cashIconImg.type = Image.Type.Sliced;
            // Tiny darker fold line to give the bill some depth.
            var billFold = NewPanel("BillFold", cashIconImg.transform, new Color(0.18f, 0.55f, 0.22f));
            SetAnchored(billFold.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(2, 0));

            var cashText = NewText("CashText", cashPanel.transform, "0", 30, TextAnchor.MiddleRight);
            cashText.fontStyle = FontStyle.Bold;
            cashText.color = new Color(0.15f, 0.15f, 0.15f); // dark text on the light pill
            SetAnchored(cashText.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
                new Vector2(-14, 0), new Vector2(-50, 0));

            // ── Top-right: Level text (below cash) ──
            var levelText = NewText("LevelText", root, "Lv 1", 24, TextAnchor.MiddleRight);
            levelText.fontStyle = FontStyle.Bold;
            SetAnchored(levelText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-30, -92), new Vector2(180, 40));

            // ── Top-left button column: MENU / UPGRADES / PRICES (no overlaps) ──
            var settingsBtn = NewButton("SettingsButton", root, "MENU", new Color(0.55f, 0.35f, 0.75f, 0.9f), out _);
            SetAnchored(settingsBtn.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -20), new Vector2(150, 56));
            RoundCorners(settingsBtn.GetComponent<Image>(), 14f);

            // ── Phone-order timer chip on the left column (reference "how buyer
            // spaw:leave.png" shows a compact truck-icon pill with "09m 35s" and
            // a small red notification dot when an order is active). Sits below
            // the MENU / UPGRADES / PRICES stack.
            var phoneChip = NewPanel("PhoneOrderChip", root, new Color(0.98f, 0.87f, 0.42f, 0.95f));
            SetAnchored(phoneChip.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -220), new Vector2(96, 68));
            RoundCorners(phoneChip, 14f);
            // Little truck-shaped glyph (grey cab + darker box)
            var truckCab = NewPanel("PhoneChipCab", phoneChip.transform, new Color(0.30f, 0.30f, 0.34f));
            SetAnchored(truckCab.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -10), new Vector2(52, 24));
            RoundCorners(truckCab, 6f);
            var phoneTimerLbl = NewText("PhoneOrderChipTimer", phoneChip.transform, "--m --s", 18, TextAnchor.LowerCenter);
            phoneTimerLbl.fontStyle = FontStyle.Bold;
            phoneTimerLbl.color = new Color(0.12f, 0.12f, 0.15f);
            SetAnchored(phoneTimerLbl.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                new Vector2(0, 6), new Vector2(-6, 26));
            // Red notification dot in the corner — only shown while an order is
            // active. HUDController toggles its GameObject.
            var phoneDot = NewPanel("PhoneOrderChipDot", phoneChip.transform, new Color(0.90f, 0.20f, 0.20f));
            SetAnchored(phoneDot.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-6, -6), new Vector2(14, 14));
            phoneDot.sprite = CircleSprite;
            phoneDot.gameObject.SetActive(false);

            // ── Pause overlay ──────────────────────────────────────────────
            var overlay = NewPanel("PauseOverlay", root, new Color(0, 0, 0, 0.65f));
            Stretch(overlay.rectTransform);
            var pausedLabel = NewText("PausedLabel", overlay.transform, "PAUSED", 72, TextAnchor.MiddleCenter);
            pausedLabel.fontStyle = FontStyle.Bold;
            SetAnchored(pausedLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 90), new Vector2(600, 110));
            var resumeBtn = NewButton("ResumeButton", overlay.transform, "RESUME", GreenColor, out _);
            SetAnchored(resumeBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -50), new Vector2(320, 90));

            // ── Phone order banner ─────────────────────────────────────────
            // Reference style: a friendly full-width lavender banner across the
            // top of the screen ("PHONE ORDER" + timer + wanted items + value +
            // COLLECT pill + red X), not a dev-looking dark card in the corner.
            var phonePanel = NewPanel("PhoneOrderPanel", root, new Color(0.72f, 0.62f, 0.95f, 0.97f));
            SetAnchored(phonePanel.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -90), new Vector2(760, 190));
            RoundCorners(phonePanel, 22f);

            var phoneTitle = NewText("PhoneOrderTitle", phonePanel.transform, "PHONE ORDER", 30, TextAnchor.MiddleCenter);
            phoneTitle.fontStyle = FontStyle.Bold;
            phoneTitle.color = Color.white;
            SetAnchored(phoneTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -8), new Vector2(400, 36));

            // Inner lighter strip holding the wanted-items text, like the
            // reference's inset row.
            var phoneInner = NewPanel("PhoneOrderInner", phonePanel.transform, new Color(0.79f, 0.70f, 0.97f, 1f));
            SetAnchored(phoneInner.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
                new Vector2(0, -22), new Vector2(-28, -66));
            RoundCorners(phoneInner, 16f);

            var phoneText = NewText("PhoneOrderText", phoneInner.transform, "", 24, TextAnchor.MiddleCenter);
            phoneText.fontStyle = FontStyle.Bold;
            phoneText.color = new Color(0.16f, 0.10f, 0.30f);
            SetAnchored(phoneText.rectTransform, new Vector2(0, 0), new Vector2(0.72f, 1), new Vector2(0.5f, 0.5f),
                new Vector2(10, 0), new Vector2(-16, 0));

            // Green COLLECT pill on the right of the inset row (greyed until
            // stock covers the order — HUDController drives interactable).
            var fulfilBtn = NewButton("FulfilOrderButton", phoneInner.transform, "COLLECT", GreenColor, out _);
            SetAnchored(fulfilBtn.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-14, 0), new Vector2(170, 58));
            RoundCorners(fulfilBtn.GetComponent<Image>(), 18f);

            // Small red X in the banner corner = dismiss, like the reference.
            var dismissBtn = NewButton("DismissOrderButton", phonePanel.transform, "X", RedColor, out _);
            SetAnchored(dismissBtn.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-10, -8), new Vector2(46, 46));
            RoundCorners(dismissBtn.GetComponent<Image>(), 12f);

            // ── Price panel (center overlay) ───────────────────────────────
            var pricePanel = NewPanel("PricePanel", root, GreenShell);
            SetAnchored(pricePanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 0), new Vector2(700, 620));
            RoundCorners(pricePanel, 16f);
            ApplyPanelArt(pricePanel);
            pricePanel.gameObject.AddComponent<PricePanelController>();
            var priceTitle = NewText("PriceTitle", pricePanel.transform, "PRICING", 36, TextAnchor.MiddleCenter);
            priceTitle.fontStyle = FontStyle.Bold;
            SetAnchored(priceTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -24), new Vector2(500, 56));
            var closePriceBtn = NewButton("ClosePricePanelButton", pricePanel.transform, "X", RedColor, out var closePriceTxt);
            ApplyCloseArt(closePriceBtn, closePriceTxt);
            SetAnchored(closePriceBtn.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-12, -12), new Vector2(56, 56));

            var openPriceBtn = NewButton("OpenPricePanelButton", root, "PRICES", AccentColor, out _);
            SetAnchored(openPriceBtn.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -156), new Vector2(160, 60));
            RoundCorners(openPriceBtn.GetComponent<Image>(), 16f);

            // ── Upgrade panel (center overlay) ───────────────────────────────
            var upgradePanel = NewPanel("UpgradePanel", root, GreenShell);
            // Batch 36: 700×560 → 740×640. Content now scrolls (UpgradePanelController
            // wraps each tab in a ScrollRect), the larger shell just shows more rows
            // before scrolling starts.
            SetAnchored(upgradePanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 0), new Vector2(740, 640));
            RoundCorners(upgradePanel, 16f);
            ApplyPanelArt(upgradePanel);
            upgradePanel.gameObject.AddComponent<UpgradePanelController>();

            var upgradeTitle = NewText("UpgradeTitle", upgradePanel.transform, "UPGRADES", 36, TextAnchor.MiddleCenter);
            upgradeTitle.fontStyle = FontStyle.Bold;
            SetAnchored(upgradeTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -24), new Vector2(500, 56));
            
            var closeUpgradeBtn = NewButton("CloseUpgradePanelButton", upgradePanel.transform, "X", RedColor, out var closeUpTxt);
            ApplyCloseArt(closeUpgradeBtn, closeUpTxt);
            SetAnchored(closeUpgradeBtn.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-12, -12), new Vector2(56, 56));

            var openUpgradeBtn = NewButton("OpenUpgradePanelButton", root, "UPGRADES", AccentColor, out _);
            SetAnchored(openUpgradeBtn.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -88), new Vector2(160, 60));
            RoundCorners(openUpgradeBtn.GetComponent<Image>(), 16f);

            // ── Inventory readout (bottom-right, above net button) ─────────
            var invText = NewText("InventoryText", root, "", 20, TextAnchor.LowerRight);
            SetAnchored(invText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-30, 220), new Vector2(360, 280));

            // ── Joystick (Floating over whole screen) ─────────────
            var touchZone = NewPanel("JoystickTouchZone", root, new Color(0, 0, 0, 0));
            Stretch(touchZone.rectTransform);
            // The touch zone must sit BEHIND every button/panel in the canvas order,
            // otherwise it swallows their clicks (MENU/UPGRADES/PRICES were dead).
            // Empty screen space still reaches it, so the floating joystick keeps working.
            touchZone.rectTransform.SetAsFirstSibling();

            // Playtest: near-white at 0.35 alpha was barely visible on the cream floor.
            // Cooler tint + higher alpha so the joystick base reads while in use.
            var joyBg = NewPanel("JoystickBackground", touchZone.transform, new Color(0.82f, 0.86f, 0.95f, 0.6f));
            var bgRt = joyBg.rectTransform;
            SetAnchored(bgRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250, 250));
            joyBg.sprite = CircleSprite; // true circle, like the reference joystick

            // Reference-parity: four small triangle arrows around the joystick
            // ring indicating the four movement directions. Rendered as tiny
            // rotated rounded quads at N/S/E/W of the background circle.
            void AddArrow(float angleDeg)
            {
                var a = NewPanel($"Arrow_{angleDeg}", joyBg.transform, new Color(1f, 1f, 1f, 0.85f));
                var art = a.rectTransform;
                float rad = angleDeg * Mathf.Deg2Rad;
                Vector2 pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 110f;
                SetAnchored(art, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    pos, new Vector2(24, 24));
                art.localRotation = Quaternion.Euler(0f, 0f, angleDeg - 90f);
                a.sprite = RoundedSprite;
                a.type = Image.Type.Sliced;
            }
            AddArrow(90);   // up
            AddArrow(0);    // right
            AddArrow(-90);  // down
            AddArrow(180);  // left

            var joyKnob = NewPanel("Handle", joyBg.transform, new Color(1f, 1f, 1f, 0.85f));
            var knobRt = joyKnob.rectTransform;
            SetAnchored(knobRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100));
            joyKnob.sprite = CircleSprite;

            // "Hand" thumb indicator on the knob: a light blue rounded diamond
            // that reads as a finger/cursor pointing to where the touch is.
            var hand = NewPanel("HandCursor", joyKnob.transform, new Color(0.42f, 0.72f, 0.95f, 1f));
            var handRt = hand.rectTransform;
            SetAnchored(handRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(6, -12), new Vector2(48, 60));
            handRt.localRotation = Quaternion.Euler(0f, 0f, -15f);
            hand.sprite = RoundedSprite;
            hand.type = Image.Type.Sliced;

            var joystick = touchZone.gameObject.AddComponent<Joystick>();
            joystick.Background = bgRt;
            joystick.Handle = knobRt;
            joystick.HandleRange = 0.5f; // restrict knob to inner 50%

            // ── Throw net button (bottom-right) ────────────────────────────
            var netBtn = NewButton("ThrowNetButton", root, "NET", new Color(0.55f, 0.35f, 0.75f, 0.9f), out _);
            SetAnchored(netBtn.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-40, 40), new Vector2(140, 140));
            netBtn.GetComponent<Image>().sprite = CircleSprite; // circular action button

            // ── Dash button (above NET) — 1.6x speed burst, 2s on / 5s cooldown ──
            var dashBtn = NewButton("DashButton", root, "DASH", new Color(0.95f, 0.60f, 0.20f, 0.9f), out _);
            SetAnchored(dashBtn.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-40, 196), new Vector2(120, 120));
            var dashImg = dashBtn.GetComponent<Image>();
            dashImg.sprite = CircleSprite;
            dashBtn.onClick.AddListener(() => { if (input != null) input.TriggerDash(); });
            // Grey the button out during cooldown so its state is always readable.
            var dashCd = dashBtn.gameObject.AddComponent<DashCooldownTint>();
            dashCd.Input = input;
            dashCd.Target = dashImg;

            // ── Pause button (Settings acts as pause toggle too) ──────────
            var pauseBtn = settingsBtn; // Re-use settings as pause toggle

            // ── Welcome-back / offline earnings panel (center, hidden by default) ──
            var offlinePanel = NewPanel("OfflinePanel", root, GreenShell);
            SetAnchored(offlinePanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 0), new Vector2(520, 400));
            var offlineTitle = NewText("OfflineTitle", offlinePanel.transform, "WELCOME BACK!", 34, TextAnchor.MiddleCenter);
            offlineTitle.fontStyle = FontStyle.Bold;
            SetAnchored(offlineTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -20), new Vector2(460, 50));
            var offlineText = NewText("OfflineText", offlinePanel.transform, "", 24, TextAnchor.UpperCenter);
            SetAnchored(offlineText.rectTransform, new Vector2(0, 0.25f), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -80), new Vector2(-40, 0));
            var collectOfflineBtn = NewButton("CollectOfflineButton", offlinePanel.transform, "COLLECT", GreenColor, out _);
            SetAnchored(collectOfflineBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 20), new Vector2(240, 64));

            // ── SETTINGS panel (purple shell — reference parity) ──
            var settingsPanel = NewPanel("SettingsPanel", root, PurpleShell);
            SetAnchored(settingsPanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 0), new Vector2(700, 420));
            RoundCorners(settingsPanel, 16f);
            ApplyPanelArt(settingsPanel);
            var settingsTitle = NewText("SettingsTitle", settingsPanel.transform, "SETTINGS", 36, TextAnchor.MiddleCenter);
            settingsTitle.fontStyle = FontStyle.Bold;
            SetAnchored(settingsTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -24), new Vector2(500, 56));
            var closeSettingsBtn = NewButton("CloseSettingsButton", settingsPanel.transform, "X", RedColor, out var closeSetTxt);
            ApplyCloseArt(closeSettingsBtn, closeSetTxt);
            SetAnchored(closeSettingsBtn.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-12, -12), new Vector2(56, 56));

            // Volume row: cyan speaker icon + cyan slider.
            var speakerIcon = NewPanel("SpeakerIcon", settingsPanel.transform, new Color(0.42f, 0.78f, 0.95f));
            SetAnchored(speakerIcon.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(-160, -140), new Vector2(48, 48));
            RoundCorners(speakerIcon, 12f);

            var volSliderGO = new GameObject("VolumeSlider", typeof(RectTransform));
            volSliderGO.transform.SetParent(settingsPanel.transform, false);
            var volRT = volSliderGO.GetComponent<RectTransform>();
            SetAnchored(volRT, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(60, -140), new Vector2(320, 20));
            var vol = volSliderGO.AddComponent<Slider>();
            vol.minValue = 0f; vol.maxValue = 1f; vol.value = 1f;
            // Track background
            var volBg = new GameObject("Background", typeof(RectTransform));
            volBg.transform.SetParent(volSliderGO.transform, false);
            SetAnchored(volBg.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var volBgImg = volBg.AddComponent<Image>();
            volBgImg.color = new Color(0.60f, 0.85f, 0.95f, 0.8f);
            volBgImg.sprite = RoundedSprite;
            volBgImg.type = Image.Type.Sliced;
            // Fill
            var volFillArea = new GameObject("FillArea", typeof(RectTransform));
            volFillArea.transform.SetParent(volSliderGO.transform, false);
            SetAnchored(volFillArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var volFill = new GameObject("Fill", typeof(RectTransform));
            volFill.transform.SetParent(volFillArea.transform, false);
            var volFillImg = volFill.AddComponent<Image>();
            volFillImg.color = new Color(0.42f, 0.85f, 0.95f);
            volFillImg.sprite = RoundedSprite;
            volFillImg.type = Image.Type.Sliced;
            var volFillRt = volFill.GetComponent<RectTransform>();
            volFillRt.anchorMin = Vector2.zero; volFillRt.anchorMax = new Vector2(0, 1);
            volFillRt.sizeDelta = new Vector2(20, 0);
            vol.fillRect = volFillRt;
            // Handle
            var volHandleArea = new GameObject("HandleArea", typeof(RectTransform));
            volHandleArea.transform.SetParent(volSliderGO.transform, false);
            SetAnchored(volHandleArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var volHandle = new GameObject("Handle", typeof(RectTransform));
            volHandle.transform.SetParent(volHandleArea.transform, false);
            var volHandleImg = volHandle.AddComponent<Image>();
            volHandleImg.color = new Color(0.55f, 0.90f, 0.98f);
            volHandleImg.sprite = CircleSprite;
            var volHandleRt = volHandle.GetComponent<RectTransform>();
            volHandleRt.sizeDelta = new Vector2(28, 28);
            vol.handleRect = volHandleRt;
            vol.onValueChanged.AddListener(v => AudioListener.volume = Mathf.Clamp01(v));

            // ── Haptics toggle + graphics preset (plan.md §7 Settings) ──
            var hapticLabel = NewText("HapticsLabel", settingsPanel.transform, "VIBRATION", 22, TextAnchor.MiddleLeft);
            hapticLabel.fontStyle = FontStyle.Bold;
            hapticLabel.color = new Color(0.20f, 0.20f, 0.30f);
            SetAnchored(hapticLabel.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(-160, -200), new Vector2(160, 34));
            var hapticBtn = NewButton("HapticsToggle", settingsPanel.transform,
                PlayerPrefs.GetInt("MiniMart_Haptics", 1) == 1 ? "ON" : "OFF",
                GreenColor, out var hapticTxt);
            SetAnchored(hapticBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(20, -200), new Vector2(90, 44));
            RoundCorners(hapticBtn.GetComponent<Image>(), 12f);
            hapticBtn.onClick.AddListener(() =>
            {
                int on = PlayerPrefs.GetInt("MiniMart_Haptics", 1) == 1 ? 0 : 1;
                PlayerPrefs.SetInt("MiniMart_Haptics", on);
                PlayerPrefs.Save();
                if (hapticTxt != null) hapticTxt.text = on == 1 ? "ON" : "OFF";
            });

            var gfxLabel = NewText("GraphicsLabel", settingsPanel.transform, "GRAPHICS", 22, TextAnchor.MiddleLeft);
            gfxLabel.fontStyle = FontStyle.Bold;
            gfxLabel.color = new Color(0.20f, 0.20f, 0.30f);
            SetAnchored(gfxLabel.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(-160, -260), new Vector2(160, 34));
            string[] gfxNames = { "LOW", "MED", "HIGH" };
            var gfxPresets = new[] { Core.QualityPreset.LowPower, Core.QualityPreset.Balanced, Core.QualityPreset.High };
            // Playtest UX: the three quality buttons had NO selected state — you
            // couldn't tell which preset was active. Track them and highlight the
            // chosen one (green) while the rest read as dim/unselected.
            var gfxSelected = new Color(0.32f, 0.80f, 0.38f);
            var gfxIdle     = new Color(0.55f, 0.62f, 0.70f);
            var gfxButtons  = new Image[3];
            int savedGfx = PlayerPrefs.GetInt("MiniMart_Gfx", 1); // default MED/Balanced
            for (int gi = 0; gi < 3; gi++)
            {
                int idx = gi;
                var b = NewButton($"Gfx_{gfxNames[gi]}", settingsPanel.transform, gfxNames[gi],
                    gfxIdle, out _);
                SetAnchored(b.GetComponent<RectTransform>(), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                    new Vector2(20 + gi * 100, -260), new Vector2(90, 44));
                var bImg = b.GetComponent<Image>();
                RoundCorners(bImg, 12f);
                gfxButtons[gi] = bImg;
                b.onClick.AddListener(() =>
                {
                    var gm = MiniMart.GameManager.Instance;
                    if (gm != null) gm.ApplyQualityPreset(gfxPresets[idx]);
                    PlayerPrefs.SetInt("MiniMart_Gfx", idx);
                    PlayerPrefs.Save();
                    for (int k = 0; k < 3; k++)
                        if (gfxButtons[k] != null) gfxButtons[k].color = k == idx ? gfxSelected : gfxIdle;
                });
            }
            for (int k = 0; k < 3; k++)
                if (gfxButtons[k] != null) gfxButtons[k].color = k == savedGfx ? gfxSelected : gfxIdle;

            // Version tag (bottom-right of settings panel).
            var verText = NewText("VersionText", settingsPanel.transform, GameVersion, 18, TextAnchor.LowerRight);
            verText.color = new Color(0.20f, 0.20f, 0.25f);
            SetAnchored(verText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-18, 12), new Vector2(160, 30));

            // ── Store level-up popup (center, hidden by default) ──
            var levelUpPanel = NewPanel("LevelUpPanel", root, GreenShell);
            SetAnchored(levelUpPanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 0), new Vector2(520, 340));
            var levelUpTitle = NewText("LevelUpTitle", levelUpPanel.transform, "LEVEL UP!", 40, TextAnchor.MiddleCenter);
            levelUpTitle.fontStyle = FontStyle.Bold;
            SetAnchored(levelUpTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -22), new Vector2(460, 56));
            var levelUpText = NewText("LevelUpText", levelUpPanel.transform, "", 24, TextAnchor.UpperCenter);
            SetAnchored(levelUpText.rectTransform, new Vector2(0, 0.28f), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -90), new Vector2(-40, 0));
            var levelUpOkBtn = NewButton("LevelUpOkButton", levelUpPanel.transform, "AWESOME!", GreenColor, out _);
            SetAnchored(levelUpOkBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 18), new Vector2(240, 60));

            // ── HUDController ─────────────────────────────────────────────
            var hudGO = new GameObject("HUDController");
            hudGO.SetActive(false);
            var hud = hudGO.AddComponent<HUDController>();
            hud.CashLabel = cashText;
            hud.LevelLabel = levelText;
            hud.PauseButton = pauseBtn;
            hud.ResumeButton = resumeBtn;
            hud.PauseOverlay = overlay.gameObject;
            hud.SettingsPanel = settingsPanel.gameObject;
            hud.CloseSettingsButton = closeSettingsBtn;
            hud.VolumeSlider = vol;
            hud.PricePanel = pricePanel.gameObject;
            hud.OpenPricePanelButton = openPriceBtn;
            hud.ClosePricePanelButton = closePriceBtn;
            
            hud.UpgradePanel = upgradePanel.gameObject;
            hud.OpenUpgradePanelButton = openUpgradeBtn;
            hud.CloseUpgradePanelButton = closeUpgradeBtn;
            
            hud.PhoneOrderPanel = phonePanel.gameObject;
            hud.PhoneOrderLabel = phoneText;
            hud.FulfilOrderButton = fulfilBtn;
            hud.DismissOrderButton = dismissBtn;
            hud.PhoneOrderChip = phoneChip.gameObject;
            hud.PhoneOrderChipTimer = phoneTimerLbl;
            hud.PhoneOrderChipDot = phoneDot.gameObject;
            hud.InventoryLabel = invText;
            hud.OfflinePanel = offlinePanel.gameObject;
            hud.OfflineLabel = offlineText;
            hud.CollectOfflineButton = collectOfflineBtn;
            hud.LevelUpPanel = levelUpPanel.gameObject;
            hud.LevelUpLabel = levelUpText;
            hud.LevelUpOkButton = levelUpOkBtn;
            hudGO.SetActive(true); // triggers Awake with all refs populated

            // ── On-device error readout (bottom-center, hidden until an error fires) ──
            var errText = NewText("RuntimeErrorText", root, "", 18, TextAnchor.LowerCenter);
            errText.color = new Color(1f, 0.4f, 0.4f, 1f);
            SetAnchored(errText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 8), new Vector2(900, 60));
            errText.gameObject.SetActive(false);
            var errOverlay = canvasGO.AddComponent<RuntimeErrorOverlay>();
            errOverlay.Bind(errText);

            // ── Wire on-screen controls into the input handler ─────────────
            if (input != null)
                input.BindOnScreenControls(joystick, netBtn);

            Debug.Log("[HUDBuilder] Dashboard built and wired.");
            return hud;
        }

        // ─────────────────────────── helpers ───────────────────────────

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Text NewText(string name, Transform parent, string content, int size, TextAnchor align)
        {
            var go = NewUI(name, parent);
            var t = go.AddComponent<Text>();
            t.text = content;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.raycastTarget = false;
            // Keep text inside its rect — overflowing labels were bleeding across the HUD.
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMaxSize = size;
            t.resizeTextMinSize = Mathf.Max(10, size / 2);
            t.font = UIFont;
            // Add outline for readability
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.6f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        private static Image NewPanel(string name, Transform parent, Color color)
        {
            var go = NewUI(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static Button NewButton(string name, Transform parent, string label, Color bg, out Text labelText)
        {
            var go = NewUI(name, parent);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            labelText = NewText(name + "_Label", go.transform, label, 24, TextAnchor.MiddleCenter);
            Stretch(labelText.rectTransform);
            return btn;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
        }

        private static void RoundCorners(Image img, float radius)
        {
            // Real rounded corners via a runtime-generated 9-sliced sprite.
            // The sprite's corner radius is 20px; pixelsPerUnitMultiplier scales it
            // so the on-screen radius approximates the requested one.
            img.sprite = RoundedSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = Mathf.Clamp(20f / Mathf.Max(radius, 4f), 0.25f, 4f);
        }

        private static void RoundCorners(Graphic graphic, float radius)
        {
            // No-op for non-Image graphics
        }
    }
}
