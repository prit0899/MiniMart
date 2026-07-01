using MiniMart.Core;

namespace MiniMart.Catalog
{
    public enum MachineType { Blender, Oven, Mill }

    /// <summary>
    /// Blender (ketchup), Oven (bread), Mill (wheat flour) and the hen coop all share the
    /// same shape: min stack 4, max stack 8, levels 1-4, every upgrade increases process speed.
    /// </summary>
    public static class ProductionCatalog
    {
        public static UpgradeCurve SharedFourLevelCurve(float[] speedMultipliers)
        {
            var costs = UpgradeCurve.BuildCostProgression(4);
            var curve = new UpgradeCurve();
            int[] stacks = { 4, 5, 6, 8 }; // min 4 -> max 8 across 4 levels
            for (int i = 0; i < 4; i++)
                curve.Steps.Add(new UpgradeStep(i + 1, stacks[i], speedMultipliers[i], costs[i]));
            return curve;
        }

        public static UpgradeCurve BlenderCurve() => SharedFourLevelCurve(new[] { 1.0f, 1.2f, 1.4f, 1.6f });
        public static UpgradeCurve OvenCurve() => SharedFourLevelCurve(new[] { 1.0f, 1.2f, 1.4f, 1.6f });
        public static UpgradeCurve MillCurve() => SharedFourLevelCurve(new[] { 1.0f, 1.2f, 1.4f, 1.6f });

        /// <summary>Chickens: 2 total, same min4/max8 lvl1-4 shape, speed = lay-rate multiplier.</summary>
        public static UpgradeCurve HenCoopCurve() => SharedFourLevelCurve(new[] { 1.0f, 1.2f, 1.4f, 1.6f });

        /// <summary>Base seconds to produce one unit at level-1 speed, before the speed multiplier is applied.</summary>
        public static readonly System.Collections.Generic.Dictionary<MachineType, float> BaseProcessSeconds =
            new System.Collections.Generic.Dictionary<MachineType, float>
            {
                { MachineType.Blender, 2.0f },
                { MachineType.Oven, 3.0f },
                { MachineType.Mill, 1.5f },
            };

        public static readonly System.Collections.Generic.Dictionary<MachineType, ItemType> MachineOutput =
            new System.Collections.Generic.Dictionary<MachineType, ItemType>
            {
                { MachineType.Blender, ItemType.TomatoKetchup },
                { MachineType.Oven, ItemType.Bread },
                { MachineType.Mill, ItemType.WheatFlour },
            };

        public static readonly System.Collections.Generic.Dictionary<MachineType, ItemType> MachineInput =
            new System.Collections.Generic.Dictionary<MachineType, ItemType>
            {
                { MachineType.Blender, ItemType.Tomato },
                { MachineType.Oven, ItemType.WheatFlour },
                { MachineType.Mill, ItemType.Wheat },
            };
    }
}
