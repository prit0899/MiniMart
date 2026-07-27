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
        // Playtest retune: the every-1-2s reference pace only fits a MATURE
        // store. At L1 (one tomato stand, hand-restocked) that demand emptied
        // the shelf instantly and filled the store with waiting statues. Start
        // gentle; level scaling ramps the crowd up as supply grows.
        public float MinInterval = 4f;
        public float MaxInterval = 7f;
        public float LevelScaleFactor = 0.75f; // each store level speeds spawns by 25%
        // B2: hard floor on the spawn interval so high-level scaling can't produce a
        // flood a base-rate store can't restock against (~1.5s ≈ a busy but
        // serviceable queue at L10).
        public float MinSpawnInterval = 1.5f;
        public int MaxConcurrentBuyers = 12;   // absolute cap per TDD 15

        /// <summary>Effective crowd cap grows with the store: 6 at L1 up to 12.</summary>
        private int LevelCap => Mathf.Min(MaxConcurrentBuyers, 4 + 2 * currentPlayerLevel);

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
            // Playtest B2: unclamped, 0.75^9 at L10 spawned a buyer every ~0.4s — a
            // flood a base-rate store can't restock against, so shelves emptied and
            // customers walked out. Floor the interval so demand can't outrun supply
            // (buyers also now wait for restock, so a steady stream keeps the store
            // busy without starving it).
            nextSpawn = Mathf.Max(MinSpawnInterval, Random.Range(MinInterval, MaxInterval) * scale);
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

            // Cap the crowd: prune buyers that finished and left, then respect the
            // level-scaled pool limit (6 at L1, growing to 12 at max level).
            activeBuyers.RemoveAll(b => b == null);
            if (activeBuyers.Count >= LevelCap) return;

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
                go.AddComponent<MiniMart.UI.CarryVisual>();
                // Same body/head/eyes build as every other character — no more bare capsules.
                // Shopper variant: color-hashed hair (brown/black/blonde) + a little
                // handbag on the side so shoppers read as distinct people, not clones.
                Engine.PrimitiveFactory.BuildCharacter(go,
                    BuyerPalette[Random.Range(0, BuyerPalette.Length)],
                    Engine.PrimitiveFactory.CharacterRole.Shopper);
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
