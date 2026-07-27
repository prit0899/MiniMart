using UnityEngine;
using UnityEngine.UI;
using MiniMart.Core;
using MiniMart.Catalog;

namespace MiniMart.UI
{
    public class PriceRow : MonoBehaviour
    {
        public ItemType Item;
        public Text ItemLabel;
        public Slider PriceSlider;
        public Text PriceText;
        public Toggle OfferToggle;
        public Text OfferLabel;

        private Economy.EconomyManager economy;

        public void Init(Economy.EconomyManager eco)
        {
            economy = eco;
            if (ItemLabel != null) ItemLabel.text = Item.ToString();

            float basePrice = PriceCatalog.BasePrice.TryGetValue(Item, out float bp) ? bp : 1f;
            if (PriceSlider != null)
            {
                // GDD 6.1: adjustable +/-50% around the base price, floor $0.01.
                PriceSlider.minValue = Mathf.Max(0.01f, basePrice * 0.5f);
                PriceSlider.maxValue = basePrice * 1.5f;
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
            // Genre convention (and the reference) show whole-dollar prices — "$3",
            // not "$3.00"; only show cents when the price actually has them.
            if (PriceText != null)
                PriceText.text = price == Mathf.Round(price) ? $"${price:F0}" : $"${price:F2}";
        }
    }

    /// <summary>Instantiates a PriceRow for every ItemType dynamically.</summary>
    public class PricePanelController : MonoBehaviour
    {
        private void Start()
        {
            var eco = GameManager.Instance?.Economy;
            if (eco == null) return;

            // B4: the rows used to be dumped straight on the panel with a
            // VerticalLayoutGroup, so ~18 rows ran off the bottom of the device with
            // no way to scroll. Wrap them in a ScrollRect (viewport + content) inset
            // inside the panel below the title bar, with a RectMask2D clip.
            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGO.transform.SetParent(transform, false);
            var vpRt = viewportGO.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(14, 14);
            vpRt.offsetMax = new Vector2(-14, -84);          // clear the title bar
            viewportGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f); // raycast target only

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRt = contentGO.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;

            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 10;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = gameObject.AddComponent<ScrollRect>();
            scroll.viewport = vpRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var font = Engine.HUDBuilder.UIFont;

