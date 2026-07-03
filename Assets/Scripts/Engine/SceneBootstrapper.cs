using UnityEngine;
using UnityEngine.Tilemaps;
using MiniMart.Characters;
using MiniMart.Production;
using MiniMart.Economy;
using MiniMart.AI;
using MiniMart.Map;
using MiniMart.Engine;
using System.Collections.Generic;

namespace MiniMart
{
    public class SceneBootstrapper : MonoBehaviour
    {
        [Header("Override prefabs (optional)")]
        public GameObject PlayerPrefab;
        public GameObject BuyerPrefab;
        public GameObject ThiefPrefab;
        public GameObject ShelverPrefab;
        public GameObject ChefPrefab;
        public GameObject FarmerPrefab;

        // Cached across Awake → Start
        private CameraFollow camFollow;
        private GameObject camGO;

        private void Awake()
        {
            // ═══════════════════════════════════════════════════════════════════
            //  CAMERA SETUP (must happen in Awake so Camera.main is valid)
            // ═══════════════════════════════════════════════════════════════════
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                mainCam = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }
            else
            {
                camGO = mainCam.gameObject;
            }

            // Configure camera for isometric view
            mainCam.orthographic = true;
            mainCam.orthographicSize = 10f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.48f, 0.72f, 0.35f);
            camGO.transform.rotation = Quaternion.Euler(35f, 45f, 0f);

