using System;
using System.Collections.Generic;
using UnityEngine;
using MiniMart.Core;

namespace MiniMart.Save
{
    /// <summary>Serializable snapshot of everything that must survive a session restart.
    /// The dictionaries are runtime-friendly APIs; they are flattened into parallel lists on
    /// serialize (via ISerializationCallbackReceiver) because JsonUtility cannot serialize
    /// Dictionary directly.</summary>
    [Serializable]
    public class GameSaveData : ISerializationCallbackReceiver
    {
        public int PlayerLevel;
        public float PlayerCash;
        public int StoreLevel;   // store progression (drives unlocks); 0 in old saves -> treated as 1
        public int StoreXp;

        [NonSerialized] public Dictionary<string, int> Inventory = new Dictionary<string, int>();     // ItemType.ToString() -> count
        [NonSerialized] public Dictionary<string, int> UpgradeLevels = new Dictionary<string, int>(); // "Machine_Blender" etc.
        [NonSerialized] public Dictionary<string, float> ManualPrices = new Dictionary<string, float>();

        public float TotalPlaySeconds;
        public string SaveTimestamp;
        public List<string> PurchasedPads = new List<string>(); // expansion pads already bought

        // ── Serialized backing storage (JsonUtility-compatible) ──
        [SerializeField] private List<string> _invKeys = new List<string>();
        [SerializeField] private List<int>    _invValues = new List<int>();
        [SerializeField] private List<string> _upgKeys = new List<string>();
        [SerializeField] private List<int>    _upgValues = new List<int>();
        [SerializeField] private List<string> _priceKeys = new List<string>();
        [SerializeField] private List<float>  _priceValues = new List<float>();

        public void OnBeforeSerialize()
        {
            Flatten(Inventory, _invKeys, _invValues);
            Flatten(UpgradeLevels, _upgKeys, _upgValues);
            Flatten(ManualPrices, _priceKeys, _priceValues);
        }

        public void OnAfterDeserialize()
        {
            Inventory = Rebuild(_invKeys, _invValues);
            UpgradeLevels = Rebuild(_upgKeys, _upgValues);
            ManualPrices = Rebuild(_priceKeys, _priceValues);
        }

        private static void Flatten<T>(Dictionary<string, T> source, List<string> keys, List<T> values)
        {
            keys.Clear();
            values.Clear();
            if (source == null) return;
            foreach (var kv in source)
            {
                keys.Add(kv.Key);
                values.Add(kv.Value);
            }
        }

        private static Dictionary<string, T> Rebuild<T>(List<string> keys, List<T> values)
        {
            var dict = new Dictionary<string, T>();
            if (keys == null || values == null) return dict;
            int count = Math.Min(keys.Count, values.Count);
            for (int i = 0; i < count; i++)
                dict[keys[i]] = values[i];
            return dict;
        }
    }

    /// <summary>
    /// Handles save and load via UnityEngine.PlayerPrefs (JSON).
    /// For production you'd swap PlayerPrefs for a file-based backend; the interface is identical.
    /// </summary>
    public static class SaveSystem
    {
        private const string SAVE_KEY = "MiniMart_Save_v1";

        public static void Save(GameSaveData data)
        {
            data.SaveTimestamp = DateTime.UtcNow.ToString("o");
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            Debug.Log("[SaveSystem] Game saved.");
        }

        public static GameSaveData Load()
        {
            string json = PlayerPrefs.GetString(SAVE_KEY, null);
            if (string.IsNullOrEmpty(json))
            {
                Debug.Log("[SaveSystem] No save found — returning default.");
                return new GameSaveData { PlayerLevel = 1, PlayerCash = 0f };
            }
            return JsonUtility.FromJson<GameSaveData>(json);
        }

        public static bool HasSave() => PlayerPrefs.HasKey(SAVE_KEY);

        public static void Delete()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            Debug.Log("[SaveSystem] Save deleted.");
        }
    }
}
