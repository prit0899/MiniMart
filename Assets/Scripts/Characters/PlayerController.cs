using UnityEngine;
using MiniMart.Core;
using MiniMart.Catalog;
using MiniMart.Economy;
using MiniMart.AI;

namespace MiniMart.Characters
{
    /// <summary>
    /// The only controllable worker. Always the fastest character in the scene (enforced by
    /// RoleCatalog.PlayerCurve having a higher speedMultiplier at every level than any NPC curve).
    /// Only the player can catch the Thief, and only while it is inside the store boundary.
    /// </summary>
    public class PlayerController : CharacterBase
    {
        public bool IsPaused { get; private set; }
        public NetTool Net;

        protected override void Awake()
        {
            Role = RoleType.Player;
            Curve = RoleCatalog.PlayerCurve();
            base.Awake();
        }

        public override void Tick(float dt)
        {
            if (IsPaused) return;
            base.Tick(dt);
        }

        // --- Player-only management actions (Section 1: "pause, stop, adjust prices, adjust stock, create offers") ---

        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;

        public void StopCurrentTask()
        {
            hasTarget = false;
            State = CharacterState.Idle;
        }

        public void AdjustPrice(EconomyManager economy, ItemType item, float newPrice) =>
            economy.SetManualPrice(item, newPrice);

        public void AdjustStockTarget(ProductionManagerStub stub, ItemType item, int desiredCount) =>
            stub?.SetRestockTarget(item, desiredCount);

        public void CreateOffer(EconomyManager economy, ItemType item, float discountPercent) =>
            economy.CreateOffer(item, discountPercent);

        // --- Theft handling ---

        /// <summary>Call when the player overlaps a Thief while the thief is still inside the store.</summary>
        public bool TryCatchThief(Thief thief)
        {
            if (thief == null || thief.HasLeftStore) return false;
            Net?.Throw();
            thief.Catch();
            return true;
        }

        /// <summary>Cash counter manning rule: from level 2 the player no longer needs to staff counter 1.</summary>
        public bool NeedsToManCounter1 => Level < PriceCatalog.Cashier1AssignableLevel;
    }

    /// <summary>Minimal placeholder hook so PlayerController compiles standalone; ProductionSystem
    /// implements the real SetRestockTarget logic against StoreInventory targets.</summary>
    public abstract class ProductionManagerStub
    {
        public abstract void SetRestockTarget(ItemType item, int desiredCount);
    }
}
