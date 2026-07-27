using UnityEngine;
using UnityEngine.UI;
using MiniMart.Core;
using MiniMart.Engine;

namespace MiniMart.UI
{
    /// <summary>
    /// Drives the in-game HUD. All UI reads from GameManager systems; buttons dispatch commands
    /// back through GameManager. Views never own gameplay truth (Architecture Spec Section 3 Rule).
    /// Uses Unity built-in UI.Text (no TMP dependency).
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Top bar")]
        public Text CashLabel;
        public Text LevelLabel;

        [Header("Pause / Resume")]
        public Button PauseButton;
        public Button ResumeButton;
        public GameObject PauseOverlay;

        [Header("Settings")]
        public GameObject SettingsPanel;
        public Button CloseSettingsButton;
        public Slider VolumeSlider;

        [Header("Price panel")]
        public GameObject PricePanel;
        public Button OpenPricePanelButton;
        public Button ClosePricePanelButton;

        [Header("Upgrade panel")]
        public GameObject UpgradePanel;
        public Button OpenUpgradePanelButton;
        public Button CloseUpgradePanelButton;

        [Header("Phone order panel")]
        public GameObject PhoneOrderPanel;
        public Text PhoneOrderLabel;
        public Button FulfilOrderButton;
        public Button DismissOrderButton;

        [Header("Phone order chip (left column)")]
        public GameObject PhoneOrderChip;      // whole chip container
        public Text PhoneOrderChipTimer;       // "09m 35s" label
        public GameObject PhoneOrderChipDot;   // red notification dot

        [Header("Inventory readout")]
        public Text InventoryLabel;

        [Header("Offline earnings")]
        public GameObject OfflinePanel;
        public Text OfflineLabel;
        public Button CollectOfflineButton;

        [Header("Level up")]
        public GameObject LevelUpPanel;
        public Text LevelUpLabel;
        public Button LevelUpOkButton;
        private int lastSeenStoreLevel = -1;
        // The popup auto-closes after this many unscaled seconds if the player
        // doesn't tap AWESOME — otherwise it sits over the shop forever (a QA
        // playthrough left it covering screen-centre for ~30 minutes).
        private const float LevelUpAutoHideSeconds = 6f;
        private float levelUpHideAt = -1f;

        private GameManager gm;
        private Engine.PhoneOrderManager phoneOrders;
        private Engine.PhoneOrder pendingOrder;

        private void Awake()
        {
            gm = GameManager.Instance;
            phoneOrders = FindAnyObjectByType<Engine.PhoneOrderManager>();

            // Settings button (top-left "MENU") now opens the SETTINGS panel and
            // pauses the sim behind it — matches the reference where the gear
            // icon is a settings sheet, not a bare pause overlay.
            PauseButton?.onClick.AddListener(OnOpenSettings);
            ResumeButton?.onClick.AddListener(OnResume);
            CloseSettingsButton?.onClick.AddListener(OnCloseSettings);
            if (VolumeSlider != null)
                VolumeSlider.onValueChanged.AddListener(v => AudioListener.volume = Mathf.Clamp01(v));
            // B3: exactly ONE center panel at a time. Every open routes through
            // CloseAllCenterPanels() first, so Settings can't draw over Pricing, etc.
            OpenPricePanelButton?.onClick.AddListener(() => { CloseAllCenterPanels(); PricePanel?.SetActive(true); });
            ClosePricePanelButton?.onClick.AddListener(() => PricePanel?.SetActive(false));
            OpenUpgradePanelButton?.onClick.AddListener(() => { CloseAllCenterPanels(); UpgradePanel?.SetActive(true); });
            CloseUpgradePanelButton?.onClick.AddListener(() => UpgradePanel?.SetActive(false));
            // Owner: phone orders are fulfilled by physically carrying the items to
            // the van (PlayerInteraction loads it on contact) — NOT by a Collect
            // button — and the card cannot be closed/dismissed. Hide both buttons so
            // the card is a pure readout of what the van still wants.
            FulfilOrderButton?.gameObject.SetActive(false);
            DismissOrderButton?.gameObject.SetActive(false);

            if (phoneOrders != null)
                phoneOrders.OnNewOrder += ShowPhoneOrder;

            CollectOfflineButton?.onClick.AddListener(() =>
            {
                gm?.DismissOfflineSummary();
                OfflinePanel?.SetActive(false);
            });

            LevelUpOkButton?.onClick.AddListener(() =>
            {
                LevelUpPanel?.SetActive(false);
                levelUpHideAt = -1f;
            });

            PauseOverlay?.SetActive(false);
            SettingsPanel?.SetActive(false);
            PhoneOrderPanel?.SetActive(false);
            PricePanel?.SetActive(false);
            UpgradePanel?.SetActive(false);
            OfflinePanel?.SetActive(false);
            LevelUpPanel?.SetActive(false);
        }

