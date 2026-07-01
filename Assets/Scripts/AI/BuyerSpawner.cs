using System.Collections.Generic;
using UnityEngine;
using MiniMart.Characters;
using MiniMart.Economy;

namespace MiniMart.AI
{
    /// <summary>
    /// Spawns buyers at the entrance on a random timer that scales with player level.
    /// No buyer spawns for an item category that hasn't been unlocked yet (enforced inside Buyer.GenerateBasket).
    /// </summary>
    public class BuyerSpawner : MonoBehaviour
    {
        public GameObject BuyerPrefab;
        public Transform EntranceDoor;
        public List<ShopShelf> AllShelves = new List<ShopShelf>();
        public List<CashCounter> Counters = new List<CashCounter>();

        [Header("Spawn Intervals (seconds)")]
        public float MinInterval = 8f;
        public float MaxInterval = 20f;
        public float LevelScaleFactor = 0.85f; // each player level shrinks the interval by 15%

        private float timer;
        private float nextSpawn;
        private int currentPlayerLevel = 1;

        private void Awake() => ScheduleNext();

        public void SetPlayerLevel(int level)
        {
            currentPlayerLevel = level;
            ScheduleNext();
        }

        private void ScheduleNext()
        {
            float scale = Mathf.Pow(LevelScaleFactor, currentPlayerLevel - 1);
            nextSpawn = Random.Range(MinInterval, MaxInterval) * scale;
            timer = 0f;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < nextSpawn) return;
            Spawn();
            ScheduleNext();
        }

        private void Spawn()
        {
            if (BuyerPrefab == null || EntranceDoor == null) return;
            var go = Instantiate(BuyerPrefab, EntranceDoor.position, Quaternion.identity);
            var buyer = go.GetComponent<Buyer>();
            buyer?.Init(currentPlayerLevel, AllShelves, Counters);
        }
    }
}
