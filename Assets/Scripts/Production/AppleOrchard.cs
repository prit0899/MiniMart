using UnityEngine;
using MiniMart.Catalog;

namespace MiniMart.Production
{
    /// <summary>
    /// Apple orchard: a small stand of trees, each ripening apples on the same
    /// GrowthSlot cycle used by every other farm source. Fills the dead-SKU gap
    /// found in the batch-34 playthrough — Apple had a shelf, a rack and a price
    /// but no production source anywhere in the game.
    /// </summary>
    public class AppleOrchard : MonoBehaviour
    {
        private GrowthSlot[] trees;

        private void InitializeIfNeeded()
        {
            if (trees != null) return;
            trees = new GrowthSlot[FarmCatalog.AppleTreeCount];
            for (int i = 0; i < trees.Length; i++)
                trees[i] = new GrowthSlot(FarmCatalog.AppleMaxPerTree, FarmCatalog.AppleGrowSecondsPerUnit);
        }

        private void Awake() => InitializeIfNeeded();

        private void Update()
        {
            InitializeIfNeeded();
            foreach (var t in trees) t.Tick(Time.deltaTime);
        }

        /// <summary>Harvest up to `amount` apples, ripest trees first.</summary>
        public int Harvest(int amount)
        {
            InitializeIfNeeded();
            int total = 0;
            while (total < amount)
            {
                GrowthSlot best = null;
                foreach (var t in trees)
                    if (t.Count > 0 && (best == null || t.Count > best.Count)) best = t;
                if (best == null) break;
                total += best.Take(1);
            }
            return total;
        }

        public int TotalRipe()
        {
            InitializeIfNeeded();
            int sum = 0;
            foreach (var t in trees) sum += t.Count;
            return sum;
        }
    }
}
