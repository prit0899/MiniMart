using UnityEngine;
using UnityEngine.Tilemaps;

namespace MiniMart.Map
{
    /// <summary>
    /// Procedurally stamps a rectangular floor tile into each zone's bounding rect.
    /// Intended to run once on Start (or via Editor tool) to populate the Tilemap.
    /// Zones that share the same tile type (e.g. floor) use FloorTile;
    /// farm zones use FarmTile; storage zones use StorageTile.
    /// </summary>
    public class MapTileGenerator : MonoBehaviour
    {
        [Header("Tilemap target")]
        public Tilemap FloorTilemap;
        public Tilemap WallTilemap;

        [Header("Tile assets — assign in inspector")]
        public TileBase FloorTile;
        public TileBase FarmTile;
        public TileBase StorageTile;
        public TileBase WallTile;
        public TileBase DoorTile;

        [Header("Map dimensions (cells)")]
        public int MapWidth = 30;
        public int MapHeight = 20;

        [Header("Zone rects — match visual layout from design doc")]
        // Each rect is defined in cell coordinates (col, row, width, height).
        // Adjust values to match your chosen tile scale in the Unity scene.
        public RectInt EntryDoorRect      = new RectInt(0,  18, 3, 2);
        public RectInt EggStorageRect     = new RectInt(0,  13, 4, 4);
        public RectInt CashCounter1Rect   = new RectInt(0,   9, 4, 3);
        public RectInt TomatoZoneRect     = new RectInt(0,   5, 4, 4);
        public RectInt HenCoopRect        = new RectInt(0,   0, 4, 5);
        public RectInt KetchupStorageRect = new RectInt(5,  13, 4, 5);
        public RectInt ShopFloorRect      = new RectInt(5,   7, 4, 6);
        public RectInt WheatStorageRect   = new RectInt(5,   3, 4, 4);
        public RectInt WheatFarmRect      = new RectInt(5,   0, 4, 3);
        public RectInt BreadStorageRect   = new RectInt(10, 13, 4, 5);
        public RectInt OvenZoneRect       = new RectInt(10,  9, 4, 4);
        public RectInt MillZoneRect       = new RectInt(10,  5, 4, 4);
        public RectInt BlenderZoneRect    = new RectInt(10,  0, 4, 5);
        public RectInt CashCounter2Rect   = new RectInt(15,  0, 4, 4);
        public RectInt ExitDoorRect       = new RectInt(15, 18, 3, 2);

        private void Start() => Generate();

        [ContextMenu("Generate Map Now")]
        public void Generate()
        {
            if (FloorTilemap == null) return;
            FloorTilemap.ClearAllTiles();
            if (WallTilemap != null) WallTilemap.ClearAllTiles();

            StampRect(FloorTilemap, new RectInt(0, 0, MapWidth, MapHeight), FloorTile);

            StampRect(FloorTilemap, EggStorageRect, StorageTile);
            StampRect(FloorTilemap, KetchupStorageRect, StorageTile);
            StampRect(FloorTilemap, WheatStorageRect, StorageTile);
            StampRect(FloorTilemap, BreadStorageRect, StorageTile);

            StampRect(FloorTilemap, TomatoZoneRect, FarmTile);
            StampRect(FloorTilemap, HenCoopRect, FarmTile);
            StampRect(FloorTilemap, WheatFarmRect, FarmTile);

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
