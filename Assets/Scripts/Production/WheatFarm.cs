using UnityEngine;
using MiniMart.Catalog;

namespace MiniMart.Production
{
    /// <summary>3x4 grid (12 boxes), each box holds exactly 1 wheat — a simpler binary growth slot.</summary>
    public class WheatFarm : MonoBehaviour
    {
        private bool[] boxes;     // true = wheat ready to harvest
        private float[] timers;

        private void InitializeIfNeeded()
        {
            if (boxes != null) return;
            boxes = new bool[FarmCatalog.WheatBoxCount];
            timers = new float[FarmCatalog.WheatBoxCount];
        }

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void Update()
        {
            InitializeIfNeeded();
            for (int i = 0; i < boxes.Length; i++)
            {
                if (boxes[i]) continue;
                timers[i] += Time.deltaTime;
                if (timers[i] >= FarmCatalog.WheatGrowSecondsPerUnit)
                {
                    timers[i] = 0f;
                    boxes[i] = true;
                }
            }
        }

        public int Harvest(int amount)
        {
            InitializeIfNeeded();
            int total = 0;
            for (int i = 0; i < boxes.Length && total < amount; i++)
            {
                if (!boxes[i]) continue;
                boxes[i] = false;
                total++;
            }
            return total;
        }

        public int ReadyCount()
        {
            InitializeIfNeeded();
            int c = 0;
            foreach (var b in boxes) if (b) c++;
            return c;
        }
    }
}
