using UnityEngine;
using MiniMart.Core;
using MiniMart.Catalog;
using MiniMart.Economy;

namespace MiniMart.Characters
{
    /// <summary>
    /// NPC cashier who stands at an assigned CashCounter. They process the queue each sim tick
    /// once the counter is unlocked and the player's level qualifies for a cashier at that counter.
    /// </summary>
    public class Cashier : CharacterBase
    {
        public CashCounter AssignedCounter;

        protected override void Awake()
        {
            Role = RoleType.Cashier;
            Curve = null; // cashiers don't level up
            baseSpeed = 0f; // they stand still
        }

        public void AssignTo(CashCounter counter)
        {
            AssignedCounter = counter;
            // Stand BEHIND the till (buyers queue on the -z side), not inside it.
            if (counter != null)
                transform.position = counter.transform.position + new Vector3(0, 0, 0.95f);
        }

        public override void Tick(float dt)
        {
            // Actual checkout is driven by CashCounter.ProcessFront via GameManager.SimTick.
            // The Cashier just needs to be present and visible.
            State = AssignedCounter != null && AssignedCounter.Line.Count > 0
                ? CharacterState.Selling
                : CharacterState.Idle;
        }
    }
}
