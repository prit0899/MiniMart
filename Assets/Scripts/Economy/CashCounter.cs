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
        [System.NonSerialized] public List<Buyer> Line = new List<Buyer>();

        public bool IsOpen => IsUnlocked && (HasCashier || ManualOverride);
        public bool ManualOverride; // true while the player is physically running the till

        /// <summary>GDD 4: seconds per checkout; the player manning the till works 2x faster.</summary>
        public float SecondsPerCheckout = 2.0f;
        private float sinceLastCheckout;

        private Characters.Cashier cashierVisual;

        public void RefreshUnlockState(int playerLevel)
        {
            switch (CounterIndex)
            {
                case 1:
                    IsUnlocked = playerLevel >= PriceCatalog.CashCounter1UnlockLevel;
                    HasCashier = playerLevel >= PriceCatalog.Cashier1AssignableLevel;
                    break;
                case 2:
                    IsUnlocked = playerLevel >= PriceCatalog.CashCounter2UnlockLevel;
                    HasCashier = IsUnlocked; // counter 2 always comes with its cashier per spec
                    break;
                case 3:
                    IsUnlocked = playerLevel >= PriceCatalog.CashCounter3UnlockLevel;
                    HasCashier = IsUnlocked;
                    break;
                default: // 4+
                    IsUnlocked = playerLevel >= PriceCatalog.CashCounter4UnlockLevel;
                    HasCashier = IsUnlocked;
                    break;
            }

            // HasCashier was only ever a bool — no character existed in the world.
            // Spawn a visible cashier behind the till when one is hired.
            if (HasCashier && cashierVisual == null)
            {
                var go = new GameObject($"Cashier_{CounterIndex}");
                go.transform.position = transform.position + new Vector3(0, 0, 0.95f);
                go.transform.rotation = Quaternion.Euler(0, 180f, 0); // Face the buyers (queue is at -Z)
                cashierVisual = go.AddComponent<Characters.Cashier>();
                cashierVisual.AssignTo(this);
                go.AddComponent<Engine.WobbleAnimator>();
                // Cashier variant: orange uniform + name tag on chest + chestnut hair.
                Engine.PrimitiveFactory.BuildCharacter(go, new Color(0.95f, 0.55f, 0.20f),
                    Engine.PrimitiveFactory.CharacterRole.Cashier);
            }
            else if (!HasCashier && cashierVisual != null)
            {
                Destroy(cashierVisual.gameObject);
                cashierVisual = null;
            }
        }

        /// <summary>World position for the Nth buyer in line — a tidy row in front of the till
        /// instead of everyone stacking on one point.</summary>
        public Vector3 GetQueueSlot(int index) =>
            transform.position + new Vector3(0, 0, -1.15f * (index + 1));

        public void Enqueue(Buyer buyer) { if (!Line.Contains(buyer)) Line.Add(buyer); }

        /// <summary>Impatient buyers abandon the queue; safe if the buyer isn't in line.</summary>
        public void RemoveFromLine(Buyer buyer) => Line.Remove(buyer);

        /// <summary>Processes the front of the line on a real-time cadence; returns revenue taken.</summary>
        public float ProcessFront(EconomyManager economy, float dt)
        {
            sinceLastCheckout += dt;
            if (Line.Count == 0 || !IsOpen) return 0f;

            // Player at the till = 2x checkout speed (GDD 4 "manual checkout override").
            float needed = ManualOverride ? SecondsPerCheckout / 2f : SecondsPerCheckout;
            if (sinceLastCheckout < needed) return 0f;
            sinceLastCheckout = 0f;

            var buyer = Line[0];
            if (buyer == null) return 0f;
            if (!buyer.IsReadyForCheckout(this)) return 0f;
            Line.RemoveAt(0);

            // Charge for what the buyer actually took off the shelves. Basket is the
            // REMAINING wish-list (it empties as they shop), so quoting it charged
            // buyers for exactly the items they failed to find.
            float total = buyer.Collected.Count > 0 ? economy.QuoteBasket(buyer.Collected) : 0f;

            // Satisfied customers (found everything on their list) sometimes tip 10-25%.
            bool tipped = false;
            if (total > 0f && buyer.Basket.Count == 0 && Random.value < 0.20f)
            {
                total += total * Random.Range(0.10f, 0.25f);
                tipped = true;
            }

            // Reference feedback: happy face on every sale, a heart when they tipped.
            if (total > 0f)
            {
                Engine.Emote.Happy(buyer.transform.position);
                if (tipped) Engine.Emote.Heart(buyer.transform.position + new Vector3(0.4f, 0.3f, 0));

                // Floating text showing cash charged to make transaction flow obvious
                Engine.Emote.Spawn(transform.position + Vector3.up * 1.5f, $"+${total:F0}", new Color(0.15f, 0.6f, 0.15f));

                Engine.AudioFx.Sale();
            }

            // Reference flow: revenue is NOT auto-banked — it piles up as a physical money
            // stack beside the till, and the player walks over it to collect (cash + XP).
            if (total > 0f)
                Engine.MoneyStack.SpawnOrMerge(transform.position + new Vector3(1.1f, 0, -0.4f), total);

            buyer.OnCheckedOut();
            // Feeds the rotating mini-goals ("Serve N customers") in Retention.cs.
            if (GameManager.Instance != null) GameManager.Instance.CustomersServed++;
            return total;
        }
    }
}
