namespace MiniMart.Core
{
    /// <summary>All tradeable / storable items in the game.</summary>
    public enum ItemType
    {
        // Raw Resources
        Egg,
        Apple,
        Tomato,
        Corn,
        Wheat,
        Milk,

        // Intermediate & Processed Goods
        Dough,
        Bread,
        BottledMilk,
        CannedTomato,
        ProcessedCorn,
        CookieDough,
        Cookie,

        // Extended production chain (Blender/Mill/Dairy/LeafProcessor/Stove/Coffee) —
        // referenced throughout Chef/Farmer/SceneBootstrapper/StorageRack/DataValidator
        // but previously missing from this enum, which broke the whole assembly build.
        TomatoKetchup,
        WheatFlour,
        Cheese,
        Herb,
        HerbPack,
        FriedEgg,
        Coffee
    }

    /// <summary>Which character role owns a piece of state / logic.</summary>
    public enum RoleType
    {
        Player,
        Stocker,
        Harvester,
        Cashier,
        Manager,    // Future
        Security,   // Future
        Buyer,
        Thief,

        // Distinct shelver identities — RoleCatalog.RoleResponsibilities and
        // Shelver.Configure key off these instead of the generic Stocker value.
        Shelver1,
        Shelver2,

        // Chef/Farmer self-identify their Role this way (CharacterBase.Role) even
        // though Configure() takes no RoleType parameter for these two.
        Chef,
        Farmer
    }

    /// <summary>Shared FSM for every character (Sec 6 of Architecture Spec).</summary>
    public enum CharacterState
    {
        Idle,
        Walking,
        Waiting,
        Carrying,
        Loading,
        Processing,
        Selling,
        Chasing,
        Fleeing,
        Resting
    }

    /// <summary>How a buyer is carrying their goods.</summary>
    public enum BagType
    {
        HandCarry, // < 5 items
        Trolley    // >= 5 items
    }

    /// <summary>Quality presets, Sec 7 Architecture Spec.</summary>
    public enum QualityPreset
    {
        LowPower,
        Balanced,
        High
    }
}
