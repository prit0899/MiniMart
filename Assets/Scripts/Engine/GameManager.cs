using System.Collections.Generic;
using UnityEngine;
using MiniMart.Core;
using MiniMart.Catalog;
using MiniMart.Runtime;
using MiniMart.Economy;
using MiniMart.Characters;
using MiniMart.AI;
using MiniMart.Production;
using MiniMart.Engine;
using MiniMart.Map;
using MiniMart.Save;

namespace MiniMart
{
    /// <summary>
    /// The single entry point that wires every subsystem together and drives the fixed simulation tick.
    /// Architecture Spec Section 4: simulation tick at 100-200ms; render loop at 60fps;
    /// production/AI decisions are tick-based; movement is frame-interpolated.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Simulation")]
        public float SimTickSeconds = 0.15f;  // 150ms fixed tick
        private float tickAccumulator;

        [Header("Quality")]
        public QualityPreset Quality = QualityPreset.Balanced;

        [Header("Scene references — assign in inspector")]
        public PlayerController Player;
        public Shelver Shelver1;
        public Shelver Shelver2;
        public Chef Chef;
        public Farmer Farmer;
        public List<Economy.CashCounter> Counters = new List<CashCounter>();
        public BuyerSpawner BuyerSpawner;
        public TheftManager TheftManager;
        public PhoneOrderManager PhoneOrderManager;
        public MapLayout MapLayout;
        public GridPathfinder Pathfinder;

        [Header("Production nodes")]
        public TomatoFarm TomatoFarm;
        public WheatFarm WheatFarm;
        public HenCoop HenCoop;
        public CowPen CowPen;
        public HerbPatch HerbPatch;
        public Machine Blender;
        public Machine Oven;
        public Machine Mill;
        public Machine Dairy;
        public Machine LeafProcessor;
        public Machine Stove;
        // New map spec (bakery/café + corn chain).
        public Machine CornProcessor;
        public Machine CookieStation;
        public Machine CoffeeDispenser;
        public Production.CornField CornField;
        public Production.HayFeedTrough HayFeedTrough;
        public AI.AssistantNode AssistantNode;

        // Core runtime systems
        public StoreInventory Inventory { get; private set; }
        public EconomyManager Economy { get; private set; }

        // ── Store progression (cherry-picked from the Mini Farm Market spec) ──
        // Store level drives ALL unlocks (items, cashiers, thief, spawn pacing).
        // The player's own upgrade level only affects carry/speed — previously the two
        // were conflated, so buying a personal upgrade suddenly unlocked bread.
        public int StoreLevel { get; private set; } = 1;
        public int StoreXp { get; private set; }
        /// <summary>Content stops at L10 (Mart 2 endgame: Coffee/Counter 4).
        /// Without a cap, XpToNextLevel grew forever with nothing to unlock —
        /// an empty treadmill (batch-34 playthrough finding).</summary>
        public const int MaxStoreLevel = 10;
        public bool IsMaxLevel => StoreLevel >= MaxStoreLevel;
        /// <summary>Lifetime checkouts this session+save — feeds the rotating
        /// mini-goals in Retention.cs. Incremented by CashCounter.ProcessFront.</summary>
        [System.NonSerialized] public int CustomersServed;
        public int XpToNextLevel => 80 * StoreLevel; // L1->2: 80, L2->3: 160, ... (softer curve for 10 levels)

        // Global "Crop Speed" upgrade (in-world Hub pad §"Crop Speed Upgrade Spot").
        // Multiplies growth rate on tomato / wheat / corn / herb farms. Levels 1-5,
        // multiplier at level N = 1 + (N-1) * 0.20 (Level 1 = base, Level 5 = 1.80x).
        public int CropSpeedLevel { get; private set; } = 1;
        public float CropSpeedMultiplier => 1f + (CropSpeedLevel - 1) * 0.20f;
        public void BumpCropSpeed()
        {
            if (CropSpeedLevel >= 5) return;
            CropSpeedLevel++;
        }

        public void AddStoreXp(int amount)
        {
            if (amount <= 0 || IsMaxLevel) return;
            StoreXp += amount;
            while (StoreXp >= XpToNextLevel && !IsMaxLevel)
            {
                StoreXp -= XpToNextLevel;
                StoreLevel++;
                Debug.Log($"[GameManager] Store levelled up to {StoreLevel}!");
                SyncLevelDependents();
                AudioFx.LevelUp();
                if (Player != null) Vfx.LevelUp(Player.transform.position); // firework burst
                if (IsMaxLevel)
                {
                    // Mart complete! Freeze the XP bar full and throw a bigger party.
                    StoreXp = 0;
                    if (Player != null) Vfx.Stars(Player.transform.position);
                    Debug.Log("[GameManager] MART COMPLETE — max level reached!");
                }
            }
        }

