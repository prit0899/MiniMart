using System.Collections.Generic;
using UnityEngine;
using MiniMart.Core;
using MiniMart.Catalog;
using MiniMart.Characters;

namespace MiniMart.AI
{
    /// <summary>
    /// One store visitor. Generates a random basket of unlocked items, chooses hand-carry or
    /// trolley based on item count, shops the shelves, then queues at a cash counter.
    /// </summary>
    public class Buyer : CharacterBase
    {
        public Dictionary<ItemType, int> Basket = new Dictionary<ItemType, int>();
        public BagType BagType;
        public bool HasCheckedOut;

        // Runtime references injected by BuyerSpawner
        private int playerLevel;
        private List<ShopShelf> allShelves;
        private List<Economy.CashCounter> counters;

        private ShopShelf currentTarget;
        private bool headingToCounter;

        public void Init(int currentPlayerLevel, List<ShopShelf> shelves, List<Economy.CashCounter> cashCounters)
        {
            Role = RoleType.Buyer;
            playerLevel = currentPlayerLevel;
            allShelves = shelves;
            counters = cashCounters;
            Curve = null; // buyers don't level up
            CarryCapacity = 20; // generous; BagType is visual only
            CarryCount = 0;

            GenerateBasket();
            AssignBagType();
        }

        private void GenerateBasket()
        {
            Basket.Clear();
            // Randomly request 1-7 items, only from unlocked item types.
            var available = new List<ItemType>();
            foreach (ItemType item in System.Enum.GetValues(typeof(ItemType)))
                if (PriceCatalog.IsUnlocked(item, playerLevel)) available.Add(item);

            if (available.Count == 0) return;

            int itemCount = Random.Range(1, 8);
            for (int i = 0; i < itemCount; i++)
            {
                var item = available[Random.Range(0, available.Count)];
                if (!Basket.ContainsKey(item)) Basket[item] = 0;
                Basket[item] += 1;
            }
        }

        private void AssignBagType()
        {
            int totalItems = 0;
            foreach (var v in Basket.Values) totalItems += v;
            BagType = totalItems < 5 ? BagType.HandCarry : BagType.Trolley;
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (HasCheckedOut || hasTarget) return;

            if (!headingToCounter)
            {
                // Find next shelf item we still need.
                ItemType needed = FindNextNeededItem();
                if (Basket.ContainsKey(needed)) // re-check validity
                {
                    ShopShelf shelf = FindShelfFor(needed);
                    if (shelf != null && shelf.Count > 0)
                    {
                        currentTarget = shelf;
                        SetTarget(shelf.transform.position);
                        return;
                    }
                }

                // Nothing left to pick — head to nearest open counter.
                var counter = FindOpenCounter();
                if (counter != null)
                {
                    headingToCounter = true;
                    SetTarget(counter.transform.position);
                }
            }
        }

        protected override void OnArrived()
        {
            if (headingToCounter)
            {
                var counter = FindOpenCounter();
                counter?.Enqueue(this);
                return;
            }

            if (currentTarget != null && Basket.TryGetValue(currentTarget.Item, out int want))
            {
                int take = Mathf.Min(want, currentTarget.Count);
                if (currentTarget.TakeStock(take))
                {
                    Basket[currentTarget.Item] -= take;
                    if (Basket[currentTarget.Item] <= 0) Basket.Remove(currentTarget.Item);
                }
                currentTarget = null;
            }
        }

        private ItemType FindNextNeededItem()
        {
            foreach (var kv in Basket)
                if (kv.Value > 0) return kv.Key;
            return ItemType.Egg; // fallback (won't match any shelf if basket is empty)
        }

        private ShopShelf FindShelfFor(ItemType item)
        {
            ShopShelf best = null;
            foreach (var s in allShelves)
                if (s.Item == item && s.Count > 0 && (best == null || s.Count > best.Count)) best = s;
            return best;
        }

        private Economy.CashCounter FindOpenCounter()
        {
            Economy.CashCounter best = null;
            foreach (var c in counters)
                if (c.IsOpen && (best == null || c.Line.Count < best.Line.Count)) best = c;
            return best;
        }

        public void OnCheckedOut() => HasCheckedOut = true;
    }
}
