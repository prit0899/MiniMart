using UnityEngine;
using UnityEngine.Tilemaps;
using MiniMart.Characters;
using MiniMart.Production;
using MiniMart.Economy;
using MiniMart.AI;
using MiniMart.Map;
using MiniMart.Engine;

namespace MiniMart
{
    /// <summary>
    /// Programmatic scene bootstrapper. Add this to an empty GameObject called _Bootstrap.
    /// It creates all required game objects at runtime with sensible defaults so the game
    /// runs without requiring every prefab and reference to be manually dragged in Inspector.
    ///
    /// You can override any field by assigning it in the inspector BEFORE the scene starts.
    /// </summary>
    public class SceneBootstrapper : MonoBehaviour
    {
        [Header("Override prefabs (optional — bootstrapper creates plain GameObjects if null)")]
        public GameObject PlayerPrefab;
        public GameObject BuyerPrefab;
        public GameObject ThiefPrefab;
        public GameObject ShelverPrefab;
        public GameObject ChefPrefab;
        public GameObject FarmerPrefab;

        private void Awake() => Bootstrap();

        private void Bootstrap()
        {
            // ─── Camera ──────────────────────────────────────────────────────
            Camera cam = Camera.main;
            GameObject camGO;
            if (cam == null)
            {
                camGO = new GameObject("Main Camera");
                cam = camGO.AddComponent<Camera>();
                cam.tag = "MainCamera";
                Debug.Log("[SceneBootstrapper] Camera created.");
            }
            else
            {
                camGO = cam.gameObject;
            }
            
            cam.orthographic = true;
            cam.orthographicSize = 12f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.65f, 0.85f, 0.65f); // light grass green
            camGO.transform.position = new Vector3(-5, 15, -5);
            camGO.transform.rotation = Quaternion.Euler(30f, 45f, 0f);

            // ─── Tilemap root ─────────────────────────────────────────────────
            var gridGO = new GameObject("Grid");
            gridGO.AddComponent<Grid>();
            var floorTmGO = new GameObject("FloorTilemap");
            floorTmGO.transform.SetParent(gridGO.transform);
            floorTmGO.AddComponent<Tilemap>();
            floorTmGO.AddComponent<TilemapRenderer>();
            var wallTmGO = new GameObject("WallTilemap");
            wallTmGO.transform.SetParent(gridGO.transform);
            wallTmGO.AddComponent<Tilemap>();
            var wallTmRenderer = wallTmGO.AddComponent<TilemapRenderer>();
            wallTmRenderer.sortingOrder = 1;

            // ─── Pathfinder ───────────────────────────────────────────────────
            var pfGO = new GameObject("Pathfinder");
            var pf = pfGO.AddComponent<GridPathfinder>();
            pf.GridWidth = 30; pf.GridHeight = 20; pf.CellSize = 1f;

            // ─── MapLayout ────────────────────────────────────────────────────
            var mapGO = new GameObject("MapLayout");
            mapGO.AddComponent<MapLayout>();

            // ─── Farm nodes ───────────────────────────────────────────────────
            var tomatoFarmGO = CreateAt("TomatoFarm", new Vector2(2, 4));
            var tomatoFarm   = tomatoFarmGO.AddComponent<TomatoFarm>();
            Ensure3DVisuals(tomatoFarmGO, Color.magenta);

            var wheatFarmGO = CreateAt("WheatFarm", new Vector2(7, 2));
            var wheatFarm   = wheatFarmGO.AddComponent<WheatFarm>();
            Ensure3DVisuals(wheatFarmGO, new Color(1f, 0.5f, 0f)); // orange

            var henCoopGO = CreateAt("HenCoop", new Vector2(2, 2));
            var henCoop   = henCoopGO.AddComponent<HenCoop>();
            Ensure3DVisuals(henCoopGO, Color.white);

            // ─── Machines ─────────────────────────────────────────────────────
            var blenderGO = CreateAt("Blender", new Vector2(12, 3));
            var blender   = blenderGO.AddComponent<Machine>();
            blender.Type  = MiniMart.Catalog.MachineType.Blender;
            Ensure3DVisuals(blenderGO, Color.gray);

            var ovenGO = CreateAt("Oven", new Vector2(12, 10));
            var oven   = ovenGO.AddComponent<Machine>();
            oven.Type  = MiniMart.Catalog.MachineType.Oven;
            Ensure3DVisuals(ovenGO, Color.gray);

            var millGO = CreateAt("Mill", new Vector2(12, 6));
            var mill   = millGO.AddComponent<Machine>();
            mill.Type  = MiniMart.Catalog.MachineType.Mill;
            Ensure3DVisuals(millGO, Color.gray);