            // Camera follow
            camFollow = camGO.GetComponent<CameraFollow>() ?? camGO.AddComponent<CameraFollow>();
            camFollow.offset = new Vector3(-8f, 14f, -8f);
        }

        private void Start()
        {
            Bootstrap();
        }

        private void Bootstrap()
        {
            // ═══════════════════════════════════════════════════════════════════
            //  1. ENVIRONMENT
            // ═══════════════════════════════════════════════════════════════════

            // Outdoor grass
            PrimitiveFactory.GrassGround(new Vector3(15f, 0, 10f), 40f, 28f);

            // Store interior floor (tan)
            PrimitiveFactory.StoreFloor(new Vector3(15f, 0, 14f), 26f, 14f);

            // Store walls
            PrimitiveFactory.Wall("Wall_South", new Vector3(15f, 0, 7f), new Vector3(26.5f, 1.6f, 0.3f));
            PrimitiveFactory.Wall("Wall_North", new Vector3(15f, 0, 21f), new Vector3(26.5f, 1.6f, 0.3f));
            PrimitiveFactory.Wall("Wall_West",  new Vector3(1.7f, 0, 14f),  new Vector3(0.3f, 1.6f, 14.3f));
            PrimitiveFactory.Wall("Wall_East",  new Vector3(28.3f, 0, 14f), new Vector3(0.3f, 1.6f, 14.3f));

            // Fence around hen coop area (outdoor bottom-left)
            PrimitiveFactory.FenceSegment(new Vector3(1f, 0, 1f),  new Vector3(8f, 0, 1f));
            PrimitiveFactory.FenceSegment(new Vector3(8f, 0, 1f),  new Vector3(8f, 0, 6.5f));
            PrimitiveFactory.FenceSegment(new Vector3(8f, 0, 6.5f), new Vector3(1f, 0, 6.5f));
            PrimitiveFactory.FenceSegment(new Vector3(1f, 0, 1f),  new Vector3(1f, 0, 6.5f));

            // Pathfinder
            var pfGO = new GameObject("Pathfinder");
            var pfComp = pfGO.AddComponent<GridPathfinder>();
            pfComp.Initialize(30, 22, 1f);
            BuildBoundaryWalls(pfComp);

            // Map layout
            var mapGO = new GameObject("MapLayout");
            mapGO.AddComponent<MapLayout>();

            // ═══════════════════════════════════════════════════════════════════
            //  2. FARMS (outdoor area, below the store)
            // ═══════════════════════════════════════════════════════════════════

            var henCoopGO = CreateAt("HenCoop", new Vector2(4, 3.5f));
            var henCoopComp = henCoopGO.AddComponent<HenCoop>();
            PrimitiveFactory.HenCoop(henCoopGO);

            var tomatoFarmGO = CreateAt("TomatoFarm", new Vector2(12, 4));
            var tomatoFarmComp = tomatoFarmGO.AddComponent<TomatoFarm>();
            PrimitiveFactory.TomatoFarm(tomatoFarmGO);

            var wheatFarmGO = CreateAt("WheatFarm", new Vector2(20, 3.5f));
            var wheatFarmComp = wheatFarmGO.AddComponent<WheatFarm>();
            PrimitiveFactory.WheatFarm(wheatFarmGO);

            // ═══════════════════════════════════════════════════════════════════
            //  3. MACHINES (inside store, right side)
            // ═══════════════════════════════════════════════════════════════════

            var blenderGO = CreateAt("Blender", new Vector2(23, 10));
            var blenderComp = blenderGO.AddComponent<Machine>();
            blenderComp.Type = Catalog.MachineType.Blender;
            PrimitiveFactory.MachineVisual(blenderGO, "Blender");

            var ovenGO = CreateAt("Oven", new Vector2(23, 14));
            var ovenComp = ovenGO.AddComponent<Machine>();
            ovenComp.Type = Catalog.MachineType.Oven;
            PrimitiveFactory.MachineVisual(ovenGO, "Oven");

            var millGO = CreateAt("Mill", new Vector2(23, 18));
            var millComp = millGO.AddComponent<Machine>();
            millComp.Type = Catalog.MachineType.Mill;
            PrimitiveFactory.MachineVisual(millGO, "Mill");

            // ═══════════════════════════════════════════════════════════════════
            //  4. CASH COUNTERS (inside store, front area)
            // ═══════════════════════════════════════════════════════════════════

            var cc1GO = CreateAt("CashCounter1", new Vector2(6, 19));
            var cc1Comp = cc1GO.AddComponent<CashCounter>();
            cc1Comp.CounterIndex = 1;
            PrimitiveFactory.CashCounter(cc1GO);

            var cc2GO = CreateAt("CashCounter2", new Vector2(18, 19));
            var cc2Comp = cc2GO.AddComponent<CashCounter>();
            cc2Comp.CounterIndex = 2;
            PrimitiveFactory.CashCounter(cc2GO);

            // ═══════════════════════════════════════════════════════════════════
            //  5. SHELVES (inside store, arranged in rows)
            // ═══════════════════════════════════════════════════════════════════

            var shelvesList = new List<ShopShelf>();
            int shelfIndex = 0;
            Vector2[] shelfPositions = {
                new Vector2(5, 10),   // Egg
                new Vector2(9, 10),   // Tomato
                new Vector2(13, 10),  // TomatoKetchup
                new Vector2(5, 15),   // Wheat
                new Vector2(9, 15),   // WheatFlour
                new Vector2(13, 15),  // Bread
            };

            foreach (Core.ItemType item in System.Enum.GetValues(typeof(Core.ItemType)))
            {
                Vector2 pos = shelfIndex < shelfPositions.Length
                    ? shelfPositions[shelfIndex]
                    : new Vector2(5 + shelfIndex * 4, 12);

                var sGO = CreateAt($"Shelf_{item}", pos);
                var shelfComp = sGO.AddComponent<ShopShelf>();
                shelfComp.Item = item;
                shelfComp.Capacity = 10;
                shelvesList.Add(shelfComp);

                Color itemCol = PrimitiveFactory.ItemColor(item);
                if (PrimitiveFactory.IsRawItem(item))
                    PrimitiveFactory.CrateDisplay(sGO, itemCol);
                else
                    PrimitiveFactory.ShelfUnit(sGO, itemCol);

                shelfIndex++;
            }

            // ═══════════════════════════════════════════════════════════════════
            //  6. DOORS & DUSTBINS
            // ═══════════════════════════════════════════════════════════════════

            var entryDoor = CreateAt("EntryDoor", new Vector2(2, 7));
            PrimitiveFactory.Door(entryDoor, true);

            var exitDoor = CreateAt("ExitDoor", new Vector2(28, 21));
            PrimitiveFactory.Door(exitDoor, false);

            var db1 = CreateAt("Dustbin1", new Vector2(17, 10));
            PrimitiveFactory.Dustbin(db1);

            var db2 = CreateAt("Dustbin2", new Vector2(17, 15));
            PrimitiveFactory.Dustbin(db2);

            // ═══════════════════════════════════════════════════════════════════
            //  7. PLAYER
            // ═══════════════════════════════════════════════════════════════════

            // Reject any assigned prefab that isn't real 3D geometry (e.g. a 2D SpriteRenderer
            // character dragged in from an unrelated asset pack). Using one here has bitten us
            // before — it silently replaces the tested capsule character with something that
            // looks wrong (or invisible) from this isometric camera and carries its own Animator
            // Controller with parameters our code never drives. Fail loud instead of silent.
            GameObject validPrefab = PlayerPrefab;
            if (validPrefab != null && validPrefab.GetComponentInChildren<MeshRenderer>() == null)
            {
                Debug.LogError($"[SceneBootstrapper] PlayerPrefab '{validPrefab.name}' has no MeshRenderer " +
                    "(likely a 2D sprite from the wrong asset pack) — ignoring it and using the built-in " +
                    "capsule character instead. Clear or replace the PlayerPrefab field on _Bootstrap.");
                validPrefab = null;
            }

            var playerGO = validPrefab != null
                ? Instantiate(validPrefab, new Vector3(8, 0, 5), Quaternion.identity)
                : CreateAt("Player", new Vector2(8, 5));

            var playerComp = playerGO.GetComponent<PlayerController>()
                ?? playerGO.AddComponent<PlayerController>();
            playerComp.Net = playerGO.GetComponent<NetTool>() ?? playerGO.AddComponent<NetTool>();

            var playerInput = playerGO.GetComponent<PlayerInputHandler>()
                ?? playerGO.AddComponent<PlayerInputHandler>();

            if (playerGO.GetComponent<UI.CarryVisual>() == null) playerGO.AddComponent<UI.CarryVisual>();
            if (playerGO.GetComponent<WobbleAnimator>() == null) playerGO.AddComponent<WobbleAnimator>();

            // Only add the built-in primitive visuals when there's no valid custom prefab —
            // otherwise they'd render doubled up alongside the prefab's own model.
            if (validPrefab == null)
                PrimitiveFactory.BuildCharacter(playerGO, new Color(0.2f, 0.6f, 1f), false, true);

            Debug.Log($"[SceneBootstrapper] Player spawned at {playerGO.transform.position}");

            // Hook up camera
            camFollow.target = playerGO.transform;
            camGO.transform.position = playerGO.transform.position + camFollow.offset;

            // ═══════════════════════════════════════════════════════════════════
            //  8. NPC WORKERS
            // ═══════════════════════════════════════════════════════════════════

            // Shelver 1 — pink
            var shelver1GO = Spawn(ShelverPrefab, "Shelver1", new Vector2(7, 12));
            var shelver1Comp = shelver1GO.GetComponent<Shelver>() ?? shelver1GO.AddComponent<Shelver>();
            shelver1Comp.Configure(Core.RoleType.Shelver1, null);
            shelver1Comp.AssignedShelves = shelvesList.ToArray();
            if (shelver1GO.GetComponent<WobbleAnimator>() == null) shelver1GO.AddComponent<WobbleAnimator>();
            PrimitiveFactory.BuildCharacter(shelver1GO, new Color(0.92f, 0.3f, 0.55f));

            // Shelver 2 — magenta
            var shelver2GO = Spawn(ShelverPrefab, "Shelver2", new Vector2(11, 12));
            var shelver2Comp = shelver2GO.GetComponent<Shelver>() ?? shelver2GO.AddComponent<Shelver>();
            shelver2Comp.Configure(Core.RoleType.Shelver2, null);
            shelver2Comp.AssignedShelves = shelvesList.ToArray();
            if (shelver2GO.GetComponent<WobbleAnimator>() == null) shelver2GO.AddComponent<WobbleAnimator>();
            PrimitiveFactory.BuildCharacter(shelver2GO, new Color(0.85f, 0.2f, 0.85f));

            // Chef — white with chef hat
            var chefGO = Spawn(ChefPrefab, "Chef", new Vector2(21, 12));
            var chefComp = chefGO.GetComponent<Chef>() ?? chefGO.AddComponent<Chef>();
            chefComp.tomatoFarm = tomatoFarmComp;
            chefComp.wheatFarm = wheatFarmComp;
            chefComp.henCoop = henCoopComp;
            chefComp.blender = blenderComp;
            chefComp.oven = ovenComp;
            chefComp.mill = millComp;
            if (chefGO.GetComponent<WobbleAnimator>() == null) chefGO.AddComponent<WobbleAnimator>();
            PrimitiveFactory.BuildCharacter(chefGO, Color.white, true);

            // Farmer — green
            var farmerGO = Spawn(FarmerPrefab, "Farmer", new Vector2(6, 4));
            var farmerComp = farmerGO.GetComponent<Farmer>() ?? farmerGO.AddComponent<Farmer>();
            farmerComp.tomatoFarm = tomatoFarmComp;
            farmerComp.wheatFarm = wheatFarmComp;
            farmerComp.henCoop = henCoopComp;
            if (farmerGO.GetComponent<WobbleAnimator>() == null) farmerGO.AddComponent<WobbleAnimator>();
            PrimitiveFactory.BuildCharacter(farmerGO, new Color(0.3f, 0.75f, 0.35f));

            // ═══════════════════════════════════════════════════════════════════
            //  9. MANAGERS (GameManager MUST be created before HUD)
            // ═══════════════════════════════════════════════════════════════════

            var spawnerGO = new GameObject("BuyerSpawner");
            var spawnerComp = spawnerGO.AddComponent<BuyerSpawner>();
            spawnerComp.BuyerPrefab = BuyerPrefab;
            spawnerComp.EntranceDoor = entryDoor.transform;
            spawnerComp.ExitDoor = exitDoor.transform;
            spawnerComp.AllShelves = shelvesList;
            spawnerComp.Counters = new List<CashCounter> { cc1Comp, cc2Comp };

            var theftGO = new GameObject("TheftManager");
            var theftComp = theftGO.AddComponent<TheftManager>();
            theftComp.ThiefPrefab = ThiefPrefab;
            theftComp.SpawnPoint = entryDoor.transform;
            theftComp.ExitWaypoint = exitDoor.transform;
            theftComp.AllShelves = shelvesList;

            var pomGO = new GameObject("PhoneOrderManager");
            var pomComp = pomGO.AddComponent<PhoneOrderManager>();

            var gmGO = new GameObject("GameManager");
            var gmComp = gmGO.AddComponent<GameManager>();
            gmComp.Player = playerComp;
            gmComp.Shelver1 = shelver1Comp;
            gmComp.Shelver2 = shelver2Comp;
            gmComp.Chef = chefComp;
            gmComp.Farmer = farmerComp;
            gmComp.Counters = new List<CashCounter> { cc1Comp, cc2Comp };
            gmComp.BuyerSpawner = spawnerComp;
            gmComp.TheftManager = theftComp;
            gmComp.PhoneOrderManager = pomComp;
            gmComp.TomatoFarm = tomatoFarmComp;
            gmComp.WheatFarm = wheatFarmComp;
            gmComp.HenCoop = henCoopComp;
            gmComp.Blender = blenderComp;
            gmComp.Oven = ovenComp;
            gmComp.Mill = millComp;

            // ═══════════════════════════════════════════════════════════════════
            //  10. HUD — built AFTER GameManager so HUDController.Awake can find it
            // ═══════════════════════════════════════════════════════════════════
            HUDBuilder.Build(playerInput);

            Debug.Log("[SceneBootstrapper] Scene fully bootstrapped with My Mini Mall visuals.");
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static void BuildBoundaryWalls(GridPathfinder pf)
        {
            int w = pf.GridWidth, h = pf.GridHeight;
            for (int x = 0; x < w; x++) { pf.SetWalkable(x, 0, false); pf.SetWalkable(x, h - 1, false); }
            for (int y = 0; y < h; y++) { pf.SetWalkable(0, y, false); pf.SetWalkable(w - 1, y, false); }
            pf.SetWalkable(0, h - 1, true);
            pf.SetWalkable(17, h - 1, true);
        }

        private static GameObject CreateAt(string name, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(pos.x, 0, pos.y);
            return go;
        }

        private static GameObject Spawn(GameObject prefab, string name, Vector2 pos)
        {
            if (prefab != null)
                return Object.Instantiate(prefab, new Vector3(pos.x, 0, pos.y), Quaternion.identity);
            return CreateAt(name, pos);
        }
    }

    public class Billboard : MonoBehaviour
    {
        public bool LockY = true;
        private Transform cam;
        void Start() { if (Camera.main != null) cam = Camera.main.transform; }
        void LateUpdate()
        {
            if (cam == null) return;
            transform.LookAt(transform.position + cam.rotation * Vector3.forward, cam.rotation * Vector3.up);
            if (LockY) transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }
    }
}