            foreach (ItemType item in System.Enum.GetValues(typeof(ItemType)))
            {
                var rowGO = new GameObject($"PriceRow_{item}", typeof(RectTransform));
                rowGO.transform.SetParent(contentGO.transform, false);
                var rowRt = rowGO.GetComponent<RectTransform>();
                rowRt.sizeDelta = new Vector2(0, 60);

                var row = rowGO.AddComponent<PriceRow>();
                row.Item = item;

                // Row Background
                var rowBgImg = rowGO.AddComponent<Image>();
                rowBgImg.color = new Color(0.4f, 0.7f, 1f, 1f);

                // Label
                var labelGO = new GameObject("Label", typeof(RectTransform));
                labelGO.transform.SetParent(rowGO.transform, false);
                var labelRt = labelGO.GetComponent<RectTransform>();
                SetAnchored(labelRt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(150, 40));
                row.ItemLabel = labelGO.AddComponent<Text>();
                row.ItemLabel.font = font;
                row.ItemLabel.color = Color.black;
                row.ItemLabel.alignment = TextAnchor.MiddleLeft;
                row.ItemLabel.fontStyle = FontStyle.Bold;

                // Slider
                var sliderGO = new GameObject("Slider", typeof(RectTransform));
                sliderGO.transform.SetParent(rowGO.transform, false);
                var sliderRt = sliderGO.GetComponent<RectTransform>();
                SetAnchored(sliderRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(200, 30));
                
                var slider = sliderGO.AddComponent<Slider>();
                row.PriceSlider = slider;
                
                var bgGO = new GameObject("Background", typeof(RectTransform));
                bgGO.transform.SetParent(sliderGO.transform, false);
                SetAnchored(bgGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var bgImg = bgGO.AddComponent<Image>();
                bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

                var fillAreaGO = new GameObject("FillArea", typeof(RectTransform));
                fillAreaGO.transform.SetParent(sliderGO.transform, false);
                SetAnchored(fillAreaGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                
                var fillGO = new GameObject("Fill", typeof(RectTransform));
                fillGO.transform.SetParent(fillAreaGO.transform, false);
                var fillImg = fillGO.AddComponent<Image>();
                fillImg.color = new Color(0.2f, 0.8f, 0.2f, 1f);
                // Without explicit anchors the fill keeps its default 100x100 sizeDelta and
                // renders as a giant block over the whole panel. Anchor it as a proper fill bar.
                var fillRt = fillGO.GetComponent<RectTransform>();
                fillRt.anchorMin = new Vector2(0, 0);
                fillRt.anchorMax = new Vector2(0, 1);
                fillRt.pivot = new Vector2(0.5f, 0.5f);
                fillRt.sizeDelta = new Vector2(10, 0);
                slider.fillRect = fillRt;

                // Price text
                var priceGO = new GameObject("PriceText", typeof(RectTransform));
                priceGO.transform.SetParent(rowGO.transform, false);
                var priceRt = priceGO.GetComponent<RectTransform>();
                SetAnchored(priceRt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-150, 0), new Vector2(100, 40));
                row.PriceText = priceGO.AddComponent<Text>();
                row.PriceText.font = font;
                row.PriceText.color = Color.black;
                row.PriceText.alignment = TextAnchor.MiddleRight;

                // Offer Toggle
                var toggleGO = new GameObject("OfferToggle", typeof(RectTransform));
                toggleGO.transform.SetParent(rowGO.transform, false);
                var toggleRt = toggleGO.GetComponent<RectTransform>();
                SetAnchored(toggleRt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(40, 40));
                
                var toggle = toggleGO.AddComponent<Toggle>();
                row.OfferToggle = toggle;
                
                var toggleBgGO = new GameObject("Background", typeof(RectTransform));
                toggleBgGO.transform.SetParent(toggleGO.transform, false);
                SetAnchored(toggleBgGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var toggleBgImg = toggleBgGO.AddComponent<Image>();
                toggleBgImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

                var toggleCheckGO = new GameObject("Checkmark", typeof(RectTransform));
                toggleCheckGO.transform.SetParent(toggleBgGO.transform, false);
                SetAnchored(toggleCheckGO.GetComponent<RectTransform>(), new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var toggleCheckImg = toggleCheckGO.AddComponent<Image>();
                toggleCheckImg.color = Color.red;
                
                toggle.targetGraphic = toggleBgImg;
                toggle.graphic = toggleCheckImg;

                // B5: the toggle was an unlabeled dark square. Add a "%" glyph so it
                // reads as the discount/offer control.
                var pctGO = new GameObject("Pct", typeof(RectTransform));
                pctGO.transform.SetParent(toggleBgGO.transform, false);
                SetAnchored(pctGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var pctTxt = pctGO.AddComponent<Text>();
                pctTxt.text = "%";
                pctTxt.font = font;
                pctTxt.color = Color.white;
                pctTxt.fontStyle = FontStyle.Bold;
                pctTxt.alignment = TextAnchor.MiddleCenter;
                pctTxt.raycastTarget = false;

                // Offer Label
                var offerGO = new GameObject("OfferText", typeof(RectTransform));
                offerGO.transform.SetParent(rowGO.transform, false);
                var offerRt = offerGO.GetComponent<RectTransform>();
                SetAnchored(offerRt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -10), new Vector2(100, 20));
                row.OfferLabel = offerGO.AddComponent<Text>();
                row.OfferLabel.font = font;
                row.OfferLabel.color = Color.red;
                row.OfferLabel.alignment = TextAnchor.LowerCenter;
                row.OfferLabel.fontSize = 14;

                row.Init(eco);
            }
        }

        private void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}