            // ─── Cash counters ────────────────────────────────────────────────
            var cc1GO = CreateAt("CashCounter1", new Vector2(2, 12));
            var cc1   = cc1GO.AddComponent<CashCounter>();
            cc1.CounterIndex = 1;
            Ensure3DVisuals(cc1GO, new Color(0f, 0.8f, 0f)); // green

            var cc2GO = CreateAt("CashCounter2", new Vector2(17, 1));
            var cc2   = cc2GO.AddComponent<CashCounter>();
            cc2.CounterIndex = 2;
            Ensure3DVisuals(cc2GO, new Color(0f, 0.8f, 0f));

            // ─── Shop shelves (one per item type) ─────────────────────────────
            float shelfX = 6f;
            var shelves = new System.Collections.Generic.List<Characters.ShopShelf>();
            foreach (MiniMart.Core.ItemType item in System.Enum.GetValues(typeof(MiniMart.Core.ItemType)))
            {
                var sGO = CreateAt($"Shelf_{item}", new Vector2(shelfX, 8));
                var shelf = sGO.AddComponent<Characters.ShopShelf>();
                shelf.Item = item; shelf.Capacity = 10;
                shelves.Add(shelf);
                Ensure3DVisuals(sGO, new Color(0.6f, 0.3f, 0f)); // brown
                shelfX += 1.5f;
            }

            // ─── Entry / exit doors ───────────────────────────────────────────
            var entryDoor = CreateAt("EntryDoor", new Vector2(0, 19));
            Ensure3DVisuals(entryDoor, Color.black);
            var exitDoor  = CreateAt("ExitDoor",  new Vector2(17, 19));
            Ensure3DVisuals(exitDoor, Color.black);

            // ─── Dustbins ──────────────────────────────────────────────────────
            var db1 = CreateAt("Dustbin1", new Vector2(4, 10));
            Ensure3DVisuals(db1, Color.red);
            var db2 = CreateAt("Dustbin2", new Vector2(9, 10));
            Ensure3DVisuals(db2, Color.red);

            // ─── Workers ─────────────────────────────────────────────────────
            var playerGO = PlayerPrefab != null
                ? Instantiate(PlayerPrefab, new Vector3(5, 0, 10), Quaternion.identity)
                : CreateAt("Player", new Vector2(5, 10));
            var playerComp = playerGO.GetComponent<PlayerController>()
                ?? playerGO.AddComponent<PlayerController>();
            playerComp.Net = playerGO.AddComponent<NetTool>();
            playerGO.AddComponent<Engine.PlayerInputHandler>();
            playerGO.AddComponent<UI.CarryVisual>();
            Ensure3DVisuals(playerGO, Color.yellow, PrimitiveType.Capsule);

            var shelver1GO = Spawn(ShelverPrefab, "Shelver1", new Vector2(3, 11));
            var shelver1   = shelver1GO.GetComponent<Shelver>() ?? shelver1GO.AddComponent<Shelver>();
            shelver1.Configure(MiniMart.Core.RoleType.Shelver1, null); // Inventory injected by GameManager
            shelver1.AssignedShelves = shelves.ToArray();
            Ensure3DVisuals(shelver1GO, Color.cyan, PrimitiveType.Capsule);

            var shelver2GO = Spawn(ShelverPrefab, "Shelver2", new Vector2(8, 11));
            var shelver2   = shelver2GO.GetComponent<Shelver>() ?? shelver2GO.AddComponent<Shelver>();
            shelver2.Configure(MiniMart.Core.RoleType.Shelver2, null);
            shelver2.AssignedShelves = shelves.ToArray();
            Ensure3DVisuals(shelver2GO, Color.blue, PrimitiveType.Capsule);

            var chefGO = Spawn(ChefPrefab, "Chef", new Vector2(11, 5));
            var chefComp = chefGO.GetComponent<Chef>() ?? chefGO.AddComponent<Chef>();
            chefComp.tomatoFarm = tomatoFarm;
            chefComp.henCoop    = henCoop;
            chefComp.blender    = blender;
            chefComp.oven       = oven;
            Ensure3DVisuals(chefGO, Color.red, PrimitiveType.Capsule);

            var farmerGO = Spawn(FarmerPrefab, "Farmer", new Vector2(4, 3));
            var farmerComp = farmerGO.GetComponent<Farmer>() ?? farmerGO.AddComponent<Farmer>();
            farmerComp.tomatoFarm = tomatoFarm;
            farmerComp.wheatFarm  = wheatFarm;
            farmerComp.henCoop    = henCoop;
            Ensure3DVisuals(farmerGO, Color.green, PrimitiveType.Capsule);