        private static string UnlockTextFor(int level) => level switch
        {
            2  => "Counter 2 available!\nA second till keeps the queue moving.",
            3  => "Wheat Farm available!\nWatch out — thieves start prowling from now on.",
            4  => "Wheat Mill available — and you can hire a Chef!\nThe kitchen opens next level.",
            5  => "The kitchen opens: Ketchup Blender, Egg Stove & Bread Oven!\nYour chef takes it from here.",
            6  => "THE MEGAMART OPENS!\nBuy the travel pad and visit your second store.",
            7  => "MegaMart grows: Milk Bottler, Apple Orchard & Cheese Dairy!",
            8  => "Corn Field & Corn Processor available in MegaMart!",
            9  => "Herb Patch & Leaf Unit available in MegaMart!",
            10 => "Coffee Bar unlocked — your empire is complete!",
            _  => "Customers arrive faster and orders get bigger!",
        };

        private void Update()
        {
            // Try to grab GameManager if it wasn't available in Awake
            if (gm == null)
            {
                gm = GameManager.Instance;
                if (gm == null) return;
            }

            if (gm.Economy == null) return;

            // Store level-up popup: fires on level changes after the first observed value,
            // so loading a save doesn't greet the player with a stale "LEVEL UP!".
            if (lastSeenStoreLevel < 0) lastSeenStoreLevel = gm.StoreLevel;
            else if (gm.StoreLevel > lastSeenStoreLevel)
            {
                lastSeenStoreLevel = gm.StoreLevel;
                if (LevelUpLabel != null)
                    LevelUpLabel.text = gm.IsMaxLevel
                        ? "MART COMPLETE!\n\nYou built the whole mini mart —\nevery upgrade is yours. Amazing!"
                        : $"Store Level {gm.StoreLevel}\n\n{UnlockTextFor(gm.StoreLevel)}";
                LevelUpPanel?.SetActive(true);
                levelUpHideAt = Time.unscaledTime + LevelUpAutoHideSeconds;
                AudioFx.LevelUp();
            }

            // Auto-dismiss the level-up popup so it never blocks the shop.
            if (levelUpHideAt > 0f && Time.unscaledTime >= levelUpHideAt)
            {
                LevelUpPanel?.SetActive(false);
                levelUpHideAt = -1f;
            }

            // Show the welcome-back report once, when the boot computed one.
            if (gm.OfflineSummary != null && OfflinePanel != null && !OfflinePanel.activeSelf)
            {
                if (OfflineLabel != null) OfflineLabel.text = gm.OfflineSummary;
                OfflinePanel.SetActive(true);
            }

            if (CashLabel != null)
                // Reference cash pill shows just the number (icon carries the "$"
                // signal). Format as integer to match the reference "1834" style.
                CashLabel.text = $"${gm.Economy.PlayerCash:F0}";

            if (LevelLabel != null)
                LevelLabel.text = gm.IsMaxLevel
                    ? $"Lv {gm.StoreLevel}  MAX"
                    : $"Lv {gm.StoreLevel}  {gm.StoreXp}/{gm.XpToNextLevel} XP";

            // Phone-order card is shown ONLY while the player stands near the
            // delivery van (owner). It's a live readout of what the van still wants;
            // the player fulfils by carrying those items over. Drop the reference
            // once the order clears so the card and chip both go quiet.
            if (pendingOrder != null && (pendingOrder.IsExpired || pendingOrder.IsFulfilled))
                pendingOrder = null;

            bool nearVan = pendingOrder != null && PlayerNearOrderVan(pendingOrder);
            if (PhoneOrderPanel != null && PhoneOrderPanel.activeSelf != nearVan)
                PhoneOrderPanel.SetActive(nearVan);
            if (nearVan)
            {
                phoneTextTimer += Time.deltaTime;
                if (phoneTextTimer >= 0.5f) { phoneTextTimer = 0f; RefreshPhoneOrderText(); }
            }

            // Reference has NO running inventory list — the storage racks in the world
            // are the readout. Keep this label empty by default; leave it available for
            // future context-sensitive tooltips.
            if (InventoryLabel != null) InventoryLabel.text = "";

            RefreshPhoneOrderChip();
        }

