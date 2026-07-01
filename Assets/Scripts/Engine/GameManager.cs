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

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            Inventory = new StoreInventory();
            Economy   = new EconomyManager();

            Boot();
        }

        private void Boot()
        {
            // Load save if it exists.
            if (SaveSystem.HasSave())
            {
                var data = SaveSystem.Load();
                Economy.PlayerCash = data.PlayerCash;
                Player?.ApplyLevel(data.PlayerLevel);
                totalPlaySeconds = data.TotalPlaySeconds;
                foreach (var kv in data.Inventory)
                    if (System.Enum.TryParse(kv.Key, out ItemType t)) Inventory.Stocks[t].Count = kv.Value;
            }

            // Configure workers.
            Shelver1?.Configure(RoleType.Shelver1, Inventory);
            Shelver2?.Configure(RoleType.Shelver2, Inventory);
            Chef?.Configure(Inventory);
            Farmer?.Configure(Inventory);

            // Wire Chef to farm nodes.
            if (Chef != null)
            {
                Chef.tomatoFarm = TomatoFarm;
                Chef.henCoop = HenCoop;
                Chef.blender = Blender;
                Chef.oven = Oven;
            }

            if (Farmer != null)
            {
                Farmer.tomatoFarm = TomatoFarm;
                Farmer.wheatFarm  = WheatFarm;
                Farmer.henCoop    = HenCoop;
            }

            // Collect all tick-able characters.
            if (Player != null)   allCharacters.Add(Player);
            if (Shelver1 != null) allCharacters.Add(Shelver1);
            if (Shelver2 != null) allCharacters.Add(Shelver2);
            if (Chef != null)     allCharacters.Add(Chef);
            if (Farmer != null)   allCharacters.Add(Farmer);

            // Wire economy manager into phone orders and cash counters.
            if (PhoneOrderManager != null)
            {
                PhoneOrderManager.Economy   = Economy;
                PhoneOrderManager.Inventory = Inventory;
            }

            // Refresh counter unlock state for current player level.
            RefreshCounterState();
            ApplyQualityPreset(Quality);

            Debug.Log("[GameManager] Boot complete.");
        }

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
        }

        /// <summary>
        /// Fixed simulation step: drives all character AI decisions, checkout processing,
        /// and economy ticks. Movement interpolation happens in each character's own Update().
        /// </summary>
        private void SimTick(float dt)
        {
            foreach (var c in allCharacters) c.Tick(dt);

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
        private void OnApplicationQuit() => DoSave();

        private void DoSave()
        {
            var data = new GameSaveData
            {
                PlayerLevel      = Player?.Level ?? 1,
                PlayerCash       = Economy.PlayerCash,
                TotalPlaySeconds = totalPlaySeconds,
            };
            foreach (var kv in Inventory.Stocks)
                data.Inventory[kv.Key.ToString()] = kv.Value.Count;
            SaveSystem.Save(data);
        }
    }
}
