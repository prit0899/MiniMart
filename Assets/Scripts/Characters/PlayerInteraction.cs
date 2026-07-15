using System.Collections.Generic;
using UnityEngine;
using MiniMart.Core;
using MiniMart.Production;
using MiniMart.Runtime;

namespace MiniMart.Characters
{
    /// <summary>
    /// Proximity interactions for the player — the core "player does everything" pillar.
    /// Stand near a farm to harvest, the storage depot to deposit, a machine to load and
    /// collect, or a shelf to stock it. Everything is automatic (no button), throttled so
    /// items visibly tick in and out one at a time like the reference game.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Tuning")]
        public float Radius = 1.9f;
        public float DepotRadius = 2.4f;
        public float ActionInterval = 0.18f; // action beat: quick enough to fill a 15-44 stack

        [Header("World references — wired by SceneBootstrapper")]
        public TomatoFarm tomatoFarm;
        public WheatFarm wheatFarm;
        public HenCoop henCoop;
        public CowPen cowPen;
        public HerbPatch herbPatch;
        public CornField cornField;
        public AppleOrchard appleOrchard;
        public Machine tomatoCanner;
        public Machine blender;
        public Machine mill;
        public Machine oven;
        public Machine doughMixer;
        public Machine milkBottler;
        public Machine dairy;
        public Machine stove;
        public Machine leafProcessor;
        public Machine cornProcessor;
        public Machine cookieLine;
        public Machine coffeeDispenser; // auto-producer, collect-only
        public HayFeedTrough hayFeedTrough;
        public List<ShopShelf> shelves = new List<ShopShelf>();
        public List<Transform> bins = new List<Transform>();

        private PlayerController player;
        private readonly Dictionary<ItemType, int> carried = new Dictionary<ItemType, int>();
        private float timer;
        private float binDwell; // time spent standing at a dustbin (prevents walk-by dumps)

        /// <summary>Exact per-item carried list for mixed-stack visuals. Not an override —
        /// PlayerInteraction is a sibling MonoBehaviour, not a CharacterBase; CarryVisual
        /// calls this directly via GetComponent&lt;PlayerInteraction&gt;().</summary>
        public System.Collections.Generic.List<ItemType> GetCarriedItems()
        {
            var list = new System.Collections.Generic.List<ItemType>();
            foreach (var kv in carried)
                for (int i = 0; i < kv.Value; i++) list.Add(kv.Key);
            return list;
        }

