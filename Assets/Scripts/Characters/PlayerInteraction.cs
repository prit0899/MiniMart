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
        public Machine blender;
        public Machine oven;
        public Machine mill;
        public Machine dairy;
        public List<ShopShelf> shelves = new List<ShopShelf>();
        public List<Transform> bins = new List<Transform>();

        private PlayerController player;
        private readonly Dictionary<ItemType, int> carried = new Dictionary<ItemType, int>();
        private float timer;

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

                // 2b) Bins: stand at a dustbin to throw away everything you carry
                //     ("dismantle" — previously there was no way to release items).
                foreach (var bin in bins)
                {
                    if (bin == null || Vector3.Distance(transform.position, bin.position) > Radius) continue;
                    carried.Clear();
                    player.DropAll();
                    Engine.Emote.Spawn(transform.position, "x", new Color(0.7f, 0.7f, 0.7f));
                    return;
                }
            }

            // 3) Machines: load carried inputs, collect finished outputs.
            if (Near(blender, Radius))
            {
                if (CarriedCount(ItemType.Tomato) > 0 && blender.InputQueued < blender.StackCapacity)
                { MoveToMachine(blender, ItemType.Tomato, 1); return; }
                if (blender.OutputReady > 0 && room > 0)
                { Pick(ItemType.TomatoKetchup, blender.CollectFinished()); return; }
            }
            if (Near(mill, Radius))
            {
                if (CarriedCount(ItemType.Wheat) > 0 && mill.InputQueued < mill.StackCapacity)
                { MoveToMachine(mill, ItemType.Wheat, 1); return; }
                if (mill.OutputReady > 0 && room > 0)
                { Pick(ItemType.WheatFlour, mill.CollectFinished()); return; }
            }
            if (Near(dairy, Radius))
            {
                if (CarriedCount(ItemType.Milk) > 0 && dairy.InputQueued < dairy.StackCapacity)
                { MoveToMachine(dairy, ItemType.Milk, 1); return; }
                if (dairy.OutputReady > 0 && room > 0)
                { Pick(ItemType.Cheese, dairy.CollectFinished()); return; }
            }
            if (Near(oven, Radius))
            {
                // Bread needs a flour + egg pair per unit.
                if (CarriedCount(ItemType.WheatFlour) > 0 && CarriedCount(ItemType.Egg) > 0
                    && oven.InputQueued < oven.StackCapacity)
                {
                    oven.LoadInput(1);
                    Consume(ItemType.WheatFlour, 1);
                    Consume(ItemType.Egg, 1);
                    return;
                }
                if (oven.OutputReady > 0 && room > 0)
                { Pick(ItemType.Bread, oven.CollectFinished()); return; }
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
            var vans = FindObjectsByType<DeliveryVan>(FindObjectsSortMode.None);
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
