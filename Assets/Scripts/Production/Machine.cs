using UnityEngine;
using MiniMart.Catalog;

namespace MiniMart.Production
{
    /// <summary>
    /// One processing machine (Blender/Oven/Mill). Stack capacity and speed scale with
    /// level 1-4 per ProductionCatalog.SharedFourLevelCurve. Converts InputItem -> OutputItem.
    /// </summary>
    public class Machine : MonoBehaviour
    {
        public MachineType Type;
        public int Level = 1;
        public int InputQueued;
        public int OutputReady;
        private float processTimer;
        private UpgradeCurve curve;
        private UpgradeStep step;

        private void InitializeIfNeeded()
        {
            if (curve != null) return;
            curve = Type switch
            {
                MachineType.Blender => ProductionCatalog.BlenderCurve(),
                MachineType.Oven => ProductionCatalog.OvenCurve(),
                MachineType.Mill => ProductionCatalog.MillCurve(),
                MachineType.Dairy => ProductionCatalog.DairyCurve(),
                _ => ProductionCatalog.BlenderCurve(),
            };
            ApplyLevel(1);
        }

        private void Awake()
        {
            InitializeIfNeeded();
        }

        public void ApplyLevel(int level)
        {
            InitializeIfNeeded();
            Level = Mathf.Clamp(level, 1, curve.MaxLevel);
            step = curve.GetStep(Level);
        }

        /// <summary>Cost of the next level, or -1 at max. Read-only — safe for UI polling.</summary>
        public int NextUpgradeCost
        {
            get
            {
                InitializeIfNeeded();
                return curve.CostForNextLevel(Level);
            }
        }

        public bool TryUpgrade(out int cost)
        {
            InitializeIfNeeded();
            cost = curve.CostForNextLevel(Level);
            if (cost < 0) return false;
            ApplyLevel(Level + 1);
            return true;
        }

        public int StackCapacity
        {
            get
            {
                InitializeIfNeeded();
                return step.stackCapacity;
            }
        }

        /// <summary>Adds raw ingredient to the input queue, capped at the machine's stack capacity.</summary>
        public void LoadInput(int amount)
        {
            InputQueued = Mathf.Min(StackCapacity, InputQueued + amount);
        }

        private void Update()
        {
            InitializeIfNeeded();
            if (InputQueued <= 0) return;

            // Output tray full: stall processing instead of consuming input and silently
            // DESTROYING the product (OutputReady was clamped, losing one item per cycle).
            if (OutputReady >= StackCapacity) return;

            float baseSeconds = ProductionCatalog.BaseProcessSeconds[Type];
            float secondsPerUnit = baseSeconds / step.speedMultiplier;

            processTimer += Time.deltaTime;
            if (processTimer >= secondsPerUnit)
            {
                processTimer = 0f;
                InputQueued -= 1;
                OutputReady = Mathf.Min(StackCapacity, OutputReady + 1);
            }
        }

        /// <summary>Workers call this to pull finished product out of the machine into their hands/storage.</summary>
        public int CollectFinished()
        {
            int amount = OutputReady;
            OutputReady = 0;
            return amount;
        }
    }
}
