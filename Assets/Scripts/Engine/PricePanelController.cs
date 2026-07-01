using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniMart.Core;
using MiniMart.Catalog;

namespace MiniMart.UI
{
    /// <summary>
    /// One row in the price panel for a single item. Drag this prefab into a VerticalLayoutGroup.
    /// </summary>
    public class PriceRow : MonoBehaviour
    {
        public ItemType Item;
        public TextMeshProUGUI ItemLabel;
        public Slider PriceSlider;       // range: 0.05 - 5.00
        public TextMeshProUGUI PriceText;
        public Toggle OfferToggle;       // activates 20% discount offer
        public TextMeshProUGUI OfferLabel;

        private Economy.EconomyManager economy;

        public void Init(Economy.EconomyManager eco)
        {
            economy = eco;
            if (ItemLabel != null) ItemLabel.text = Item.ToString();

            float basePrice = PriceCatalog.BasePrice.TryGetValue(Item, out float bp) ? bp : 1f;
            if (PriceSlider != null)
            {
                PriceSlider.minValue = 0.05f;
                PriceSlider.maxValue = 5f;
                PriceSlider.value    = basePrice;
                PriceSlider.onValueChanged.AddListener(OnSliderChanged);
            }

            if (OfferToggle != null)
            {
                OfferToggle.isOn = false;
                OfferToggle.onValueChanged.AddListener(OnOfferToggled);
            }

            RefreshDisplay(basePrice);
        }

        private void OnSliderChanged(float val)
        {
            economy?.SetManualPrice(Item, val);
            RefreshDisplay(val);
        }

        private void OnOfferToggled(bool active)
        {
            if (active)
            {
                economy?.CreateOffer(Item, 20f);
                if (OfferLabel != null) OfferLabel.text = "OFFER -20%";
            }
            else
            {
                economy?.ClearOffer(Item);
                if (OfferLabel != null) OfferLabel.text = "";
            }
        }

        private void RefreshDisplay(float price)
        {
            if (PriceText != null) PriceText.text = $"${price:F2}";
        }
    }

    /// <summary>Instantiates a PriceRow for every ItemType and populates the panel.</summary>
    public class PricePanelController : MonoBehaviour
    {
        public GameObject PriceRowPrefab;
        public Transform RowContainer;

        private void Start()
        {
            var eco = GameManager.Instance?.Economy;
            if (eco == null || PriceRowPrefab == null) return;

            foreach (ItemType item in System.Enum.GetValues(typeof(ItemType)))
            {
                var go = Instantiate(PriceRowPrefab, RowContainer);
                var row = go.GetComponent<PriceRow>();
                if (row != null) { row.Item = item; row.Init(eco); }
            }
        }
    }
}
