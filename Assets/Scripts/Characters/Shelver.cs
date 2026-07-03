using System.Collections.Generic;
using UnityEngine;
using MiniMart.Core;
using MiniMart.Catalog;
using MiniMart.Runtime;

namespace MiniMart.Characters
{
    /// <summary>
    /// Generic shelver used for both Shelver 1 (egg, ketchup, tomato) and Shelver 2
    /// (wheat, wheat flour, bread). Responsibilities are data-driven via RoleCatalog.
    /// </summary>
    public class Shelver : CharacterBase
    {
        public RoleType ShelverRole; // RoleType.Shelver1 or RoleType.Shelver2
        public ShopShelf[] AssignedShelves;
        private StoreInventory inventory;

        public void Configure(RoleType role, StoreInventory storeInventory)
        {
            ShelverRole = role;
            Role = role;
            inventory = storeInventory;
            Curve = role == RoleType.Shelver1 ? RoleCatalog.Shelver1Curve() : RoleCatalog.Shelver2Curve();
            ApplyLevel(1);
        }

        public IEnumerable<ItemType> Responsibilities =>
            RoleCatalog.RoleResponsibilities.TryGetValue(ShelverRole, out var items) ? items : new ItemType[0];

        private ShopShelf pendingShelf;
        private int pendingAmount;

        /// <summary>Simple greedy loop: pick the emptiest assigned shelf, fetch from storage, restock it.</summary>
        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (inventory == null && GameManager.Instance != null)
                inventory = GameManager.Instance.Inventory;

            if (hasTarget || AssignedShelves == null || AssignedShelves.Length == 0 || inventory == null) return;

            ShopShelf needsRestock = null;
            int worstDeficit = -1;
            foreach (var shelf in AssignedShelves)
            {
                if (!IsResponsibleFor(shelf.Item)) continue;
                int deficit = shelf.Capacity - shelf.Count;
                if (deficit > worstDeficit)
                {
                    worstDeficit = deficit;
                    needsRestock = shelf;
                }
            }

            if (needsRestock != null && worstDeficit > 0 && inventory.CountOf(needsRestock.Item) > 0)
            {
                int amount = Mathf.Min(worstDeficit, CarryCapacity - CarryCount, inventory.CountOf(needsRestock.Item));
                if (amount > 0)
                {
                    inventory.Withdraw(needsRestock.Item, amount);
                    TryPickUp(amount);
                    SetTarget(needsRestock.transform.position);
                    pendingShelf = needsRestock;
                    pendingAmount = amount;
                }
            }
        }

        protected override void OnArrived()
        {
            base.OnArrived();
            if (pendingShelf != null)
            {
                pendingShelf.AddStock(pendingAmount);
                CarryCount -= pendingAmount;
                pendingShelf = null;
                pendingAmount = 0;
            }
        }

        private bool IsResponsibleFor(ItemType item)
        {
            foreach (var i in Responsibilities)
                if (i == item) return true;
            return false;
        }
    }

    /// <summary>A physical shelf slot in the shop floor that buyers pull stock from.
    /// Shows a floating "n/cap" badge like the reference game so stock changes are visible.</summary>
    public class ShopShelf : MonoBehaviour
    {
        public ItemType Item;
        public int Capacity = 10;
        public int Count;

        private TextMesh badge;
        private int lastShown = -1;

        private void Start()
        {
            var go = new GameObject("StockBadge");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, 2.4f, 0);
            badge = go.AddComponent<TextMesh>();
            badge.fontSize = 42;
            badge.characterSize = 0.08f;
            badge.anchor = TextAnchor.MiddleCenter;
            badge.alignment = TextAlignment.Center;
            badge.color = Color.white;
            var font = Engine.HUDBuilder.UIFont;
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
            if (badge != null && Count != lastShown)
            {
                lastShown = Count;
                badge.text = $"{Count}/{Capacity}";
            }
        }

        public void AddStock(int amount) => Count = Mathf.Min(Capacity, Count + amount);
        public bool TakeStock(int amount)
        {
            if (amount > Count) return false;
            Count -= amount;
            return true;
        }
    }
}
