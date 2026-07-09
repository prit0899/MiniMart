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
            // Section 8 Base Prices
            { ItemType.Apple, 2.0f },
            { ItemType.Tomato, 2.0f },
            { ItemType.Corn, 2.0f },
            { ItemType.Bread, 6.0f },
            { ItemType.Cookie, 8.0f },
            { ItemType.CannedTomato, 5.0f },
            { ItemType.BottledMilk, 4.0f },
            { ItemType.Egg, 3.0f },
            
            // Intermediates (Not listed in Section 8 for direct sale, but needed for Phone Orders)
            { ItemType.Wheat, 1.0f },
            { ItemType.Milk, 2.0f },
            { ItemType.Dough, 3.0f },
            { ItemType.CookieDough, 5.0f },
            { ItemType.ProcessedCorn, 4.0f },

            // Extended production chain (Blender/Mill/Dairy/LeafProcessor/Stove/Coffee).
            // Each priced above its raw input per the value-add rule (DataValidator
            // Suite 4/12): Tomato<Ketchup, Wheat<Flour<Bread, Milk<Cheese, Herb<HerbPack,
            // Egg<FriedEgg.
            { ItemType.Herb, 1.5f },
            { ItemType.TomatoKetchup, 4.0f },
            { ItemType.WheatFlour, 3.0f },
            { ItemType.Cheese, 5.0f },
            { ItemType.HerbPack, 4.0f },
            { ItemType.FriedEgg, 5.0f },
            { ItemType.Coffee, 6.0f },
        };

        /// <summary>Player level at which an item becomes purchasable/visible to buyers.</summary>
        public static readonly Dictionary<ItemType, int> UnlockLevel = new Dictionary<ItemType, int>
        {
            { ItemType.Tomato, 1 },
            { ItemType.Egg, 1 },
            // Apple unlocks with the Apple Orchard pad (L2, batch 35). Was
            // temporarily parked at 99 while it had no production source.
            { ItemType.Apple, 2 },
            { ItemType.Wheat, 2 },
            { ItemType.Dough, 2 },
            { ItemType.Bread, 2 }, 
            { ItemType.Milk, 3 },
            { ItemType.BottledMilk, 3 },
            { ItemType.Corn, 4 },
            { ItemType.ProcessedCorn, 4 },
            { ItemType.CannedTomato, 4 },
            { ItemType.CookieDough, 5 },
            { ItemType.Cookie, 5 },

            // WheatFlour must unlock at/before Bread (its consumer) per the
            // OutputUnlockGEInput chain check — same level as Wheat.
            { ItemType.WheatFlour, 2 },
            { ItemType.TomatoKetchup, 4 },
            { ItemType.Herb, 4 },
            { ItemType.HerbPack, 5 },
            { ItemType.Cheese, 5 },
            { ItemType.FriedEgg, 4 },
            { ItemType.Coffee, 6 }
        };

        public const int CashCounter1UnlockLevel = 1; // available by default
        // Counter 1 is staffed by a cashier from the very start so buyers are ALWAYS
        // checked out and money flows (a closed L1 counter left buyers queuing forever
        // then leaving unpaid). The player can still stand at the till for a 3x speed boost.
        public const int Cashier1AssignableLevel = 1;
        // Batch 35: was L4; the buyer spawn rate scales 0.9^level so a single
        // counter overflowed hard at L3 — the mid-game frustration valley.
        public const int CashCounter2UnlockLevel = 3; // second counter + its cashier
        // Reference image §Cashier Section lists FOUR registers total: Main + Expanded 1/2 + Register 3/4.
        // Counter 3 opens mid-late game, Counter 4 is the final "big store" milestone.
        public const int CashCounter3UnlockLevel = 5;
        public const int CashCounter4UnlockLevel = 6;

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
