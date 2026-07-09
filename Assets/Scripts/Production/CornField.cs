using UnityEngine;
using MiniMart.Catalog;

namespace MiniMart.Production
{
    /// <summary>
    /// Agriculture-East corn field (new map spec §"Agriculture East"). Rows of
    /// tall stalks, each stalk holds up to 3 cobs. Grows on a slower cadence
    /// than tomato but faster than wheat. Serviced by the player, farmer, and
    /// (optionally) the Assistant Node auto-harvester.
    /// </summary>
    public class CornField : MonoBehaviour
    {
        private GrowthSlot[] stalks;

        private void InitializeIfNeeded()
        {
            if (stalks != null) return;
            stalks = new GrowthSlot[FarmCatalog.CornStalkCount];
            for (int i = 0; i < stalks.Length; i++)
                stalks[i] = new GrowthSlot(FarmCatalog.CornMaxPerStalk, FarmCatalog.CornGrowSecondsPerUnit);
        }

        private void Awake() => InitializeIfNeeded();

        private void Update()
        {
            InitializeIfNeeded();
            foreach (var s in stalks) s.Tick(Time.deltaTime);
        }

        /// <summary>Harvest up to `amount` cobs, greedy from fullest stalk.</summary>
        public int Harvest(int amount)
        {
            InitializeIfNeeded();
            int total = 0;
            while (total < amount)
            {
                GrowthSlot best = null;
                foreach (var s in stalks)
                    if (s.Count > 0 && (best == null || s.Count > best.Count)) best = s;
                if (best == null) break;
                total += best.Take(1);
            }
            return total;
        }

        public int TotalRipe()
        {
            InitializeIfNeeded();
            int sum = 0;
            foreach (var s in stalks) sum += s.Count;
            return sum;
        }
    }
}
