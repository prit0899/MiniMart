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
        private int level = 1;

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
            var step = curve.GetStep(level);
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
            level = Mathf.Clamp(newLevel, 1, curve.MaxLevel);
            RebuildSlots();
        }

        public bool TryUpgrade(out int cost)
        {
            InitializeIfNeeded();
            cost = curve.CostForNextLevel(level);
            if (cost < 0) return false;
            ApplyLevel(level + 1);
            return true;
        }

        private void Update()
        {
            InitializeIfNeeded();
            foreach (var h in hens) h.Tick(Time.deltaTime);
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
