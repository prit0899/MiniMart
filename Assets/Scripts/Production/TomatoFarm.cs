using UnityEngine;
using MiniMart.Catalog;

namespace MiniMart.Production
{
    /// <summary>One growth slot. Reused identically for tomato plants and egg-laying hens.</summary>
    public class GrowthSlot
    {
        public int Count;
        public readonly int MaxCount;
        public readonly float SecondsPerUnit;
        private float timer;

        public GrowthSlot(int maxCount, float secondsPerUnit)
        {
            MaxCount = maxCount;
            SecondsPerUnit = secondsPerUnit;
        }

        public void Tick(float dt)
        {
            if (Count >= MaxCount) return;
            timer += dt;
            if (timer >= SecondsPerUnit)
            {
                timer -= SecondsPerUnit;
                Count = Mathf.Min(MaxCount, Count + 1);
            }
        }

        public int Take(int amount)
        {
            int taken = Mathf.Min(amount, Count);
            Count -= taken;
            return taken;
        }
    }

    /// <summary>2 columns x 3 rows = 6 plants, each capped at 3 tomatoes, growing 1 every 0.5s.</summary>
    public class TomatoFarm : MonoBehaviour
    {
        private GrowthSlot[] plants;

        private void InitializeIfNeeded()
        {
            if (plants != null) return;
            plants = new GrowthSlot[FarmCatalog.TomatoPlantCount];
            for (int i = 0; i < plants.Length; i++)
                plants[i] = new GrowthSlot(FarmCatalog.TomatoMaxPerPlant, FarmCatalog.TomatoGrowSecondsPerUnit);
        }

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void Update()
        {
            InitializeIfNeeded();
            foreach (var p in plants) p.Tick(Time.deltaTime);
        }

        /// <summary>Harvests up to `amount` tomatoes, drawing from whichever plants are most ripe first.</summary>
        public int Harvest(int amount)
        {
            InitializeIfNeeded();
            int total = 0;
            // Greedy: always take from the fullest plant first.
            while (total < amount)
            {
                GrowthSlot best = null;
                foreach (var p in plants)
                    if (p.Count > 0 && (best == null || p.Count > best.Count)) best = p;
                if (best == null) break;
                total += best.Take(1);
            }
            return total;
        }

        public int TotalRipe()
        {
            int sum = 0;
            foreach (var p in plants) sum += p.Count;
            return sum;
        }
    }
}
