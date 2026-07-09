using UnityEngine;
using UnityEngine.Tilemaps;

namespace MiniMart.Map
{
    /// <summary>
    /// Procedurally stamps rectangular floor tiles into each zone's bounding rect.
    /// Intended to run once on Start (or via Editor tool) to populate the Tilemap.
    ///
    /// Zone layout matches the reference image (Map.png):
    ///   Farm (grass):        z ∈ [0,  8]    — bottom band
    ///   Processing strip:    z ∈ [8, 12]    — factory + machines
    ///   Road divider:        z ∈ [12, 13.2] — asphalt road
    ///   Supermarket floor:   z ∈ [13.2, 22] — three store columns
    ///
    ///   Cashier Section:     x ∈ [ 2, 13]   (beige floor)
    ///   Central Aisles:      x ∈ [13, 25]   (beige floor)
    ///   Bakery & Café:       x ∈ [25, 34]   (pink floor)
    /// </summary>
    public class MapTileGenerator : MonoBehaviour
    {
        [Header("Tilemap target")]
        public Tilemap FloorTilemap;
        public Tilemap WallTilemap;

        [Header("Tile assets — assign in inspector")]
        public TileBase FloorTile;       // Beige store floor
        public TileBase PinkFloorTile;   // Bakery & Café pink floor
        public TileBase FarmTile;        // Green grass
        public TileBase StorageTile;     // Storage area
        public TileBase WallTile;
        public TileBase DoorTile;
        public TileBase RoadTile;        // Asphalt road

        [Header("Map dimensions (cells)")]
        public int MapWidth = 44;
        public int MapHeight = 44;

        [Header("Zone rects — match reference image layout")]
        // ── Supermarket Zone (top band) ──
        public RectInt CashierSectionRect     = new RectInt(2,  14, 11, 8);   // x=[2,13],  z=[14,22]
        public RectInt CentralAislesRect      = new RectInt(13, 14, 12, 8);   // x=[13,25], z=[14,22]
        public RectInt BakeryCafeRect         = new RectInt(25, 14, 9,  8);   // x=[25,34], z=[14,22]

        // ── Processing Area (middle strip) ──
        public RectInt ProcessingAreaRect     = new RectInt(2,  8,  32, 4);   // x=[2,34],  z=[8,12]

        // ── Road ──
        public RectInt RoadRect               = new RectInt(0, 23, 44, 21);   // broadened road at top

        // ── Supply Farm (bottom band) ──
        public RectInt LivestockWestRect      = new RectInt(2,  0,  12, 8);   // x=[2,14],  z=[0,8]
        public RectInt UpgradeHubRect         = new RectInt(14, 0,  14, 8);   // x=[14,28], z=[0,8]
        public RectInt AgricultureEastRect    = new RectInt(28, 0,  14, 8);   // x=[28,42], z=[0,8]

        // ── Doors ──
        public RectInt EntryDoorRect          = new RectInt(4,  22, 3,  1);   // North wall, left
        public RectInt ExitDoorRect           = new RectInt(25, 22, 3,  1);   // North wall, right

        private void Start() => Generate();

        [ContextMenu("Generate Map Now")]
        public void Generate()
        {
            if (FloorTilemap == null) return;
            FloorTilemap.ClearAllTiles();
            if (WallTilemap != null) WallTilemap.ClearAllTiles();

            // Base: grass everywhere
            StampRect(FloorTilemap, new RectInt(0, 0, MapWidth, MapHeight), FarmTile);

            // Store floors: beige for cashier + central, pink for bakery/café
            StampRect(FloorTilemap, CashierSectionRect, FloorTile);
            StampRect(FloorTilemap, CentralAislesRect, FloorTile);
            StampRect(FloorTilemap, BakeryCafeRect, PinkFloorTile ?? FloorTile);

            // Road
            StampRect(FloorTilemap, RoadRect, RoadTile ?? FloorTile);

            // Farm zones
            StampRect(FloorTilemap, LivestockWestRect, FarmTile);
            StampRect(FloorTilemap, UpgradeHubRect, FarmTile);
            StampRect(FloorTilemap, AgricultureEastRect, FarmTile);

            // Doors
            StampRect(FloorTilemap, EntryDoorRect, DoorTile);
            StampRect(FloorTilemap, ExitDoorRect, DoorTile);

            // Outer walls
            if (WallTilemap != null && WallTile != null)
            {
                for (int x = -1; x <= MapWidth; x++)
                {
                    WallTilemap.SetTile(new Vector3Int(x, -1, 0), WallTile);
                    WallTilemap.SetTile(new Vector3Int(x, MapHeight, 0), WallTile);
                }
                for (int y = 0; y < MapHeight; y++)
                {
                    WallTilemap.SetTile(new Vector3Int(-1, y, 0), WallTile);
                    WallTilemap.SetTile(new Vector3Int(MapWidth, y, 0), WallTile);
                }
            }
        }

        private void StampRect(Tilemap map, RectInt rect, TileBase tile)
        {
            if (tile == null) return;
            for (int x = rect.x; x < rect.x + rect.width; x++)
                for (int y = rect.y; y < rect.y + rect.height; y++)
                    map.SetTile(new Vector3Int(x, y, 0), tile);
        }
    }
}
