using System;
using System.Collections.Generic;
using UnityEngine;
using MiniMart.Core;

namespace MiniMart.Save
{
    /// <summary>Serializable snapshot of everything that must survive a session restart.</summary>
    [Serializable]
    public class GameSaveData
    {
        public int PlayerLevel;
        public float PlayerCash;
        public Dictionary<string, int> Inventory = new Dictionary<string, int>(); // ItemType.ToString() -> count
        public Dictionary<string, int> UpgradeLevels = new Dictionary<string, int>(); // "Machine_Blender" etc.
        public Dictionary<string, float> ManualPrices = new Dictionary<string, float>();
        public float TotalPlaySeconds;
        public string SaveTimestamp;
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
