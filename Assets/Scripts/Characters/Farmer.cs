using MiniMart.Core;
using MiniMart.Catalog;
using MiniMart.Production;
using MiniMart.Runtime;
using UnityEngine;

namespace MiniMart.Characters
{
    /// <summary>Manages the hen coop, wheat farm, and tomato farm; ferries surplus to storage.</summary>
    public class Farmer : CharacterBase
    {
        public HenCoop henCoop;
        public WheatFarm wheatFarm;
        public TomatoFarm tomatoFarm;
        public CowPen cowPen;
        public HerbPatch herbPatch;
        private StoreInventory inventory;

        private enum FarmerState
        {
            Deciding,
            GoingToHenCoop,
            GoingToWheatFarm,
            GoingToTomatoFarm,
            GoingToCowPen,
            GoingToHerbPatch,
            GoingToDeposit
        }

        private FarmerState fState = FarmerState.Deciding;
        private int eggsCount = 0;
        private int wheatCount = 0;
        private int tomatoCount = 0;
        private int milkCount = 0;
        private int herbCount = 0;

        public void Configure(StoreInventory storeInventory)
        {
            Role = RoleType.Farmer;
            inventory = storeInventory;
            Curve = RoleCatalog.FarmerCurve();
            ApplyLevel(1);
        }

        public bool TryPickUpItem(int amount, ItemType item)
        {
            if (TryPickUp(amount))
            {
                CarryColor = Engine.PrimitiveFactory.ItemColor(item);
                if (item == ItemType.Egg) eggsCount += amount;
                else if (item == ItemType.Wheat) wheatCount += amount;
                else if (item == ItemType.Tomato) tomatoCount += amount;
                else if (item == ItemType.Milk) milkCount += amount;
                else if (item == ItemType.Herb) herbCount += amount;
                return true;
            }
            return false;
        }

        /// <summary>Real item meshes in the carry stack, same as the player.</summary>
        public override System.Collections.Generic.List<ItemType> GetCarriedItems()
        {
            var list = new System.Collections.Generic.List<ItemType>();
            void Add(ItemType t, int n) { for (int i = 0; i < n; i++) list.Add(t); }
            Add(ItemType.Egg, eggsCount);
            Add(ItemType.Wheat, wheatCount);
            Add(ItemType.Tomato, tomatoCount);
            Add(ItemType.Milk, milkCount);
            Add(ItemType.Herb, herbCount);
            return list;
        }

        public void DropAllFarmer()
        {
            DropAll();
            eggsCount = 0;
            wheatCount = 0;
            tomatoCount = 0;
            milkCount = 0;
            herbCount = 0;
        }

        private int farmCursor; // rotates coop -> wheat -> tomato so every farm gets serviced

        private bool HasStorageRoom(ItemType item) =>
            inventory != null && inventory.Stocks.TryGetValue(item, out var s) && s.Count < s.MaxCapacity;

        private bool FarmActive(Component c) => c != null && c.gameObject.activeInHierarchy;

        /// <summary>Walk to the rack of whichever item we carry most of (racks sit
        /// next to their sources now — no more single central depot).</summary>
        private Vector3 DepositTarget()
        {
            ItemType best = ItemType.Egg;
            int most = eggsCount;
            if (wheatCount > most) { best = ItemType.Wheat; most = wheatCount; }
            if (tomatoCount > most) { best = ItemType.Tomato; most = tomatoCount; }
            if (milkCount > most) { best = ItemType.Milk; most = milkCount; }
            if (herbCount > most) { best = ItemType.Herb; most = herbCount; }
            return Engine.StorageRack.PositionOf(best, new Vector3(5f, 0f, 10f));
        }

