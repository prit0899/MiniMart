using UnityEngine;
using MiniMart.Catalog;

namespace MiniMart.Production
{
    /// <summary>
    /// One cow producing milk on the hen-coop pattern: a growth slot that fills over time,
    /// collected by the player or farmer walking up. Upgrade levels 1-4 raise the milking rate.
    /// </summary>
    public class CowPen : MonoBehaviour
    {
        private GrowthSlot[] cows;
        private UpgradeCurve curve;
        public int Level { get; private set; } = 1;

        private void InitializeIfNeeded()
        {
            if (cows != null) return;
            curve = ProductionCatalog.DairyCurve();
            cows = new GrowthSlot[FarmCatalog.CowCount];
            RebuildSlots();
        }

        private void Awake() => InitializeIfNeeded();

        private void RebuildSlots()
        {
            if (cows == null) return;
            var step = curve.GetStep(Level);
            float secondsPerUnit = FarmCatalog.MilkGrowSecondsPerUnit / step.speedMultiplier;
            for (int i = 0; i < cows.Length; i++)
            {
                int carryOver = cows[i]?.Count ?? 0;
                cows[i] = new GrowthSlot(FarmCatalog.MilkMaxPerCow, secondsPerUnit);
                cows[i].Count = carryOver;
            }
        }

        public void ApplyLevel(int newLevel)
        {
            InitializeIfNeeded();
            Level = Mathf.Clamp(newLevel, 1, curve.MaxLevel);
            RebuildSlots();
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

        private void Update()
        {
            InitializeIfNeeded();
            foreach (var c in cows) c.Tick(Time.deltaTime);
        }

        public int TotalMilkReady()
        {
            InitializeIfNeeded();
            int sum = 0;
            foreach (var c in cows) sum += c.Count;
            return sum;
        }

        public int Collect(int amount)
        {
            InitializeIfNeeded();
            int total = 0;
            while (total < amount)
            {
                GrowthSlot best = null;
                foreach (var c in cows)
                    if (c.Count > 0 && (best == null || c.Count > best.Count)) best = c;
                if (best == null) break;
                total += best.Take(1);
            }
            return total;
        }
    }
}
