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
        [System.NonSerialized] public Dictionary<ItemType, int> Basket = new Dictionary<ItemType, int>();
        /// <summary>What the buyer physically took off shelves — this is what they pay for.
        /// (Basket is the remaining wish-list and empties as they shop.)</summary>
        [System.NonSerialized] public Dictionary<ItemType, int> Collected = new Dictionary<ItemType, int>();
        public BagType BagType;
        public bool HasCheckedOut;

        // Runtime references injected by BuyerSpawner
        private int playerLevel;
        private List<ShopShelf> allShelves;
        private List<Economy.CashCounter> counters;

        private ShopShelf currentTarget;
        private bool headingToCounter;

        /// <summary>Where to walk after checkout before despawning. Set by BuyerSpawner.</summary>
        [System.NonSerialized] public Transform ExitDoor;
        private bool leaving;

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

            // GDD 8.2: 5+ items means the buyer pushes a trolley (visual).
            if (BagType == BagType.Trolley)
                Engine.PrimitiveFactory.Trolley(gameObject);
        }

        private void GenerateBasket()
        {
            Basket.Clear();
            // Randomly request 1-7 items, only from unlocked item types.
            var available = new List<ItemType>();
            foreach (ItemType item in System.Enum.GetValues(typeof(ItemType)))
                if (PriceCatalog.IsUnlocked(item, playerLevel)) available.Add(item);

            if (available.Count == 0) return;

            var eco = GameManager.Instance?.Economy;
            int itemCount = Random.Range(1, 8);
            for (int i = 0; i < itemCount; i++)
            {
                var item = available[Random.Range(0, available.Count)];
                // GDD 8.3 price elasticity: items marked up over +20% get skipped 35% of the time.
                if (eco != null && eco.IsOverpriced(item) && Random.value < 0.35f) continue;
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
            if (leaving)
            {
                Destroy(gameObject);
                return;
            }

            if (headingToCounter)
            {
                var counter = FindOpenCounter();
                counter?.Enqueue(this);
                return;
            }

            if (currentTarget != null && Basket.TryGetValue(currentTarget.Item, out int want))
            {
                int take = Mathf.Min(want, currentTarget.Count);
                if (take > 0 && currentTarget.TakeStock(take))
                {
                    Basket[currentTarget.Item] -= take;
                    if (Basket[currentTarget.Item] <= 0) Basket.Remove(currentTarget.Item);

                    if (!Collected.ContainsKey(currentTarget.Item)) Collected[currentTarget.Item] = 0;
                    Collected[currentTarget.Item] += take;
                    TryPickUp(take); // drives the carry-stack visual
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

        public void OnCheckedOut()
        {
            // Checked-out buyers used to freeze at the counter forever, clogging the store
            // until the concurrency cap silently stopped all future spawns. Walk out instead.
            HasCheckedOut = true;
            leaving = true;
            if (ExitDoor != null) SetTarget(ExitDoor.position);
            else Destroy(gameObject, 1.5f);
        }
    }
}
