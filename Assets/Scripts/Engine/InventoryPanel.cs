using UnityEngine;
using UnityEngine.UI;

using MiniMart.Core;
using MiniMart.Runtime;

namespace MiniMart.UI
{
    public class InventoryRow : MonoBehaviour
    {
        public ItemType Item;
        public Text Label;
        public Slider FillBar;
        public Text CountText;

        public void Refresh(StoreInventory inv)
        {
            var stock = inv.Stocks[Item];
            if (Label != null) Label.text = Item.ToString();
            if (FillBar != null)
            {
                FillBar.maxValue = stock.MaxCapacity;
                FillBar.value = stock.Count;
            }
            if (CountText != null) CountText.text = $"{stock.Count}/{stock.MaxCapacity}";
        }
    }

    public class InventoryPanel : MonoBehaviour
    {
        public GameObject RowPrefab;
        public Transform Container;

        private InventoryRow[] rows;

        private void Start()
        {
            var types = (ItemType[])System.Enum.GetValues(typeof(ItemType));
            rows = new InventoryRow[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                if (RowPrefab == null) break;
                var go = Instantiate(RowPrefab, Container);
                var row = go.GetComponent<InventoryRow>();
                if (row != null) { row.Item = types[i]; rows[i] = row; }
            }
        }

        private void Update()
        {
            var inv = GameManager.Instance?.Inventory;
            if (inv == null || rows == null) return;
            foreach (var row in rows)
                row?.Refresh(inv);
        }
    }
}