            // ─── Buyer spawner ────────────────────────────────────────────────
            var spawnerGO = new GameObject("BuyerSpawner");
            var spawner   = spawnerGO.AddComponent<BuyerSpawner>();
            spawner.BuyerPrefab    = BuyerPrefab;
            spawner.EntranceDoor   = entryDoor.transform;
            spawner.AllShelves     = shelves;
            spawner.Counters       = new System.Collections.Generic.List<CashCounter> { cc1, cc2 };

            // ─── Theft manager ─────────────────────────────────────────────────
            var theftGO = new GameObject("TheftManager");
            var theft   = theftGO.AddComponent<TheftManager>();
            theft.ThiefPrefab    = ThiefPrefab;
            theft.SpawnPoint     = entryDoor.transform;
            theft.ExitWaypoint   = exitDoor.transform;
            theft.AllShelves     = shelves;

            // ─── Phone order manager ───────────────────────────────────────────
            var pomGO = new GameObject("PhoneOrderManager");
            pomGO.AddComponent<PhoneOrderManager>();

            // ─── GameManager ──────────────────────────────────────────────────
            var gmGO = new GameObject("GameManager");
            var gm   = gmGO.AddComponent<GameManager>();
            gm.Player          = playerComp;
            gm.Shelver1        = shelver1;
            gm.Shelver2        = shelver2;
            gm.Chef            = chefComp;
            gm.Farmer          = farmerComp;
            gm.Counters        = new System.Collections.Generic.List<CashCounter> { cc1, cc2 };
            gm.BuyerSpawner    = spawner;
            gm.TheftManager    = theft;
            gm.PhoneOrderManager = pomGO.GetComponent<PhoneOrderManager>();
            gm.TomatoFarm      = tomatoFarm;
            gm.WheatFarm       = wheatFarm;
            gm.HenCoop         = henCoop;
            gm.Blender         = blender;
            gm.Oven            = oven;
            gm.Mill            = mill;

            Debug.Log("[SceneBootstrapper] Scene fully bootstrapped.");
        }

        private static GameObject CreateAt(string name, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(pos.x, 0, pos.y);
            return go;
        }

        private static GameObject Spawn(GameObject prefab, string fallbackName, Vector2 pos)
        {
            if (prefab != null) return Instantiate(prefab, new Vector3(pos.x, 0, pos.y), Quaternion.identity);
            return CreateAt(fallbackName, pos);
        }

        private static void Ensure3DVisuals(GameObject go, Color color, PrimitiveType type = PrimitiveType.Cube)
        {
            if (go == null) return;
            
            // Remove SpriteRenderer if any
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(sr);
#else
                Destroy(sr);
#endif
            }

            // Create 3D primitive as child
            var visual = GameObject.CreatePrimitive(type);
            visual.transform.SetParent(go.transform, false);
            
            // Disable child collider so it doesn't mess with root triggers
            var col = visual.GetComponent<Collider>();
            if (col != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(col);
#else
                Destroy(col);
#endif
            }

            // Adjust capsule size slightly
            if (type == PrimitiveType.Capsule)
            {
                visual.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                visual.transform.localPosition = new Vector3(0, 0.6f, 0); // raise half height
            }
            else
            {
                visual.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                visual.transform.localPosition = new Vector3(0, 0.4f, 0);
            }

            // Apply color material
            var mr = visual.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                // We use standard shader
                mr.material = new Material(Shader.Find("Standard"));
                mr.material.color = color;
            }
        }
        
        private static Mesh CreateCubeMesh()
        {
            var mesh = new Mesh();
            float s = 0.5f;
            
            mesh.vertices = new Vector3[]
            {
                new Vector3(-s, -s, -s), new Vector3(s, -s, -s),
                new Vector3(s, s, -s), new Vector3(-s, s, -s),
                new Vector3(-s, -s, s), new Vector3(s, -s, s),
                new Vector3(s, s, s), new Vector3(-s, s, s)
            };
            
            mesh.triangles = new int[]
            {
                // Front face
                0, 2, 1, 0, 3, 2,
                // Back face
                4, 5, 6, 4, 6, 7,
                // Left
                0, 7, 3, 0, 4, 7,
                // Right
                1, 2, 6, 1, 6, 5,
                // Top
                3, 7, 6, 3, 6, 2,
                // Bottom
                4, 0, 1, 4, 1, 5
            };
            
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
