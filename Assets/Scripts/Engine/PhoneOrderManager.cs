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
        public EconomyManager Economy;
        public StoreInventory Inventory;

        public List<PhoneOrder> ActiveOrders = new List<PhoneOrder>();

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
            var order = new PhoneOrder
            {
                Id = $"Order_{++orderCounter}",
                TimeRemaining = PriceCatalog.PhoneOrderWindowMinutes * 60f
            };

            // Build a random bundle whose total falls within $45-$300.
            var available = new List<ItemType>();
            foreach (ItemType item in System.Enum.GetValues(typeof(ItemType)))
                if (PriceCatalog.IsUnlocked(item, playerLevel)) available.Add(item);

            if (available.Count == 0) return;

            float target = Random.Range(PriceCatalog.PhoneOrderMin, PriceCatalog.PhoneOrderMax);
            float accumulated = 0f;
            int safety = 200;
            while (accumulated < target && safety-- > 0)
            {
                var item = available[Random.Range(0, available.Count)];
                if (!order.Items.ContainsKey(item)) order.Items[item] = 0;
                order.Items[item]++;
                accumulated += Economy != null ? Economy.GetUnitPrice(item) : PriceCatalog.BasePrice[item];
            }

            order.Value = accumulated;
            ActiveOrders.Add(order);
            Debug.Log($"Phone order {order.Id} arrived — value ${order.Value:F2}");
            OnNewOrder?.Invoke(order);
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

            float revenue = order.TotalFulfillmentValue(Economy);
            Economy?.Deposit(revenue);
            order.IsFulfilled = true;
            ActiveOrders.Remove(order);
            Debug.Log($"Phone order {order.Id} fulfilled — earned ${revenue:F2}");
            return true;
        }

        public System.Action<PhoneOrder> OnNewOrder;
    }
}
