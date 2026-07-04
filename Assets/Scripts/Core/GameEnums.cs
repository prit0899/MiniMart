namespace MiniMart.Core
{
    /// <summary>All tradeable / storable items in the game.</summary>
    public enum ItemType
    {
        Egg,
        Tomato,
        TomatoKetchup,
        Wheat,
        WheatFlour,
        Bread,
        Milk,   // cow pen (reference dairy chain)
        Cheese  // milk -> dairy processor
    }

    /// <summary>Which character role owns a piece of state / logic.</summary>
    public enum RoleType
    {
        Player,
        Shelver1,
        Shelver2,
        Chef,
        Farmer,
        Buyer,
        Thief,
        Cashier
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
