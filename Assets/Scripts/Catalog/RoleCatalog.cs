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

        // ---- Player: reference-flow carry scale. Starts limited but USABLE (8)
        //      so the carry upgrade is a real, felt loop — the reference game's
        //      core hook — while the top reaches 44 (reference "44+", and the
        //      DataValidator requires MaxCarry >= 44). Owner feedback
        //      (2026-07-14): base 15 let you max the game without upgrading
        //      (pads pointless); a too-small base of 5 read as "can't carry
        //      anything". 8 → 44 keeps the limit felt AND playable, with a big
        //      5.5× payoff by the top. Speed still rises every level and the
        //      player stays the fastest character (base 4.6 × 1.20 = 5.52 u/s
        //      already beats every NPC's absolute max of 3.0 × 1.6 = 4.8 u/s). ----
        public static UpgradeCurve PlayerCurve() => Build(
            new[] { 8, 16, 25, 34, 44 },                      // CARRY 8 -> 44
            new[] { 1.20f, 1.45f, 1.70f, 1.90f, 2.15f },      // always above every NPC curve below
            new[] { 0, 50, 150, 300, 600});

        // ---- Shelver 1 & 2: stack 3 -> 5 over 5 levels, speed rises every upgrade. ----
        public static UpgradeCurve Shelver1Curve() => Build(
            new[] { 3, 4, 5, 6, 7},
            new[] { 1.0f, 1.15f, 1.3f, 1.45f, 1.6f },
            new[] { 0, 50, 100, 200, 500 });

        public static UpgradeCurve Shelver2Curve() => Shelver1Curve();

        // ---- Chef: the premium worker. Cost ladder must top out >= shelvers (GDD 6.2). ----
        public static UpgradeCurve ChefCurve() => Build(
            new[] { 3, 4, 5, 6, 7 },
            new[] { 1.0f, 1.15f, 1.3f, 1.45f, 1.6f },
            new[] { 0, 200, 500, 1000, 2000 });

        // ---- Farmer: manages hens, wheat, tomato — chef stats, mid-tier cost. ----
        public static UpgradeCurve FarmerCurve() => Build(
            new[] { 3, 4, 5, 6, 7 },
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