        private void Awake() => player = GetComponent<PlayerController>();

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < ActionInterval) return;
            timer = 0f;

            if (player == null || player.IsPaused) return;
            var inv = GameManager.Instance?.Inventory;
            if (inv == null) return;

            int room = player.CarryCapacity - player.CarryCount;

            // 1) Harvest whichever farm we're standing at (one unit per beat).
            //    Only gated on carry room — NOT on storage fullness. (A previous
            //    "carry-wedge" guard blocked harvesting whenever raw storage was
            //    full, but the player harvests to STOCK SHELVES too, so that
            //    guard starved the shelves when the farmer had filled storage and
            //    no shelver was hired yet — the store made no sales and it looked
            //    like the player "couldn't carry anything". Overflow on deposit is
            //    already handled by the bin, so plain carry-room gating is safe.)
            if (room > 0)
            {
                if (Near(henCoop, Radius) && henCoop.TotalEggsReady() > 0)
                { Pick(ItemType.Egg, henCoop.Collect(1)); return; }
                if (Near(tomatoFarm, Radius) && tomatoFarm.TotalRipe() > 0)
                { Pick(ItemType.Tomato, tomatoFarm.Harvest(1)); return; }
                if (Near(wheatFarm, Radius) && wheatFarm.ReadyCount() > 0)
                { Pick(ItemType.Wheat, wheatFarm.Harvest(1)); return; }
                if (Near(cowPen, Radius) && cowPen.TotalMilkReady() > 0)
                { Pick(ItemType.Milk, cowPen.Collect(1)); return; }
                if (Near(herbPatch, Radius) && herbPatch.TotalRipe() > 0)
                { Pick(ItemType.Herb, herbPatch.Harvest(1)); return; }
                if (Near(cornField, Radius) && cornField.TotalRipe() > 0)
                { Pick(ItemType.Corn, cornField.Harvest(1)); return; }
                if (Near(appleOrchard, Radius) && appleOrchard.TotalRipe() > 0)
                { Pick(ItemType.Apple, appleOrchard.Harvest(1)); return; }
            }

            // 1b) Feed the cow: stand at the Hay Feed Trough with wheat to
            //     top it up. Trough consumes wheat over time to speed milk.
            if (hayFeedTrough != null && Near(hayFeedTrough, Radius))
            {
                if (CarriedCount(ItemType.Wheat) > 0)
                {
                    int fit = hayFeedTrough.AddHay(1);
                    if (fit > 0) { Consume(ItemType.Wheat, fit); return; }
                }
            }

            // 2) Deposit carried items at their per-source storage racks
            //    (egg rack by the coop, tomato/ketchup racks by the plants, ...).
            if (player.CarryCount > 0)
            {
                foreach (var kv in carried)
                {
                    if (kv.Value <= 0) continue;
                    var rack = Engine.StorageRack.Get(kv.Key);
                    if (rack != null && Vector3.Distance(transform.position, rack.transform.position) < DepotRadius)
                    {
                        inv.Deposit(kv.Key, kv.Value);
                        Consume(kv.Key, kv.Value);
                        return;
                    }
                }

                // 2b) Bins: DELIBERATELY stand at a dustbin to throw items away, a couple
                //     per beat. A dwell timer + tight radius stops the old bug where merely
                //     walking past a bin instantly trashed a full MAX stack.
                bool atBin = false;
                foreach (var bin in bins)
                {
                    if (bin == null || Vector3.Distance(transform.position, bin.position) > 1.2f) continue;
                    atBin = true;
                    binDwell += ActionInterval;
                    if (binDwell < 0.9f) break; // must linger before dumping starts
                    foreach (var key in new List<ItemType>(carried.Keys))
                    {
                        if (carried[key] <= 0) continue;
                        Consume(key, Mathf.Min(carried[key], 2));
                        Engine.Emote.Spawn(transform.position, "x", new Color(0.7f, 0.7f, 0.7f));
                        break; // one item type per beat
                    }
                    break;
                }
                if (!atBin) binDwell = 0f;
                if (atBin && binDwell >= 0.9f) return;
            }

            // 3) Machines: load carried inputs, collect finished outputs.
            if (Near(tomatoCanner, Radius))
            {
                if (CarriedCount(ItemType.Tomato) > 0 && tomatoCanner.InputQueued < tomatoCanner.StackCapacity)
                { MoveToMachine(tomatoCanner, ItemType.Tomato, 1); return; }
                if (tomatoCanner.OutputReady > 0 && room > 0)
                { Pick(ItemType.CannedTomato, tomatoCanner.CollectFinished()); return; }
            }
            if (Near(doughMixer, Radius))
            {
                if (CarriedCount(ItemType.Wheat) > 0 && doughMixer.InputQueued < doughMixer.StackCapacity)
                { MoveToMachine(doughMixer, ItemType.Wheat, 1); return; }
                if (doughMixer.OutputReady > 0 && room > 0)
                { Pick(ItemType.Dough, doughMixer.CollectFinished()); return; }
            }
            if (Near(milkBottler, Radius))
            {
                if (CarriedCount(ItemType.Milk) > 0 && milkBottler.InputQueued < milkBottler.StackCapacity)
                { MoveToMachine(milkBottler, ItemType.Milk, 1); return; }
                if (milkBottler.OutputReady > 0 && room > 0)
                { Pick(ItemType.BottledMilk, milkBottler.CollectFinished()); return; }
            }
            if (Near(oven, Radius))
            {
                // Oven takes Dough -> Bread
                if (CarriedCount(ItemType.Dough) > 0 && oven.InputQueued < oven.StackCapacity)
                { MoveToMachine(oven, ItemType.Dough, 1); return; }
                if (oven.OutputReady > 0 && room > 0)
                { Pick(ItemType.Bread, oven.CollectFinished()); return; }
            }
            if (Near(cornProcessor, Radius))
            {
                if (CarriedCount(ItemType.Corn) > 0 && cornProcessor.InputQueued < cornProcessor.StackCapacity)
                { MoveToMachine(cornProcessor, ItemType.Corn, 1); return; }
                if (cornProcessor.OutputReady > 0 && room > 0)
                { Pick(ItemType.ProcessedCorn, cornProcessor.CollectFinished()); return; }
            }
            if (Near(cookieLine, Radius))
            {
                // Cookies need Wheat + Milk per unit
                if (CarriedCount(ItemType.Wheat) > 0 && CarriedCount(ItemType.Milk) > 0
                    && cookieLine.InputQueued < cookieLine.StackCapacity)
                {
                    cookieLine.LoadInput(1);
                    Consume(ItemType.Wheat, 1);
                    Consume(ItemType.Milk, 1);
                    return;
                }
                if (cookieLine.OutputReady > 0 && room > 0)
                { Pick(ItemType.Cookie, cookieLine.CollectFinished()); return; }
            }
            // Blender: Tomato → TomatoKetchup
            if (Near(blender, Radius))
            {
                if (CarriedCount(ItemType.Tomato) > 0 && blender.InputQueued < blender.StackCapacity)
                { MoveToMachine(blender, ItemType.Tomato, 1); return; }
                if (blender.OutputReady > 0 && room > 0)
                { Pick(ItemType.TomatoKetchup, blender.CollectFinished()); return; }
            }
            // Mill: Wheat → WheatFlour
            if (Near(mill, Radius))
            {
                if (CarriedCount(ItemType.Wheat) > 0 && mill.InputQueued < mill.StackCapacity)
                { MoveToMachine(mill, ItemType.Wheat, 1); return; }
                if (mill.OutputReady > 0 && room > 0)
                { Pick(ItemType.WheatFlour, mill.CollectFinished()); return; }
            }
            // Dairy: Milk → Cheese
            if (Near(dairy, Radius))
            {
                if (CarriedCount(ItemType.Milk) > 0 && dairy.InputQueued < dairy.StackCapacity)
                { MoveToMachine(dairy, ItemType.Milk, 1); return; }
                if (dairy.OutputReady > 0 && room > 0)
                { Pick(ItemType.Cheese, dairy.CollectFinished()); return; }
            }
            // Stove: Egg → FriedEgg
            if (Near(stove, Radius))
            {
                if (CarriedCount(ItemType.Egg) > 0 && stove.InputQueued < stove.StackCapacity)
                { MoveToMachine(stove, ItemType.Egg, 1); return; }
                if (stove.OutputReady > 0 && room > 0)
                { Pick(ItemType.FriedEgg, stove.CollectFinished()); return; }
            }
            // LeafProcessor: Herb → HerbPack
            if (Near(leafProcessor, Radius))
            {
                if (CarriedCount(ItemType.Herb) > 0 && leafProcessor.InputQueued < leafProcessor.StackCapacity)
                { MoveToMachine(leafProcessor, ItemType.Herb, 1); return; }
                if (leafProcessor.OutputReady > 0 && room > 0)
                { Pick(ItemType.HerbPack, leafProcessor.CollectFinished()); return; }
            }
            // CoffeeDispenser: auto-producer — player only collects the cups.
            if (Near(coffeeDispenser, Radius))
            {
                if (coffeeDispenser.OutputReady > 0 && room > 0)
                { Pick(ItemType.Coffee, coffeeDispenser.CollectFinished()); return; }
            }

            // 4) Stock the shelf we're standing at with matching carried items.
            foreach (var shelf in shelves)
            {
                if (shelf == null || !shelf.gameObject.activeInHierarchy || !Near(shelf, Radius)) continue;
                int have = CarriedCount(shelf.Item);
                int space = shelf.Capacity - shelf.Count;
                if (have > 0 && space > 0)
                {
                    int move = Mathf.Min(have, space, 2); // 2 per beat feels brisk but readable
                    shelf.AddStock(move);
                    Consume(shelf.Item, move);
                    return;
                }
            }
            // 5) Load delivery vans for phone orders
            var vans = FindObjectsByType<DeliveryVan>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var van in vans)
            {
                if (!Near(van, Radius * 1.5f)) continue; // generous radius for vans
                foreach (var kv in carried)
                {
                    if (kv.Value > 0 && van.Needs(kv.Key))
                    {
                        int move = Mathf.Min(kv.Value, 2); // 2 per beat
                        van.LoadItem(kv.Key, move);
                        Consume(kv.Key, move);
                        return; // do one action per beat
                    }
                }
            }
        }

        // ─── helpers ─────────────────────────────────────────────────────────

        private bool Near(Component c, float radius) =>
            c != null && c.gameObject.activeInHierarchy &&
            Vector3.Distance(transform.position, c.transform.position) < radius;

        private int CarriedCount(ItemType item) =>
            carried.TryGetValue(item, out int n) ? n : 0;

        private void Pick(ItemType item, int amount)
        {
            if (amount <= 0) return;
            if (!player.TryPickUp(amount)) return;
            carried[item] = CarriedCount(item) + amount;
            player.CarryColor = Engine.PrimitiveFactory.ItemColor(item);
        }

        private void Consume(ItemType item, int amount)
        {
            int have = CarriedCount(item);
            int used = Mathf.Min(have, amount);
            carried[item] = have - used;
            player.CarryCount = Mathf.Max(0, player.CarryCount - used);
        }

        private void MoveToMachine(Machine machine, ItemType item, int amount)
        {
            machine.LoadInput(amount);
            Consume(item, amount);
        }
    }
}
