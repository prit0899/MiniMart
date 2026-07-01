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
        // ---- Player: lvl1 stack4 -> lvl5 stack7, speed rises every level, always fastest. ----
        public static UpgradeCurve PlayerCurve()
        {
            var costs = UpgradeCurve.BuildCostProgression(5); // levels 1..5
            var curve = new UpgradeCurve();
            int[] stacks = { 4, 5, 6, 6, 7 };
            float[] speeds = { 1.30f, 1.45f, 1.60f, 1.75f, 1.90f }; // always above every NPC curve below
            for (int i = 0; i < 5; i++)
                curve.Steps.Add(new UpgradeStep(i + 1, stacks[i], speeds[i], costs[i]));
            return curve;
        }

        // ---- Shelver 1: stack 3 -> 5, speed increases every upgrade. (3 levels) ----
        public static UpgradeCurve Shelver1Curve()
        {
            var costs = UpgradeCurve.BuildCostProgression(3);
            var curve = new UpgradeCurve();
            int[] stacks = { 3, 4, 5 };
            float[] speeds = { 1.0f, 1.15f, 1.3f };
            for (int i = 0; i < 3; i++)
                curve.Steps.Add(new UpgradeStep(i + 1, stacks[i], speeds[i], costs[i]));
            return curve;
        }

        // ---- Shelver 2: mirrors Shelver 1 shape (wheat / flour / bread route). ----
        public static UpgradeCurve Shelver2Curve() => Shelver1Curve();

        // ---- Chef: ceiling must be >= shelver ceiling. Give chef one extra level & higher speed cap. ----
        public static UpgradeCurve ChefCurve()
        {
            var costs = UpgradeCurve.BuildCostProgression(4);
            var curve = new UpgradeCurve();
            int[] stacks = { 3, 4, 5, 6 };
            float[] speeds = { 1.0f, 1.15f, 1.3f, 1.45f }; // top speed (1.45) >= shelver top (1.3)
            for (int i = 0; i < 4; i++)
                curve.Steps.Add(new UpgradeStep(i + 1, stacks[i], speeds[i], costs[i]));
            return curve;
        }

        // ---- Farmer: manages hens, wheat, tomato — same general shape as chef. ----
        public static UpgradeCurve FarmerCurve() => ChefCurve();

        /// <summary>What each shelver role is responsible for stocking, per spec Section 1.</summary>
        public static readonly Dictionary<RoleType, ItemType[]> RoleResponsibilities = new Dictionary<RoleType, ItemType[]>
        {
            { RoleType.Shelver1, new[] { ItemType.Egg, ItemType.TomatoKetchup, ItemType.Tomato } },
            { RoleType.Shelver2, new[] { ItemType.Wheat, ItemType.WheatFlour, ItemType.Bread } },
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
