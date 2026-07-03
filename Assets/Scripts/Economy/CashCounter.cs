using System.Collections.Generic;
using UnityEngine;
using MiniMart.Catalog;
using MiniMart.AI;
using MiniMart.Economy;

namespace MiniMart.Economy
{
    /// <summary>
    /// One checkout point. Counter 1 sits near the opening door and is available by default;
    /// the player must staff it manually until level 2, when a cashier can be hired to run it.
    /// Counter 2 sits near the exit and unlocks (counter + cashier) at level 4.
    /// </summary>
    public class CashCounter : MonoBehaviour
    {
        public int CounterIndex; // 1 or 2
        public bool IsUnlocked;
        public bool HasCashier;
        [System.NonSerialized] public Queue<Buyer> Line = new Queue<Buyer>();

        public bool IsOpen => IsUnlocked && (HasCashier || ManualOverride);
        public bool ManualOverride; // true while the player is physically running the till

        public void RefreshUnlockState(int playerLevel)
        {
            if (CounterIndex == 1)
            {
                IsUnlocked = playerLevel >= PriceCatalog.CashCounter1UnlockLevel;
                HasCashier = playerLevel >= PriceCatalog.Cashier1AssignableLevel;
            }
            else
            {
                IsUnlocked = playerLevel >= PriceCatalog.CashCounter2UnlockLevel;
                HasCashier = IsUnlocked; // counter 2 always comes with its cashier per spec
            }
        }

        public void Enqueue(Buyer buyer) => Line.Enqueue(buyer);

        /// <summary>Processes the front of the line once per checkout tick; returns revenue taken.</summary>
        public float ProcessFront(EconomyManager economy)
        {
            if (Line.Count == 0 || !IsOpen) return 0f;
            var buyer = Line.Dequeue();

            // Charge for what the buyer actually took off the shelves. Basket is the
            // REMAINING wish-list (it empties as they shop), so quoting it charged
            // buyers for exactly the items they failed to find.
            float total = buyer.Collected.Count > 0 ? economy.QuoteBasket(buyer.Collected) : 0f;
            if (total > 0f) economy.Deposit(total);
            buyer.OnCheckedOut();
            return total;
        }
    }
}
