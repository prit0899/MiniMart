using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniMart.Core;
using MiniMart.Engine;

namespace MiniMart.UI
{
    /// <summary>
    /// Drives the in-game HUD. All UI reads from GameManager systems; buttons dispatch commands
    /// back through GameManager. Views never own gameplay truth (Architecture Spec Section 3 Rule).
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Top bar")]
        public TextMeshProUGUI CashText;
        public TextMeshProUGUI LevelText;

        [Header("Pause / Resume")]
        public Button PauseButton;
        public Button ResumeButton;
        public GameObject PauseOverlay;

        [Header("Price panel")]
        public GameObject PricePanel;
        public Button OpenPricePanelButton;
        public Button ClosePricePanelButton;

        [Header("Phone order panel")]
        public GameObject PhoneOrderPanel;
        public TextMeshProUGUI PhoneOrderText;
        public Button FulfilOrderButton;
        public Button DismissOrderButton;

        private GameManager gm;
        private Engine.PhoneOrderManager phoneOrders;
        private Engine.PhoneOrder pendingOrder;

        private void Awake()
        {
            gm = GameManager.Instance;
            phoneOrders = FindObjectOfType<Engine.PhoneOrderManager>();

            PauseButton?.onClick.AddListener(OnPause);
            ResumeButton?.onClick.AddListener(OnResume);
            OpenPricePanelButton?.onClick.AddListener(() => PricePanel?.SetActive(true));
            ClosePricePanelButton?.onClick.AddListener(() => PricePanel?.SetActive(false));
            FulfilOrderButton?.onClick.AddListener(OnFulfilOrder);
            DismissOrderButton?.onClick.AddListener(() => PhoneOrderPanel?.SetActive(false));

            if (phoneOrders != null)
                phoneOrders.OnNewOrder += ShowPhoneOrder;

            PauseOverlay?.SetActive(false);
            PhoneOrderPanel?.SetActive(false);
            PricePanel?.SetActive(false);
        }

        private void Update()
        {
            if (gm == null) return;
            if (CashText != null)
                CashText.text = $"${gm.Economy.PlayerCash:F2}";
            if (LevelText != null && gm.Player != null)
                LevelText.text = $"Lv {gm.Player.Level}";
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
            if (PhoneOrderText != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"<b>Phone Order!</b>  Value: ${order.Value:F2}");
                sb.AppendLine($"Time: {Mathf.CeilToInt(order.TimeRemaining / 60f)} min");
                foreach (var kv in order.Items)
                    sb.AppendLine($"  {kv.Key}: x{kv.Value}");
                PhoneOrderText.text = sb.ToString();
            }
            PhoneOrderPanel?.SetActive(true);
        }

        private void OnFulfilOrder()
        {
            if (pendingOrder == null || phoneOrders == null) return;
            bool ok = phoneOrders.TryFulfil(pendingOrder);
            Debug.Log(ok ? "Order fulfilled!" : "Not enough stock.");
            PhoneOrderPanel?.SetActive(false);
            pendingOrder = null;
        }
    }
}
