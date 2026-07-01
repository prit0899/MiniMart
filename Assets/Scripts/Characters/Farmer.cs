using MiniMart.Core;
using MiniMart.Catalog;
using MiniMart.Production;
using MiniMart.Runtime;

namespace MiniMart.Characters
{
    /// <summary>Manages the hen coop, wheat farm, and tomato farm; ferries surplus to storage.</summary>
    public class Farmer : CharacterBase
    {
        public HenCoop henCoop;
        public WheatFarm wheatFarm;
        public TomatoFarm tomatoFarm;
        private StoreInventory inventory;

        public void Configure(StoreInventory storeInventory)
        {
            Role = RoleType.Farmer;
            inventory = storeInventory;
            Curve = RoleCatalog.FarmerCurve();
            ApplyLevel(1);
        }

        /// <summary>Greedy upkeep pass: pull whatever is ready from each farm node into storage,
        /// respecting carry capacity per trip.</summary>
        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (hasTarget) return;

            int room = CarryCapacity - CarryCount;
            if (room <= 0) return;

            int eggs = henCoop != null ? henCoop.Collect(room) : 0;
            if (eggs > 0)
            {
                inventory.Deposit(ItemType.Egg, eggs);
                room -= eggs;
            }

            if (room > 0)
            {
                int wheat = wheatFarm != null ? wheatFarm.Harvest(room) : 0;
                if (wheat > 0)
                {
                    inventory.Deposit(ItemType.Wheat, wheat);
                    room -= wheat;
                }
            }

            if (room > 0)
            {
                int tomato = tomatoFarm != null ? tomatoFarm.Harvest(room) : 0;
                if (tomato > 0) inventory.Deposit(ItemType.Tomato, tomato);
            }
        }
    }
}