        private bool TryChooseFarm()
        {
            for (int i = 0; i < 5; i++)
            {
                int pick = (farmCursor + i) % 5;
                if (pick == 0 && FarmActive(henCoop) && henCoop.TotalEggsReady() > 0 && HasStorageRoom(ItemType.Egg))
                {
                    farmCursor = 1;
                    fState = FarmerState.GoingToHenCoop;
                    SetTarget(henCoop.transform.position);
                    return true;
                }
                if (pick == 1 && FarmActive(wheatFarm) && wheatFarm.ReadyCount() > 0 && HasStorageRoom(ItemType.Wheat))
                {
                    farmCursor = 2;
                    fState = FarmerState.GoingToWheatFarm;
                    SetTarget(wheatFarm.transform.position);
                    return true;
                }
                if (pick == 2 && FarmActive(tomatoFarm) && tomatoFarm.TotalRipe() > 0 && HasStorageRoom(ItemType.Tomato))
                {
                    farmCursor = 3;
                    fState = FarmerState.GoingToTomatoFarm;
                    SetTarget(tomatoFarm.transform.position);
                    return true;
                }
                if (pick == 3 && FarmActive(cowPen) && cowPen.TotalMilkReady() > 0 && HasStorageRoom(ItemType.Milk))
                {
                    farmCursor = 4;
                    fState = FarmerState.GoingToCowPen;
                    SetTarget(cowPen.transform.position);
                    return true;
                }
                if (pick == 4 && FarmActive(herbPatch) && herbPatch.TotalRipe() > 0 && HasStorageRoom(ItemType.Herb))
                {
                    farmCursor = 0;
                    fState = FarmerState.GoingToHerbPatch;
                    SetTarget(herbPatch.transform.position);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Patrols to each farm node, harvests what is ready, and deposits it back to the store inventory.</summary>
        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (inventory == null && GameManager.Instance != null)
                inventory = GameManager.Instance.Inventory;

            if (hasTarget || inventory == null) return;

            int room = CarryCapacity - CarryCount;

            switch (fState)
            {
                case FarmerState.Deciding:
                    // Round-robin between the three farms. The old fixed priority (eggs first)
                    // starved wheat and tomato forever: hens lay every 0.5 s, so "eggs ready"
                    // was ALWAYS true and the farmer never visited the other farms.
                    if (room > 0 && TryChooseFarm()) break;
                    if (CarryCount > 0)
                    {
                        fState = FarmerState.GoingToDeposit;
                        SetTarget(DepositTarget());
                    }
                    break;

                case FarmerState.GoingToHenCoop:
                    if (henCoop != null)
                    {
                        int eggs = henCoop.Collect(room);
                        if (eggs > 0) TryPickUpItem(eggs, ItemType.Egg);
                    }
                    fState = FarmerState.Deciding;
                    break;

                case FarmerState.GoingToWheatFarm:
                    if (wheatFarm != null)
                    {
                        int wheat = wheatFarm.Harvest(room);
                        if (wheat > 0) TryPickUpItem(wheat, ItemType.Wheat);
                    }
                    fState = FarmerState.Deciding;
                    break;

                case FarmerState.GoingToTomatoFarm:
                    if (tomatoFarm != null)
                    {
                        int tomato = tomatoFarm.Harvest(room);
                        if (tomato > 0) TryPickUpItem(tomato, ItemType.Tomato);
                    }
                    fState = FarmerState.Deciding;
                    break;

                case FarmerState.GoingToCowPen:
                    if (cowPen != null)
                    {
                        int milk = cowPen.Collect(room);
                        if (milk > 0) TryPickUpItem(milk, ItemType.Milk);
                    }
                    fState = FarmerState.Deciding;
                    break;

                case FarmerState.GoingToHerbPatch:
                    if (herbPatch != null)
                    {
                        int herbs = herbPatch.Harvest(room);
                        if (herbs > 0) TryPickUpItem(herbs, ItemType.Herb);
                    }
                    fState = FarmerState.Deciding;
                    break;

                case FarmerState.GoingToDeposit:
                    if (CarryCount > 0 && inventory != null)
                    {
                        if (eggsCount > 0) inventory.Deposit(ItemType.Egg, eggsCount);
                        if (wheatCount > 0) inventory.Deposit(ItemType.Wheat, wheatCount);
                        if (tomatoCount > 0) inventory.Deposit(ItemType.Tomato, tomatoCount);
                        if (milkCount > 0) inventory.Deposit(ItemType.Milk, milkCount);
                        if (herbCount > 0) inventory.Deposit(ItemType.Herb, herbCount);
                        DropAllFarmer();
                    }
                    fState = FarmerState.Deciding;
                    break;
            }
        }
    }
}
