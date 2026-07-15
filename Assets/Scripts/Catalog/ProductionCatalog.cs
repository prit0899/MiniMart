using MiniMart.Core;

namespace MiniMart.Catalog
{
    public enum MachineType
    {
        TomatoCanner,
        CornProcessor,
        DoughMixer,
        Oven,
        MilkBottler,

        // Extended production chain — referenced by Machine.cs/CowPen.cs/
        // SceneBootstrapper's Gate() calls but previously missing from this
        // enum, which broke the whole assembly build.
        Blender,
        Mill,
        Dairy,
        LeafProcessor,
        Stove,
        CookieStation,
        CoffeeDispenser
    }

    public static class ProductionCatalog
    {
        public static UpgradeCurve SharedFourLevelCurve(float[] speedMultipliers)
        {
            var costs = UpgradeCurve.BuildCostProgression(4);
            var curve = new UpgradeCurve();
            int[] stacks = { 4, 5, 6, 8 }; 
            for (int i = 0; i < 4; i++)
                curve.Steps.Add(new UpgradeStep(i + 1, stacks[i], speedMultipliers[i], costs[i]));
            return curve;
        }

        public static UpgradeCurve TomatoCannerCurve() => SharedFourLevelCurve(new[] { 1.0f, 1.2f, 1.4f, 1.6f });
        public static UpgradeCurve CornProcessorCurve() => SharedFourLevelCurve(new[] { 1.0f, 1.2f, 1.4f, 1.6f });
        public static UpgradeCurve DoughMixerCurve() => SharedFourLevelCurve(new[] { 1.0f, 1.2f, 1.4f, 1.6f });
        public static UpgradeCurve OvenCurve() => SharedFourLevelCurve(new[] { 1.0f, 1.2f, 1.4f, 1.6f });
        public static UpgradeCurve MilkBottlerCurve() => SharedFourLevelCurve(new[] { 1.0f, 1.2f, 1.4f, 1.6f });

        public static UpgradeCurve HenCoopCurve() => SharedFourLevelCurve(new[] { 1.0f, 1.2f, 1.4f, 1.6f });

        // Extended production chain curves — same shared 4-level shape as every
        // other machine; distinct methods so Machine.cs's per-type switch reads
        // cleanly and future balancing can diverge per-machine if needed.
        public static UpgradeCurve BlenderCurve()        => SharedFourLevelCurve(new[] { 1.0f, 1.2f, 1.4f, 1.6f });
        public static UpgradeCurve MillCurve()            => SharedFourLevelCurve(new[] { 1.0f, 1.2f, 1.4f, 1.6f });
        public static UpgradeCurve DairyCurve()           => SharedFourLevelCurve(new[] { 1.0f, 1.2f, 1.4f, 1.6f });
        public static UpgradeCurve LeafProcessorCurve()   => SharedFourLevelCurve(new[] { 1.0f, 1.2f, 1.4f, 1.6f });
        public static UpgradeCurve StoveCurve()           => SharedFourLevelCurve(new[] { 1.0f, 1.2f, 1.4f, 1.6f });
        public static UpgradeCurve CookieStationCurve()   => SharedFourLevelCurve(new[] { 1.0f, 1.2f, 1.4f, 1.6f });
        public static UpgradeCurve CoffeeDispenserCurve() => SharedFourLevelCurve(new[] { 1.0f, 1.2f, 1.4f, 1.6f });

        /// <summary>Base seconds to produce one unit at level-1 speed.</summary>
        public static readonly System.Collections.Generic.Dictionary<MachineType, float> BaseProcessSeconds =
            new System.Collections.Generic.Dictionary<MachineType, float>
            {
                { MachineType.TomatoCanner, 4.0f },
                { MachineType.CornProcessor, 4.0f },
                { MachineType.DoughMixer, 5.0f },
                { MachineType.Oven, 5.0f },
                { MachineType.MilkBottler, 4.0f },
                { MachineType.Blender, 4.0f },
                { MachineType.Mill, 4.0f },
                { MachineType.Dairy, 5.0f },
                { MachineType.LeafProcessor, 4.0f },
                { MachineType.Stove, 3.0f },
                { MachineType.CookieStation, 8.0f },
                { MachineType.CoffeeDispenser, 3.0f }
            };

        public static readonly System.Collections.Generic.Dictionary<MachineType, ItemType> MachineOutput =
            new System.Collections.Generic.Dictionary<MachineType, ItemType>
            {
                { MachineType.TomatoCanner, ItemType.CannedTomato },
                { MachineType.CornProcessor, ItemType.ProcessedCorn },
                { MachineType.DoughMixer, ItemType.Dough },
                { MachineType.Oven, ItemType.Bread },
                { MachineType.MilkBottler, ItemType.BottledMilk },
                { MachineType.Blender, ItemType.TomatoKetchup },
                { MachineType.Mill, ItemType.WheatFlour },
                { MachineType.Dairy, ItemType.Cheese },
                { MachineType.LeafProcessor, ItemType.HerbPack },
                { MachineType.Stove, ItemType.FriedEgg },
                { MachineType.CookieStation, ItemType.Cookie },
                { MachineType.CoffeeDispenser, ItemType.Coffee }
            };

        public static readonly System.Collections.Generic.Dictionary<MachineType, ItemType> MachineInput =
            new System.Collections.Generic.Dictionary<MachineType, ItemType>
            {
                { MachineType.TomatoCanner, ItemType.Tomato },
                { MachineType.CornProcessor, ItemType.Corn },
                { MachineType.DoughMixer, ItemType.Wheat },
                // Reference chain: Mill turns Wheat into WheatFlour, and the Oven
                // bakes WheatFlour (+ Egg, handled directly by Chef.cs) into Bread —
                // not the old Dough pipeline. DoughMixer/Dough stay available as a
                // separate, still-functioning legacy chain.
                { MachineType.Oven, ItemType.WheatFlour },
                { MachineType.MilkBottler, ItemType.Milk },
                { MachineType.Blender, ItemType.Tomato },
                { MachineType.Mill, ItemType.Wheat },
                { MachineType.Dairy, ItemType.Milk },
                { MachineType.LeafProcessor, ItemType.Herb },
                { MachineType.Stove, ItemType.Egg }
                // CoffeeDispenser deliberately has no input entry — it's an
                // auto-producer (Machine.IsAutoProducer), matching the earlier fix
                // that removed its self-referencing Coffee->Coffee input.
            };
    }
}
