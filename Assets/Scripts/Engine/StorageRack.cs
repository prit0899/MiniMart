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
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < 0.4f) return;
            timer = 0f;
            var inv = GameManager.Instance?.Inventory;
            if (inv == null || badge == null) return;
            badge.text = $"{ShortName(Item)}\n{inv.CountOf(Item)}/{inv.CapacityOf(Item)}";
        }

        private static string ShortName(ItemType t) => t switch
        {
            ItemType.TomatoKetchup => "Ketchup",
            ItemType.WheatFlour => "Flour",
            _ => t.ToString(),
        };
    }
}
