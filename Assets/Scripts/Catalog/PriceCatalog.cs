using System.Collections.Generic;
using MiniMart.Core;

namespace MiniMart.Catalog
{
    /// <summary>
    /// Per-unit base sell prices and the level at which each item/system unlocks.
    /// Hard rule from the design doc: a "full basket/bundle" must never total under $1,
    /// so unit prices are kept well above $0.15 each even at the cheapest tier.
    /// </summary>
    public static class PriceCatalog
    {
        public static readonly Dictionary<ItemType, float> BasePrice = new Dictionary<ItemType, float>
        {
            { ItemType.Egg, 0.40f },
            { ItemType.Tomato, 0.35f },
            { ItemType.TomatoKetchup, 1.20f },
            { ItemType.Wheat, 0.30f },
            { ItemType.WheatFlour, 0.80f },
            { ItemType.Bread, 1.50f },
            { ItemType.Milk, 0.50f },
            { ItemType.Cheese, 1.35f },
        };

        /// <summary>Player level at which an item becomes purchasable/visible to buyers.</summary>
        public static readonly Dictionary<ItemType, int> UnlockLevel = new Dictionary<ItemType, int>
        {
            { ItemType.Tomato, 1 },
            { ItemType.Egg, 1 },
            { ItemType.TomatoKetchup, 2 },
            { ItemType.Wheat, 2 },
            { ItemType.WheatFlour, 3 },
            { ItemType.Bread, 4 }, // GDD 7: bread (oven) is the level-4 unlock
            { ItemType.Milk, 2 },
            { ItemType.Cheese, 3 },
        };

        public const int CashCounter1UnlockLevel = 1; // available by default (manned from lvl 2, see EconomyCatalog)
        public const int Cashier1AssignableLevel = 2; // player no longer needs to stand at counter 1
        public const int CashCounter2UnlockLevel = 4; // second counter + its cashier

        public const float MinBundlePrice = 1.00f; // floor: no full basket/bundle should price under $1

        public const float PhoneOrderMin = 45f;
        public const float PhoneOrderMax = 300f;
        public const float PhoneOrderWindowMinutes = 10f;     // call stays open for up to 10 minutes

        /// <summary>Dev switch: true = events fire every few seconds for quick testing.
        /// MUST be false for real builds — spec is one call/thief per 4-5 minutes.</summary>
        public static bool FastEventTimers = false;

        public static float PhoneOrderMinIntervalMin => FastEventTimers ? 0.2f : 4f;
        public static float PhoneOrderMaxIntervalMin => FastEventTimers ? 0.3f : 5f;

        public static float TheftMinIntervalMin => FastEventTimers ? 0.3f : 4f;
        public static float TheftMaxIntervalMin => FastEventTimers ? 0.5f : 5f;

        /// <summary>Clamps any computed bundle/basket total up to the $1 floor.</summary>
        public static float ApplyBundleFloor(float rawTotal) => rawTotal < MinBundlePrice ? MinBundlePrice : rawTotal;

        public static bool IsUnlocked(ItemType item, int playerLevel) =>
            UnlockLevel.TryGetValue(item, out var lvl) && playerLevel >= lvl;
    }
}
