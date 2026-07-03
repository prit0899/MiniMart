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
        public Machine Blender;
        public Machine Oven;
        public Machine Mill;

        // Core runtime systems
        public StoreInventory Inventory { get; private set; }
        public EconomyManager Economy { get; private set; }

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

            // Load save if it exists.
            if (SaveSystem.HasSave())
            {
                var data = SaveSystem.Load();
                Economy.PlayerCash = data.PlayerCash;
                Player?.ApplyLevel(data.PlayerLevel);
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
                HenCoop?.ApplyLevel(Lvl("HenCoop"));

                // Player-adjusted shelf prices.
                foreach (var kv in data.ManualPrices)
                    if (System.Enum.TryParse(kv.Key, out ItemType pt)) Economy.SetManualPrice(pt, kv.Value);

                Debug.Log($"[GameManager] Save loaded — cash ${Economy.PlayerCash:F2}, level {Player?.Level ?? 1}.");
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

            // Sync every level-dependent system with the (possibly loaded) player level.
            int levelNow = Player != null ? Player.Level : 1;
            BuyerSpawner?.SetPlayerLevel(levelNow);
            PhoneOrderManager?.SetPlayerLevel(levelNow);
            TheftManager?.SetPlayerLevel(levelNow);

            // Refresh counter unlock state for current player level.
            RefreshCounterState();
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
            foreach (var c in allCharacters) c.Tick(dt);

            // GDD 7/8.4: at level 1 the player must physically man Counter 1 — a counter with
            // no cashier only opens while the player stands next to it. ManualOverride was
            // never set anywhere, so no buyer could EVER check out before level 2.
            if (Player != null)
            {
                foreach (var counter in Counters)
                    counter.ManualOverride =
                        Vector3.Distance(Player.transform.position, counter.transform.position) < 2.2f;
            }

            // Process checkout queues.
            foreach (var counter in Counters)
                counter.ProcessFront(Economy);
        }

        // --- Pause / Resume (player-accessible, spec Section 1) ---

        public void Pause()  { isPaused = true;  Player?.Pause(); }
        public void Resume() { isPaused = false; Player?.Resume(); }

        // --- Level-up ---

        public void OnPlayerLevelUp(int newLevel)
        {
            RefreshCounterState();
            BuyerSpawner?.SetPlayerLevel(newLevel);
            PhoneOrderManager?.SetPlayerLevel(newLevel);
            TheftManager?.SetPlayerLevel(newLevel);
            Debug.Log($"[GameManager] Player levelled up to {newLevel}");
        }

        private void RefreshCounterState()
        {
            int level = Player != null ? Player.Level : 1;
            foreach (var c in Counters) c.RefreshUnlockState(level);
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
            if (HenCoop != null)  data.UpgradeLevels["HenCoop"] = HenCoop.Level;

            // Player-adjusted shelf prices.
            foreach (var kv in Economy.ManualPrices)
                data.ManualPrices[kv.Key.ToString()] = kv.Value;

            SaveSystem.Save(data);
        }
    }
}
