using System.Collections.Generic;
using MiniMart.Core;

namespace MiniMart.Catalog
{
    /// <summary>
    /// Static, designer-tunable upgrade ladders for every character role.
    /// Numbers come directly from Phase1-Core-Loop-Spec.md Section 1.
    /// </summary>
    public static class RoleCatalog
    {
        // All curves follow GDD 3.1 (levels/stacks) and GDD 6.2 (cost ladders).
        // Cost arrays are "cost to REACH this level" (level 1 = 0, it's the starting level).

        private static UpgradeCurve Build(int[] stacks, float[] speeds, int[] costs)
        {
            var curve = new UpgradeCurve();
            for (int i = 0; i < stacks.Length; i++)
                curve.Steps.Add(new UpgradeStep(i + 1, stacks[i], speeds[i], costs[i]));
            return curve;
        }

        // ---- Player: reference-flow carry scale (starts ~15, upgrades toward 44+),
        //      speed rises every level, always fastest. ----
        public static UpgradeCurve PlayerCurve() => Build(
            new[] { 15, 22, 29, 36, 44 },                     // reference video: CARRY 15 -> 44+
            // Batch 36 retune: with baseSpeed 3.0 this yields 4.2 → 6.3 u/s
            // across levels, so the Speed upgrade FEELS like an upgrade.
            new[] { 1.40f, 1.55f, 1.70f, 1.90f, 2.10f },      // always above every NPC curve below
            new[] { 0, 50, 100, 200, 500 });

        // ---- Shelver 1 & 2: stack 3 -> 5 over 5 levels, speed rises every upgrade. ----
        public static UpgradeCurve Shelver1Curve() => Build(
            new[] { 3, 3, 4, 4, 5 },
            new[] { 1.0f, 1.15f, 1.3f, 1.45f, 1.6f },
            new[] { 0, 50, 100, 200, 500 });

        public static UpgradeCurve Shelver2Curve() => Shelver1Curve();

        // ---- Chef: the premium worker. Cost ladder must top out >= shelvers (GDD 6.2). ----
        public static UpgradeCurve ChefCurve() => Build(
            new[] { 3, 4, 4, 5, 6 },
            new[] { 1.0f, 1.15f, 1.3f, 1.45f, 1.6f },
            new[] { 0, 200, 500, 1000, 2000 });

        // ---- Farmer: manages hens, wheat, tomato — chef stats, mid-tier cost. ----
        public static UpgradeCurve FarmerCurve() => Build(
            new[] { 3, 4, 4, 5, 6 },
            new[] { 1.0f, 1.15f, 1.3f, 1.45f, 1.6f },
            new[] { 0, 100, 200, 500, 1000 });

        /// <summary>What each shelver role is responsible for stocking, per spec Section 1.</summary>
        public static readonly Dictionary<RoleType, ItemType[]> RoleResponsibilities = new Dictionary<RoleType, ItemType[]>
        {
            // Bug fix: Herb, HerbPack, and FriedEgg were missing — their shelves would
            // never be restocked by NPCs. Added to balance Shelver 1 & 2 workload.
            { RoleType.Shelver1, new[] { ItemType.Egg, ItemType.TomatoKetchup, ItemType.Tomato, ItemType.Milk, ItemType.FriedEgg } },
            { RoleType.Shelver2, new[] { ItemType.Wheat, ItemType.WheatFlour, ItemType.Bread, ItemType.Cheese, ItemType.Herb, ItemType.HerbPack } },
        };

        /// <summary>
        /// Validates the design rule: "Chef upgrade ceiling must not be lower than shelver upgrade value."
        /// Call this once at boot (e.g. in editor tests) to catch tuning regressions early.
        /// </summary>
        public static bool ValidateChefVsShelverCeiling()
        {
            var chefTop = ChefCurve().GetStep(ChefCurve().MaxLevel).speedMultiplier;
            var shelverTop = Shelver1Curve().GetStep(Shelver1Curve().MaxLevel).speedMultiplier;
            return chefTop >= shelverTop;
        }
    }
}
