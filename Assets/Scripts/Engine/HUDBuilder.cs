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

        private static readonly Color PanelColor  = new Color(0.10f, 0.12f, 0.16f, 0.92f);
        // Reference-style bright green shell for the big center panels (My Mini Mart look).
        private static readonly Color GreenShell  = new Color(0.45f, 0.76f, 0.32f, 0.98f);
        private static readonly Color AccentColor = new Color(0.20f, 0.60f, 0.95f, 1f);
        private static readonly Color GreenColor  = new Color(0.20f, 0.70f, 0.30f, 1f);
        private static readonly Color RedColor    = new Color(0.85f, 0.25f, 0.25f, 1f);
        private static readonly Color CashBgColor = new Color(0.15f, 0.55f, 0.15f, 0.95f);

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
            var cashPanel = NewPanel("CashPanel", root, CashBgColor);
            SetAnchored(cashPanel.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-24, -24), new Vector2(220, 60));
            RoundCorners(cashPanel, 30f);

            var cashIcon = NewText("CashIcon", cashPanel.transform, "$", 28, TextAnchor.MiddleLeft);
            SetAnchored(cashIcon.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(12, 0), new Vector2(40, 0));

            var cashText = NewText("CashText", cashPanel.transform, "$0", 30, TextAnchor.MiddleRight);
            cashText.fontStyle = FontStyle.Bold;
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

            // ── Phone order panel (top-right, below cash) ──────────────────
            var phonePanel = NewPanel("PhoneOrderPanel", root, PanelColor);
            SetAnchored(phonePanel.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-24, -150), new Vector2(400, 300));
            RoundCorners(phonePanel, 12f);
            var phoneText = NewText("PhoneOrderText", phonePanel.transform, "", 22, TextAnchor.UpperLeft);
            SetAnchored(phoneText.rectTransform, new Vector2(0, 0.3f), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -14), new Vector2(-24, 0));
            var fulfilBtn = NewButton("FulfilOrderButton", phonePanel.transform, "FULFIL", GreenColor, out _);
            SetAnchored(fulfilBtn.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(16, 16), new Vector2(160, 60));
            var dismissBtn = NewButton("DismissOrderButton", phonePanel.transform, "DISMISS", RedColor, out _);
            SetAnchored(dismissBtn.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-16, 16), new Vector2(160, 60));

            // ── Price panel (center overlay) ───────────────────────────────
            var pricePanel = NewPanel("PricePanel", root, GreenShell);
            SetAnchored(pricePanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 0), new Vector2(700, 620));
            RoundCorners(pricePanel, 16f);
            pricePanel.gameObject.AddComponent<PricePanelController>();
            var priceTitle = NewText("PriceTitle", pricePanel.transform, "PRICING", 36, TextAnchor.MiddleCenter);
            priceTitle.fontStyle = FontStyle.Bold;
            SetAnchored(priceTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -24), new Vector2(500, 56));
            var closePriceBtn = NewButton("ClosePricePanelButton", pricePanel.transform, "X", RedColor, out _);
            SetAnchored(closePriceBtn.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-12, -12), new Vector2(56, 56));

            var openPriceBtn = NewButton("OpenPricePanelButton", root, "PRICES", AccentColor, out _);
            SetAnchored(openPriceBtn.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -148), new Vector2(150, 56));

            // ── Upgrade panel (center overlay) ───────────────────────────────
            var upgradePanel = NewPanel("UpgradePanel", root, GreenShell);
            SetAnchored(upgradePanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 0), new Vector2(700, 560));
            RoundCorners(upgradePanel, 16f);
            upgradePanel.gameObject.AddComponent<UpgradePanelController>();

            var upgradeTitle = NewText("UpgradeTitle", upgradePanel.transform, "UPGRADES", 36, TextAnchor.MiddleCenter);
            upgradeTitle.fontStyle = FontStyle.Bold;
            SetAnchored(upgradeTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -24), new Vector2(500, 56));
            
            var closeUpgradeBtn = NewButton("CloseUpgradePanelButton", upgradePanel.transform, "X", RedColor, out _);
            SetAnchored(closeUpgradeBtn.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-12, -12), new Vector2(56, 56));

            var openUpgradeBtn = NewButton("OpenUpgradePanelButton", root, "UPGRADES", AccentColor, out _);
            SetAnchored(openUpgradeBtn.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -84), new Vector2(150, 56));

            // ── Inventory readout (bottom-right, above net button) ─────────
            var invText = NewText("InventoryText", root, "", 20, TextAnchor.LowerRight);
            SetAnchored(invText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-30, 220), new Vector2(360, 280));

            // ── Joystick (Floating over whole screen) ─────────────
            var touchZone = NewPanel("JoystickTouchZone", root, new Color(0, 0, 0, 0));
            Stretch(touchZone.rectTransform);

            var joyBg = NewPanel("JoystickBackground", touchZone.transform, new Color(1f, 1f, 1f, 0.4f));
            var bgRt = joyBg.rectTransform;
            SetAnchored(bgRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250, 250));
            RoundCorners(joyBg, 125f);
            
            var joyKnob = NewPanel("Handle", joyBg.transform, new Color(1f, 1f, 1f, 0.8f));
            var knobRt = joyKnob.rectTransform;
            SetAnchored(knobRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100));
            RoundCorners(joyKnob, 50f);

            var joystick = touchZone.gameObject.AddComponent<Joystick>();
            joystick.Background = bgRt;
            joystick.Handle = knobRt;
            joystick.HandleRange = 0.5f; // restrict knob to inner 50%

            // ── Throw net button (bottom-right) ────────────────────────────
            var netBtn = NewButton("ThrowNetButton", root, "NET", new Color(0.55f, 0.35f, 0.75f, 0.9f), out _);
            SetAnchored(netBtn.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-40, 40), new Vector2(140, 140));
            RoundCorners(netBtn.GetComponent<Image>(), 70f);

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
            // Unity's built-in Image doesn't support rounded corners natively,
            // but we can fake it with sprite type. This is a visual hint only.
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
        }

        private static void RoundCorners(Graphic graphic, float radius)
        {
            // No-op for non-Image graphics
        }
    }
}
