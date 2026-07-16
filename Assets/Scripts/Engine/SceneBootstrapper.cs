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
#if UNITY_EDITOR
            // QA hook: marker files in Logs/ drive unattended tester runs when
            // the editor can't be scripted externally. `testerbot.freshrun`
            // wipes the save (consumed once); `testerbot.enabled` spawns the bot.
            string logsDir = System.IO.Path.GetFullPath(Application.dataPath + "/../Logs");
            string fresh = System.IO.Path.Combine(logsDir, "testerbot.freshrun");
            if (System.IO.File.Exists(fresh))
            {
                MiniMart.Save.SaveSystem.Delete();
                System.IO.File.Delete(fresh);
            }
            if (System.IO.File.Exists(System.IO.Path.Combine(logsDir, "testerbot.enabled"))
                && FindAnyObjectByType<Engine.TesterBot>() == null)
                new GameObject("TesterBot").AddComponent<Engine.TesterBot>();
#endif
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
            // Flanks beside the store (map plan: buyers walk the west flank down to
            // the entry doors and leave along the east flank back to the road).
            PrimitiveFactory.GrassGround(new Vector3(0f, 0, 50f), 100f, 20f);

            // Store floor tiles cover the WHOLE enclosed building so the shop and
            // the farm/processing yard read as one interior, not a store sitting
            // on open grass. Upper = shop floor (z 36..60), lower = farm yard
            // floor (z 14..36) under the plots and machines.
            PrimitiveFactory.StoreFloor(new Vector3(-5f, 0, 48f), 30f, 24f);   // shop floor  z 36..60
            PrimitiveFactory.StoreFloor(new Vector3(-5f, 0, 25f), 30f, 22f);   // farm yard   z 14..36

            // Broad asphalt road along the north edge (Z = 62 to 78 — 16 units wide,
            // 100 units long). Buyers walk in along it and delivery trucks drive down
            // it, per the reference map.
            PrimitiveFactory.Road(new Vector3(0f, 0, 70f), 100f, 16f);

            // Perimeter trees. Ring the enlarged lot (X ±50, Z 0-82).
            PrimitiveFactory.TreePerimeter(-50f, 50f, 0f, 82f, 3.4f);

            // ── Store walls (Mart 1) ──
            // Map plan (Refer/map-plan-mart1.pdf): the WHOLE building is one
            // enclosed rectangle x[-20,10] z[14,60]. The shop floor is the upper
            // half (z 36..60); the farm+processing zone is the lower half
            // (z 14..36), separated ONLY by an interior divider wall at z=36 that
            // has one middle service gap (x 1..9). Entry doors on the WEST wall,
            // exit doors on the EAST wall — those are the only openings, so
            // nothing (farms, machines) sits outside the building any more.
            PrimitiveFactory.Wall("Wall_Interior_A", new Vector3(-9.5f, 0, 36f), new Vector3(21f, 0.85f, 0.3f)); // x -20..1
            PrimitiveFactory.Wall("Wall_Interior_B", new Vector3( 9.5f, 0, 36f), new Vector3(1f,  0.85f, 0.3f)); // x 9..10
            PrimitiveFactory.Wall("Wall_North",  new Vector3(-5f, 0, 60f), new Vector3(30f, 0.85f, 0.3f));       // z=60 closed
            PrimitiveFactory.Wall("Wall_South",  new Vector3(-5f, 0, 14f), new Vector3(30f, 0.85f, 0.3f));       // z=14 closed
            // West wall x=-20, z[14,60], entry-door gaps at z[44,46] and z[54,56].
            PrimitiveFactory.Wall("Wall_West_1", new Vector3(-20f, 0, 29f),   new Vector3(0.3f, 0.85f, 30f)); // z 14..44
            PrimitiveFactory.Wall("Wall_West_2", new Vector3(-20f, 0, 50f),   new Vector3(0.3f, 0.85f, 8f));  // z 46..54
            PrimitiveFactory.Wall("Wall_West_3", new Vector3(-20f, 0, 58f),   new Vector3(0.3f, 0.85f, 4f));  // z 56..60
            // East wall x=10, same z-span, exit-door gaps at z[44,46] and z[54,56].
            PrimitiveFactory.Wall("Wall_East_1", new Vector3( 10f, 0, 29f),   new Vector3(0.3f, 0.85f, 30f));
            PrimitiveFactory.Wall("Wall_East_2", new Vector3( 10f, 0, 50f),   new Vector3(0.3f, 0.85f, 8f));
            PrimitiveFactory.Wall("Wall_East_3", new Vector3( 10f, 0, 58f),   new Vector3(0.3f, 0.85f, 4f));



            // Red-white candy-striped angled barrier marking the west delivery entrance.
            PrimitiveFactory.StripedGate(new Vector3(-20f, 0, 55f), 5f);
            
            // Pathfinder covers the enlarged 100 x 82 world (extra breadth for the
            // wider road + tree perimeter push to Z=82).
            var pfGO = new GameObject("Pathfinder");
            var pfComp = pfGO.AddComponent<GridPathfinder>();
            pfComp.GridOrigin = new Vector3(-50f, 0, 0f);
            pfComp.Initialize(100, 82, 1f);
            BuildBoundaryWalls(pfComp);

            // Bake the navigation mesh AFTER every wall is stamped into the grid:
            // it merges the walkable cells into convex polygons + portals, which is
            // what lets workers walk smooth diagonals instead of 90° staircases.
            var navGO = new GameObject("NavMesh");
            var nav = navGO.AddComponent<Map.NavMesh>();
            nav.Bake(pfComp);

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

            // Map plan farm row (left→right): Tomato Farm, Wheat Farm, Hen. All
            // inside the enclosed lower half of the building (z ~22..28), close
            // to the shop's interior gap — not stranded on distant grass.
            var henCoopGO = CreateAt("HenCoop", new Vector2(5f, 24f));
            var henCoopComp = henCoopGO.AddComponent<HenCoop>();
            PrimitiveFactory.HenCoop(henCoopGO);

            var cowPenGO = CreateAt("CowPen", new Vector2(-17f, 20f));   // retired in Mart 1
            var cowPenComp = cowPenGO.AddComponent<CowPen>();
            PrimitiveFactory.CowPen(cowPenGO);

            var hayTroughGO = CreateAt("HayFeedTrough", new Vector2(-17f, 22f));
            var hayTroughComp = hayTroughGO.AddComponent<HayFeedTrough>();
            hayTroughComp.LinkedCow = cowPenComp;
            cowPenComp.FedBy = hayTroughComp;
            PrimitiveFactory.HayFeedTrough(hayTroughGO);

            // ═══════════════════════════════════════════════════════════════════
            //  HUB CENTER — 3 in-world upgrade pads + "purchasable plots" outline.
            //  Reference image center-bottom.
            // ═══════════════════════════════════════════════════════════════════
            // Player upgrade pads — a tidy row along the enclosed farm zone's
            // bottom edge (inside the walls), not stranded on distant grass.
            UpgradePad.Create(new Vector3(-16f, 0f, 17f), UpgradePad.Kind.PlayerSpeed);
            UpgradePad.Create(new Vector3(-13f, 0f, 17f), UpgradePad.Kind.PlayerCarry);
            UpgradePad.Create(new Vector3(-10f, 0f, 17f), UpgradePad.Kind.CropSpeed);

            // ═══════════════════════════════════════════════════════════════════
            //  FARM ROW (inside the building's lower half, left→right per map)
            // ═══════════════════════════════════════════════════════════════════
            var tomatoFarmGO = CreateAt("TomatoFarm", new Vector2(-15f, 25f));
            var tomatoFarmComp = tomatoFarmGO.AddComponent<TomatoFarm>();
            PrimitiveFactory.TomatoFarm(tomatoFarmGO);

            var wheatFarmGO = CreateAt("WheatFarm", new Vector2(-5f, 25f));
            var wheatFarmComp = wheatFarmGO.AddComponent<WheatFarm>();
            PrimitiveFactory.WheatFarm(wheatFarmGO);



            // ═══════════════════════════════════════════════════════════════════
            //  PROCESSING AREA — Factory center: (0, 0, 32)
            // ═══════════════════════════════════════════════════════════════════
            var factoryGO = CreateAt("ProcessingFactory", new Vector2(7f, 32f));
            PrimitiveFactory.ProcessingFactory(factoryGO);


            
            var doughMixerGO = CreateAt("DoughMixer", new Vector2(7f, 30f));
            var doughMixerComp = doughMixerGO.AddComponent<Machine>();
            doughMixerComp.Type = Catalog.MachineType.DoughMixer;
            PrimitiveFactory.MachineVisual(doughMixerGO, "DoughMixer");
            doughMixerGO.AddComponent<MachineBadge>();

            var ovenGO = CreateAt("Oven", new Vector2(2f, 52f));
            var ovenComp = ovenGO.AddComponent<Machine>();
            ovenComp.Type = Catalog.MachineType.Oven;
            PrimitiveFactory.MachineVisual(ovenGO, "Oven");
            ovenGO.AddComponent<MachineBadge>();

            var milkBottlerGO = CreateAt("MilkBottler", new Vector2(9f, 30f));
            var milkBottlerComp = milkBottlerGO.AddComponent<Machine>();
            milkBottlerComp.Type = Catalog.MachineType.MilkBottler;
            PrimitiveFactory.MachineVisual(milkBottlerGO, "MilkBottler");
            milkBottlerGO.AddComponent<MachineBadge>();





            // ═══════════════════════════════════════════════════════════════════
            //  4. CASH COUNTERS (inside store, front area)
            // ═══════════════════════════════════════════════════════════════════

            var cc1GO = CreateAt("CashCounter1", new Vector2(-16, 51));
            var cc1Comp = cc1GO.AddComponent<CashCounter>();
            cc1Comp.CounterIndex = 1;
            PrimitiveFactory.CashCounter(cc1GO);

            var cc2GO = CreateAt("CashCounter2", new Vector2(7, 46));
            var cc2Comp = cc2GO.AddComponent<CashCounter>();
            cc2Comp.CounterIndex = 2;
            PrimitiveFactory.CashCounter(cc2GO);



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
                // Mart 1 (Basic Items)
                new Vector2(-5, 52),   // Tomato       (map: center)
                new Vector2(2, 47),    // Egg          (map: lower right-of-center)
                new Vector2(-14, 57),  // TomatoKetchup (map: top-left)
                new Vector2(2, 57),    // Wheat        (map: top-right)
                new Vector2(-5, 57),   // WheatFlour   (map: top-center)
                new Vector2(5, 52),    // Bread        (map: beside its oven)
                new Vector2(-17, 44),  // BottledMilk  (retired in Mart 1)
                new Vector2(-2, 47),   // FriedEgg     (not on map; egg row)
            };

            foreach (Core.ItemType item in System.Enum.GetValues(typeof(Core.ItemType)))
            {
                // Skip non-retail items AND Mart 2 items
                if (item == Core.ItemType.Dough || 
                    item == Core.ItemType.Milk || item == Core.ItemType.CookieDough)
                    continue;
                
                if (item == Core.ItemType.Apple || item == Core.ItemType.Corn || item == Core.ItemType.ProcessedCorn ||
                    item == Core.ItemType.CannedTomato || item == Core.ItemType.Herb || item == Core.ItemType.Cheese ||
                    item == Core.ItemType.HerbPack || item == Core.ItemType.Cookie || item == Core.ItemType.Coffee)
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

            // Map plan: buyers ENTER through the west wall and EXIT through the
            // east wall; a door marks each wall gap (z 54..56 and z 44..46).
            var entryDoorN = CreateAt("EntryDoor", new Vector2(-20f, 55f));
            PrimitiveFactory.Door(entryDoorN, true);
            var entryDoorS = CreateAt("EntryDoor2", new Vector2(-20f, 45f));
            PrimitiveFactory.Door(entryDoorS, true);
            var exitDoorN = CreateAt("ExitDoor", new Vector2(10f, 55f));
            PrimitiveFactory.Door(exitDoorN, false);
            var exitDoorS = CreateAt("ExitDoor2", new Vector2(10f, 45f));
            PrimitiveFactory.Door(exitDoorS, false);



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

            var rackEgg     = MakeRack(Core.ItemType.Egg, new Vector2(5f, 28f));
            var rackTomato  = MakeRack(Core.ItemType.Tomato, new Vector2(-15f, 28f));
            var rackWheat   = MakeRack(Core.ItemType.Wheat, new Vector2(-5f, 28f));
            var rackMilk    = MakeRack(Core.ItemType.Milk, new Vector2(-17f, 18f));
            
            // Intermediate Storage (near machines in Z=32)
            var rackDough       = MakeRack(Core.ItemType.Dough, new Vector2(7f, 28f));
            var rackBread       = MakeRack(Core.ItemType.Bread, new Vector2(2f, 49f));
            var rackBMilk       = MakeRack(Core.ItemType.BottledMilk, new Vector2(9f, 28f));

            // ═══════════════════════════════════════════════════════════════════
            //  Extended production chain (Blender/Mill/Dairy/Stove/HerbPatch/
            //  LeafProcessor/CoffeeDispenser) — these GameObjects were referenced
            //  by the Gate() purchase-pad calls below and by GameManager's fields
            //  but were never actually instantiated, which meant the file could
            //  never compile once the missing ItemType/MachineType enum values
            //  were added. Building them here, next to their storage racks, same
            //  pattern as the original six machines above.
            // ═══════════════════════════════════════════════════════════════════
            var blenderGO = CreateAt("Blender", new Vector2(-15f, 31f));
            var blenderComp = blenderGO.AddComponent<Machine>();
            blenderComp.Type = Catalog.MachineType.Blender;
            PrimitiveFactory.MachineVisual(blenderGO, "Blender");
            blenderGO.AddComponent<MachineBadge>();
            var rackKetchup = MakeRack(Core.ItemType.TomatoKetchup, new Vector2(-15f, 33f));

            var millGO = CreateAt("WheatMill", new Vector2(-9f, 31f));
            var millComp = millGO.AddComponent<Machine>();
            millComp.Type = Catalog.MachineType.Mill;
            PrimitiveFactory.MachineVisual(millGO, "Mill");
            millGO.AddComponent<MachineBadge>();
            var rackFlour = MakeRack(Core.ItemType.WheatFlour, new Vector2(-9f, 33f));

            var stoveGO = CreateAt("EggStove", new Vector2(-2f, 31f));
            var stoveComp = stoveGO.AddComponent<Machine>();
            stoveComp.Type = Catalog.MachineType.Stove;
            PrimitiveFactory.MachineVisual(stoveGO, "Stove");
            stoveGO.AddComponent<MachineBadge>();
            var rackFried = MakeRack(Core.ItemType.FriedEgg, new Vector2(-2f, 33f));

            // ── Owner economy spec: Mart-1 stations are 4-in / 4-out, each with two
            //    capacity upgrade tracks (input buffer + output buffer, 4 → 6 → 8) at
            //    the owner's costs. MegaMart machines are untouched (SplitCapacity
            //    stays false there, so they keep the old single-track behaviour).
            void ConfigStation(Machine m, int[] inCosts, int[] outCosts)
            {
                if (m == null) return;
                m.SplitCapacity = true;
                m.InputUpgradeCosts = inCosts;
                m.OutputUpgradeCosts = outCosts;
            }
            ConfigStation(blenderComp, Catalog.StationCatalog.BlenderInput, Catalog.StationCatalog.BlenderOutput);
            ConfigStation(millComp,    Catalog.StationCatalog.MillInput,    Catalog.StationCatalog.MillOutput);
            ConfigStation(stoveComp,   Catalog.StationCatalog.StoveInput,   Catalog.StationCatalog.StoveOutput);
            ConfigStation(ovenComp,    Catalog.StationCatalog.OvenInput,    Catalog.StationCatalog.OvenOutput);
            ovenComp.InputItem2 = Core.ItemType.Egg;   // Bread = Flour (input 1) + Egg (input 2)

            var db1 = CreateAt("Dustbin1", new Vector2(8f, 18f));
            PrimitiveFactory.Dustbin(db1);

            var db2 = CreateAt("Dustbin2", new Vector2(8f, 20f));
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
                ? Instantiate(validPrefab, new Vector3(0, 0, 22), Quaternion.identity)
                : CreateAt("Player", new Vector2(0, 22));

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
            interact.oven = ovenComp;
            interact.doughMixer = doughMixerComp;
            interact.milkBottler = milkBottlerComp;
            interact.blender = blenderComp;
            interact.mill = millComp;
            interact.stove = stoveComp;
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
            shelver1Comp.Hen = henCoopComp;   // owner spec: shelver also feeds the hen
            if (shelver1GO.GetComponent<WobbleAnimator>() == null) shelver1GO.AddComponent<WobbleAnimator>();
            if (shelver1GO.GetComponent<UI.CarryVisual>() == null) shelver1GO.AddComponent<UI.CarryVisual>();
            PrimitiveFactory.BuildCharacter(shelver1GO, new Color(0.92f, 0.3f, 0.55f),
                PrimitiveFactory.CharacterRole.Shelver);



            // Factory Worker — dedicated Chef class (1-arg Configure, applied later in
            // GameManager.Boot once Inventory exists).
            var chefGO = Spawn(ChefPrefab, "FactoryWorker", new Vector2(-2, 30));
            var chefComp = chefGO.GetComponent<Chef>() ?? chefGO.AddComponent<Chef>();
            if (chefGO.GetComponent<WobbleAnimator>() == null) chefGO.AddComponent<WobbleAnimator>();
            if (chefGO.GetComponent<UI.CarryVisual>() == null) chefGO.AddComponent<UI.CarryVisual>();
            PrimitiveFactory.BuildCharacter(chefGO, Color.white, true);

            // Harvester — dedicated Farmer class.
            var farmerGO = Spawn(FarmerPrefab, "Harvester", new Vector2(-2, 20));
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
            buyerSpawn.transform.position = new Vector3(-30f, 0, 66f);

            // Exit spot lives on the road too — after checkout the buyer walks out
            // through the north-east door and off along the road, matching the
            // "customers come from and leave via the road" reference behaviour.
            var buyerExit = new GameObject("BuyerExitSpot");
            buyerExit.transform.position = new Vector3(20f, 0, 66f);

            var spawnerGO = new GameObject("BuyerSpawner");
            var spawnerComp = spawnerGO.AddComponent<BuyerSpawner>();
            spawnerComp.BuyerPrefab = BuyerPrefab;
            spawnerComp.EntranceDoor = buyerSpawn.transform;
            spawnerComp.ExitDoor = buyerExit.transform;
            spawnerComp.AllShelves = shelvesList;
            spawnerComp.Counters = new List<CashCounter> { cc1Comp, cc2Comp };

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
            vanSpawn.transform.position = new Vector3(-48f, 0, 55f);

            var vanPickup = new GameObject("VanPickupSpot");
            vanPickup.transform.position = new Vector3(-23f, 0, 55f);

            // Painted white parking rectangle under the van's pickup spot, matching
            // the reference marker.
            PrimitiveFactory.VanParkingSpot(new Vector3(-23f, 0, 55f));

            var pomGO = new GameObject("PhoneOrderManager");
            var pomComp = pomGO.AddComponent<PhoneOrderManager>();
            pomComp.SpawnSpot = vanSpawn.transform;
            pomComp.PickupSpot = vanPickup.transform;
            pomComp.AllShelves = shelvesList;

            var gmGO = new GameObject("GameManager");
            gmGO.AddComponent<DataValidator>(); // Run QA validations on boot
            var gmComp = gmGO.AddComponent<GameManager>();
            gmComp.Player = playerComp;
            gmComp.Shelver1 = shelver1Comp;
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
            gmComp.CowPen = cowPenComp;
            gmComp.Stove = stoveComp;
            gmComp.HayFeedTrough = hayTroughComp;

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

            // ══════════ TWO-MART SPLIT ══════════
            // Mart 1 "Mini Mart" (Game.unity):  tomato, egg, wheat chains.
            // Mart 2 "MegaMart" (MegaMart.unity): milk, corn, herb, coffee,
            //   apple chains — a separate PLACE like the reference's
            //   "GO TO Cafe Mart". Shared cash/XP, travel pads both ways.
            // Unlock pacing per user feedback: at most THREE pads per level,
            // ladder spans L1-L10 across the two marts.
            bool isMegaMart = UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().name.Contains("MegaMart");

            void Retire(params GameObject[] gos)
            {
                foreach (var g in gos) if (g != null) Object.Destroy(g);
            }

            // Dough Mixer chain retired from both marts: redundant with the
            // Mill→Oven bread path and its Dough SKU has no shelf.
            Retire(doughMixerGO, rackDough.gameObject);

            if (!isMegaMart)
            {
                // ── MART 1 ladder (user's exact pacing) ──
                Gate(15f,  1, "Hire Farmer",    farmerGO);
                Gate(25f,  1, "Hen Coop",       henCoopGO, ShelfOf(Core.ItemType.Egg)?.gameObject, rackEgg.gameObject);
                Gate(40f,  1, "Hire Shelver",   shelver1GO);

                // Owner rule: every cooking machine unlocks AFTER the chef exists
                // (chef hires at L4, kitchen opens at L5) and never more than 3
                // unlocks per level.
                Gate(300f, 2, "Counter 2",      cc2GO);

                Gate(50f,  3, "Wheat Farm",     wheatFarmGO, ShelfOf(Core.ItemType.Wheat)?.gameObject, rackWheat.gameObject);

                Gate(125f, 4, "Wheat Mill",     millGO, ShelfOf(Core.ItemType.WheatFlour)?.gameObject, rackFlour.gameObject);
                Gate(150f, 4, "Hire Chef",      chefGO);

                Gate(75f,  5, "Ketchup Blender", blenderGO, ShelfOf(Core.ItemType.TomatoKetchup)?.gameObject, rackKetchup.gameObject);
                Gate(200f, 5, "Bread Oven",     ovenGO, ShelfOf(Core.ItemType.Bread)?.gameObject, rackBread.gameObject);
                Gate(110f, 5, "Egg Stove",      stoveGO, ShelfOf(Core.ItemType.FriedEgg)?.gameObject, rackFried.gameObject);

                // L6: the big milestone — the road to MegaMart opens.
                var travelPad = SceneTransition.Create(new Vector3(35f, 0, 55f), "MegaMart", "GO TO MEGAMART").gameObject;
                Gate(500f, 6, "MegaMart",       travelPad);

                // Milk chain lives in MegaMart now — remove it from this scene.
                Retire(cowPenGO, hayTroughGO, milkBottlerGO,
                       rackMilk.gameObject, rackBMilk.gameObject,
                       ShelfOf(Core.ItemType.Milk)?.gameObject,
                       ShelfOf(Core.ItemType.BottledMilk)?.gameObject);
            }

            // First-run guided onboarding: bouncing arrow + banner walking a brand
            // new player through harvest → stock → collect → build. Skipped for
            // returning players (save exists).
            if (!MiniMart.Save.SaveSystem.HasSave())
                new GameObject("TutorialGuide").AddComponent<TutorialGuide>();

            // ── Locator icons (real-user feedback: "map is too big, can't find
            // the tomato farm"). A giant floating item silhouette above every
            // production source, readable from across the lot. Parented to the
            // source GameObject, so gated sources keep their icon hidden until
            // the unlock pad is bought — matching the reference's "locked
            // content is invisible until reached" rule.
            void Locator(GameObject source, Core.ItemType item)
            {
                if (source == null) return;
                var icon = PrimitiveFactory.ItemMesh(item, source.transform, new Vector3(0, 3.4f, 0), 3.5f);
                icon.name = $"Locator_{item}";
                icon.AddComponent<LocatorBob>();
            }
            Locator(tomatoFarmGO,   Core.ItemType.Tomato);
            Locator(wheatFarmGO,    Core.ItemType.Wheat);
            Locator(henCoopGO,      Core.ItemType.Egg);
            Locator(cowPenGO,       Core.ItemType.Milk);

            // Retention loops: daily bonus + rotating "serve N customers" goals.
            new GameObject("Retention").AddComponent<Retention>();
            new GameObject("UnlockGuide").AddComponent<UnlockGuide>();

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

            // ── Building walls — MUST match the visual walls in Bootstrap() ──
            // One enclosed rectangle x[-20,10] z[14,60]. These grid blocks are what
            // actually stop characters (visual walls are just meshes), so they and
            // the meshes have to agree cell-for-cell or players slide into nothing.

            // Interior divider z=36 (shop floor ↕ farm zone), service gap x 1..9.
            for (int wx = -20; wx <= 10; wx++)
            {
                if (wx >= 1 && wx <= 9) continue;
                BlockWorld(wx, 36f);
            }

            // North wall z=60 and South wall z=14: both fully closed.
            for (int wx = -20; wx <= 10; wx++)
            {
                BlockWorld(wx, 60f);
                BlockWorld(wx, 14f);
            }

            // West wall x=-20 and East wall x=10, z[14,60], with the two door
            // gaps (z 54..56 entry/exit-north, z 44..46 entry/exit-south). The
            // farm-zone stretch of these walls (z 14..44) is fully closed, so the
            // only way into the farm zone is the interior gap from the shop floor.
            for (int wz = 14; wz <= 60; wz++)
            {
                if (wz >= 54 && wz <= 56) continue;
                if (wz >= 44 && wz <= 46) continue;
                BlockWorld(-20f, wz);
                BlockWorld(10f, wz);
            }

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

    /// <summary>Gentle float + slow spin for the farm locator icons so they
    /// read as living markers rather than static debris.</summary>
    public class LocatorBob : MonoBehaviour
    {
        private Vector3 basePos;
        private float t;
        private void Start() => basePos = transform.localPosition;
        private void Update()
        {
            t += Time.deltaTime;
            transform.localPosition = basePos + Vector3.up * (Mathf.Sin(t * 2f) * 0.25f);
            transform.Rotate(0f, 40f * Time.deltaTime, 0f);
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