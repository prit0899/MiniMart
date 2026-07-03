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
        public Transform ExitDoor;
        public List<ShopShelf> AllShelves = new List<ShopShelf>();
        public List<CashCounter> Counters = new List<CashCounter>();

        [Header("Spawn Intervals (seconds)")]
        // Reference pacing (My Mini Mart): a new buyer roughly every 1-2 seconds.
        public float MinInterval = 1.5f;
        public float MaxInterval = 3f;
        public float LevelScaleFactor = 0.9f; // each player level shrinks the interval by 10%
        public int MaxConcurrentBuyers = 12;  // pooled cap per TDD 15

        private float timer;
        private float nextSpawn;
        private int currentPlayerLevel = 1;
        private readonly List<Buyer> activeBuyers = new List<Buyer>();

        // Cheerful customer palette — matches the flat low-poly look of the reference.
        private static readonly Color[] BuyerPalette =
        {
            new Color(0.95f, 0.75f, 0.20f), // yellow
            new Color(0.90f, 0.45f, 0.40f), // coral
            new Color(0.40f, 0.75f, 0.90f), // sky
            new Color(0.60f, 0.85f, 0.45f), // lime
            new Color(0.80f, 0.55f, 0.90f), // lilac
            new Color(0.95f, 0.60f, 0.75f), // pink
            new Color(0.55f, 0.60f, 0.95f), // periwinkle
        };

        private void Awake() => ScheduleNext();

        public void SetPlayerLevel(int level)
        {
            currentPlayerLevel = level;
            ScheduleNext();
        }

        private void ScheduleNext()
        {
            float scale = Mathf.Pow(LevelScaleFactor, currentPlayerLevel - 1);
            // GDD 8.3: an active discount (>=10%) pulls in +50% more customers.
            var eco = GameManager.Instance?.Economy;
            if (eco != null && eco.AnyDiscountActive()) scale *= 0.67f;
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
            if (EntranceDoor == null) return;

            // Cap the crowd: prune buyers that finished and left, then respect the pool limit.
            activeBuyers.RemoveAll(b => b == null);
            if (activeBuyers.Count >= MaxConcurrentBuyers) return;

            GameObject go;
            if (BuyerPrefab != null)
            {
                go = Instantiate(BuyerPrefab, EntranceDoor.position, Quaternion.identity);
            }
            else
            {
                go = new GameObject("Buyer");
                go.transform.position = EntranceDoor.position;
                go.AddComponent<Buyer>();
                go.AddComponent<Engine.WobbleAnimator>();
                // Same body/head/eyes build as every other character — no more bare capsules.
                Engine.PrimitiveFactory.BuildCharacter(go, BuyerPalette[Random.Range(0, BuyerPalette.Length)]);
            }
            var buyer = go.GetComponent<Buyer>();
            buyer?.Init(currentPlayerLevel, AllShelves, Counters);
            if (buyer != null)
            {
                buyer.ExitDoor = ExitDoor != null ? ExitDoor : EntranceDoor;
                activeBuyers.Add(buyer);
                spawnedTotal++;
                if (spawnedTotal <= 3)
                    Debug.Log($"[BuyerSpawner] Buyer #{spawnedTotal} spawned ({buyer.BagType}, {activeBuyers.Count} active)");
            }
        }

        private int spawnedTotal;
    }
}
