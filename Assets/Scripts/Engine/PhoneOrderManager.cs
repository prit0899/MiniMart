using System.Collections.Generic;
using UnityEngine;
using MiniMart.Core;
using MiniMart.Catalog;
using MiniMart.Runtime;
using MiniMart.Economy;
using MiniMart.Characters;

namespace MiniMart.Engine
{
    /// <summary>
    /// Represents one incoming phone-call order. The player has up to PhoneOrderWindowMinutes
    /// to fulfil it before it expires.
    /// </summary>
    public class PhoneOrder
    {
        public string Id;
        public Dictionary<ItemType, int> Items = new Dictionary<ItemType, int>();
        public float Value;
        public float TimeRemaining;
        public bool IsExpired => TimeRemaining <= 0f;
        public bool IsFulfilled;
        public bool IsDismissed;

        public float TotalFulfillmentValue(EconomyManager economy)
        {
            float total = 0f;
            foreach (var kv in Items)
                total += economy.GetUnitPrice(kv.Key) * kv.Value;
            return PriceCatalog.ApplyBundleFloor(total);
        }
    }

    /// <summary>
    /// Fires phone-call orders on a 4-5 minute interval. Each call requests a random bundle
    /// worth $45-$300. The player fills the order from store inventory; window is 10 minutes.
    /// </summary>
    public class PhoneOrderManager : MonoBehaviour
    {
        [System.NonSerialized] public EconomyManager Economy;
        [System.NonSerialized] public StoreInventory Inventory;

        [System.NonSerialized] public List<PhoneOrder> ActiveOrders = new List<PhoneOrder>();

        [System.NonSerialized] public Transform SpawnSpot;
        [System.NonSerialized] public Transform PickupSpot;
        /// <summary>All shop shelves — orders may only request items whose shelf is
        /// actually purchased/active (same rule buyers follow). Playtest: at L1 with
        /// only the tomato stand, the first order demanded eggs — unfulfillable.</summary>
        [System.NonSerialized] public List<ShopShelf> AllShelves;

        private float spawnTimer;
        private float nextSpawnTime;
        private int playerLevel = 1;
        private int orderCounter;

        private void Awake() => ScheduleNext();

        public void SetPlayerLevel(int level) => playerLevel = level;

        private void ScheduleNext()
        {
            if (orderCounter == 0)
            {
                nextSpawnTime = 180f; // Give the player time to set up before the first order
            }
            else
            {
                nextSpawnTime = Random.Range(
                    PriceCatalog.PhoneOrderMinIntervalMin * 60f,
                    PriceCatalog.PhoneOrderMaxIntervalMin * 60f);
            }
            spawnTimer = 0f;
        }

        private void Update()
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= nextSpawnTime)
            {
                SpawnOrder();
                ScheduleNext();
            }