        private void RefreshPhoneOrderChip()
        {
            if (PhoneOrderChip == null) return;
            bool hasActive = pendingOrder != null && !pendingOrder.IsExpired && !pendingOrder.IsFulfilled;
            if (PhoneOrderChipDot != null) PhoneOrderChipDot.SetActive(hasActive);
            if (PhoneOrderChipTimer != null)
            {
                if (hasActive)
                {
                    int s = Mathf.Max(0, Mathf.CeilToInt(pendingOrder.TimeRemaining));
                    PhoneOrderChipTimer.text = $"{s / 60:00}m {s % 60:00}s";
                }
                else PhoneOrderChipTimer.text = "--m --s";
            }
        }

        private void OnOpenSettings()
        {
            // Reference gear icon opens SETTINGS (which soft-pauses the sim);
            // Resume closes the whole overlay. Both keep the pause overlay OFF
            // — the settings panel itself is the visual "pause" state.
            CloseAllCenterPanels();   // B3: never stack over Pricing/Upgrades
            gm?.Pause();
            SettingsPanel?.SetActive(true);
        }

        /// <summary>B3: close every center panel before opening a new one. Resumes
        /// the sim if Settings (which soft-pauses) was the one open.</summary>
        private void CloseAllCenterPanels()
        {
            if (SettingsPanel != null && SettingsPanel.activeSelf)
            {
                SettingsPanel.SetActive(false);
                gm?.Resume();
            }
            PricePanel?.SetActive(false);
            UpgradePanel?.SetActive(false);
        }

        private void OnCloseSettings()
        {
            gm?.Resume();
            SettingsPanel?.SetActive(false);
        }

        // Legacy hook — some entry points still call OnPause via reflection or
        // save-triggered flows; keep it functional so nothing regresses.
        private void OnPause() => OnOpenSettings();

        private void OnResume()
        {
            gm?.Resume();
            PauseOverlay?.SetActive(false);
            SettingsPanel?.SetActive(false);
        }

        private void ShowPhoneOrder(Engine.PhoneOrder order)
        {
            // Register the order (drives the persistent chip). The full card only
            // appears once the player walks up to the van — see Update().
            pendingOrder = order;
            RefreshPhoneOrderText();
        }

        /// <summary>True while the player stands next to the van carrying this order.</summary>
        private bool PlayerNearOrderVan(Engine.PhoneOrder order)
        {
            if (gm == null || gm.Player == null) return false;
            Vector3 pp = gm.Player.transform.position;
            foreach (var van in FindObjectsByType<MiniMart.Characters.DeliveryVan>(FindObjectsSortMode.None))
                if (van.Order == order && Vector3.Distance(pp, van.transform.position) <= 4.5f)
                    return true;
            return false;
        }

        private float phoneTextTimer;
        private void RefreshPhoneOrderText()
        {
            if (pendingOrder == null || PhoneOrderLabel == null) return;
            // Banner-friendly compact layout: wanted items on one line, timer +
            // reward on the next. Big and readable for players of any age.
            var sb = new System.Text.StringBuilder();
            bool first = true;
            foreach (var kv in pendingOrder.Items)
            {
                if (!first) sb.Append("   ");
                sb.Append($"{kv.Value}x {kv.Key}");
                first = false;
            }
            int secs = Mathf.Max(0, Mathf.CeilToInt(pendingOrder.TimeRemaining));
            sb.Append($"\n{secs / 60:00}m {secs % 60:00}s   —   reward ${pendingOrder.Value:F0}");
            PhoneOrderLabel.text = sb.ToString();
        }

        private void OnFulfilOrder()
        {
            if (pendingOrder == null || phoneOrders == null) return;
            bool ok = phoneOrders.TryFulfil(pendingOrder);
            Debug.Log(ok ? "Order fulfilled!" : "Not enough stock.");
            if (!ok) return; // keep the card up so the player can restock and retry
            PhoneOrderPanel?.SetActive(false);
            pendingOrder = null;
        }

        private void OnDismissOrder()
        {
            if (pendingOrder != null) phoneOrders?.Dismiss(pendingOrder);
            pendingOrder = null;
            PhoneOrderPanel?.SetActive(false);
        }
    }
}