        private void SyncLevelDependents()
        {
            BuyerSpawner?.SetPlayerLevel(StoreLevel);
            PhoneOrderManager?.SetPlayerLevel(StoreLevel);
            TheftManager?.SetPlayerLevel(StoreLevel);
            foreach (var c in Counters) c.RefreshUnlockState(StoreLevel);
        }

        private float totalPlaySeconds;
        private bool isPaused;

        private readonly List<CharacterBase> allCharacters = new List<CharacterBase>();

        public void RegisterCharacter(CharacterBase c) { if (!allCharacters.Contains(c)) allCharacters.Add(c); }
        public void UnregisterCharacter(CharacterBase c) { allCharacters.Remove(c); }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            Inventory = new StoreInventory();
            Economy   = new EconomyManager();
        }

        private void Start()
        {
            Boot();
        }


        private void Boot()
        {
            // Configure workers FIRST — Configure() resets every worker to level 1,
            // so applying the save before it silently wiped all loaded progress.
            Shelver1?.Configure(RoleType.Shelver1, Inventory);
            Shelver2?.Configure(RoleType.Shelver2, Inventory);
            Chef?.Configure(Inventory);
            Farmer?.Configure(Inventory);

            // New game: a little pocket money (reference starts you with a few coins on the
            // ground). The first pad (HIRE FARMER, $15) needs a few tomato sales first —
            // that's the intended early progression.
            if (!SaveSystem.HasSave())
                Economy.PlayerCash = 10f;

            // Load save if it exists.
            if (SaveSystem.HasSave())
            {
                var data = SaveSystem.Load();
                Economy.PlayerCash = data.PlayerCash;
                Player?.ApplyLevel(data.PlayerLevel);
                StoreLevel = Mathf.Max(1, data.StoreLevel);
                StoreXp = Mathf.Max(0, data.StoreXp);
                totalPlaySeconds = data.TotalPlaySeconds;
                foreach (var kv in data.Inventory)
                    if (System.Enum.TryParse(kv.Key, out ItemType t)) Inventory.Stocks[t].Count = kv.Value;

                // Worker / machine / coop levels (default 1 when the key is absent).
                int Lvl(string key) => data.UpgradeLevels.TryGetValue(key, out int l) ? l : 1;
                // Split Stack/Speed tracks: newer saves carry per-track keys
                // (`Shelver1_Stack`, `Shelver1_Speed`). Fall back to the legacy
                // combined `Shelver1` key so pre-split saves still load cleanly.
                int SLvl(string key, string track) =>
                    data.UpgradeLevels.TryGetValue($"{key}_{track}", out int split) ? split : Lvl(key);
                void ApplySplit(Characters.CharacterBase c, string key)
                {
                    if (c == null) return;
                    c.ApplyStackLevel(SLvl(key, "Stack"));
                    c.ApplySpeedLevel(SLvl(key, "Speed"));
                }
                ApplySplit(Shelver1, "Shelver1");
                ApplySplit(Shelver2, "Shelver2");
                ApplySplit(Chef,     "Chef");
                ApplySplit(Farmer,   "Farmer");
                Blender?.ApplyLevel(Lvl("Machine_Blender"));
                Oven?.ApplyLevel(Lvl("Machine_Oven"));
                Mill?.ApplyLevel(Lvl("Machine_Mill"));
                Dairy?.ApplyLevel(Lvl("Machine_Dairy"));
                LeafProcessor?.ApplyLevel(Lvl("Machine_LeafProcessor"));
                Stove?.ApplyLevel(Lvl("Machine_Stove"));
                HenCoop?.ApplyLevel(Lvl("HenCoop"));
                CowPen?.ApplyLevel(Lvl("CowPen"));

                // Player-adjusted shelf prices.
                foreach (var kv in data.ManualPrices)
                    if (System.Enum.TryParse(kv.Key, out ItemType pt)) Economy.SetManualPrice(pt, kv.Value);

                // Re-apply expansion purchases: bought pads activate their targets for free.
                if (data.PurchasedPads != null)
                {
                    foreach (var label in data.PurchasedPads) MarkPadPurchased(label);
                    foreach (var pad in FindObjectsByType<Engine.PurchasePad>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        if (purchasedPads.Contains(pad.Label)) pad.RestorePurchased();
                }

                // Restore partial pad payments (money already sunk must not evaporate).
                if (data.PadProgressLabels != null)
                {
                    for (int i = 0; i < data.PadProgressLabels.Count && i < data.PadProgressRemaining.Count; i++)
                    {
                        string lbl = data.PadProgressLabels[i];
                        float rem = data.PadProgressRemaining[i];
                        foreach (var pad in FindObjectsByType<Engine.PurchasePad>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                            if (pad.Label == lbl) { pad.ApplyProgress(rem); break; }
                    }
                }

                Debug.Log($"[GameManager] Save loaded — cash ${Economy.PlayerCash:F2}, store level {StoreLevel}.");

                ComputeOfflineEarnings(data);
            }

            // Wire Chef to farm nodes and all machines.
            if (Chef != null)
            {
                Chef.tomatoFarm    = TomatoFarm;
                Chef.wheatFarm     = WheatFarm;
                Chef.henCoop       = HenCoop;
                Chef.blender       = Blender;
                Chef.oven          = Oven;
                Chef.mill          = Mill;
                Chef.stove         = Stove;         // Bug #5: was missing — FriedEgg chain broken
                Chef.leafProcessor = LeafProcessor; // Bug #5: was missing — HerbPack chain broken
            }

            if (Farmer != null)
            {
                Farmer.tomatoFarm = TomatoFarm;
                Farmer.wheatFarm  = WheatFarm;
                Farmer.henCoop    = HenCoop;
                Farmer.cowPen     = CowPen;
                Farmer.herbPatch  = HerbPatch; // Bug #6: was missing — herb harvest chain broken
            }

            // Collect all tick-able characters.
            if (Player != null)   RegisterCharacter(Player);
            if (Shelver1 != null) RegisterCharacter(Shelver1);
            if (Shelver2 != null) RegisterCharacter(Shelver2);
            if (Chef != null)     RegisterCharacter(Chef);
            if (Farmer != null)   RegisterCharacter(Farmer);

            // Wire economy manager into phone orders and cash counters.
            if (PhoneOrderManager != null)
            {
                PhoneOrderManager.Economy   = Economy;
                PhoneOrderManager.Inventory = Inventory;
            }

            // Sync every level-dependent system with the (possibly loaded) store level.
            SyncLevelDependents();
            ApplyQualityPreset(Quality);

            Debug.Log("[GameManager] Boot complete.");
        }

        private float autosaveTimer;

        private void Update()
        {
            if (isPaused) return;

            totalPlaySeconds += Time.deltaTime;

            tickAccumulator += Time.deltaTime;
            while (tickAccumulator >= SimTickSeconds)
            {
                tickAccumulator -= SimTickSeconds;
                SimTick(SimTickSeconds);
            }

            // Autosave every 30 s (TDD 12). Force-killing an app skips OnApplicationQuit
            // entirely (and Xcode's Stop button skips ALL callbacks), so periodic saving
            // is the only reliable persistence on mobile.
            autosaveTimer += Time.deltaTime;
            if (autosaveTimer >= 30f)
            {
                autosaveTimer = 0f;
                DoSave();
            }
        }

        /// <summary>
        /// Fixed simulation step: drives all character AI decisions, checkout processing,
        /// and economy ticks. Movement interpolation happens in each character's own Update().
        /// </summary>
        private void SimTick(float dt)
        {
            // Skip characters that are still behind an unpurchased PurchasePad (inactive).
            foreach (var c in allCharacters)
            {
                if (c == null || !c.gameObject.activeInHierarchy) continue;
                c.Tick(dt);
            }

            // GDD 7/8.4: at level 1 the player must physically man Counter 1 — a counter with
            // no cashier only opens while the player stands next to it. ManualOverride was
            // never set anywhere, so no buyer could EVER check out before level 2.
            if (Player != null)
            {
                foreach (var counter in Counters)
                    counter.ManualOverride =
                        Vector3.Distance(Player.transform.position, counter.transform.position) < 3.0f;
            }

            // Process checkout queues. Revenue lands as MoneyStacks on the counter;
            // cash and store XP are granted when the player collects the stack.
            foreach (var counter in Counters)
                counter.ProcessFront(Economy, dt);
        }

        // ── Offline earnings (cherry-picked from the Mini Farm Market spec) ──
        // The farm keeps trickling while the app is closed: modest production rates
        // (a fraction of live play), capped at 4 hours and by storage space.

        // ── Purchase-pad persistence (reference expansion flow) ──
        private readonly System.Collections.Generic.HashSet<string> purchasedPads =
            new System.Collections.Generic.HashSet<string>();

        public bool IsPadPurchased(string label)
        {
            if (string.IsNullOrEmpty(label)) return false;
            return purchasedPads.Contains(label);
        }

        public void MarkPadPurchased(string label)
        {
            if (string.IsNullOrEmpty(label)) return;
            purchasedPads.Add(label);

            // Batch 40 onboarding: a plain-language "what now?" hint the moment
            // each station is bought. Guarded by timeSinceLevelLoad so the
            // save-restore replay at boot doesn't fire a toast barrage.
            if (Time.timeSinceLevelLoad > 5f)
            {
                string hint = label switch
                {
                    "Hen Coop"        => "Chickens lay eggs — pick them up and shelve them!",
                    "Hire Farmer"     => "Your farmer now harvests crops for you!",
                    "Hire Shelver"    => "Your shelver keeps the shelves stocked!",
                    "Hire Shelver B"  => "Your shelver keeps the shelves stocked!",
                    "Hire Chef"       => "Your chef runs the machines for you!",
                    "Ketchup Blender" => "Carry tomatoes to the Blender to make ketchup!",
                    "Wheat Farm"      => "Harvest wheat — the Mill turns it into flour!",
                    "Wheat Mill"      => "Carry wheat to the Mill to make flour!",
                    "Bread Oven"      => "The Oven bakes flour + eggs into bread!",
                    "Egg Stove"       => "Carry eggs to the Stove to fry them!",
                    "Cow Pen"         => "Feed the cow hay, then collect the milk!",
                    "Hay Trough"      => "Carry wheat here to feed the cow!",
                    "Milk Bottler"    => "Carry milk to the Bottler to bottle it!",
                    "Cheese Dairy"    => "Carry milk to the Dairy to make cheese!",
                    "Corn Field"      => "Harvest corn when the cobs turn yellow!",
                    "Corn Processor"  => "Carry corn here to process it!",
                    "Apple Orchard"   => "Pick apples from the trees!",
                    "Herb Patch"      => "Harvest herbs when the bushes fill out!",
                    "Leaf Unit"       => "Carry herbs here to pack them!",
                    "Coffee Bar"      => "Fresh coffee brews itself — collect the cups!",
                    "MegaMart"        => "Stand on the blue pad to visit your MegaMart!",
                    "Counter 2"       => "A second till opens — shorter queues!",
                    "Counter 3"       => "Another till — the crowd flows faster!",
                    "Counter 4"       => "Full checkout row — maximum throughput!",
                    _                 => null,
                };
                if (hint != null) Toast.Show(hint, 5f);
            }
        }

        /// <summary>Human-readable report shown once by the HUD; null when nothing pending.</summary>
        public string OfflineSummary { get; private set; }

        public void DismissOfflineSummary() => OfflineSummary = null;

        private void ComputeOfflineEarnings(GameSaveData data)
        {
            if (string.IsNullOrEmpty(data.SaveTimestamp)) return;
            if (!System.DateTime.TryParse(data.SaveTimestamp, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var savedAt)) return;

            double seconds = (System.DateTime.UtcNow - savedAt).TotalSeconds;
            if (seconds < 120) return;                    // ignore tiny gaps
            seconds = System.Math.Min(seconds, 4 * 3600); // cap at 4 hours

            int eggs     = DepositUpToCap(ItemType.Egg,    (int)(seconds / 30));
            int tomatoes = DepositUpToCap(ItemType.Tomato, (int)(seconds / 45));
            int wheat    = DepositUpToCap(ItemType.Wheat,  (int)(seconds / 60));

            float coins = Mathf.Round(Mathf.Min((float)seconds / 60f, 240f) * 0.3f * StoreLevel);
            Economy.Deposit(coins);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"You were away {FormatDuration(seconds)}.");
            sb.AppendLine();
            if (eggs > 0)     sb.AppendLine($"Eggs collected: +{eggs}");
            if (tomatoes > 0) sb.AppendLine($"Tomatoes grown: +{tomatoes}");
            if (wheat > 0)    sb.AppendLine($"Wheat grown: +{wheat}");
            sb.AppendLine();
            sb.AppendLine($"Offline sales: ${coins:F0}");
            OfflineSummary = sb.ToString();

            Debug.Log($"[GameManager] Offline earnings: ${coins:F0}, +{eggs} eggs, +{tomatoes} tomatoes, +{wheat} wheat.");
        }

        private int DepositUpToCap(ItemType item, int amount)
        {
            if (amount <= 0 || !Inventory.Stocks.TryGetValue(item, out var s)) return 0;
            int add = Mathf.Clamp(amount, 0, s.MaxCapacity - s.Count);
            s.Count += add;
            return add;
        }

        private static string FormatDuration(double seconds)
        {
            var ts = System.TimeSpan.FromSeconds(seconds);
            return ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}h {ts.Minutes}m" : $"{ts.Minutes}m";
        }

        // --- Pause / Resume (player-accessible, spec Section 1) ---

        public void Pause()  { isPaused = true;  Player?.Pause(); }
        public void Resume() { isPaused = false; Player?.Resume(); }

        // --- Level-up ---

        /// <summary>Player bought a personal upgrade. Grants store XP; unlocks stay
        /// driven by StoreLevel (see SyncLevelDependents).</summary>
        public void OnPlayerLevelUp(int newLevel)
        {
            AddStoreXp(25);
            Debug.Log($"[GameManager] Player upgraded to {newLevel}");
        }

        // --- Quality preset (Architecture Spec Section 7) ---

        public void ApplyQualityPreset(QualityPreset preset)
        {
            Quality = preset;
            switch (preset)
            {
                case QualityPreset.LowPower:
                    Application.targetFrameRate = 30;
                    SimTickSeconds = 0.2f;
                    break;
                case QualityPreset.Balanced:
                    Application.targetFrameRate = 60;
                    SimTickSeconds = 0.15f;
                    break;
                case QualityPreset.High:
                    Application.targetFrameRate = 60;
                    SimTickSeconds = 0.1f;
                    break;
            }
        }

        // --- Save on quit ---

        private void OnApplicationPause(bool pause) { if (pause) DoSave(); }
        private void OnApplicationFocus(bool focus) { if (!focus) DoSave(); }
        private void OnApplicationQuit() => DoSave();

        private void DoSave()
        {
            if (Economy == null || Inventory == null) return;

            var data = new GameSaveData
            {
                PlayerLevel      = Player?.Level ?? 1,
                PlayerCash       = Economy.PlayerCash,
                StoreLevel       = StoreLevel,
                StoreXp          = StoreXp,
                TotalPlaySeconds = totalPlaySeconds,
            };

            if (Inventory.Stocks != null)
            {
                foreach (var kv in Inventory.Stocks)
                    data.Inventory[kv.Key.ToString()] = kv.Value.Count;
            }

            // Worker / machine / coop levels.
            // Split Stack/Speed tracks: write per-track keys. The combined `Level`
            // key is kept for pre-split code paths that only read a single level
            // (e.g. UI legacy fallback and the CashCounter unlock check).
            void WriteSplit(Characters.CharacterBase c, string key)
            {
                if (c == null) return;
                data.UpgradeLevels[key] = c.Level;
                data.UpgradeLevels[$"{key}_Stack"] = c.StackLevel;
                data.UpgradeLevels[$"{key}_Speed"] = c.SpeedLevel;
            }
            WriteSplit(Shelver1, "Shelver1");
            WriteSplit(Shelver2, "Shelver2");
            WriteSplit(Chef,     "Chef");
            WriteSplit(Farmer,   "Farmer");
            if (Blender != null)  data.UpgradeLevels["Machine_Blender"] = Blender.Level;
            if (Oven != null)     data.UpgradeLevels["Machine_Oven"] = Oven.Level;
            if (Mill != null)     data.UpgradeLevels["Machine_Mill"] = Mill.Level;
            if (Dairy != null)    data.UpgradeLevels["Machine_Dairy"] = Dairy.Level;
            if (LeafProcessor != null) data.UpgradeLevels["Machine_LeafProcessor"] = LeafProcessor.Level;
            if (Stove != null)    data.UpgradeLevels["Machine_Stove"] = Stove.Level;
            if (HenCoop != null)  data.UpgradeLevels["HenCoop"] = HenCoop.Level;
            if (CowPen != null)   data.UpgradeLevels["CowPen"] = CowPen.Level;

            // Player-adjusted shelf prices.
            foreach (var kv in Economy.ManualPrices)
                data.ManualPrices[kv.Key.ToString()] = kv.Value;

            // Expansion purchases.
            data.PurchasedPads.AddRange(purchasedPads);

            // Partially-paid pads keep their progress across sessions.
            foreach (var pad in FindObjectsByType<Engine.PurchasePad>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (pad.Remaining < pad.Cost - 0.01f)
                {
                    data.PadProgressLabels.Add(pad.Label);
                    data.PadProgressRemaining.Add(pad.Remaining);
                }
            }

            SaveSystem.Save(data);
        }
    }
}
