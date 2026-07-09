using UnityEngine;
using MiniMart.Catalog;

namespace MiniMart.Production
{
    /// <summary>
    /// Herb / leafy-greens patch (GDD 5.1 mid-game specialty crop): 4 bushes, up to 3
    /// leaves each, regrowing on a slower cadence than tomatoes. Same GrowthSlot cycle
    /// as the tomato farm so all farm logic stays uniform.
    /// </summary>
    public class HerbPatch : MonoBehaviour
    {
        private GrowthSlot[] bushes;

        private void InitializeIfNeeded()
        {
            if (bushes != null) return;
            bushes = new GrowthSlot[FarmCatalog.HerbBushCount];
            for (int i = 0; i < bushes.Length; i++)
                bushes[i] = new GrowthSlot(FarmCatalog.HerbMaxPerBush, FarmCatalog.HerbGrowSecondsPerUnit);
        }

        private void Awake() => InitializeIfNeeded();

        private void Update()
        {
            InitializeIfNeeded();
            foreach (var b in bushes) b.Tick(Time.deltaTime);
        }

        /// <summary>Harvests up to `amount` leaves, fullest bush first.</summary>
        public int Harvest(int amount)
        {
            InitializeIfNeeded();
            int total = 0;
            while (total < amount)
            {
                GrowthSlot best = null;
                foreach (var b in bushes)
                    if (b.Count > 0 && (best == null || b.Count > best.Count)) best = b;
                if (best == null) break;
                total += best.Take(1);
            }
            return total;
        }

        public int TotalRipe()
        {
            InitializeIfNeeded();
            int sum = 0;
            foreach (var b in bushes) sum += b.Count;
            return sum;
        }
    }
}
