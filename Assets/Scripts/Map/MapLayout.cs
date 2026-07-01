using UnityEngine;

namespace MiniMart.Map
{
    /// <summary>
    /// Every named zone in the map. Assign GameObjects in the inspector; this enum keeps
    /// code references stable even if the scene hierarchy changes.
    /// Layout from the design doc (left->right, top->down):
    ///   Entry door (top-left)
    ///   Inner-left column  : EggStorage | CashCounter1 | TomatoZone | HenCoop
    ///   Corridor / bins    : SecondaryExit | Bin | SecondaryExit
    ///   Inner-right column : KetchupStorage | ShopFloor | WheatStorage | WheatFarm
    ///   Corridor / bins    : SecondaryExit | Bin | SecondaryExit
    ///   Right column       : BreadStorage | Oven
    ///   Exit area          : CashCounter2 | ExitDoor
    /// </summary>
    public enum ZoneId
    {
        EntryDoor,
        EggStorage,
        CashCounter1Zone,
        TomatoZone,
        HenCoopZone,
        KetchupStorage,
        ShopFloor,
        WheatStorage,
        WheatFarmZone,
        BreadStorage,
        OvenZone,
        MillZone,
        BlenderZone,
        CashCounter2Zone,
        ExitDoor,
        SecondaryExit1,
        SecondaryExit2,
        SecondaryExit3,
        SecondaryExit4,
        Dustbin1,
        Dustbin2,
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
