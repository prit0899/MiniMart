using System.Collections.Generic;
using UnityEngine;
using MiniMart.Core;

namespace MiniMart.Engine
{
    /// <summary>
    /// Physical access point for ONE item's storage, placed next to its source
    /// (egg rack by the coop, milk/cheese by the cow, wheat/flour by the wheat farm,
    /// tomato/ketchup by the tomato plants, bread by the oven). The logical pool is
    /// still StoreInventory — racks are where characters walk to deposit/withdraw,
    /// and each shows its own live "n/cap" badge.
    /// </summary>
    public class StorageRack : MonoBehaviour
    {
        public ItemType Item;

        private static readonly Dictionary<ItemType, StorageRack> All = new Dictionary<ItemType, StorageRack>();

        // Clear the registry on every play start so stale rack references never survive
        // (important when "Enter Play Mode Options" disables domain reload).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => All.Clear();

        private TextMesh badge;
        private float timer;

        /// <summary>Active rack for an item, or null (e.g. still behind a purchase pad).</summary>
        public static StorageRack Get(ItemType item) =>
            All.TryGetValue(item, out var r) && r != null ? r : null;

        /// <summary>World position of an item's rack; falls back when not yet purchased.</summary>
        public static Vector3 PositionOf(ItemType item, Vector3 fallback)
        {
            var r = Get(item);
            return r != null ? r.transform.position : fallback;
        }

        private void OnEnable() => All[Item] = this;
        private void OnDisable()
        {
            if (All.TryGetValue(Item, out var r) && r == this) All.Remove(Item);
        }

        /// <summary>Builds the crate visuals + badge. Called once by the bootstrapper.</summary>
        public void Build()
        {
            var col = PrimitiveFactory.ItemColor(Item);
            var wood = new Color(0.62f, 0.44f, 0.24f);
            PrimitiveFactory.Part(PrimitiveType.Cube, transform, new Vector3(0, 0.05f, 0), new Vector3(1.5f, 0.1f, 1.5f), wood);
            PrimitiveFactory.Part(PrimitiveType.Cube, transform, new Vector3(-0.32f, 0.38f, -0.2f), new Vector3(0.55f, 0.55f, 0.55f), col);
            PrimitiveFactory.Part(PrimitiveType.Cube, transform, new Vector3(0.35f, 0.38f, 0.22f), new Vector3(0.55f, 0.55f, 0.55f), col);
            PrimitiveFactory.Part(PrimitiveType.Cube, transform, new Vector3(0.02f, 0.92f, 0f), new Vector3(0.5f, 0.5f, 0.5f), col);

            var go = new GameObject("Badge");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, 2.1f, 0);
            badge = go.AddComponent<TextMesh>();
            badge.fontSize = 40;
            badge.characterSize = 0.075f;
            badge.anchor = TextAnchor.MiddleCenter;
            badge.alignment = TextAlignment.Center;
            badge.color = Color.white;
            var font = HUDBuilder.UIFont;
            if (font != null)
            {
                badge.font = font;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = font.material;
            }
            go.AddComponent<Billboard>();

            // Visuals to showcase items like ShopShelf — distinct silhouette per
            // SKU via ItemMesh so every rack reads at a glance.
            itemVisuals = new GameObject[10]; // max 10 visuals
            for (int i = 0; i < itemVisuals.Length; i++)
            {
                float x = (i % 2 == 0) ? -0.25f : 0.25f;
                float y = 0.2f + (i / 2) * 0.45f;
                var vis = Engine.PrimitiveFactory.ItemMesh(Item, transform,
                    new Vector3(x, y + 0.15f, 0.35f), 1.0f); // piled in the crate
                vis.SetActive(false);
                itemVisuals[i] = vis;
            }
        }

        private GameObject[] itemVisuals;
        private int lastShown = -1;

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < 0.4f) return;
            timer = 0f;
            var inv = GameManager.Instance?.Inventory;
            if (inv == null || badge == null) return;
            int count = inv.CountOf(Item);
            int cap = inv.CapacityOf(Item);
            // Only show the badge when the rack is EMPTY (needs restock) or FULL
            // (ready to sell / withdraw). Idle-normal shows nothing — matches the
            // reference which only surfaces counts at actionable states.
            if (count == 0)      { badge.text = "0"; badge.gameObject.SetActive(true); }
            else if (count >= cap){ badge.text = "MAX";badge.gameObject.SetActive(true); }
            else                 { badge.gameObject.SetActive(false); }
            
            if (count != lastShown && itemVisuals != null)
            {
                lastShown = count;
                // Show proportional visuals: max 10 cubes representing the stock
                int visCount = Mathf.Min(count, itemVisuals.Length);
                for (int i = 0; i < itemVisuals.Length; i++)
                {
                    itemVisuals[i].SetActive(i < visCount);
                }
            }
        }

        private static string ShortName(ItemType t) => t switch
        {
            ItemType.TomatoKetchup => "Ketchup",
            ItemType.WheatFlour => "Flour",
            _ => t.ToString(),
        };
    }
}
