using UnityEngine;

namespace MiniMart.Map
{
    /// <summary>
    /// Every named zone in the map, matching the reference-image layout:
    ///
    ///   ┌─────────────────────── THE SUPERMARKET ZONE ───────────────────────┐
    ///   │ Cashier Section │ Central Display Aisles │ Bakery & Café (Pink)    │
    ///   │  (4 registers)  │ (Tomato/Egg/Canned/    │ (Bread/Cookie/Milk/     │
    ///   │                 │  ProcessedCorn stands)  │  Coffee Dispenser)      │
    ///   └─────────────────┴────────────────────────┴────────────────────────┘
    ///   ════════════════════ PROCESSING AREA (road strip) ═══════════════════
    ///   │ TomatoProcessor │  CornProcessor  │  EggMilkCookingStation        │
    ///   ════════════════════════════════════════════════════════════════════
    ///   ┌─────────────────── THE SUPPLY FARM (GRASS) ───────────────────────┐
    ///   │ Livestock West  │ Upgrade Hub Center │ Agriculture East           │
    ///   │ (Chickens/Cows) │ (Speed/Carry/Crop) │ (Tomato plots/Corn field) │
    ///   └─────────────────┴────────────────────┴───────────────────────────┘
    /// </summary>
    public enum ZoneId
    {
        // ── Supermarket Zone (top band, z ∈ [13.2, 22]) ──
        CashierSection,           // x ∈ [ 2, 13]  — 4 cash registers
        CentralDisplayAisles,     // x ∈ [13, 25]  — product stands & shelves
        BakeryCafe,               // x ∈ [25, 34]  — pink floor: bread, cookie, milk, coffee

        // ── Processing Area (middle strip, z ∈ [8, 12]) ──
        ProcessingArea,           // Central factory + 3 machines
        TomatoProcessorZone,      // Tomatoes → Canned Jars
        CornProcessorZone,        // Corn → Processed Corn
        EggMilkCookingZone,       // Eggs + Milk → Cookies

        // ── Supply Farm (bottom band, z ∈ [0, 8]) ──
        LivestockWest,            // Chicken coops + cow pasture
        UpgradeHubCenter,         // Player speed / carry / crop upgrade pads
        AgricultureEast,          // Tomato crop field + corn field

        // ── Doors & Utility ──
        EntryDoor,
        ExitDoor,
        Dustbin1,
        Dustbin2,

        // ── Legacy compatibility (kept so existing code doesn't break) ──
        ShopFloor,
        CashCounter1Zone,
        CashCounter2Zone,
        CashCounter3Zone,
        CashCounter4Zone,
        TomatoZone,
        HenCoopZone,
        WheatFarmZone,
        OvenZone,
        MillZone,
        BlenderZone,
    }

    [System.Serializable]
    public struct ZoneEntry
    {
        public ZoneId Id;
        public Transform Anchor; // world-space position of this zone's centre
    }

    /// <summary>
    /// Holds references to every named zone anchor. Attach to a persistent scene object.
    /// Drag transforms from the scene hierarchy into the Zones array in the inspector.
    /// </summary>
    public class MapLayout : MonoBehaviour
    {
        public ZoneEntry[] Zones;

        public Transform GetAnchor(ZoneId id)
        {
            foreach (var z in Zones)
                if (z.Id == id) return z.Anchor;
            Debug.LogWarning($"MapLayout: no anchor for zone {id}");
            return null;
        }

        public Vector3 WorldPos(ZoneId id)
        {
            var t = GetAnchor(id);
            return t != null ? t.position : Vector3.zero;
        }
    }
}
