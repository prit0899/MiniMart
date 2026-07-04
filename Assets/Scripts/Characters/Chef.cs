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
        public WheatFarm wheatFarm;
        public HenCoop henCoop;
        public Machine blender; // ketchup
        public Machine oven;    // bread
        public Machine mill;    // flour
        private StoreInventory inventory;

        private enum ChefState
        {
            Deciding,
            GoingToWithdrawTomato,
            GoingToBlenderToLoad,
            GoingToWithdrawWheat,
            GoingToMillToLoad,
            GoingToWithdrawOvenIngredients,
            GoingToOvenToLoad,
            GoingToBlenderToCollect,
            GoingToMillToCollect,
            GoingToOvenToCollect,
            GoingToDeposit,
            GoingToSelfFetchTomato,
            GoingToSelfFetchWheat
        }

        private ChefState cState = ChefState.Deciding;
        private int tomatoCount = 0;
        private int wheatCount = 0;
        private int eggCount = 0;
        private int flourCount = 0;
        private int ketchupCount = 0;
        private int breadCount = 0;

        public void Configure(StoreInventory storeInventory)
        {
            Role = RoleType.Chef;
            inventory = storeInventory;
            Curve = RoleCatalog.ChefCurve();
            ApplyLevel(1);
        }

        public bool TryPickUpItem(int amount, ItemType item)
        {
            if (TryPickUp(amount))
            {
                CarryColor = Engine.PrimitiveFactory.ItemColor(item);
                if (item == ItemType.Tomato) tomatoCount += amount;
                else if (item == ItemType.Wheat) wheatCount += amount;
                else if (item == ItemType.Egg) eggCount += amount;
                else if (item == ItemType.WheatFlour) flourCount += amount;
                else if (item == ItemType.TomatoKetchup) ketchupCount += amount;
                else if (item == ItemType.Bread) breadCount += amount;
                return true;
            }
            return false;
        }

        public void DropAllChef()
        {
            DropAll();
            tomatoCount = 0;
            wheatCount = 0;
            eggCount = 0;
            flourCount = 0;
            ketchupCount = 0;
            breadCount = 0;
        }

        /// <summary>Self-fetch tomato directly from the farm (bypasses shelver/storage hand-off).</summary>
        public bool SelfFetchTomato(int amount)
        {
            int got = tomatoFarm != null ? tomatoFarm.Harvest(amount) : 0;
            if (got <= 0) return false;
            return TryPickUpItem(got, ItemType.Tomato);
        }

        /// <summary>Self-fetch eggs directly from the farm.</summary>
        public bool SelfFetchEgg(int amount)
        {
            int got = henCoop != null ? henCoop.Collect(amount) : 0;
            if (got <= 0) return false;
            return TryPickUpItem(got, ItemType.Egg);
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

        private Vector3 RackPos(ItemType item) =>
            Engine.StorageRack.PositionOf(item, new Vector3(5f, 0f, 10f));

        /// <summary>Rack of the primary output we're carrying (racks sit by their sources).</summary>
        private Vector3 DepositPos()
        {
            if (ketchupCount > 0) return RackPos(ItemType.TomatoKetchup);
            if (breadCount > 0) return RackPos(ItemType.Bread);
            if (flourCount > 0) return RackPos(ItemType.WheatFlour);
            if (wheatCount > 0) return RackPos(ItemType.Wheat);
            if (tomatoCount > 0) return RackPos(ItemType.Tomato);
            if (eggCount > 0) return RackPos(ItemType.Egg);
            return RackPos(ItemType.Tomato);
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (inventory == null && GameManager.Instance != null)
                inventory = GameManager.Instance.Inventory;

            if (hasTarget || inventory == null) return;

            int room = CarryCapacity - CarryCount;

            switch (cState)
            {
                case ChefState.Deciding:
                    // Priority 1: Collect finished outputs if we have carry space
                    if (room > 0)
                    {
                        if (blender != null && blender.OutputReady > 0)
                        {
                            cState = ChefState.GoingToBlenderToCollect;
                            SetTarget(blender.transform.position);
                            return;
                        }
                        else if (mill != null && mill.OutputReady > 0)
                        {
                            cState = ChefState.GoingToMillToCollect;
                            SetTarget(mill.transform.position);
                            return;
                        }
                        else if (oven != null && oven.OutputReady > 0)
                        {
                            cState = ChefState.GoingToOvenToCollect;
                            SetTarget(oven.transform.position);
                            return;
                        }
                    }

                    // If we have items carried, let's go deposit them first!
                    if (CarryCount > 0 && (ketchupCount > 0 || breadCount > 0 || flourCount > 0))
                    {
                        cState = ChefState.GoingToDeposit;
                        SetTarget(DepositPos());
                        return;
                    }

                    // Priority 2: Load machines if they need input and we have raw ingredients in storage
                    if (room > 0)
                    {
                        // Check Oven (Bread): Needs 1 WheatFlour + 1 Egg
                        if (oven != null && oven.InputQueued < oven.StackCapacity && inventory.CountOf(ItemType.WheatFlour) >= 1 && inventory.CountOf(ItemType.Egg) >= 1)
                        {
                            cState = ChefState.GoingToWithdrawOvenIngredients;
                            SetTarget(RackPos(ItemType.WheatFlour));
                            return;
                        }
                        // Check Mill (Flour): Needs 1 Wheat
                        else if (mill != null && mill.InputQueued < mill.StackCapacity && inventory.CountOf(ItemType.Wheat) >= 1)
                        {
                            cState = ChefState.GoingToWithdrawWheat;
                            SetTarget(RackPos(ItemType.Wheat));
                            return;
                        }
                        // Check Blender (Ketchup): Needs 1 Tomato
                        else if (blender != null && blender.InputQueued < blender.StackCapacity && inventory.CountOf(ItemType.Tomato) >= 1)
                        {
                            cState = ChefState.GoingToWithdrawTomato;
                            SetTarget(RackPos(ItemType.Tomato));
                            return;
                        }
                        // Spec: chef fetches from the FARM by himself when storage has none.
                        else if (blender != null && blender.InputQueued < blender.StackCapacity
                                 && tomatoFarm != null && tomatoFarm.TotalRipe() > 0)
                        {
                            cState = ChefState.GoingToSelfFetchTomato;
                            SetTarget(tomatoFarm.transform.position);
                            return;
                        }
                        else if (mill != null && mill.InputQueued < mill.StackCapacity
                                 && wheatFarm != null && wheatFarm.ReadyCount() > 0)
                        {
                            cState = ChefState.GoingToSelfFetchWheat;
                            SetTarget(wheatFarm.transform.position);
                            return;
                        }
                    }

                    // Fallbacks
                    if (tomatoCount > 0 && blender != null && blender.InputQueued < blender.StackCapacity)
                    {
                        cState = ChefState.GoingToBlenderToLoad;
                        SetTarget(blender.transform.position);
                    }
                    else if (wheatCount > 0 && mill != null && mill.InputQueued < mill.StackCapacity)
                    {
                        cState = ChefState.GoingToMillToLoad;
                        SetTarget(mill.transform.position);
                    }
                    else if (flourCount > 0 && eggCount > 0 && oven != null && oven.InputQueued < oven.StackCapacity)
                    {
                        cState = ChefState.GoingToOvenToLoad;
                        SetTarget(oven.transform.position);
                    }
                    else if (CarryCount > 0)
                    {
                        cState = ChefState.GoingToDeposit;
                        SetTarget(DepositPos());
                    }
                    break;

                case ChefState.GoingToWithdrawTomato:
                    {
                        int amount = Mathf.Min(room, blender.StackCapacity - blender.InputQueued, inventory.CountOf(ItemType.Tomato));
                        if (amount > 0 && inventory.Withdraw(ItemType.Tomato, amount))
                        {
                            TryPickUpItem(amount, ItemType.Tomato);
                            cState = ChefState.GoingToBlenderToLoad;
                            SetTarget(blender.transform.position);
                        }
                        else
                        {
                            cState = ChefState.Deciding;
                        }
                    }
                    break;

                case ChefState.GoingToBlenderToLoad:
                    if (blender != null && tomatoCount > 0)
                    {
                        int load = Mathf.Min(tomatoCount, blender.StackCapacity - blender.InputQueued);
                        blender.LoadInput(load);
                        tomatoCount -= load;
                        CarryCount -= load;
                    }
                    cState = ChefState.Deciding;
                    break;

                case ChefState.GoingToWithdrawWheat:
                    {
                        int amount = Mathf.Min(room, mill.StackCapacity - mill.InputQueued, inventory.CountOf(ItemType.Wheat));
                        if (amount > 0 && inventory.Withdraw(ItemType.Wheat, amount))
                        {
                            TryPickUpItem(amount, ItemType.Wheat);
                            cState = ChefState.GoingToMillToLoad;
                            SetTarget(mill.transform.position);
                        }
                        else
                        {
                            cState = ChefState.Deciding;
                        }
                    }
                    break;

                case ChefState.GoingToMillToLoad:
                    if (mill != null && wheatCount > 0)
                    {
                        int load = Mathf.Min(wheatCount, mill.StackCapacity - mill.InputQueued);
                        mill.LoadInput(load);
                        wheatCount -= load;
                        CarryCount -= load;
                    }
                    cState = ChefState.Deciding;
                    break;

                case ChefState.GoingToWithdrawOvenIngredients:
                    {
                        int limitByRoom = room / 2;
                        int limitByOven = (oven.StackCapacity - oven.InputQueued);
                        int amount = Mathf.Min(limitByRoom, limitByOven, inventory.CountOf(ItemType.WheatFlour), inventory.CountOf(ItemType.Egg));
                        if (amount > 0 && inventory.Withdraw(ItemType.WheatFlour, amount) && inventory.Withdraw(ItemType.Egg, amount))
                        {
                            TryPickUpItem(amount, ItemType.WheatFlour);
                            TryPickUpItem(amount, ItemType.Egg);
                            cState = ChefState.GoingToOvenToLoad;
                            SetTarget(oven.transform.position);
                        }
                        else
                        {
                            cState = ChefState.Deciding;
                        }
                    }
                    break;

                case ChefState.GoingToOvenToLoad:
                    if (oven != null && flourCount > 0 && eggCount > 0)
                    {
                        int load = Mathf.Min(flourCount, eggCount, oven.StackCapacity - oven.InputQueued);
                        oven.LoadInput(load);
                        flourCount -= load;
                        eggCount -= load;
                        CarryCount -= (load * 2);
                    }
                    cState = ChefState.Deciding;
                    break;

                case ChefState.GoingToBlenderToCollect:
                    if (blender != null)
                    {
                        int ready = blender.CollectFinished();
                        if (ready > 0) TryPickUpItem(ready, ItemType.TomatoKetchup);
                    }
                    cState = ChefState.Deciding;
                    break;

                case ChefState.GoingToMillToCollect:
                    if (mill != null)
                    {
                        int ready = mill.CollectFinished();
                        if (ready > 0) TryPickUpItem(ready, ItemType.WheatFlour);
                    }
                    cState = ChefState.Deciding;
                    break;

                case ChefState.GoingToOvenToCollect:
                    if (oven != null)
                    {
                        int ready = oven.CollectFinished();
                        if (ready > 0) TryPickUpItem(ready, ItemType.Bread);
                    }
                    cState = ChefState.Deciding;
                    break;

                case ChefState.GoingToSelfFetchTomato:
                    {
                        int want = Mathf.Min(room, blender != null ? blender.StackCapacity - blender.InputQueued : 0);
                        if (want > 0 && SelfFetchTomato(want))
                        {
                            cState = ChefState.GoingToBlenderToLoad;
                            SetTarget(blender.transform.position);
                        }
                        else
                        {
                            cState = ChefState.Deciding;
                        }
                    }
                    break;

                case ChefState.GoingToSelfFetchWheat:
                    {
                        int want = Mathf.Min(room, mill != null ? mill.StackCapacity - mill.InputQueued : 0);
                        int got = (want > 0 && wheatFarm != null) ? wheatFarm.Harvest(want) : 0;
                        if (got > 0 && TryPickUpItem(got, ItemType.Wheat))
                        {
                            cState = ChefState.GoingToMillToLoad;
                            SetTarget(mill.transform.position);
                        }
                        else
                        {
                            cState = ChefState.Deciding;
                        }
                    }
                    break;

                case ChefState.GoingToDeposit:
                    if (inventory != null)
                    {
                        if (ketchupCount > 0) inventory.Deposit(ItemType.TomatoKetchup, ketchupCount);
                        if (breadCount > 0) inventory.Deposit(ItemType.Bread, breadCount);
                        if (flourCount > 0) inventory.Deposit(ItemType.WheatFlour, flourCount);
                        if (tomatoCount > 0) inventory.Deposit(ItemType.Tomato, tomatoCount);
                        if (wheatCount > 0) inventory.Deposit(ItemType.Wheat, wheatCount);
                        if (eggCount > 0) inventory.Deposit(ItemType.Egg, eggCount);
                        DropAllChef();
                    }
                    cState = ChefState.Deciding;
                    break;
            }
        }
    }
}
