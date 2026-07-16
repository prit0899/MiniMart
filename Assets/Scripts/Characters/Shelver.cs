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

        /// <summary>Wired by the bootstrapper (Mart 1). Owner spec: the shelver also
        /// "feeds tomato to hen" — it's a transfer job like any other.</summary>
        public Production.HenCoop Hen;
        private bool feedingHen;      // second leg of the hen trip (rack -> coop)
        private int henCarry;         // tomatoes in hand for the hen

        /// <summary>Simple greedy loop: pick the emptiest assigned shelf, fetch from storage, restock it.</summary>
        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (inventory == null && GameManager.Instance != null)
                inventory = GameManager.Instance.Inventory;

            if (hasTarget || AssignedShelves == null || AssignedShelves.Length == 0 || inventory == null) return;

            // Pick the emptiest shelf WE CAN ACTUALLY REFILL RIGHT NOW.
            //
            // The old code chose the emptiest shelf outright and then bailed out if that
            // one item happened to have no stock in storage — so the shelver stood idle
            // while other shelves it was responsible for sat empty with plenty of stock
            // waiting on the rack. Same greedy trap the buyers had.
            ShopShelf needsRestock = null;
            int worstDeficit = 0;
            foreach (var shelf in AssignedShelves)
            {
                if (shelf == null || !shelf.gameObject.activeInHierarchy) continue; // not purchased yet
                if (!IsResponsibleFor(shelf.Item)) continue;

                int deficit = shelf.Capacity - shelf.Count;
                if (deficit <= 0) continue;                        // already full
                if (inventory.CountOf(shelf.Item) <= 0) continue;  // nothing to refill it with

                if (deficit > worstDeficit)
                {
                    worstDeficit = deficit;
                    needsRestock = shelf;
                }
            }

            if (needsRestock != null)
            {
                // Two-leg trip: walk to the item's storage rack first, pick up there,
                // THEN carry to the shelf. (Previously the shelver withdrew from thin
                // air wherever it stood — invisible, and looked like it did nothing.)
                pendingShelf = needsRestock;
                fetching = true;
                SetTarget(Engine.StorageRack.PositionOf(needsRestock.Item, needsRestock.transform.position));
                return;
            }

            // Secondary task (owner spec): with no shelf to restock, keep the hen fed —
            // fetch tomatoes from storage and carry them to the coop.
            if (Hen != null && Hen.gameObject.activeInHierarchy && Hen.TomatoRoom > 0
                && inventory.CountOf(ItemType.Tomato) > 0 && henCarry == 0)
            {
                feedingHen = false;   // first leg: go to the tomato rack
                fetching = false;
                pendingShelf = null;
                henTrip = true;
                SetTarget(Engine.StorageRack.PositionOf(ItemType.Tomato, Hen.transform.position));
            }
        }

        private bool henTrip;

        private bool fetching;

        protected override void OnArrived()
        {
            base.OnArrived();

            if (henTrip)
            {
                if (!feedingHen)
                {
                    // At the tomato rack: pick up what the hen can take.
                    int want = Mathf.Min(CarryCapacity - CarryCount,
                                         Hen != null ? Hen.TomatoRoom : 0,
                                         inventory != null ? inventory.CountOf(ItemType.Tomato) : 0);
                    if (want > 0 && inventory.Withdraw(ItemType.Tomato, want))
                    {
                        CarryColor = Engine.PrimitiveFactory.ItemColor(ItemType.Tomato);
                        TryPickUp(want);
                        henCarry = want;
                        feedingHen = true;
                        SetTarget(Hen.transform.position);
                    }
                    else
                    {
                        henTrip = false;
                    }
                }
                else
                {
                    // At the coop: feed everything we brought.
                    if (Hen != null && henCarry > 0)
                    {
                        int fed = Hen.LoadTomato(henCarry);
                        CarryCount = Mathf.Max(0, CarryCount - henCarry);
                        // Anything the hen couldn't take goes back to storage.
                        if (henCarry - fed > 0) inventory?.Deposit(ItemType.Tomato, henCarry - fed);
                    }
                    henCarry = 0;
                    feedingHen = false;
                    henTrip = false;
                }
                return;
            }

            if (fetching && pendingShelf != null)
            {
                fetching = false;
                int amount = Mathf.Min(pendingShelf.Capacity - pendingShelf.Count,
                                       CarryCapacity - CarryCount,
                                       inventory != null ? inventory.CountOf(pendingShelf.Item) : 0);
                if (amount > 0 && inventory.Withdraw(pendingShelf.Item, amount))
                {
                    CarryColor = Engine.PrimitiveFactory.ItemColor(pendingShelf.Item);
                    TryPickUp(amount);
                    pendingAmount = amount;
                    SetTarget(pendingShelf.transform.position);
                }
                else
                {
                    pendingShelf = null;
                    pendingAmount = 0;
                }
                return;
            }

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

        /// <summary>Real item meshes in the carry stack, same as the player.</summary>
        public override List<ItemType> GetCarriedItems()
        {
            var list = new List<ItemType>();
            if (henCarry > 0)
                for (int i = 0; i < henCarry; i++) list.Add(ItemType.Tomato);
            else if (pendingShelf != null)
                for (int i = 0; i < CarryCount; i++) list.Add(pendingShelf.Item);
            return list;
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
        private GameObject restockArrow;

        private GameObject[] itemVisuals;

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

            // Reference-style "restock this shelf" indicator: a small downward-pointing
            // yellow arrow that floats above the shelf when it needs stocking. Hidden
            // until Update() decides Count is low.
            restockArrow = new GameObject("RestockArrow");
            restockArrow.transform.SetParent(transform, false);
            restockArrow.transform.localPosition = new Vector3(0f, 2.85f, 0f);
            var arrowYellow = new Color(1.0f, 0.86f, 0.20f);
            // Shaft (thin vertical bar) + head (downward cone). Cone in Unity is the
            // top half of a cylinder scaled to a point — approximate with a small
            // pyramid built from a rotated tetrahedron-ish cube stack.
            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.transform.SetParent(restockArrow.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            shaft.transform.localScale = new Vector3(0.18f, 0.52f, 0.18f);
            Destroy(shaft.GetComponent<Collider>());
            shaft.GetComponent<MeshRenderer>().material =
                Engine.PrimitiveFactory.NewColoredMaterial(arrowYellow);
            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.transform.SetParent(restockArrow.transform, false);
            head.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            head.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);
            head.transform.localScale = new Vector3(0.30f, 0.30f, 0.30f);
            Destroy(head.GetComponent<Collider>());
            head.GetComponent<MeshRenderer>().material =
                Engine.PrimitiveFactory.NewColoredMaterial(arrowYellow);
            restockArrow.SetActive(false);

            // Visuals — one distinct silhouette per SKU (egg ellipsoid, tomato w/
            // stem, ketchup bottle, bread loaf, wheat sheaf...) instead of the
            // identical tinted cubes that made every shelf look the same.
            itemVisuals = new GameObject[Capacity];
            for (int i = 0; i < Capacity; i++)
            {
                float x = (i % 2 == 0) ? -0.25f : 0.25f;
                float y = 0.2f + (i / 2) * 0.45f;
                var vis = Engine.PrimitiveFactory.ItemMesh(Item, transform, new Vector3(x, y, 0), 1.0f);
                vis.SetActive(false);
                itemVisuals[i] = vis;
            }
        }

        private void Update()
        {
            if (badge != null && Count != lastShown)
            {
                lastShown = Count;
                // Reference: shelves only show a badge when empty or full.
                // Idle-partial stays quiet so the store reads as calm & clean.
                // Reference shows persistent "n/m" fractional badges on shelves —
                // MAX only replaces the number when Count == Capacity.
                if (Count >= Capacity) badge.text = "MAX";
                else                   badge.text = Count + "/" + Capacity;
                badge.gameObject.SetActive(true);

                for (int i = 0; i < itemVisuals.Length; i++)
                    itemVisuals[i].SetActive(i < Count);
            }
            // Reference: yellow "please restock" arrow when shelf is under a third full.
            // Kept outside the change-guard so Start()'s late arrow creation still gets
            // its initial visibility on the next tick.
            if (restockArrow != null)
            {
                bool wantsArrow = Count <= Mathf.Max(1, Capacity / 3);
                if (restockArrow.activeSelf != wantsArrow) restockArrow.SetActive(wantsArrow);
            }
            // Gentle bob so the arrow reads as an active indicator, not a decal.
            if (restockArrow != null && restockArrow.activeSelf)
            {
                float y = 2.85f + Mathf.Sin(Time.time * 3.5f) * 0.08f;
                var p = restockArrow.transform.localPosition;
                restockArrow.transform.localPosition = new Vector3(p.x, y, p.z);
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
