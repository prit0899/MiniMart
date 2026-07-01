using System;
using System.Collections.Generic;
using MiniMart.Core;
using MiniMart.Catalog;

namespace MiniMart.Runtime
{
    /// <summary>
    /// Mutable runtime storage for a single item type, bounded by StorageCatalog.MaxStorage.
    /// Anything pushed beyond capacity is routed to a dustbin rather than silently dropped.
    /// </summary>
    [Serializable]
    public class ItemStock
    {
        public ItemType Item;
        public int Count;
        public int MaxCapacity;

        public ItemStock(ItemType item)
        {
            Item = item;
            Count = 0;
            MaxCapacity = StorageCatalog.MaxStorage.TryGetValue(item, out var max) ? max : 10;
        }

        public int SpaceLeft => Math.Max(0, MaxCapacity - Count);

        /// <returns>The amount that actually fit (rest is overflow, to be sent to a dustbin).</returns>
        public int Add(int amount)
        {
            int fit = Math.Min(amount, SpaceLeft);
            Count += fit;
            return fit;
        }

        public bool Remove(int amount)
        {
            if (amount > Count) return false;
            Count -= amount;
            return true;
        }
    }

    /// <summary>One of the two dustbins. Overflow items are thrown away here by player or NPCs.</summary>
    [Serializable]
    public class Dustbin
    {
        public string Id;
        public int ItemsDiscarded;

        public Dustbin(string id) { Id = id; }

        public void Discard(int amount) => ItemsDiscarded += amount;
    }

    /// <summary>Aggregates all per-item stock plus the two dustbins, per Section 4 of the spec.</summary>
    public class StoreInventory
    {
        public readonly Dictionary<ItemType, ItemStock> Stocks = new Dictionary<ItemType, ItemStock>();
        public readonly List<Dustbin> Dustbins = new List<Dustbin>();

        public StoreInventory()
        {
            foreach (ItemType item in Enum.GetValues(typeof(ItemType)))
                Stocks[item] = new ItemStock(item);

            for (int i = 0; i < StorageCatalog.DustbinCount; i++)
                Dustbins.Add(new Dustbin($"Dustbin_{i + 1}"));
        }

        /// <summary>
        /// Deposits items; whatever doesn't fit is thrown into the nearest/least-full dustbin.
        /// This is the single overflow entry point every producer (chef, farmer, machine) should call.
        /// </summary>
        public void Deposit(ItemType item, int amount)
        {
            var stock = Stocks[item];
            int fit = stock.Add(amount);
            int overflow = amount - fit;
            if (overflow > 0) DiscardOverflow(overflow);
        }

        public bool Withdraw(ItemType item, int amount) => Stocks[item].Remove(amount);

        private void DiscardOverflow(int amount)
        {
            // Send to whichever bin currently has the least discarded (cheap load-balance).
            Dustbin target = Dustbins[0];
            foreach (var bin in Dustbins)
                if (bin.ItemsDiscarded < target.ItemsDiscarded) target = bin;
            target.Discard(amount);
        }

        public int CountOf(ItemType item) => Stocks[item].Count;
        public int CapacityOf(ItemType item) => Stocks[item].MaxCapacity;
    }
}
