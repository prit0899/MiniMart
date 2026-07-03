using UnityEngine;
using MiniMart.Catalog;

namespace MiniMart.Production
{
    /// <summary>
    /// 2 hens, each behaving like a tomato plant: caps at EggMaxPerHen, grows on the same
    /// per-unit cadence. Upgrade levels 1-4 raise speedMultiplier and thus effective lay rate.
    /// </summary>
    public class HenCoop : MonoBehaviour
    {
        private GrowthSlot[] hens;
        private UpgradeCurve curve;
        public int Level { get; private set; } = 1;

        private void InitializeIfNeeded()
        {
            if (hens != null) return;
            curve = ProductionCatalog.HenCoopCurve();
            hens = new GrowthSlot[FarmCatalog.HenCount];
            RebuildSlots();
        }

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void RebuildSlots()
        {
            if (hens == null) return;
            var step = curve.GetStep(Level);
            float secondsPerUnit = FarmCatalog.EggGrowSecondsPerUnit / step.speedMultiplier;
            for (int i = 0; i < hens.Length; i++)
            {
                int carryOver = hens[i]?.Count ?? 0;
                hens[i] = new GrowthSlot(FarmCatalog.EggMaxPerHen, secondsPerUnit);
                hens[i].Count = carryOver;
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
            foreach (var h in hens) h.Tick(Time.deltaTime);
        }

        public int TotalEggsReady()
        {
            InitializeIfNeeded();
            int sum = 0;
            foreach (var h in hens) sum += h.Count;
            return sum;
        }

        public int Collect(int amount)
        {
            InitializeIfNeeded();
            int total = 0;
            while (total < amount)
            {
                GrowthSlot best = null;
                foreach (var h in hens)
                    if (h.Count > 0 && (best == null || h.Count > best.Count)) best = h;
                if (best == null) break;
                total += best.Take(1);
            }
            return total;
        }
    }
}
