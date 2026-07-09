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
            mainCam.orthographicSize = 7.5f; // closer, more intimate framing (reference feel)
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
            //  0. LIGHTING — a bright warm "sun" + high ambient so the flat cartoon
            //  colours read as saturated and cheerful (reference look), not the dim
            //  grey wash of Unity's default near-black ambient in a code-built scene.
            // ═══════════════════════════════════════════════════════════════════
            var sunGO = new GameObject("Sun");
            var sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.97f, 0.9f);   // warm white
            sun.intensity = 0.85f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.3f;                // gentle, not harsh
            sunGO.transform.rotation = Quaternion.Euler(52f, -40f, 0f);

            // Flat ambient tuned so lit surfaces read at their true colour (tan floor stays
            // tan, not clipped white) while shadowed sides stay colourful.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.45f, 0.5f);
            RenderSettings.ambientIntensity = 1f;

            // ═══════════════════════════════════════════════════════════════════
            //  1. ENVIRONMENT
            // ═══════════════════════════════════════════════════════════════════

            // ═══════════════════════════════════════════════════════════════════
            //  REFERENCE-IMAGE COORDINATE SYSTEM (Map.png) -> plan.md
            //  Map expands from X = -40 to +40, Z = 0 to +70.
            //    Livestock West:       X ∈ [-30, -5], Z ∈ [4, 16]
            //    Agriculture East:     X ∈ [8, 32],   Z ∈ [4, 16]
            //    Supply Grass:         Z ∈ [18, 26]
            //    Processing Area:      Z ∈ [28, 38]
            //    Supermarket floor:    Z ∈ [40, 60]
            // ═══════════════════════════════════════════════════════════════════

            // Grass ground covers the farm and processing areas (Z = 0 to 40).
            // Widened X-bounds from ±40 to ±50 so both farms have breathing room
            // and the road above can extend past the store on both sides.
            PrimitiveFactory.GrassGround(new Vector3(0f, 0, 20f), 100f, 40f);

            // Thin pavement strip between store north wall (Z=60) and the road (Z=62),
            // so the store doesn't sit flush against the asphalt.
            PrimitiveFactory.GrassGround(new Vector3(0f, 0, 61f), 100f, 2f);

            // Store floor: Supermarket Zone (Z = 40 to 60)
            PrimitiveFactory.StoreFloor(new Vector3(-5f,  0, 50f), 30f, 20f);   // Cashier/Central (beige)
            PrimitiveFactory.CafeFloor (new Vector3(20f, 0, 50f), 20f,  20f);   // Bakery & Café (pink)

            // Broad asphalt road along the north edge (Z = 62 to 78 — 16 units wide,
            // 100 units long). Buyers walk in along it and delivery trucks drive down
            // it, per the reference map.
            PrimitiveFactory.Road(new Vector3(0f, 0, 70f), 100f, 16f);

            // Perimeter trees. Ring the enlarged lot (X ±50, Z 0-82).
            PrimitiveFactory.TreePerimeter(-50f, 50f, 0f, 82f, 3.4f);

            // ── Store walls ──
            // South wall along z=40 with service opening for farm↔store.
            PrimitiveFactory.Wall("Wall_South_A", new Vector3(-10f, 0, 40f), new Vector3(20f, 0.85f, 0.3f));
            PrimitiveFactory.Wall("Wall_South_B", new Vector3( 20f, 0, 40f), new Vector3(20f, 0.85f, 0.3f));
            // North wall along z=60 with two customer door gaps, aligned to the
            // actual Door objects (entry x=-15, exit x=25 — each ~3.5u wide).
            // Previous segment sizes covered the door positions and left the
            // visible gaps elsewhere, so buyers appeared to walk through walls.
            PrimitiveFactory.Wall("Wall_North_A", new Vector3(-18.4f, 0, 60f), new Vector3(3.2f,  0.85f, 0.3f));
            PrimitiveFactory.Wall("Wall_North_B", new Vector3(  5f,   0, 60f), new Vector3(36.5f, 0.85f, 0.3f));
            PrimitiveFactory.Wall("Wall_North_C", new Vector3( 28.4f, 0, 60f), new Vector3(3.2f,  0.85f, 0.3f));
            // Side walls enclosing the store. West wall has a gap for the delivery
            // entrance (Z=53..57) so the player can walk through to load the van.
            PrimitiveFactory.Wall("Wall_West_N", new Vector3(-20f, 0, 45f), new Vector3(0.3f, 0.85f, 10f));
            PrimitiveFactory.Wall("Wall_West_S", new Vector3(-20f, 0, 58f), new Vector3(0.3f, 0.85f, 4f));
            PrimitiveFactory.Wall("Wall_East",   new Vector3( 30f, 0, 50f), new Vector3(0.3f, 0.85f, 20f));

            // Red-white candy-striped angled barrier marking the west delivery entrance.
            PrimitiveFactory.StripedGate(new Vector3(-20f, 0, 55f), 5f);
            
            // Pathfinder covers the enlarged 100 x 82 world (extra breadth for the
            // wider road + tree perimeter push to Z=82).
            var pfGO = new GameObject("Pathfinder");
            var pfComp = pfGO.AddComponent<GridPathfinder>();
            pfComp.GridOrigin = new Vector3(-50f, 0, 0f);
            pfComp.Initialize(100, 82, 1f);
            BuildBoundaryWalls(pfComp);

            // Map layout
            var mapGO = new GameObject("MapLayout");
            mapGO.AddComponent<MapLayout>();

            // ═══════════════════════════════════════════════════════════════════
            //  2. FARMS (outdoor area, below the store)
            // ═══════════════════════════════════════════════════════════════════

            // ═══════════════════════════════════════════════════════════════════
            //  LIVESTOCK WEST — Chicken coop (-20, 0, 10), Cow pen (-12, 0, 10)
            // ═══════════════════════════════════════════════════════════════════
            var redBarnGO = CreateAt("RedBarn", new Vector2(-25f, 10f));
            PrimitiveFactory.RedBarn(redBarnGO);

            var henCoopGO = CreateAt("HenCoop", new Vector2(-20f, 10f));
            var henCoopComp = henCoopGO.AddComponent<HenCoop>();
            PrimitiveFactory.HenCoop(henCoopGO);

            var cowPenGO = CreateAt("CowPen", new Vector2(-12f, 10f));
            var cowPenComp = cowPenGO.AddComponent<CowPen>();
            PrimitiveFactory.CowPen(cowPenGO);

            var hayTroughGO = CreateAt("HayFeedTrough", new Vector2(-12f, 12f));
            var hayTroughComp = hayTroughGO.AddComponent<HayFeedTrough>();
            hayTroughComp.LinkedCow = cowPenComp;
            cowPenComp.FedBy = hayTroughComp;
            PrimitiveFactory.HayFeedTrough(hayTroughGO);

            // ═══════════════════════════════════════════════════════════════════
            //  HUB CENTER — 3 in-world upgrade pads + "purchasable plots" outline.
            //  Reference image center-bottom.
            // ═══════════════════════════════════════════════════════════════════
            UpgradePad.Create(new Vector3(18f, 0f, 3.5f), UpgradePad.Kind.PlayerSpeed);
            UpgradePad.Create(new Vector3(21f, 0f, 3.5f), UpgradePad.Kind.PlayerCarry);
            UpgradePad.Create(new Vector3(24f, 0f, 3.5f), UpgradePad.Kind.CropSpeed);
            // Dashed "purchasable plots" callout outline — reference image.
            PrimitiveFactory.DashedRect(new Vector3(27f, 0f, 5f), 4f, 6f,
                new Color(0.94f, 0.94f, 0.94f));

            // ═══════════════════════════════════════════════════════════════════
            //  AGRICULTURE EAST — Tomato patch (16,12), Wheat (16,8), Corn (24,8)
            // ═══════════════════════════════════════════════════════════════════
            var tomatoFarmGO = CreateAt("TomatoFarm", new Vector2(16f, 12f));
            var tomatoFarmComp = tomatoFarmGO.AddComponent<TomatoFarm>();
            PrimitiveFactory.TomatoFarm(tomatoFarmGO);

            var wheatFarmGO = CreateAt("WheatFarm", new Vector2(16f, 8f));
            var wheatFarmComp = wheatFarmGO.AddComponent<WheatFarm>();
            PrimitiveFactory.WheatFarm(wheatFarmGO);

            var cornFieldGO = CreateAt("CornField", new Vector2(24f, 8f));
            var cornFieldComp = cornFieldGO.AddComponent<CornField>();
            PrimitiveFactory.CornField(cornFieldGO);

            // Apple orchard — plan.md §3 "Apple trees: (24, 0, 12)". Fills the
            // dead-SKU gap found in the batch-34 playthrough.
            var appleOrchardGO = CreateAt("AppleOrchard", new Vector2(24f, 12f));
            var appleOrchardComp = appleOrchardGO.AddComponent<AppleOrchard>();
            PrimitiveFactory.AppleOrchard(appleOrchardGO);

            var assistantGO = CreateAt("AssistantNode", new Vector2(0f, 20f));
            var assistantComp = assistantGO.AddComponent<AI.AssistantNode>();
            assistantComp.cornField = cornFieldComp;

            // ═══════════════════════════════════════════════════════════════════
            //  PROCESSING AREA — Factory center: (0, 0, 32)
            // ═══════════════════════════════════════════════════════════════════
            var factoryGO = CreateAt("ProcessingFactory", new Vector2(0f, 32f));
            PrimitiveFactory.ProcessingFactory(factoryGO);

            var tomatoCannerGO = CreateAt("TomatoCanner", new Vector2(-10f, 32f));
            var tomatoCannerComp = tomatoCannerGO.AddComponent<Machine>();
            tomatoCannerComp.Type = Catalog.MachineType.TomatoCanner;
            PrimitiveFactory.MachineVisual(tomatoCannerGO, "TomatoCanner");
            tomatoCannerGO.AddComponent<MachineBadge>();

            var cornProcGO = CreateAt("CornProcessor", new Vector2(-5f, 32f));
            var cornProcComp = cornProcGO.AddComponent<Machine>();
            cornProcComp.Type = Catalog.MachineType.CornProcessor;
            PrimitiveFactory.MachineVisual(cornProcGO, "CornProcessor");
            cornProcGO.AddComponent<MachineBadge>();
            
            var doughMixerGO = CreateAt("DoughMixer", new Vector2(5f, 32f));
            var doughMixerComp = doughMixerGO.AddComponent<Machine>();
            doughMixerComp.Type = Catalog.MachineType.DoughMixer;
            PrimitiveFactory.MachineVisual(doughMixerGO, "DoughMixer");
            doughMixerGO.AddComponent<MachineBadge>();

            var ovenGO = CreateAt("Oven", new Vector2(10f, 32f));
            var ovenComp = ovenGO.AddComponent<Machine>();
            ovenComp.Type = Catalog.MachineType.Oven;
            PrimitiveFactory.MachineVisual(ovenGO, "Oven");
            ovenGO.AddComponent<MachineBadge>();

            var milkBottlerGO = CreateAt("MilkBottler", new Vector2(15f, 32f));
            var milkBottlerComp = milkBottlerGO.AddComponent<Machine>();
            milkBottlerComp.Type = Catalog.MachineType.MilkBottler;
            PrimitiveFactory.MachineVisual(milkBottlerGO, "MilkBottler");
            milkBottlerGO.AddComponent<MachineBadge>();

            var cookieGO = CreateAt("CookieLine", new Vector2(20f, 32f));
            var cookieComp = cookieGO.AddComponent<Machine>();
            cookieComp.Type = Catalog.MachineType.CookieStation;
            PrimitiveFactory.MachineVisual(cookieGO, "CookieLine");
            cookieGO.AddComponent<MachineBadge>();

            // ═══════════════════════════════════════════════════════════════════
            //  4. CASH COUNTERS (inside store, front area)
            // ═══════════════════════════════════════════════════════════════════

            var cc1GO = CreateAt("CashCounter1", new Vector2(-15, 55));
            var cc1Comp = cc1GO.AddComponent<CashCounter>();
            cc1Comp.CounterIndex = 1;
            PrimitiveFactory.CashCounter(cc1GO);

            var cc2GO = CreateAt("CashCounter2", new Vector2(-15, 50));
            var cc2Comp = cc2GO.AddComponent<CashCounter>();
            cc2Comp.CounterIndex = 2;
            PrimitiveFactory.CashCounter(cc2GO);

            var cc3GO = CreateAt("CashCounter3", new Vector2(-10, 55));
            var cc3Comp = cc3GO.AddComponent<CashCounter>();
            cc3Comp.CounterIndex = 3;
            PrimitiveFactory.CashCounter(cc3GO);

            var cc4GO = CreateAt("CashCounter4", new Vector2(-10, 50));
            var cc4Comp = cc4GO.AddComponent<CashCounter>();
            cc4Comp.CounterIndex = 4;
            PrimitiveFactory.CashCounter(cc4GO);

            // ═══════════════════════════════════════════════════════════════════
            //  5. SHELVES (inside store, arranged in rows)
            // ═══════════════════════════════════════════════════════════════════

            // Shelves arranged per reference image zones:
            //   Central Display Aisles: Tomato, Egg, Canned Goods, Processed Corn stands
            //   Bakery & Café (pink):   Bread, Cookie, Milk Fridge, Coffee
            //   Remaining items fill the beige aisles.
            var shelvesList = new List<ShopShelf>();
            int shelfIndex = 0;
            Vector2[] shelfPositions = {
                // Central Display Aisles (produce and canned goods)
                new Vector2(-2, 50),   // Egg Stand
                new Vector2(2, 50),    // Tomato Stand
                new Vector2(6, 50),    // Corn Stand
                new Vector2(10, 50),   // Apple Stand
                new Vector2(-2, 55),   // Canned Tomato Shelf
                new Vector2(2, 55),    // Processed Corn Shelf
                // Bakery & Café (pink zone)
                new Vector2(18, 50),   // Bread Shelf
                new Vector2(22, 50),   // Cookie Display
                new Vector2(26, 50),   // Milk Fridge
                new Vector2(18, 55),   // Bottled Milk
            };

            foreach (Core.ItemType item in System.Enum.GetValues(typeof(Core.ItemType)))
            {
                // Only create shelves for retail items (processed + specific raw ones)
                if (item == Core.ItemType.Wheat || item == Core.ItemType.Dough || 
                    item == Core.ItemType.Milk || item == Core.ItemType.CookieDough)
                    continue;

                Vector2 pos = shelfIndex < shelfPositions.Length
                    ? shelfPositions[shelfIndex]
                    : new Vector2(-5 + shelfIndex * 4, 45);

                var sGO = CreateAt($"Shelf_{item}", pos);
                var shelfComp = sGO.AddComponent<ShopShelf>();
                shelfComp.Item = item;
                shelfComp.Capacity = 20; // per plan.md section 8
                shelvesList.Add(shelfComp);

                Color itemCol = PrimitiveFactory.ItemColor(item);
                bool needsFridge = item == Core.ItemType.BottledMilk;
                if (needsFridge)
                    PrimitiveFactory.FridgeUnit(sGO, itemCol);
                else if (PrimitiveFactory.IsRawItem(item))
                    PrimitiveFactory.CrateDisplay(sGO, itemCol);
                else
                    PrimitiveFactory.ShelfUnit(sGO, itemCol);

                shelfIndex++;
            }

            // ═══════════════════════════════════════════════════════════════════
            //  6. DOORS & DUSTBINS
            // ═══════════════════════════════════════════════════════════════════

            var entryDoor = CreateAt("EntryDoor", new Vector2(-15f, 60.5f));
            PrimitiveFactory.Door(entryDoor, true);

            var exitDoor = CreateAt("ExitDoor", new Vector2(25f, 60.5f));
            PrimitiveFactory.Door(exitDoor, false);

            var officeGO = CreateAt("OfficeDesk", new Vector2(25f, 55f));
            PrimitiveFactory.OfficeDesk(officeGO);

            var nextMartGO = CreateAt("NextMartPreview", new Vector2(35f, 55f));
            PrimitiveFactory.NextMartPreview(nextMartGO);

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

            var rackEgg     = MakeRack(Core.ItemType.Egg,           new Vector2(-18f, 8f));
            var rackTomato  = MakeRack(Core.ItemType.Tomato,        new Vector2(14f, 10f));
            var rackCorn    = MakeRack(Core.ItemType.Corn,          new Vector2(26f, 6f));
            var rackWheat   = MakeRack(Core.ItemType.Wheat,         new Vector2(14f, 6f));
            var rackApple   = MakeRack(Core.ItemType.Apple,         new Vector2(26f, 10f));
            var rackMilk    = MakeRack(Core.ItemType.Milk,          new Vector2(-10f, 8f));
            
            // Intermediate Storage (near machines in Z=32)
            var rackDough       = MakeRack(Core.ItemType.Dough,        new Vector2(5f, 30f));
            var rackCDough      = MakeRack(Core.ItemType.CookieDough,  new Vector2(20f, 30f));
            var rackCTomato     = MakeRack(Core.ItemType.CannedTomato, new Vector2(-10f, 34f));
            var rackPCorn       = MakeRack(Core.ItemType.ProcessedCorn,new Vector2(-5f, 34f));
            var rackBread       = MakeRack(Core.ItemType.Bread,        new Vector2(10f, 34f));
            var rackBMilk       = MakeRack(Core.ItemType.BottledMilk,  new Vector2(15f, 34f));
            var rackCookie      = MakeRack(Core.ItemType.Cookie,       new Vector2(20f, 34f));

            // ═══════════════════════════════════════════════════════════════════
            //  Extended production chain (Blender/Mill/Dairy/Stove/HerbPatch/
            //  LeafProcessor/CoffeeDispenser) — these GameObjects were referenced
            //  by the Gate() purchase-pad calls below and by GameManager's fields
            //  but were never actually instantiated, which meant the file could
            //  never compile once the missing ItemType/MachineType enum values
            //  were added. Building them here, next to their storage racks, same
            //  pattern as the original six machines above.
            // ═══════════════════════════════════════════════════════════════════
            var blenderGO = CreateAt("Blender", new Vector2(-15f, 36f));
            var blenderComp = blenderGO.AddComponent<Machine>();
            blenderComp.Type = Catalog.MachineType.Blender;
            PrimitiveFactory.MachineVisual(blenderGO, "Blender");
            blenderGO.AddComponent<MachineBadge>();
            var rackKetchup = MakeRack(Core.ItemType.TomatoKetchup, new Vector2(-15f, 38f));

            var millGO = CreateAt("WheatMill", new Vector2(-10f, 36f));
            var millComp = millGO.AddComponent<Machine>();
            millComp.Type = Catalog.MachineType.Mill;
            PrimitiveFactory.MachineVisual(millGO, "Mill");
            millGO.AddComponent<MachineBadge>();
            var rackFlour = MakeRack(Core.ItemType.WheatFlour, new Vector2(-10f, 38f));

            var dairyGO = CreateAt("Dairy", new Vector2(-5f, 36f));
            var dairyComp = dairyGO.AddComponent<Machine>();
            dairyComp.Type = Catalog.MachineType.Dairy;
            PrimitiveFactory.MachineVisual(dairyGO, "Dairy");
            dairyGO.AddComponent<MachineBadge>();
            var rackCheese = MakeRack(Core.ItemType.Cheese, new Vector2(-5f, 38f));

            var stoveGO = CreateAt("EggStove", new Vector2(0f, 36f));
            var stoveComp = stoveGO.AddComponent<Machine>();
            stoveComp.Type = Catalog.MachineType.Stove;
            PrimitiveFactory.MachineVisual(stoveGO, "Stove");
            stoveGO.AddComponent<MachineBadge>();
            var rackFried = MakeRack(Core.ItemType.FriedEgg, new Vector2(0f, 38f));

            var leafGO = CreateAt("LeafProcessor", new Vector2(5f, 36f));
            var leafComp = leafGO.AddComponent<Machine>();
            leafComp.Type = Catalog.MachineType.LeafProcessor;
            PrimitiveFactory.MachineVisual(leafGO, "LeafProcessor");
            leafGO.AddComponent<MachineBadge>();
            var rackHerbPk = MakeRack(Core.ItemType.HerbPack, new Vector2(5f, 38f));

            var coffeeGO = CreateAt("CoffeeDispenser", new Vector2(10f, 36f));
            var coffeeComp = coffeeGO.AddComponent<Machine>();
            coffeeComp.Type = Catalog.MachineType.CoffeeDispenser;
            PrimitiveFactory.CoffeeDispenser(coffeeGO);
            coffeeGO.AddComponent<MachineBadge>();
            var rackCoffee = MakeRack(Core.ItemType.Coffee, new Vector2(10f, 38f));

            // Herb patch lives in the farm band next to the other crops, not the
            // factory strip — it's a growable crop, not a machine.
            var herbPatchGO = CreateAt("HerbPatch", new Vector2(20f, 12f));
            var herbPatchComp = herbPatchGO.AddComponent<Production.HerbPatch>();
            PrimitiveFactory.HerbPatch(herbPatchGO);
            var rackHerb = MakeRack(Core.ItemType.Herb, new Vector2(22f, 14f));

            var db1 = CreateAt("Dustbin1", new Vector2(14, 17));
            PrimitiveFactory.Dustbin(db1);

            var db2 = CreateAt("Dustbin2", new Vector2(14, 20));
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
                ? Instantiate(validPrefab, new Vector3(0, 0, 20), Quaternion.identity)
                : CreateAt("Player", new Vector2(0, 20));

            var playerComp = playerGO.GetComponent<PlayerController>()
                ?? playerGO.AddComponent<PlayerController>();
            playerComp.Net = playerGO.GetComponent<NetTool>() ?? playerGO.AddComponent<NetTool>();

            var playerInput = playerGO.GetComponent<PlayerInputHandler>()
                ?? playerGO.AddComponent<PlayerInputHandler>();

            if (playerGO.GetComponent<UI.CarryVisual>() == null) playerGO.AddComponent<UI.CarryVisual>();
            if (playerGO.GetComponent<WobbleAnimator>() == null) playerGO.AddComponent<WobbleAnimator>();

            // Genies Avatar SDK: asynchronously swaps the primitive body for the
            // user's own avatar (or the SDK default when not logged in). Fully
            // defensive — the primitive body stays until/unless the load succeeds.
            if (playerGO.GetComponent<GeniesPlayerSkin>() == null)
                playerGO.AddComponent<GeniesPlayerSkin>();

            // Proximity interactions: harvest / deposit / load machines / stock shelves.
            var interact = playerGO.GetComponent<PlayerInteraction>() ?? playerGO.AddComponent<PlayerInteraction>();
            interact.tomatoFarm = tomatoFarmComp;
            interact.wheatFarm = wheatFarmComp;
            interact.henCoop = henCoopComp;
            interact.cowPen = cowPenComp;
            interact.tomatoCanner = tomatoCannerComp;
            interact.oven = ovenComp;
            interact.doughMixer = doughMixerComp;
            interact.milkBottler = milkBottlerComp;
            interact.cornField = cornFieldComp;
            interact.appleOrchard = appleOrchardComp;
            interact.cornProcessor = cornProcComp;
            interact.cookieLine = cookieComp;
            interact.hayFeedTrough = hayTroughComp;
            interact.shelves = shelvesList;
            interact.bins = new List<Transform> { db1.transform, db2.transform };

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

            // Stocker 1 — uses the dedicated Shelver class (not the generic Worker),
            // matching GameManager.Shelver1's type and its 2-arg Configure(role, inventory).
            var shelver1GO = Spawn(ShelverPrefab, "Stocker1", new Vector2(5, 50));
            var shelver1Comp = shelver1GO.GetComponent<Shelver>() ?? shelver1GO.AddComponent<Shelver>();
            shelver1Comp.AssignedShelves = shelvesList.FindAll(s =>
                RoleCatalog.RoleResponsibilities[Core.RoleType.Shelver1].Contains(s.Item)).ToArray();
            if (shelver1GO.GetComponent<WobbleAnimator>() == null) shelver1GO.AddComponent<WobbleAnimator>();
            if (shelver1GO.GetComponent<UI.CarryVisual>() == null) shelver1GO.AddComponent<UI.CarryVisual>();
            PrimitiveFactory.BuildCharacter(shelver1GO, new Color(0.92f, 0.3f, 0.55f),
                PrimitiveFactory.CharacterRole.Shelver);

            // Stocker 2
            var shelver2GO = Spawn(ShelverPrefab, "Stocker2", new Vector2(20, 50));
            var shelver2Comp = shelver2GO.GetComponent<Shelver>() ?? shelver2GO.AddComponent<Shelver>();
            shelver2Comp.AssignedShelves = shelvesList.FindAll(s =>
                RoleCatalog.RoleResponsibilities[Core.RoleType.Shelver2].Contains(s.Item)).ToArray();
            if (shelver2GO.GetComponent<WobbleAnimator>() == null) shelver2GO.AddComponent<WobbleAnimator>();
            if (shelver2GO.GetComponent<UI.CarryVisual>() == null) shelver2GO.AddComponent<UI.CarryVisual>();
            PrimitiveFactory.BuildCharacter(shelver2GO, new Color(0.85f, 0.2f, 0.85f),
                PrimitiveFactory.CharacterRole.Shelver);

            // Factory Worker — dedicated Chef class (1-arg Configure, applied later in
            // GameManager.Boot once Inventory exists).
            var chefGO = Spawn(ChefPrefab, "FactoryWorker", new Vector2(0, 30));
            var chefComp = chefGO.GetComponent<Chef>() ?? chefGO.AddComponent<Chef>();
            if (chefGO.GetComponent<WobbleAnimator>() == null) chefGO.AddComponent<WobbleAnimator>();
            if (chefGO.GetComponent<UI.CarryVisual>() == null) chefGO.AddComponent<UI.CarryVisual>();
            PrimitiveFactory.BuildCharacter(chefGO, Color.white, true);

            // Harvester — dedicated Farmer class.
            var farmerGO = Spawn(FarmerPrefab, "Harvester", new Vector2(-20, 10));
            var farmerComp = farmerGO.GetComponent<Farmer>() ?? farmerGO.AddComponent<Farmer>();
            if (farmerGO.GetComponent<WobbleAnimator>() == null) farmerGO.AddComponent<WobbleAnimator>();
            if (farmerGO.GetComponent<UI.CarryVisual>() == null) farmerGO.AddComponent<UI.CarryVisual>();
            PrimitiveFactory.BuildCharacter(farmerGO, new Color(0.3f, 0.75f, 0.35f),
                PrimitiveFactory.CharacterRole.Farmer);

            // ═══════════════════════════════════════════════════════════════════
            //  9. MANAGERS (GameManager MUST be created before HUD)
            // ═══════════════════════════════════════════════════════════════════

            // Buyers walk in from the road (Z = 62-78, center 70). Spawn just off
            // the west end of the road so they visibly stride across the pavement
            // before entering the store, matching the reference approach.
            var buyerSpawn = new GameObject("BuyerSpawnSpot");
            buyerSpawn.transform.position = new Vector3(-15, 0, 74f);

            // Exit spot lives on the road too — after checkout the buyer walks out
            // through the north-east door and off along the road, matching the
            // "customers come from and leave via the road" reference behaviour.
            var buyerExit = new GameObject("BuyerExitSpot");
            buyerExit.transform.position = new Vector3(26f, 0, 74f);

            var spawnerGO = new GameObject("BuyerSpawner");
            var spawnerComp = spawnerGO.AddComponent<BuyerSpawner>();
            spawnerComp.BuyerPrefab = BuyerPrefab;
            spawnerComp.EntranceDoor = buyerSpawn.transform;
            spawnerComp.ExitDoor = buyerExit.transform;
            spawnerComp.AllShelves = shelvesList;
            spawnerComp.Counters = new List<CashCounter> { cc1Comp, cc2Comp, cc3Comp, cc4Comp };

            var theftGO = new GameObject("TheftManager");
            var theftComp = theftGO.AddComponent<TheftManager>();
            theftComp.ThiefPrefab = ThiefPrefab;
            theftComp.SpawnPoint = buyerSpawn.transform;
            theftComp.ExitWaypoint = buyerExit.transform;
            theftComp.AllShelves = shelvesList;
            
            // Reference "how pickup van.png": the delivery truck comes in on the
            // road from the WEST, then turns south and parks on a white-marked
            // rectangle on the grass strip just outside the store's west wall
            // (so the player can load it through the west entrance). It reverses
            // back out onto the road when done.
            var vanSpawn = new GameObject("VanSpawnSpot");
            vanSpawn.transform.position = new Vector3(-48f, 0, 70f);

            var vanPickup = new GameObject("VanPickupSpot");
            vanPickup.transform.position = new Vector3(-18f, 0, 55f);

            // Painted white parking rectangle under the van's pickup spot, matching
            // the reference marker.
            PrimitiveFactory.VanParkingSpot(new Vector3(-18f, 0, 55f));

            var pomGO = new GameObject("PhoneOrderManager");
            var pomComp = pomGO.AddComponent<PhoneOrderManager>();
            pomComp.SpawnSpot = vanSpawn.transform;
            pomComp.PickupSpot = vanPickup.transform;

            var gmGO = new GameObject("GameManager");
            gmGO.AddComponent<DataValidator>(); // Run QA validations on boot
            var gmComp = gmGO.AddComponent<GameManager>();
            gmComp.Player = playerComp;
            gmComp.Shelver1 = shelver1Comp;
            gmComp.Shelver2 = shelver2Comp;
            gmComp.Chef = chefComp;
            gmComp.Farmer = farmerComp;
            gmComp.Counters = new List<CashCounter> { cc1Comp, cc2Comp, cc3Comp, cc4Comp };
            gmComp.BuyerSpawner = spawnerComp;
            gmComp.TheftManager = theftComp;
            gmComp.PhoneOrderManager = pomComp;
            gmComp.TomatoFarm = tomatoFarmComp;
            gmComp.WheatFarm = wheatFarmComp;
            gmComp.HenCoop = henCoopComp;
            gmComp.Blender = blenderComp;
            gmComp.Oven = ovenComp;
            gmComp.Mill = millComp;
            gmComp.Dairy = dairyComp;
            gmComp.CowPen = cowPenComp;
            gmComp.HerbPatch = herbPatchComp;
            gmComp.LeafProcessor = leafComp;
            gmComp.Stove = stoveComp;
            gmComp.CornProcessor = cornProcComp;
            gmComp.CookieStation = cookieComp;
            gmComp.CoffeeDispenser = coffeeComp;
            gmComp.CornField = cornFieldComp;
            gmComp.HayFeedTrough = hayTroughComp;
            gmComp.AssistantNode = assistantComp;

            // ═══════════════════════════════════════════════════════════════════
            //  10. HUD — built AFTER GameManager so HUDController.Awake can find it
            // ═══════════════════════════════════════════════════════════════════
            HUDBuilder.Build(playerInput);

            // ═══════════════════════════════════════════════════════════════════
            //  11. PROGRESSIVE EXPANSION (reference flow)
            //  The lot starts nearly empty: tomato plot, tomato stand, counter 1,
            //  storage depot. Everything else sits behind an arrow-marked purchase
            //  pad the player walks onto to buy (cost drains while standing).
            // ═══════════════════════════════════════════════════════════════════
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

            // Progressive disclosure (reference-game drip feed): a fresh player sees
            // only the level-1 pads; each store level-up reveals the next batch.
            // Keeps the map readable for young/new players and gives every level-up
            // a visible reward burst.
            Gate(15f,  1, "Hire Farmer",    farmerGO);
            Gate(25f,  1, "Hen Coop",       henCoopGO, ShelfOf(Core.ItemType.Egg)?.gameObject, rackEgg.gameObject);
            Gate(40f,  1, "Hire Shelver A", shelver1GO);
            Gate(50f,  2, "Wheat Farm",     wheatFarmGO, ShelfOf(Core.ItemType.Wheat)?.gameObject, rackWheat.gameObject);
            Gate(75f,  2, "Blender",        blenderGO, ShelfOf(Core.ItemType.TomatoKetchup)?.gameObject, rackKetchup.gameObject);
            Gate(60f,  2, "Hire Shelver B", shelver2GO);
            Gate(125f, 2, "Wheat Mill",     millGO, ShelfOf(Core.ItemType.WheatFlour)?.gameObject, rackFlour.gameObject);
            Gate(150f, 3, "Hire Chef",      chefGO);
            Gate(100f, 3, "Cow Pen",        cowPenGO, ShelfOf(Core.ItemType.Milk)?.gameObject, rackMilk.gameObject);
            Gate(175f, 4, "Dairy",          dairyGO, ShelfOf(Core.ItemType.Cheese)?.gameObject, rackCheese.gameObject);
            Gate(200f, 3, "Bread Oven",     ovenGO, ShelfOf(Core.ItemType.Bread)?.gameObject, rackBread.gameObject);
            Gate(110f, 4, "Egg Stove",      stoveGO, ShelfOf(Core.ItemType.FriedEgg)?.gameObject, rackFried.gameObject);
            Gate(90f,  4, "Herb Patch",     herbPatchGO, ShelfOf(Core.ItemType.Herb)?.gameObject, rackHerb.gameObject);
            Gate(140f, 5, "Leaf Unit",      leafGO, ShelfOf(Core.ItemType.HerbPack)?.gameObject, rackHerbPk.gameObject);
            Gate(300f, 3, "Counter 2",      cc2GO); // batch 35: L4→L3 (queue-overflow fix)
            Gate(400f, 5, "Counter 3",      cc3GO);
            Gate(500f, 6, "Counter 4",      cc4GO);
            // New map spec content: corn chain + bakery/café.
            Gate(120f, 2, "Corn Field",      cornFieldGO, ShelfOf(Core.ItemType.Corn)?.gameObject, rackCorn.gameObject);
            Gate(65f,  2, "Apple Orchard",   appleOrchardGO, ShelfOf(Core.ItemType.Apple)?.gameObject, rackApple.gameObject);
            // Legacy machines/shelves that previously started active (off-pattern
            // clutter at L1): gate them like everything else.
            Gate(80f,  2, "Dough Mixer",     doughMixerGO, rackDough.gameObject);
            Gate(120f, 3, "Milk Bottler",    milkBottlerGO, ShelfOf(Core.ItemType.BottledMilk)?.gameObject, rackBMilk.gameObject);
            Gate(130f, 4, "Tomato Canner",   tomatoCannerGO, ShelfOf(Core.ItemType.CannedTomato)?.gameObject, rackCTomato.gameObject);
            Gate(180f, 3, "Corn Processor",  cornProcGO, ShelfOf(Core.ItemType.ProcessedCorn)?.gameObject, rackPCorn.gameObject);
            Gate(250f, 5, "Assistant Node",  assistantGO);
            Gate(160f, 3, "Hay Trough",      hayTroughGO);
            Gate(220f, 5, "Cookie Station",  cookieGO, ShelfOf(Core.ItemType.Cookie)?.gameObject, rackCookie.gameObject);
            Gate(280f, 6, "Coffee",          coffeeGO, ShelfOf(Core.ItemType.Coffee)?.gameObject, rackCoffee.gameObject);
            // Spec §3.4: NEXT MART expansion pad at the far right. Cost $880 to
            // "unlock" the preview tile — the truck + tan slab remain visible as
            // a "future store" indicator once purchased. Full second-store build
            // is v1.1 content; this pad just clears the marker for now.
            Gate(880f, 6, "Next Mart",      nextMartGO);

            // First-run guided onboarding: bouncing arrow + banner walking a brand
            // new player through harvest → stock → collect → build. Skipped for
            // returning players (save exists).
            if (!MiniMart.Save.SaveSystem.HasSave())
                new GameObject("TutorialGuide").AddComponent<TutorialGuide>();

            // Retention loops: daily bonus + rotating "serve N customers" goals.
            new GameObject("Retention").AddComponent<Retention>();

            // Calm shop BGM loop (Resources/Music/shop_loop) — no-ops if missing.
            AudioFx.StartMusic();

            Debug.Log("[SceneBootstrapper] Scene fully bootstrapped with My Mini Mall visuals.");
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static void BuildBoundaryWalls(GridPathfinder pf)
        {
            int w = pf.GridWidth, h = pf.GridHeight;

            // All blocking below is expressed in WORLD coordinates and converted
            // through the grid origin (-50, 0, 0). The previous version used raw
            // grid-cell numbers written for the original tiny map, which — after
            // the batch-21 map expansion moved the origin — landed as invisible
            // walls strewn across the farm. Players slid into nothing and stopped,
            // which read as "bounded in a specific area".
            void BlockWorld(float wx, float wz)
            {
                int cx = Mathf.RoundToInt(wx) + 50;   // origin.x = -50
                int cz = Mathf.RoundToInt(wz);        // origin.z = 0
                if (cx >= 0 && cx < w && cz >= 0 && cz < h)
                    pf.SetWalkable(cx, cz, false);
            }

            // Outer border of the whole 100×82 world.
            for (int x = 0; x < w; x++) { pf.SetWalkable(x, 0, false); pf.SetWalkable(x, h - 1, false); }
            for (int y = 0; y < h; y++) { pf.SetWalkable(0, y, false); pf.SetWalkable(w - 1, y, false); }

            // ── Store walls (visuals: x -20..30, z 40..60) ──
            // South wall z=40 with the farm↔store service opening at x 1..9
            // (matches the visible gap between Wall_South_A and Wall_South_B).
            for (int wx = -20; wx <= 30; wx++)
            {
                if (wx >= 1 && wx <= 9) continue;
                BlockWorld(wx, 40f);
            }

            // North wall z=60 with two customer door gaps aligned to the actual
            // Door objects: entry at x=-15, exit at x=25 (each 3 units wide).
            for (int wx = -20; wx <= 30; wx++)
            {
                if (wx >= -16 && wx <= -14) continue; // entry door gap
                if (wx >= 24  && wx <= 26)  continue; // exit door gap
                BlockWorld(wx, 60f);
            }

            // West wall x=-20 with the delivery-gate gap at z 53..57 (matches the
            // Wall_West_N / Wall_West_S split + striped gate from batch 23).
            for (int wz = 40; wz <= 60; wz++)
            {
                if (wz >= 53 && wz <= 57) continue;
                BlockWorld(-20f, wz);
            }

            // East wall x=30, solid (matches the visual Wall_East).
            for (int wz = 40; wz <= 60; wz++)
                BlockWorld(30f, wz);

            // NOTE: the old hen-pen / cow-pen fence blocks were deliberately NOT
            // re-added — their visual fences no longer exist at those coordinates,
            // and animals are stationary props. If pen fences return visually,
            // block them here in world coords.
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