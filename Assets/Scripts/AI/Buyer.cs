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
        /// <summary>Light personality system (from the Farm-Market spec): shapes walk
        /// speed, basket size, and how long they'll tolerate a checkout queue.</summary>
        public enum Personality { Normal, Impatient, Rich, Bargain }
        public Personality Kind;

        [System.NonSerialized] public Dictionary<ItemType, int> Basket = new Dictionary<ItemType, int>();
        /// <summary>What the buyer physically took off shelves — this is what they pay for.
        /// (Basket is the remaining wish-list and empties as they shop.)</summary>
        [System.NonSerialized] public Dictionary<ItemType, int> Collected = new Dictionary<ItemType, int>();

        /// <summary>Snapshot of the wish-list at spawn, for thought-bubble "have/want" display.</summary>
        [System.NonSerialized] public Dictionary<ItemType, int> OriginalWant = new Dictionary<ItemType, int>();

        /// <summary>True while standing in a checkout line (thought bubble shows "$").</summary>
        public bool InQueue => queuedCounter != null;
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

        private float queuePatienceSeconds;
        private float queueWait;
        private bool shownSoldOut;
        private Economy.CashCounter queuedCounter;
        private ShopShelf pickingShelf;
        private float pickBeat;
        private bool waitingForCounter; // arrived at the tills but none is open yet
        private float shopWait;         // time spent waiting for an out-of-stock item to restock
        private float shopPatienceSeconds;

        /// <summary>Real item meshes in the carry stack, same as the player.</summary>
        public override List<ItemType> GetCarriedItems()
        {
            var list = new List<ItemType>();
            foreach (var kv in Collected)
                for (int i = 0; i < kv.Value; i++) list.Add(kv.Key);
            return list;
        }

        public void Init(int currentPlayerLevel, List<ShopShelf> shelves, List<Economy.CashCounter> cashCounters)
        {
            Role = RoleType.Buyer;
            playerLevel = currentPlayerLevel;
            allShelves = shelves;
            counters = cashCounters;
            Curve = null; // buyers don't level up
            CarryCapacity = 20; // generous; BagType is visual only
            CarryCount = 0;

            // Roll a personality: mostly normal, with flavourful outliers.
            float roll = Random.value;
            Kind = roll < 0.55f ? Personality.Normal
                 : roll < 0.75f ? Personality.Impatient
                 : roll < 0.90f ? Personality.Bargain
                 : Personality.Rich;
            switch (Kind)
            {
                case Personality.Impatient: speedMultiplier = 1.35f; queuePatienceSeconds = 12f; break;
                case Personality.Rich:      speedMultiplier = 1.00f; queuePatienceSeconds = 35f; break;
                case Personality.Bargain:   speedMultiplier = 0.90f; queuePatienceSeconds = 45f; break;
                default:                    speedMultiplier = 1.10f; queuePatienceSeconds = 25f; break;
            }
            // Playtest fix: ×2.5 meant up to 112s of motionless waiting at empty
            // shelves — the store looked full of statues and buyers then left
            // unpaid in visible waves. Keep a short linger only.
            shopPatienceSeconds = queuePatienceSeconds * 0.8f;

            GenerateBasket();
            OriginalWant.Clear();
            foreach (var kv in Basket) OriginalWant[kv.Key] = kv.Value;
            AssignBagType();

            // GDD 8.2: 5+ items means the buyer pushes a trolley (visual).
            if (BagType == BagType.Trolley)
                Engine.PrimitiveFactory.Trolley(gameObject);

            // Reference-style thought bubble (wish icon + progress fraction).
            if (GetComponent<Engine.ThoughtBubble>() == null)
                gameObject.AddComponent<Engine.ThoughtBubble>();
        }

        private void GenerateBasket()
        {
            Basket.Clear();
            // Randomly request 1-7 items, only from unlocked item types.
            var available = new List<ItemType>();
            foreach (ItemType item in System.Enum.GetValues(typeof(ItemType)))
                if (PriceCatalog.IsUnlocked(item, playerLevel) && HasActiveShelf(item)) available.Add(item);

            if (available.Count == 0) return;

            var eco = GameManager.Instance?.Economy;
            int itemCount = Kind switch
            {
                Personality.Rich    => Random.Range(4, 10), // big baskets, trolley likely
                Personality.Bargain => Random.Range(1, 4),  // small careful baskets
                _                   => Random.Range(1, 8),
            };
            for (int i = 0; i < itemCount; i++)
            {
                var item = available[Random.Range(0, available.Count)];
                // GDD 8.3 price elasticity: items marked up over +20% get skipped 35% of the time.
                if (eco != null && eco.IsOverpriced(item) && Random.value < 0.35f) continue;
                if (!Basket.ContainsKey(item)) Basket[item] = 0;
                Basket[item] += 1;
            }
        }

        /// <summary>Wishlist sanity (reference behaviour): buyers only want items whose
        /// shelf has actually been purchased/placed in the store.</summary>
        private bool HasActiveShelf(ItemType item)
        {
            if (allShelves == null) return false;
            foreach (var s in allShelves)
                if (s != null && s.gameObject.activeInHierarchy && s.Item == item) return true;
            return false;
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

            // Standing in a checkout line: keep our queue slot, patience runs down,
            // impatient shoppers walk out.
            if (queuedCounter != null)
            {
                queueWait += dt;
                if (queueWait >= queuePatienceSeconds)
                {
                    queuedCounter.RemoveFromLine(this);
                    queuedCounter = null;
                    LeaveWithoutPaying("ran out of patience in the queue");
                    return;
                }

                // Shuffle forward to our slot as the line advances (no more one-point pileups).
                int idx = queuedCounter.Line.IndexOf(this);
                if (idx >= 0)
                {
                    Vector3 slot = queuedCounter.GetQueueSlot(idx);
                    Vector3 flat = transform.position; flat.y = 0; slot.y = 0;
                    if ((flat - slot).sqrMagnitude > 0.3f) SetTarget(slot);
                }
                return;
            }

            // Arrived at the tills while none was open (e.g. level 1, player elsewhere):
            // wait around with queue patience; join the moment a counter opens.
            // Previously these buyers froze here FOREVER, silently filling the 12-buyer
            // cap and stopping all future spawns.
            if (waitingForCounter)
            {
                queueWait += dt;
                var open = FindUnlockedCounter();
                if (open != null)
                {
                    waitingForCounter = false;
                    open.Enqueue(this);
                    queuedCounter = open;
                    queueWait = 0f;
                }
                else if (queueWait >= queuePatienceSeconds)
                {
                    waitingForCounter = false;
                    LeaveWithoutPaying("gave up waiting for an open counter");
                }
                return;
            }

            // Standing at a shelf, taking items ONE PER BEAT. Several buyers can pick
            // from the same shelf at once and simply interleave, so stock is shared
            // naturally — first-come takes one, next takes one, and whoever is still
            // there when it runs dry leaves with a partial basket and pays for that.
            if (pickingShelf != null)
            {
                // Drifted out of arm's reach (pushed by the crowd)? Step back in.
                if (!WithinReach(pickingShelf))
                {
                    SetTarget(pickingShelf.transform.position);
                    return;
                }

                pickBeat += dt;
                if (pickBeat < 0.25f) return;
                pickBeat = 0f;

                var it = pickingShelf.Item;
                if (pickingShelf.gameObject.activeInHierarchy &&
                    Basket.TryGetValue(it, out int want) && want > 0 &&
                    pickingShelf.Count > 0 && pickingShelf.TakeStock(1))
                {
                    Basket[it] = want - 1;
                    if (Basket[it] <= 0) Basket.Remove(it);
                    if (!Collected.ContainsKey(it)) Collected[it] = 0;
                    Collected[it] += 1;
                    TryPickUp(1);
                    CarryColor = Engine.PrimitiveFactory.ItemColor(it);
                    shopWait = 0f; // made progress — reset the restock-wait budget
                }
                else
                {
                    pickingShelf = null; // line done or shelf ran dry — move on
                }
                return;
            }

            if (!headingToCounter)
            {
                // Shop for ANYTHING on our list that is actually in stock — nearest first.
                //
                // The old code took basket entry #1 and, if that shelf was empty, parked
                // the buyer there waiting for a restock (or made it give up). It never
                // looked at the rest of the basket. So a buyer blocked on out-of-stock
                // bread would completely ignore a FULL tomato shelf beside it — which is
                // why crowds of shoppers stood around a maxed shelf without taking a
                // single item.
                ShopShelf shelf = FindBestStockedShelf();
                if (shelf != null)
                {
                    shopWait = 0f;
                    // Already close enough? Start picking immediately. Requiring a pinpoint
                    // arrival meant a jostling crowd could keep everyone just outside the
                    // arrival radius, so nobody ever started picking.
                    if (WithinReach(shelf))
                    {
                        pickingShelf = shelf;
                        currentTarget = null;
                        pickBeat = 0f;
                    }
                    else
                    {
                        currentTarget = shelf;
                        SetTarget(shelf.transform.position);
                    }
                    return;
                }

                // Nothing we still want is in stock ANYWHERE.
                ItemType needed = FindNextNeededItem();
                bool stillWants = Basket.TryGetValue(needed, out int needCount) && needCount > 0;

                if (stillWants)
                {

                    // The item's shelf exists but is empty.
                    if (HasActiveShelf(needed))
                    {
                        // Playtest fix: buyers who already have SOMETHING in the
                        // basket now go pay for it instead of statue-waiting for a
                        // restock (waves of "wait 2 minutes then leave unpaid"
                        // made the store look broken and earned nothing).
                        if (Collected.Count > 0)
                        {
                            // fall through to the checkout path below
                        }
                        else
                        {
                            // Nothing collected yet: browse INSIDE the store near
                            // the wanted shelf (buyers used to wait frozen at the
                            // road spawn point, looking like a bug), with a short
                            // patience budget.
                            if (!shownSoldOut)
                            {
                                shownSoldOut = true;
                                Engine.Emote.SoldOut(transform.position);
                            }
                            var emptyShelf = FindAnyShelfObject(needed);
                            if (emptyShelf != null &&
                                (transform.position - emptyShelf.transform.position).sqrMagnitude > 9f)
                            {
                                SetTarget(emptyShelf.transform.position +
                                    new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f)));
                                return;
                            }
                            shopWait += dt;
                            if (shopWait < shopPatienceSeconds)
                                return; // brief linger near the shelf
                            // Waited long enough — give up on the remaining items.
                        }
                    }
                }

                // Done shopping (basket satisfied, or gave up waiting).
                if (Collected.Count == 0)
                {
                    // Never found anything — leave without occupying a till.
                    LeaveWithoutPaying("found nothing in stock");
                    return;
                }

                // Head to nearest open counter with what we collected.
                var counter = FindUnlockedCounter();
                if (counter != null)
                {
                    headingToCounter = true;
                    counter.Enqueue(this);
                    queuedCounter = counter;

                    int idx = counter.Line.IndexOf(this);
                    if (idx >= 0)
                    {
                        Vector3 slot = counter.GetQueueSlot(idx);
                        SetTarget(slot);
                    }
                }
                else
                {
                    waitingForCounter = true;
                    queueWait = 0f;
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
                // We've arrived at our queue slot. We don't need to do anything here,
                // the Tick() method handles shuffling forward as the line moves.
                return;
            }

            if (currentTarget != null)
            {
                // Reference behaviour: items are taken ONE AT A TIME on a beat, so the
                // thought bubble visibly ticks 1/4 -> 2/4 -> ... (bulk-grab made the
                // requirement over their head look like it never updated).
                pickingShelf = currentTarget;
                pickBeat = 0f;
                currentTarget = null;
            }
        }

        private ItemType FindNextNeededItem()
        {
            foreach (var kv in Basket)
                if (kv.Value > 0) return kv.Key;
            return ItemType.Egg; // fallback (won't match any shelf if basket is empty)
        }

        /// <summary>Any active shelf for the item, stocked or not — used to browse
        /// near the shelf while waiting for a restock.</summary>
        private ShopShelf FindAnyShelfObject(ItemType item)
        {
            foreach (var s in allShelves)
                if (s != null && s.gameObject.activeInHierarchy && s.Item == item) return s;
            return null;
        }

        /// <summary>Arm's reach of a shelf. Generous on purpose: a crowd of shoppers
        /// must not be able to block each other out of picking.</summary>
        private const float ShelfReach = 2.0f;

        private bool WithinReach(ShopShelf s)
        {
            if (s == null) return false;
            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = s.transform.position; b.y = 0f;
            return (a - b).sqrMagnitude <= ShelfReach * ShelfReach;
        }

        /// <summary>The nearest shelf holding ANY item we still want. This is what stops
        /// a buyer stalling on one out-of-stock line while other wanted goods sit on a
        /// full shelf next to it.</summary>
        private ShopShelf FindBestStockedShelf()
        {
            ShopShelf best = null;
            float bestD = float.MaxValue;
            foreach (var kv in Basket)
            {
                if (kv.Value <= 0) continue;
                var s = FindShelfFor(kv.Key);          // active + Count > 0
                if (s == null) continue;
                float d = (s.transform.position - transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }

        private ShopShelf FindShelfFor(ItemType item)
        {
            ShopShelf best = null;
            foreach (var s in allShelves)
            {
                if (s == null || !s.gameObject.activeInHierarchy) continue; // not purchased yet
                if (s.Item == item && s.Count > 0 && (best == null || s.Count > best.Count)) best = s;
            }
            return best;
        }

        private Economy.CashCounter FindUnlockedCounter()
        {
            Economy.CashCounter best = null;
            foreach (var c in counters)
                if (c != null && c.gameObject.activeInHierarchy && c.IsUnlocked && (best == null || c.Line.Count < best.Line.Count)) best = c;
            return best;
        }

        /// <summary>Diagnostic snapshot for live probes (frozen-buyer investigations).</summary>
        public string DebugState() =>
            $"hasTarget={hasTarget} wp={(pathWaypoints == null ? -1 : pathWaypoints.Count)}/{currentWaypointIndex} " +
            $"picking={(pickingShelf != null)} queued={(queuedCounter != null)} waiting={waitingForCounter} " +
            $"heading={headingToCounter} leaving={leaving} state={State}";

        private void LeaveWithoutPaying(string reason)
        {
            Engine.Emote.Angry(transform.position);
            Debug.Log($"[Buyer] {Kind} buyer {reason} — leaving.");
            HasCheckedOut = true; // never pays, never re-shops
            leaving = true;
            if (ExitDoor != null) SetTarget(ExitDoor.position);
            else Destroy(gameObject, 1f);
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
