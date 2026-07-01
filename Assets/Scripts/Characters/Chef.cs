using UnityEngine;
using MiniMart.Core;
using MiniMart.Catalog;
using MiniMart.Production;
using MiniMart.Runtime;

namespace MiniMart.Characters
{
    /// <summary>
    /// Makes ketchup (from tomato) and bread (from wheat flour). Per spec, the chef takes
    /// tomato and eggs directly from the farm "by self", and takes wheat from the farm too
    /// (wheat is then milled into flour before becoming bread). Chef ceiling is enforced to be
    /// >= shelver ceiling via RoleCatalog.ChefCurve.
    /// </summary>
    public class Chef : CharacterBase
    {
        public TomatoFarm tomatoFarm;
        public HenCoop henCoop;
        public Machine blender; // ketchup
        public Machine oven;    // bread
        private StoreInventory inventory;

        public void Configure(StoreInventory storeInventory)
        {
            Role = RoleType.Chef;
            inventory = storeInventory;
            Curve = RoleCatalog.ChefCurve();
            ApplyLevel(1);
        }

        /// <summary>Self-fetch tomato directly from the farm (bypasses shelver/storage hand-off).</summary>
        public bool SelfFetchTomato(int amount)
        {
            int got = tomatoFarm != null ? tomatoFarm.Harvest(amount) : 0;
            if (got <= 0) return false;
            return TryPickUp(got);
        }

        /// <summary>Self-fetch eggs directly from the farm.</summary>
        public bool SelfFetchEgg(int amount)
        {
            int got = henCoop != null ? henCoop.Collect(amount) : 0;
            if (got <= 0) return false;
            return TryPickUp(got);
        }

        /// <summary>Loads carried tomato into the blender to start a ketchup batch.</summary>
        public void LoadBlender(int amount)
        {
            if (CarryCount < amount) return;
            blender.LoadInput(amount);
            CarryCount -= amount;
            State = CharacterState.Loading;
        }

        /// <summary>Loads carried wheat flour into the oven to start a bread batch.</summary>
        public void LoadOven(int amount)
        {
            if (CarryCount < amount) return;
            oven.LoadInput(amount);
            CarryCount -= amount;
            State = CharacterState.Loading;
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            // Collect finished batches into store inventory.
            int ketchupReady = blender != null ? blender.CollectFinished() : 0;
            if (ketchupReady > 0) inventory.Deposit(ItemType.TomatoKetchup, ketchupReady);

            int breadReady = oven != null ? oven.CollectFinished() : 0;
            if (breadReady > 0) inventory.Deposit(ItemType.Bread, breadReady);
        }
    }
}
