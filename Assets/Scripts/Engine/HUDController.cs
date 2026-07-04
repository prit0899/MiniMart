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

        private GameManager gm;
        private Engine.PhoneOrderManager phoneOrders;
        private Engine.PhoneOrder pendingOrder;

        private void Awake()
        {
            gm = GameManager.Instance;
            phoneOrders = FindAnyObjectByType<Engine.PhoneOrderManager>();

            PauseButton?.onClick.AddListener(OnPause);
            ResumeButton?.onClick.AddListener(OnResume);
            // Only one center panel at a time — opening one closes the other.
            OpenPricePanelButton?.onClick.AddListener(() => { UpgradePanel?.SetActive(false); PricePanel?.SetActive(true); });
            ClosePricePanelButton?.onClick.AddListener(() => PricePanel?.SetActive(false));
            OpenUpgradePanelButton?.onClick.AddListener(() => { PricePanel?.SetActive(false); UpgradePanel?.SetActive(true); });
            CloseUpgradePanelButton?.onClick.AddListener(() => UpgradePanel?.SetActive(false));
            FulfilOrderButton?.onClick.AddListener(OnFulfilOrder);
            DismissOrderButton?.onClick.AddListener(OnDismissOrder);

            if (phoneOrders != null)
                phoneOrders.OnNewOrder += ShowPhoneOrder;

            CollectOfflineButton?.onClick.AddListener(() =>
            {
                gm?.DismissOfflineSummary();
                OfflinePanel?.SetActive(false);
            });

            LevelUpOkButton?.onClick.AddListener(() => LevelUpPanel?.SetActive(false));

            PauseOverlay?.SetActive(false);
            PhoneOrderPanel?.SetActive(false);
            PricePanel?.SetActive(false);
            UpgradePanel?.SetActive(false);
            OfflinePanel?.SetActive(false);
            LevelUpPanel?.SetActive(false);
        }

        private static string UnlockTextFor(int level) => level switch
        {
            2 => "Cashier 1 hired — Counter 1 runs itself now!\nKetchup unlocked for sale.",
            3 => "Wheat Flour unlocked!\nWatch out — thieves start prowling from now on.",
            4 => "Bread unlocked!\nCash Counter 2 opens with its own cashier.",
            _ => "Customers arrive faster and orders get bigger!",
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
                    LevelUpLabel.text = $"Store Level {gm.StoreLevel}\n\n{UnlockTextFor(gm.StoreLevel)}";
                LevelUpPanel?.SetActive(true);
            }

            // Show the welcome-back report once, when the boot computed one.
            if (gm.OfflineSummary != null && OfflinePanel != null && !OfflinePanel.activeSelf)
            {
                if (OfflineLabel != null) OfflineLabel.text = gm.OfflineSummary;
                OfflinePanel.SetActive(true);
            }

            if (CashLabel != null)
                CashLabel.text = $"${gm.Economy.PlayerCash:F0}";

            if (LevelLabel != null)
                LevelLabel.text = $"Lv {gm.StoreLevel}  {gm.StoreXp}/{gm.XpToNextLevel} XP";

            // Grey out FULFIL until storage can actually cover the order — tapping it
            // with short stock did nothing but log "Not enough stock".
            if (FulfilOrderButton != null && pendingOrder != null && phoneOrders != null)
                FulfilOrderButton.interactable = phoneOrders.CanFulfil(pendingOrder);

            // Live countdown on the order card; auto-hide when the order expires.
            if (pendingOrder != null && PhoneOrderPanel != null && PhoneOrderPanel.activeSelf)
            {
                if (pendingOrder.IsExpired || pendingOrder.IsFulfilled)
                {
                    PhoneOrderPanel.SetActive(false);
                    pendingOrder = null;
                }
                else
                {
                    phoneTextTimer += Time.deltaTime;
                    if (phoneTextTimer >= 1f)
                    {
                        phoneTextTimer = 0f;
                        RefreshPhoneOrderText();
                    }
                }
            }

            if (InventoryLabel != null && gm.Inventory != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("<b>Storage</b>");
                foreach (var kv in gm.Inventory.Stocks)
                {
                    if (kv.Value != null)
                        sb.AppendLine($"{kv.Key}: {kv.Value.Count}/{kv.Value.MaxCapacity}");
                }
                InventoryLabel.text = sb.ToString();
            }
        }

        private void OnPause()
        {
            gm?.Pause();
            PauseOverlay?.SetActive(true);
        }

        private void OnResume()
        {
            gm?.Resume();
            PauseOverlay?.SetActive(false);
        }

        private void ShowPhoneOrder(Engine.PhoneOrder order)
        {
            pendingOrder = order;
            RefreshPhoneOrderText();
            PhoneOrderPanel?.SetActive(true);
        }

        private float phoneTextTimer;
        private void RefreshPhoneOrderText()
        {
            if (pendingOrder == null || PhoneOrderLabel == null) return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>Phone Order!</b>  Value: ${pendingOrder.Value:F2}");
            int secs = Mathf.Max(0, Mathf.CeilToInt(pendingOrder.TimeRemaining));
            sb.AppendLine($"Time left: {secs / 60}:{secs % 60:00}");
            foreach (var kv in pendingOrder.Items)
                sb.AppendLine($"  {kv.Key}: x{kv.Value}");
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
