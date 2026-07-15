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
            // Batch 36: $6 → $8. Bread consumes Flour ($3) + Egg ($3), so at $6
            // the oven added work but zero profit — the only margin-dead chain.
            { ItemType.Bread, 8.0f },
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
            // SINGLE SOURCE OF TRUTH: these levels mirror the purchase-pad
            // ladder in SceneBootstrapper exactly. If a pad moves, move the
            // item here too — a mismatch makes buyers/phone-orders demand
            // items whose production pad isn't buyable yet.

            // ── Mart 1 "Mini Mart" (L1-5) ──
            { ItemType.Tomato, 1 },           // starter shelf
            { ItemType.Egg, 1 },              // L1: Hen Coop pad
            { ItemType.TomatoKetchup, 2 },    // L2: Ketchup Blender pad
            { ItemType.Wheat, 3 },            // L3: Wheat Farm pad
            { ItemType.WheatFlour, 4 },       // L4: Wheat Mill pad
            { ItemType.Bread, 5 },            // L5: Bread Oven pad
            { ItemType.FriedEgg, 5 },         // L5: Egg Stove pad

            // ── Mart 2 "MegaMart" (L6-10, SceneBootstrapper2 ladder) ──
            { ItemType.Milk, 6 },             // L6: Cow Pen pad
            { ItemType.BottledMilk, 7 },      // L7: Milk Bottler pad
            { ItemType.Apple, 7 },            // L7: Apple Orchard pad
            { ItemType.Corn, 7 },             // L7: Corn Field pad
            { ItemType.ProcessedCorn, 8 },    // L8: Corn Processor pad
            { ItemType.Herb, 8 },             // L8: Herb Patch pad
            { ItemType.Cheese, 8 },           // L8: Cheese Dairy pad
            { ItemType.HerbPack, 9 },         // L9: Leaf Unit pad
            { ItemType.Coffee, 10 },          // L10: Coffee Bar pad

            // Retired chains are deliberately ABSENT: a missing key means
            // IsUnlocked() is false forever, keeping them out of every buyer/
            // phone-order pool. The Retired set below documents this for the
            // validator.
        };

        /// <summary>Items whose production chains are retired from the two-mart
        /// split (their recipes would need ingredients from the other mart).
        /// They keep a BasePrice (old saves may still hold stock) but have no
        /// UnlockLevel entry, no shelves, and no purchase pads.</summary>
        public static readonly HashSet<ItemType> Retired = new HashSet<ItemType>
        {
            ItemType.Dough, ItemType.CannedTomato, ItemType.CookieDough, ItemType.Cookie,
        };

        public const int CashCounter1UnlockLevel = 1; // available by default
        // Counter 1 is staffed by a cashier from the very start so buyers are ALWAYS
        // checked out and money flows (a closed L1 counter left buyers queuing forever
        // then leaving unpaid). The player can still stand at the till for a 3x speed boost.
        public const int Cashier1AssignableLevel = 1;
        // Batch 39 reconcile: the Counter 2 pad sells at L3 (queue-overflow fix,
        // batch 35) — this constant MUST match or the bought counter stays closed.
        public const int CashCounter2UnlockLevel = 3;
        public const int CashCounter3UnlockLevel = 6;  // Mart 2's only till until L10 - open on arrival
        public const int CashCounter4UnlockLevel = 10;  // Mart 2 endgame

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