            // Tick down active order windows and purge expired ones, process fulfilled ones.
            for (int i = ActiveOrders.Count - 1; i >= 0; i--)
            {
                var order = ActiveOrders[i];
                if (!order.IsFulfilled && order.Items.Count == 0)
                {
                    // Physically fulfilled by loading the van!
                    float revenue = order.Value;
                    Economy?.Deposit(revenue);
                    GameManager.Instance?.AddStoreXp(25);
                    order.IsFulfilled = true;
                    Debug.Log($"Phone order {order.Id} physically fulfilled — earned ${revenue:F2}");
                    ActiveOrders.RemoveAt(i);
                    continue;
                }
                
                order.TimeRemaining -= Time.deltaTime;
                if (order.IsExpired)
                {
                    Debug.Log($"Phone order {order.Id} expired.");
                    ActiveOrders.RemoveAt(i);
                }
            }
        }

        private void SpawnOrder()
        {
            // GDD 9.1: at most 2 active orders.
            if (ActiveOrders.Count >= 2) return;

            var order = new PhoneOrder
            {
                Id = $"Order_{++orderCounter}",
                TimeRemaining = PriceCatalog.PhoneOrderWindowMinutes * 60f
            };

            // Only offer items that are BOTH level-unlocked AND have an active shelf
            // in the store. This prevents orders for Eggs when the hen coop hasn't been
            // purchased yet (the shelf is gated behind the purchase pad).
            var available = new List<ItemType>();
            foreach (ItemType item in System.Enum.GetValues(typeof(ItemType)))
            {
                if (!PriceCatalog.IsUnlocked(item, playerLevel)) continue;
                if (!HasActiveShelf(item)) continue;
                available.Add(item);
            }

            if (available.Count == 0) return;

            // GDD 9.1: quantities must be FULFILLABLE — capped by each item's storage capacity.
            // The $45-$300 payout is a wholesale premium on top of the tiny retail prices,
            // not a sum of them (a 15-egg storage could never add up to $45 at $0.05/egg).
            // Keep orders FULFILLABLE at a realistic production rate. Quantities
            // used to run all the way up to each item's storage cap (~20), so late
            // orders demanded "14x BottledMilk" or "15 Herb + 7 Corn + 3 Milk" —
            // impossible to fill inside the window (QA finding). Cap each line to a
            // small amount and the whole order to a modest item budget.
            const int MaxPerLine = 8;
            const int MaxTotalItems = 12;
            int typeCount = Mathf.Min(available.Count, Random.Range(1, 4)); // 1-3 item types
            float retailTotal = 0f;
            int totalItems = 0;
            for (int t = 0; t < typeCount; t++)
            {
                var item = available[Random.Range(0, available.Count)];
                if (order.Items.ContainsKey(item)) continue;

                int cap = 10;
                if (Inventory != null && Inventory.Stocks.TryGetValue(item, out var stock))
                    cap = stock.MaxCapacity;

                int budget = MaxTotalItems - totalItems;
                if (budget < 2) break;                       // order is already full enough
                int lineMax = Mathf.Min(MaxPerLine, cap, budget);
                int qty = Random.Range(2, lineMax + 1);      // 2..min(8, cap, remaining budget)
                order.Items[item] = qty;
                totalItems += qty;
                float unit = Economy != null ? Economy.GetUnitPrice(item) : PriceCatalog.BasePrice[item];
                retailTotal += unit * qty;
            }

            // Premium payout scaled by order size AND store level. The GDD range
            // ($45-300) only fully opens up by L5 — the first order used to fire
            // at t=15s worth up to $300 against $10 starting cash, a jackpot
            // that trivialized the whole early game (batch-34 playthrough).
            float levelMin = Mathf.Min(PriceCatalog.PhoneOrderMin, 15f * playerLevel);          // L1:$15, L2:$30, L3+:$45
            float levelMax = Mathf.Min(PriceCatalog.PhoneOrderMax, 60f * playerLevel);          // L1:$60 ... L5+:$300
            order.Value = Mathf.Clamp(levelMin + retailTotal * 60f, levelMin, levelMax);
            ActiveOrders.Add(order);
            
            if (SpawnSpot != null && PickupSpot != null)
            {
                var vanGO = new GameObject($"DeliveryVan_{order.Id}");
                var vanComp = vanGO.AddComponent<Characters.DeliveryVan>();
                PrimitiveFactory.VanVisual(vanGO);
                vanComp.Init(order, PickupSpot, SpawnSpot);
            }
            
            Debug.Log($"Phone order {order.Id} arrived — value ${order.Value:F2}");
            OnNewOrder?.Invoke(order);
        }

        /// <summary>Player declined the call — remove it so it stops counting against the
        /// 2-active-orders limit and never pays out.</summary>
        public void Dismiss(PhoneOrder order)
        {
            if (order == null) return;
            order.IsDismissed = true;
            ActiveOrders.Remove(order);
            Debug.Log($"Phone order {order.Id} dismissed.");
        }

        /// <summary>Van finished loading all requested items — pay out and close the order.
        /// Without this, loading the van drained Order.Items but never paid, and the
        /// emptied order became vacuously "fulfillable" via the HUD for free money.</summary>
        public void CompleteByVan(PhoneOrder order)
        {
            if (order == null || order.IsExpired || order.IsFulfilled) return;
            order.IsFulfilled = true;
            Economy?.Deposit(order.Value);
            GameManager.Instance?.AddStoreXp(25);
            ActiveOrders.Remove(order);
            Debug.Log($"Phone order {order.Id} loaded onto the van — earned ${order.Value:F2}");
        }

        /// <summary>True when current store inventory can cover every line of the order.</summary>
        public bool CanFulfil(PhoneOrder order)
        {
            if (order == null || order.IsExpired || order.IsFulfilled || Inventory == null) return false;
            if (order.Items.Count == 0) return false; // drained by the van — nothing to fulfil
            foreach (var kv in order.Items)
                if (Inventory.CountOf(kv.Key) < kv.Value) return false;
            return true;
        }

        /// <summary>Player / UI calls this to attempt fulfilment from current store inventory.</summary>
        public bool TryFulfil(PhoneOrder order)
        {
            if (order == null || order.IsExpired || order.IsFulfilled) return false;
            if (order.Items.Count == 0) return false; // drained by the van — nothing to fulfil
            // Check we have enough stock for every line item.
            foreach (var kv in order.Items)
                if (Inventory.CountOf(kv.Key) < kv.Value) return false;
            // Deduct.
            foreach (var kv in order.Items)
                Inventory.Withdraw(kv.Key, kv.Value);

            // Pay the premium value the player was shown — TotalFulfillmentValue is the retail
            // sum (cents), which would silently pay ~$1 for a "$75" order.
            float revenue = order.Value;
            Economy?.Deposit(revenue);
            GameManager.Instance?.AddStoreXp(25); // phone orders are the big XP earner
            order.IsFulfilled = true;
            ActiveOrders.Remove(order);
            Debug.Log($"Phone order {order.Id} fulfilled — earned ${revenue:F2}");
            return true;
        }

        public System.Action<PhoneOrder> OnNewOrder;

        /// <summary>True if the item has a visible, purchased shelf in the store.</summary>
        private bool HasActiveShelf(ItemType item)
        {
            if (AllShelves == null) return false;
            foreach (var s in AllShelves)
                if (s != null && s.gameObject.activeInHierarchy && s.Item == item) return true;
            return false;
        }
    }
}
