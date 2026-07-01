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
            { ItemType.Bread, 12 },
            { ItemType.WheatFlour, 12 },
            { ItemType.Tomato, 15 },
            { ItemType.TomatoKetchup, 15 },
            { ItemType.Egg, 15 },
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

        // Wheat
        public const int WheatCols = 3;
        public const int WheatRows = 4;
        public const int WheatBoxCount = WheatCols * WheatRows; // 12
        public const int WheatMaxPerBox = 1;
        public const float WheatGrowSecondsPerUnit = 1.0f; // tunable; one wheat per box per cycle
    }
}
