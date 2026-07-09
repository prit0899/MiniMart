using System.Collections.Generic;
using MiniMart.Core;

namespace MiniMart.Catalog
{
    /// <summary>Max units a storage bin can hold per item (Phase1-Core-Loop-Spec Section 4).</summary>
    public static class StorageCatalog
    {
        public static readonly Dictionary<ItemType, int> MaxStorage = new Dictionary<ItemType, int>
        {
            { ItemType.Wheat, 15 },
            { ItemType.Bread, 15 },
            { ItemType.Dough, 15 },
            { ItemType.Tomato, 15 },
            { ItemType.CannedTomato, 15 },
            { ItemType.Egg, 15 },
            { ItemType.Milk, 15 },
            { ItemType.BottledMilk, 15 },
            { ItemType.Corn, 15 },
            { ItemType.ProcessedCorn, 15 },
            { ItemType.CookieDough, 15 },
            { ItemType.Cookie, 15 },
            { ItemType.Apple, 15 },

            // Extended production chain storage caps (Blender/Mill/Dairy/
            // LeafProcessor/Stove/Coffee chain).
            { ItemType.TomatoKetchup, 15 },
            { ItemType.WheatFlour, 15 },
            { ItemType.Cheese, 15 },
            { ItemType.Herb, 15 },
            { ItemType.HerbPack, 15 },
            { ItemType.FriedEgg, 15 },
            { ItemType.Coffee, 15 },
        };

        public const int DustbinCount = 2;
    }

    /// <summary>
    /// Tomato farm: 2 columns x 3 rows = 6 plants, each holding up to 3 tomatoes,
    /// growing 1 tomato every 0.5s. Eggs reuse the identical per-slot growth cycle.
    /// Wheat farm: 3x4 grid, each box holds exactly 1 wheat (binary grown/not-grown).
    /// </summary>
    public static class FarmCatalog
    {
        // Tomato
        public const int TomatoCols = 2;
        public const int TomatoRows = 3;
        public const int TomatoPlantCount = TomatoCols * TomatoRows; // 6
        public const int TomatoMaxPerPlant = 3;
        public const float TomatoGrowSecondsPerUnit = 0.5f;

        // Eggs: same per-slot cadence as tomato, but slot count is tied to hen count (2 hens),
        // each hen acting as one growth "plant" with the same max-per-slot/grow-rate shape.
        public const int HenCount = 2;
        public const int EggMaxPerHen = TomatoMaxPerPlant; // mirrors tomato-style cycle
        public const float EggGrowSecondsPerUnit = TomatoGrowSecondsPerUnit;

        // Cow pen: one cow behaving like a hen (same slot-growth shape, slower cadence).
        public const int CowCount = 1;
        public const int MilkMaxPerCow = 3;
        public const float MilkGrowSecondsPerUnit = 2.0f;

        // Wheat
        public const int WheatCols = 3;
        public const int WheatRows = 4;
        public const int WheatBoxCount = WheatCols * WheatRows; // 12
        public const int WheatMaxPerBox = 1;
        public const float WheatGrowSecondsPerUnit = 1.0f; // tunable; one wheat per box per cycle

        // Herb patch: 4 bushes, up to 3 leaves each, slower regrowth (mid-game specialty crop).
        public const int HerbBushCount = 4;
        public const int HerbMaxPerBush = 3;
        public const float HerbGrowSecondsPerUnit = 1.2f;

        // Corn field (Agriculture East). Spec: "Up to 15+ purchasable plots" —
        // ship 4x3 = 12 stalks by default, upgradeable via purchase pads.
        public const int CornRows = 4;
        public const int CornCols = 3;
        public const int CornStalkCount = CornRows * CornCols;
        public const int CornMaxPerStalk = 3;
        public const float CornGrowSecondsPerUnit = 0.8f;

        // Apple orchard (plan.md §2 Agriculture East "apple trees"). Filled the
        // dead-SKU gap found in the batch-34 playthrough: Apple had a shelf,
        // rack and price but no source anywhere.
        public const int AppleTreeCount = 4;
        public const int AppleMaxPerTree = 3;
        public const float AppleGrowSecondsPerUnit = 1.0f;
    }
}
