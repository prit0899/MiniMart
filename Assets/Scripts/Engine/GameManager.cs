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
        public Machine Blender;
        public Machine Oven;
        public Machine Mill;
        public Machine Dairy;

        // Core runtime systems
        public StoreInventory Inventory { get; private set; }
        public EconomyManager Economy { get; private set; }

        // ── Store progression (cherry-picked from the Mini Farm Market spec) ──
        // Store level drives ALL unlocks (items, cashiers, thief, spawn pacing).
        // The player's own upgrade level only affects carry/speed — previously the two
        // were conflated, so buying a personal upgrade suddenly unlocked bread.
        public int StoreLevel { get; private set; } = 1;
        public int StoreXp { get; private set; }
        public int XpToNextLevel => 100 * StoreLevel; // L1->2: 100, L2->3: 200, ...

        public void AddStoreXp(int amount)
        {
            if (amount <= 0) return;
            StoreXp += amount;
            while (StoreXp >= XpToNextLevel)
            {
                StoreXp -= XpToNextLevel;
                StoreLevel++;
                Debug.Log($"[GameManager] Store levelled up to {StoreLevel}!");
                SyncLevelDependents();
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

            // New game: a little pocket money so the first purchase pads are reachable
            // after a few tomato sales (reference starts you with coins on the ground).
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
                Shelver1?.ApplyLevel(Lvl("Shelver1"));
                Shelver2?.ApplyLevel(Lvl("Shelver2"));
                Chef?.ApplyLevel(Lvl("Chef"));
                Farmer?.ApplyLevel(Lvl("Farmer"));
                Blender?.ApplyLevel(Lvl("Machine_Blender"));
                Oven?.ApplyLevel(Lvl("Machine_Oven"));
                Mill?.ApplyLevel(Lvl("Machine_Mill"));
                Dairy?.ApplyLevel(Lvl("Machine_Dairy"));
                HenCoop?.ApplyLevel(Lvl("HenCoop"));
                CowPen?.ApplyLevel(Lvl("CowPen"));

                // Player-adjusted shelf prices.
                foreach (var kv in data.ManualPrices)
                    if (System.Enum.TryParse(kv.Key, out ItemType pt)) Economy.SetManualPrice(pt, kv.Value);

                // Re-apply expansion purchases: bought pads activate their targets for free.
                if (data.PurchasedPads != null)
                {
                    foreach (var label in data.PurchasedPads) MarkPadPurchased(label);
                    foreach (var pad in FindObjectsByType<Engine.PurchasePad>(FindObjectsSortMode.None))
                        if (purchasedPads.Contains(pad.Label)) pad.RestorePurchased();
                }

                Debug.Log($"[GameManager] Save loaded — cash ${Economy.PlayerCash:F2}, store level {StoreLevel}.");

                ComputeOfflineEarnings(data);
            }

            // Wire Chef to farm nodes.
            if (Chef != null)
            {
                Chef.tomatoFarm = TomatoFarm;
                Chef.wheatFarm = WheatFarm;
                Chef.henCoop = HenCoop;
                Chef.blender = Blender;
                Chef.oven = Oven;
                Chef.mill = Mill;
            }

            if (Farmer != null)
            {
                Farmer.tomatoFarm = TomatoFarm;
                Farmer.wheatFarm  = WheatFarm;
                Farmer.henCoop    = HenCoop;
                Farmer.cowPen     = CowPen;
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

        public void MarkPadPurchased(string label)
        {
            if (!string.IsNullOrEmpty(label)) purchasedPads.Add(label);
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
            if (Shelver1 != null) data.UpgradeLevels["Shelver1"] = Shelver1.Level;
            if (Shelver2 != null) data.UpgradeLevels["Shelver2"] = Shelver2.Level;
            if (Chef != null)     data.UpgradeLevels["Chef"] = Chef.Level;
            if (Farmer != null)   data.UpgradeLevels["Farmer"] = Farmer.Level;
            if (Blender != null)  data.UpgradeLevels["Machine_Blender"] = Blender.Level;
            if (Oven != null)     data.UpgradeLevels["Machine_Oven"] = Oven.Level;
            if (Mill != null)     data.UpgradeLevels["Machine_Mill"] = Mill.Level;
            if (Dairy != null)    data.UpgradeLevels["Machine_Dairy"] = Dairy.Level;
            if (HenCoop != null)  data.UpgradeLevels["HenCoop"] = HenCoop.Level;
            if (CowPen != null)   data.UpgradeLevels["CowPen"] = CowPen.Level;

            // Player-adjusted shelf prices.
            foreach (var kv in Economy.ManualPrices)
                data.ManualPrices[kv.Key.ToString()] = kv.Value;

            // Expansion purchases.
            data.PurchasedPads.AddRange(purchasedPads);

            SaveSystem.Save(data);
        }
    }
}
