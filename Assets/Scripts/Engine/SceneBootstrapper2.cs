using UnityEngine;
using UnityEngine.Tilemaps;
using MiniMart.Characters;
using MiniMart.Production;
using MiniMart.Economy;
using MiniMart.AI;
using MiniMart.Map;
using MiniMart.Engine;
using System.Collections.Generic;
using System.Linq;
using MiniMart.Catalog;

namespace MiniMart
{
    public class SceneBootstrapper2 : MonoBehaviour
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
            mainCam.orthographicSize = 7.5f;
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
            var sunGO = new GameObject("Sun");
            var sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.97f, 0.9f);
            sun.intensity = 0.85f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.3f;
            sunGO.transform.rotation = Quaternion.Euler(52f, -40f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.45f, 0.5f);
            RenderSettings.ambientIntensity = 1f;

            PrimitiveFactory.GrassGround(new Vector3(0f, 0, 20f), 80f, 40f);
            PrimitiveFactory.GrassGround(new Vector3(0f, 0, 61f), 80f, 2f);
            PrimitiveFactory.StoreFloor(new Vector3(0f, 0, 50f), 40f, 20f);
            PrimitiveFactory.Road(new Vector3(0f, 0, 70f), 80f, 16f);
            PrimitiveFactory.TreePerimeter(-40f, 40f, 0f, 82f, 3.4f);

            PrimitiveFactory.Wall("Wall_South", new Vector3(0f, 0, 40f), new Vector3(40f, 0.85f, 0.3f));
            PrimitiveFactory.Wall("Wall_North", new Vector3(0f, 0, 60f), new Vector3(40f, 0.85f, 0.3f));
            PrimitiveFactory.Wall("Wall_West", new Vector3(-20f, 0, 50f), new Vector3(0.3f, 0.85f, 20f));
            PrimitiveFactory.Wall("Wall_East", new Vector3(20f, 0, 50f), new Vector3(0.3f, 0.85f, 20f));
            PrimitiveFactory.StripedGate(new Vector3(-20f, 0, 55f), 5f);

            var pfGO = new GameObject("Pathfinder");
            var pfComp = pfGO.AddComponent<GridPathfinder>();
            pfComp.GridOrigin = new Vector3(-40f, 0, 0f);
            pfComp.Initialize(80, 82, 1f);
            BuildBoundaryWalls(pfComp);

            var mapGO = new GameObject("MapLayout");
            mapGO.AddComponent<MapLayout>();

            // Farms
            var cornFieldGO = CreateAt("CornField", new Vector2(16f, 8f));
            var cornFieldComp = cornFieldGO.AddComponent<CornField>();
            PrimitiveFactory.CornField(cornFieldGO);

            var appleOrchardGO = CreateAt("AppleOrchard", new Vector2(16f, 12f));
            var appleOrchardComp = appleOrchardGO.AddComponent<AppleOrchard>();
            PrimitiveFactory.AppleOrchard(appleOrchardGO);

            var herbPatchGO = CreateAt("HerbPatch", new Vector2(20f, 12f));
            var herbPatchComp = herbPatchGO.AddComponent<HerbPatch>();
            PrimitiveFactory.HerbPatch(herbPatchGO);

            var assistantGO = CreateAt("AssistantNode", new Vector2(0f, 20f));
            var assistantComp = assistantGO.AddComponent<AssistantNode>();
            assistantComp.cornField = cornFieldComp;

            var factoryGO = CreateAt("ProcessingFactory", new Vector2(0f, 32f));
            PrimitiveFactory.ProcessingFactory(factoryGO);

            // ── Milk chain (moved here from Mart 1 in the two-mart split) ──
            // Without it the Dairy below had NO milk source anywhere in the
            // game — a dead machine, the "refuses input" playtest bug again.
            // (Tomato Canner and Cookie Line are retired: their raw inputs —
            // tomatoes and wheat — only exist in Mart 1, so here they could
            // never be loaded.)
            var cowPenGO = CreateAt("CowPen", new Vector2(-12f, 10f));
            var cowPenComp = cowPenGO.AddComponent<CowPen>();
            PrimitiveFactory.CowPen(cowPenGO);

            var hayTroughGO = CreateAt("HayFeedTrough", new Vector2(-12f, 12f));
            var hayTroughComp = hayTroughGO.AddComponent<HayFeedTrough>();
            hayTroughComp.LinkedCow = cowPenComp;
            cowPenComp.FedBy = hayTroughComp;
            PrimitiveFactory.HayFeedTrough(hayTroughGO);

            var milkBottlerGO = CreateAt("MilkBottler", new Vector2(-10f, 32f));
            var milkBottlerComp = milkBottlerGO.AddComponent<Machine>();
            milkBottlerComp.Type = Catalog.MachineType.MilkBottler;
            PrimitiveFactory.MachineVisual(milkBottlerGO, "MilkBottler");
            milkBottlerGO.AddComponent<MachineBadge>();

            var cornProcGO = CreateAt("CornProcessor", new Vector2(-5f, 32f));
            var cornProcComp = cornProcGO.AddComponent<Machine>();
            cornProcComp.Type = Catalog.MachineType.CornProcessor;
            PrimitiveFactory.MachineVisual(cornProcGO, "CornProcessor");
            cornProcGO.AddComponent<MachineBadge>();

            var dairyGO = CreateAt("Dairy", new Vector2(0f, 32f));
            var dairyComp = dairyGO.AddComponent<Machine>();
            dairyComp.Type = Catalog.MachineType.Dairy;
            PrimitiveFactory.MachineVisual(dairyGO, "Dairy");
            dairyGO.AddComponent<MachineBadge>();

            var leafGO = CreateAt("LeafProcessor", new Vector2(5f, 32f));
            var leafComp = leafGO.AddComponent<Machine>();
            leafComp.Type = Catalog.MachineType.LeafProcessor;
            PrimitiveFactory.MachineVisual(leafGO, "LeafProcessor");
            leafGO.AddComponent<MachineBadge>();

            var coffeeGO = CreateAt("CoffeeDispenser", new Vector2(15f, 32f));
            var coffeeComp = coffeeGO.AddComponent<Machine>();
            coffeeComp.Type = Catalog.MachineType.CoffeeDispenser;
            PrimitiveFactory.CoffeeDispenser(coffeeGO);
            coffeeGO.AddComponent<MachineBadge>();

            StorageRack MakeRack(Core.ItemType item, Vector2 pos)
            {
                var go = new GameObject($"Rack_{item}");
                go.transform.position = new Vector3(pos.x, 0, pos.y);
                go.SetActive(false);
                var rack = go.AddComponent<StorageRack>();
                rack.Item = item;
                rack.Build();
                go.SetActive(true);
                return rack;
            }

            var rackCorn    = MakeRack(Core.ItemType.Corn,          new Vector2(18f, 6f));
            var rackApple   = MakeRack(Core.ItemType.Apple,         new Vector2(18f, 10f));
            var rackHerb    = MakeRack(Core.ItemType.Herb,          new Vector2(22f, 14f));
            var rackMilk    = MakeRack(Core.ItemType.Milk,          new Vector2(-16f, 10f));
            var rackBMilk   = MakeRack(Core.ItemType.BottledMilk,   new Vector2(-10f, 34f));
            var rackPCorn   = MakeRack(Core.ItemType.ProcessedCorn, new Vector2(-5f, 34f));
            var rackCheese  = MakeRack(Core.ItemType.Cheese,        new Vector2(0f, 34f));
            var rackHerbPk  = MakeRack(Core.ItemType.HerbPack,      new Vector2(5f, 34f));
            var rackCoffee  = MakeRack(Core.ItemType.Coffee,        new Vector2(15f, 34f));

            var cc3GO = CreateAt("CashCounter3", new Vector2(-10f, 55f));
            var cc3Comp = cc3GO.AddComponent<CashCounter>();
            cc3Comp.CounterIndex = 3;
            PrimitiveFactory.CashCounter(cc3GO);

            var cc4GO = CreateAt("CashCounter4", new Vector2(-10f, 50f));
            var cc4Comp = cc4GO.AddComponent<CashCounter>();
            cc4Comp.CounterIndex = 4;
            PrimitiveFactory.CashCounter(cc4GO);

            var shelvesList = new List<ShopShelf>();
            Vector2[] shelfPositions = {
                new Vector2(-6, 50),   // Apple
                new Vector2(-2, 50),   // Corn
                new Vector2(2, 50),    // ProcessedCorn
                new Vector2(6, 50),    // CannedTomato
                new Vector2(10, 50),   // Cheese
                new Vector2(14, 55),   // Herb
                new Vector2(18, 55),   // HerbPack
                new Vector2(22, 55),   // Cookie
                new Vector2(26, 55),   // Coffee
            };
            Core.ItemType[] shelfItems = {
                Core.ItemType.Apple, Core.ItemType.Corn, Core.ItemType.ProcessedCorn,
                Core.ItemType.CannedTomato, Core.ItemType.Cheese, Core.ItemType.Herb,
                Core.ItemType.HerbPack, Core.ItemType.Cookie, Core.ItemType.Coffee
            };

            for (int i = 0; i < shelfItems.Length; i++)
            {
                var item = shelfItems[i];
                var pos = shelfPositions[i];
                var sGO = CreateAt($"Shelf_{item}", pos);
                var shelfComp = sGO.AddComponent<ShopShelf>();
                shelfComp.Item = item;
                shelfComp.Capacity = 20;
                shelvesList.Add(shelfComp);
                Color itemCol = PrimitiveFactory.ItemColor(item);
                bool needsFridge = item == Core.ItemType.BottledMilk;
                if (needsFridge) PrimitiveFactory.FridgeUnit(sGO, itemCol);
                else if (PrimitiveFactory.IsRawItem(item)) PrimitiveFactory.CrateDisplay(sGO, itemCol);
                else PrimitiveFactory.ShelfUnit(sGO, itemCol);
            }

            var entryDoor = CreateAt("EntryDoor", new Vector2(-10f, 60.5f));
            PrimitiveFactory.Door(entryDoor, true);
            var exitDoor = CreateAt("ExitDoor", new Vector2(-13f, 60.5f));
            PrimitiveFactory.Door(exitDoor, false);

            var db1 = CreateAt("Dustbin1", new Vector2(14, 17));
            PrimitiveFactory.Dustbin(db1);

            GameObject validPrefab = PlayerPrefab;
            if (validPrefab != null && validPrefab.GetComponentInChildren<MeshRenderer>() == null) validPrefab = null;
            var playerGO = validPrefab != null ? Instantiate(validPrefab, new Vector3(0, 0, 20), Quaternion.identity) : CreateAt("Player", new Vector2(0, 20));
            var playerComp = playerGO.GetComponent<PlayerController>() ?? playerGO.AddComponent<PlayerController>();
            playerComp.Net = playerGO.GetComponent<NetTool>() ?? playerGO.AddComponent<NetTool>();
            var playerInput = playerGO.GetComponent<PlayerInputHandler>() ?? playerGO.AddComponent<PlayerInputHandler>();
            if (playerGO.GetComponent<UI.CarryVisual>() == null) playerGO.AddComponent<UI.CarryVisual>();
            if (playerGO.GetComponent<WobbleAnimator>() == null) playerGO.AddComponent<WobbleAnimator>();
            if (playerGO.GetComponent<GeniesPlayerSkin>() == null) playerGO.AddComponent<GeniesPlayerSkin>();
            
            var interact = playerGO.GetComponent<PlayerInteraction>() ?? playerGO.AddComponent<PlayerInteraction>();
            interact.cornField = cornFieldComp;
            interact.appleOrchard = appleOrchardComp;
            interact.herbPatch = herbPatchComp;
            interact.cowPen = cowPenComp;
            interact.hayFeedTrough = hayTroughComp;
            interact.milkBottler = milkBottlerComp;
            interact.cornProcessor = cornProcComp;
            interact.dairy = dairyComp;
            interact.leafProcessor = leafComp;
            interact.coffeeDispenser = coffeeComp;
            interact.shelves = shelvesList;
            interact.bins = new List<Transform> { db1.transform };

            if (validPrefab == null)
                PrimitiveFactory.BuildCharacter(playerGO, new Color(0.2f, 0.6f, 1f), false, true);
            camFollow.target = playerGO.transform;
            camGO.transform.position = playerGO.transform.position + camFollow.offset;

            var shelver2GO = Spawn(ShelverPrefab, "Stocker2", new Vector2(5, 50));
            var shelver2Comp = shelver2GO.GetComponent<Shelver>() ?? shelver2GO.AddComponent<Shelver>();
            shelver2Comp.AssignedShelves = shelvesList.FindAll(s => RoleCatalog.RoleResponsibilities[Core.RoleType.Shelver2].Contains(s.Item)).ToArray();
            if (shelver2GO.GetComponent<WobbleAnimator>() == null) shelver2GO.AddComponent<WobbleAnimator>();
            if (shelver2GO.GetComponent<UI.CarryVisual>() == null) shelver2GO.AddComponent<UI.CarryVisual>();
            PrimitiveFactory.BuildCharacter(shelver2GO, new Color(0.85f, 0.2f, 0.85f), PrimitiveFactory.CharacterRole.Shelver);

            var buyerSpawn = new GameObject("BuyerSpawnSpot");
            buyerSpawn.transform.position = new Vector3(-10, 0, 74f);
            var buyerExit = new GameObject("BuyerExitSpot");
            buyerExit.transform.position = new Vector3(20f, 0, 74f);

            var spawnerGO = new GameObject("BuyerSpawner");
            var spawnerComp = spawnerGO.AddComponent<BuyerSpawner>();
            spawnerComp.BuyerPrefab = BuyerPrefab;
            spawnerComp.EntranceDoor = buyerSpawn.transform;
            spawnerComp.ExitDoor = buyerExit.transform;
            spawnerComp.AllShelves = shelvesList;
            spawnerComp.Counters = new List<CashCounter> { cc3Comp, cc4Comp };

            var theftGO = new GameObject("TheftManager");
            var theftComp = theftGO.AddComponent<TheftManager>();
            theftComp.ThiefPrefab = ThiefPrefab;
            theftComp.SpawnPoint = buyerSpawn.transform;
            theftComp.ExitWaypoint = buyerExit.transform;
            theftComp.AllShelves = shelvesList;

            var vanSpawn = new GameObject("VanSpawnSpot");
            vanSpawn.transform.position = new Vector3(-38f, 0, 55f);
            var vanPickup = new GameObject("VanPickupSpot");
            vanPickup.transform.position = new Vector3(-18f, 0, 55f);
            PrimitiveFactory.VanParkingSpot(new Vector3(-18f, 0, 55f));

            var pomGO = new GameObject("PhoneOrderManager");
            var pomComp = pomGO.AddComponent<PhoneOrderManager>();
            pomComp.SpawnSpot = vanSpawn.transform;
            pomComp.PickupSpot = vanPickup.transform;
            pomComp.AllShelves = shelvesList;

            var gmGO = new GameObject("GameManager");
            gmGO.AddComponent<DataValidator>();
            var gmComp = gmGO.AddComponent<GameManager>();
            gmComp.Player = playerComp;
            gmComp.Shelver2 = shelver2Comp;
            gmComp.Counters = new List<CashCounter> { cc3Comp, cc4Comp };
            gmComp.BuyerSpawner = spawnerComp;
            gmComp.TheftManager = theftComp;
            gmComp.PhoneOrderManager = pomComp;
            gmComp.CornProcessor = cornProcComp;
            gmComp.CoffeeDispenser = coffeeComp;
            gmComp.Dairy = dairyComp;
            gmComp.LeafProcessor = leafComp;
            gmComp.CornField = cornFieldComp;
            gmComp.CowPen = cowPenComp;
            gmComp.HayFeedTrough = hayTroughComp;
            gmComp.AssistantNode = assistantComp;

            HUDBuilder.Build(playerInput);

            ShopShelf ShelfOf(Core.ItemType t) => shelvesList.Find(s => s.Item == t);
            void Gate(float cost, int minLevel, string label, params GameObject[] targets)
            {
                foreach (var t in targets)
                    if (t != null) t.SetActive(false);
                if (targets.Length > 0 && targets[0] != null)
                {
                    var pad = PurchasePad.Create(targets[0].transform.position, cost, label, targets);
                    pad.MinLevel = minLevel;
                }
            }

            // MegaMart ladder (L6-L10, ≤3 pads per level — user pacing rule).
            // Levels mirror PriceCatalog.UnlockLevel exactly.
            Gate(100f, 6, "Cow Pen",       cowPenGO, ShelfOf(Core.ItemType.Milk)?.gameObject, rackMilk.gameObject);
            Gate(160f, 6, "Hay Trough",    hayTroughGO);
            Gate(60f,  6, "Hire Shelver B", shelver2GO);

            Gate(120f, 7, "Milk Bottler",  milkBottlerGO, ShelfOf(Core.ItemType.BottledMilk)?.gameObject, rackBMilk.gameObject);
            Gate(65f,  7, "Apple Orchard", appleOrchardGO, ShelfOf(Core.ItemType.Apple)?.gameObject, rackApple.gameObject);
            Gate(120f, 7, "Corn Field",    cornFieldGO, ShelfOf(Core.ItemType.Corn)?.gameObject, rackCorn.gameObject);

            Gate(130f, 8, "Corn Processor", cornProcGO, ShelfOf(Core.ItemType.ProcessedCorn)?.gameObject, rackPCorn.gameObject);
            Gate(90f,  8, "Herb Patch",    herbPatchGO, ShelfOf(Core.ItemType.Herb)?.gameObject, rackHerb.gameObject);
            Gate(175f, 8, "Cheese Dairy",  dairyGO, ShelfOf(Core.ItemType.Cheese)?.gameObject, rackCheese.gameObject);

            Gate(140f, 9, "Leaf Unit",     leafGO, ShelfOf(Core.ItemType.HerbPack)?.gameObject, rackHerbPk.gameObject);
            Gate(400f, 9, "Counter 3",     cc3GO);

            Gate(280f, 10, "Coffee Bar",   coffeeGO, ShelfOf(Core.ItemType.Coffee)?.gameObject, rackCoffee.gameObject);
            Gate(500f, 10, "Counter 4",    cc4GO);

            SceneTransition.Create(new Vector3(-15f, 0, 50f), "Game", "Return to MiniMart");

            if (!MiniMart.Save.SaveSystem.HasSave())
                new GameObject("TutorialGuide").AddComponent<TutorialGuide>();

            void Locator(GameObject source, Core.ItemType item)
            {
                if (source == null) return;
                var icon = PrimitiveFactory.ItemMesh(item, source.transform, new Vector3(0, 3.4f, 0), 3.5f);
                icon.name = $"Locator_{item}";
                icon.AddComponent<LocatorBob>();
            }
            Locator(cornFieldGO, Core.ItemType.Corn);
            Locator(appleOrchardGO, Core.ItemType.Apple);
            Locator(herbPatchGO, Core.ItemType.Herb);

            new GameObject("Retention").AddComponent<Retention>();
            new GameObject("UnlockGuide").AddComponent<UnlockGuide>();
            AudioFx.StartMusic();
        }

        private static void BuildBoundaryWalls(GridPathfinder pf)
        {
            int w = pf.GridWidth, h = pf.GridHeight;
            void BlockWorld(float wx, float wz)
            {
                int cx = Mathf.RoundToInt(wx) + 40;
                int cz = Mathf.RoundToInt(wz);
                if (cx >= 0 && cx < w && cz >= 0 && cz < h)
                    pf.SetWalkable(cx, cz, false);
            }

            for (int x = 0; x < w; x++) { pf.SetWalkable(x, 0, false); pf.SetWalkable(x, h - 1, false); }
            for (int y = 0; y < h; y++) { pf.SetWalkable(0, y, false); pf.SetWalkable(w - 1, y, false); }

            for (int wx = -20; wx <= 20; wx++)
            {
                if (wx >= 1 && wx <= 9) continue;
                BlockWorld(wx, 40f);
            }

            for (int wx = -20; wx <= 20; wx++)
            {
                if (wx >= -11 && wx <= -9) continue;
                if (wx >= -14 && wx <= -12) continue;
                BlockWorld(wx, 60f);
            }

            for (int wz = 40; wz <= 60; wz++)
            {
                if (wz >= 53 && wz <= 57) continue;
                BlockWorld(-20f, wz);
            }

            for (int wz = 40; wz <= 60; wz++)
                BlockWorld(20f, wz);
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
}
