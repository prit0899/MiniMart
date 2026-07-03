using System.Collections.Generic;
using UnityEngine;
using MiniMart.Core;
using MiniMart.Catalog;
using MiniMart.Runtime;
using MiniMart.Economy;

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

        private float spawnTimer;
        private float nextSpawnTime;
        private int playerLevel = 1;
        private int orderCounter;

        private void Awake() => ScheduleNext();

        public void SetPlayerLevel(int level) => playerLevel = level;

        private void ScheduleNext()
        {
            nextSpawnTime = Random.Range(
                PriceCatalog.PhoneOrderMinIntervalMin * 60f,
                PriceCatalog.PhoneOrderMaxIntervalMin * 60f);
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

            // Tick down active order windows and purge expired ones.
            for (int i = ActiveOrders.Count - 1; i >= 0; i--)
            {
                ActiveOrders[i].TimeRemaining -= Time.deltaTime;
                if (ActiveOrders[i].IsExpired)
                {
                    Debug.Log($"Phone order {ActiveOrders[i].Id} expired.");
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

            var available = new List<ItemType>();
            foreach (ItemType item in System.Enum.GetValues(typeof(ItemType)))
                if (PriceCatalog.IsUnlocked(item, playerLevel)) available.Add(item);

            if (available.Count == 0) return;

            // GDD 9.1: quantities must be FULFILLABLE — capped by each item's storage capacity.
            // The $45-$300 payout is a wholesale premium on top of the tiny retail prices,
            // not a sum of them (a 15-egg storage could never add up to $45 at $0.05/egg).
            int typeCount = Mathf.Min(available.Count, Random.Range(1, 4)); // 1-3 item types
            float retailTotal = 0f;
            for (int t = 0; t < typeCount; t++)
            {
                var item = available[Random.Range(0, available.Count)];
                if (order.Items.ContainsKey(item)) continue;

                int cap = 10;
                if (Inventory != null && Inventory.Stocks.TryGetValue(item, out var stock))
                    cap = stock.MaxCapacity;

                int qty = Random.Range(3, Mathf.Max(4, cap + 1)); // 3..cap, always <= storage cap
                order.Items[item] = qty;
                float unit = Economy != null ? Economy.GetUnitPrice(item) : PriceCatalog.BasePrice[item];
                retailTotal += unit * qty;
            }

            // Premium payout scaled by order size, clamped to the GDD range.
            order.Value = Mathf.Clamp(45f + retailTotal * 60f, 45f, 300f);
            ActiveOrders.Add(order);
            Debug.Log($"Phone order {order.Id} arrived — value ${order.Value:F2}");
            OnNewOrder?.Invoke(order);
        }

        /// <summary>Player declined the call — remove it so it stops counting against the
        /// 2-active-orders limit and never pays out.</summary>
        public void Dismiss(PhoneOrder order)
        {
            if (order == null) return;
            ActiveOrders.Remove(order);
            Debug.Log($"Phone order {order.Id} dismissed.");
        }

        /// <summary>True when current store inventory can cover every line of the order.</summary>
        public bool CanFulfil(PhoneOrder order)
        {
            if (order == null || order.IsExpired || order.IsFulfilled || Inventory == null) return false;
            foreach (var kv in order.Items)
                if (Inventory.CountOf(kv.Key) < kv.Value) return false;
            return true;
        }

        /// <summary>Player / UI calls this to attempt fulfilment from current store inventory.</summary>
        public bool TryFulfil(PhoneOrder order)
        {
            if (order == null || order.IsExpired || order.IsFulfilled) return false;
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
            order.IsFulfilled = true;
            ActiveOrders.Remove(order);
            Debug.Log($"Phone order {order.Id} fulfilled — earned ${revenue:F2}");
            return true;
        }

        public System.Action<PhoneOrder> OnNewOrder;
    }
}
