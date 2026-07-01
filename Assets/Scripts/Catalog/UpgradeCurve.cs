using System;
using System.Collections.Generic;

namespace MiniMart.Catalog
{
    /// <summary>
    /// One row of an upgrade ladder: capacity + speed multiplier + cost to reach this level.
    /// Used identically by characters (carry stack) and production nodes (item stack).
    /// </summary>
    [Serializable]
    public struct UpgradeStep
    {
        public int level;
        public int stackCapacity;
        public float speedMultiplier; // 1.0 = base speed
        public int upgradeCost;       // cost to go FROM previous level TO this level (0 for level 1)

        public UpgradeStep(int level, int stackCapacity, float speedMultiplier, int upgradeCost)
        {
            this.level = level;
            this.stackCapacity = stackCapacity;
            this.speedMultiplier = speedMultiplier;
            this.upgradeCost = upgradeCost;
        }
    }

    /// <summary>
    /// A full ladder of UpgradeSteps plus helpers. Static, tunable data — never mutated at runtime.
    /// </summary>
    [Serializable]
    public class UpgradeCurve
    {
        public List<UpgradeStep> Steps = new List<UpgradeStep>();

        public int MaxLevel => Steps.Count == 0 ? 1 : Steps[Steps.Count - 1].level;

        public UpgradeStep GetStep(int level)
        {
            foreach (var s in Steps)
                if (s.level == level) return s;
            // Fallback: clamp to nearest valid level.
            return level < Steps[0].level ? Steps[0] : Steps[Steps.Count - 1];
        }

        public bool CanUpgrade(int currentLevel) => currentLevel < MaxLevel;

        public int CostForNextLevel(int currentLevel)
        {
            if (!CanUpgrade(currentLevel)) return -1;
            return GetStep(currentLevel + 1).upgradeCost;
        }

        /// <summary>
        /// Builds the canonical upgrade-cost progression requested in the design doc:
        /// 50, 100, 200, 500, 1000, 2000... (each step ~2x, first jump 2x too).
        /// stepCount includes level 1 (cost 0).
        /// </summary>
        public static List<int> BuildCostProgression(int stepCount)
        {
            var costs = new List<int> { 0 }; // level 1 is free (starting level)
            int[] seed = { 50, 100, 200, 500 };
            for (int i = 1; i < stepCount; i++)
            {
                if (i - 1 < seed.Length) costs.Add(seed[i - 1]);
                else costs.Add(costs[costs.Count - 1] * 2); // continue doubling upward
            }
            return costs;
        }
    }
}
