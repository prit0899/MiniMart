using System.Collections.Generic;
using UnityEngine;
using MiniMart.Core;
using MiniMart.Characters;
using MiniMart.Runtime;

namespace MiniMart.AI
{
    using PriceCatalog = MiniMart.Catalog.PriceCatalog;
    using ShopShelf = MiniMart.Characters.ShopShelf;

    /// <summary>
    /// Theft event. The thief enters, grabs items from shelves/storage, then flees toward the exit.
    /// Once HasLeftStore is true, the player can no longer catch them — this is a zone rule, not physics.
    /// Only the PlayerController.TryCatchThief path can end the event early.
    /// </summary>
    public class Thief : CharacterBase
    {
        public bool HasLeftStore { get; private set; }
        public bool IsCaught { get; private set; }

        public List<ShopShelf> TargetShelves;
        public Transform ExitWaypoint;
        [System.NonSerialized] public StoreInventory Inventory;

        private int stolenItemCount;
        private ShopShelf currentTarget;
        private bool fleeing;

        protected override void Awake()
        {
            Role = RoleType.Thief;
            Curve = null;
            baseSpeed = 2.2f;      // faster than workers/buyers, but still beatable by the player
            speedMultiplier = 1.0f;
            CarryCapacity = 5;
        }

        public void BeginTheft()
        {
            HasLeftStore = false;
            IsCaught = false;
            fleeing = false;
            stolenItemCount = 0;
            State = CharacterState.Walking;
            PickNextTarget();
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
        }

        private void PickNextTarget()
        {
            if (CarryCount >= CarryCapacity || TargetShelves == null)
            {
                StartFlee();
                return;
            }
            // Pick the shelf with the most stock (easiest grab).
            ShopShelf best = null;
            foreach (var s in TargetShelves)
            {
                if (s == null || !s.gameObject.activeInHierarchy) continue; // not purchased yet
                if (s.Count > 0 && (best == null || s.Count > best.Count)) best = s;
            }

            if (best == null) { StartFlee(); return; }
            currentTarget = best;
            SetTarget(best.transform.position);
        }

        protected override void OnArrived()
        {
            if (fleeing)
            {
                HasLeftStore = true;
                State = CharacterState.Fleeing;
                // The thief has escaped; the event is over — TheftManager will clean this up.
                return;
            }

            if (currentTarget != null && currentTarget.Count > 0)
            {
                int steal = Mathf.Min(2, currentTarget.Count, CarryCapacity - CarryCount);
                if (currentTarget.TakeStock(steal))
                {
                    CarryCount += steal;
                    stolenItemCount += steal;
                }
            }
            PickNextTarget();
        }

        private void StartFlee()
        {
            if (ExitWaypoint == null) { HasLeftStore = true; return; }
            fleeing = true;
            State = CharacterState.Fleeing;
            SetTarget(ExitWaypoint.position);
        }

        /// <summary>Called by PlayerController when the net lands.</summary>
        public void Catch()
        {
            IsCaught = true;
            HasLeftStore = false;
            State = CharacterState.Idle;
            Debug.Log($"Thief caught! Had stolen {stolenItemCount} items.");
            Destroy(gameObject, 1f);
        }
    }

    /// <summary>
    /// Manages the recurring theft event timer (4-5 minutes) and spawns one thief at a time.
    /// </summary>
    public class TheftManager : MonoBehaviour
    {
        public GameObject ThiefPrefab;
        public Transform SpawnPoint;
        public Transform ExitWaypoint;
        public List<ShopShelf> AllShelves;
        [System.NonSerialized] public StoreInventory Inventory;

        /// <summary>GDD 9.2: theft events only begin once the player reaches this level.</summary>
        public int MinPlayerLevel = 3;

        private float timer;
        private float nextTheftTime;
        private bool activeTheft;
        private int currentPlayerLevel = 1;

        public void SetPlayerLevel(int level) => currentPlayerLevel = level;

        private void Awake() => ScheduleNext();

        private void ScheduleNext()
        {
            nextTheftTime = Random.Range(
                PriceCatalog.TheftMinIntervalMin * 60f,
                PriceCatalog.TheftMaxIntervalMin * 60f);
            timer = 0f;
        }

        private void Update()
        {
            if (activeTheft || currentPlayerLevel < MinPlayerLevel) return;
            timer += Time.deltaTime;
            if (timer < nextTheftTime) return;
            SpawnThief();
            ScheduleNext();
        }

        private void SpawnThief()
        {
            // ThiefPrefab was never assigned in the scene, so the old null-check here meant
            // thieves NEVER spawned. Fall back to a runtime-built character like buyers do.
            if (SpawnPoint == null) return;
            activeTheft = true;
            GameObject go;
            if (ThiefPrefab != null)
            {
                go = Instantiate(ThiefPrefab, SpawnPoint.position, Quaternion.identity);
            }
            else
            {
                go = new GameObject("Thief");
                go.transform.position = SpawnPoint.position;
                go.AddComponent<Thief>();
                go.AddComponent<Engine.WobbleAnimator>();
                // Dark hooded look so the player can spot the shoplifter in the crowd.
                Engine.PrimitiveFactory.BuildCharacter(go, new Color(0.22f, 0.22f, 0.28f));
            }
            var thief = go.GetComponent<Thief>();
            if (thief == null) { activeTheft = false; return; }
            thief.TargetShelves = AllShelves;
            thief.ExitWaypoint = ExitWaypoint;
            thief.Inventory = Inventory;
            thief.BeginTheft();
            StartCoroutine(WatchThief(thief));

            // One-time teaching moment: the NET button is never explained anywhere
            // else, so the very first thief arrives with a how-to toast.
            if (!PlayerPrefs.HasKey("MiniMart_ThiefTipShown"))
            {
                PlayerPrefs.SetInt("MiniMart_ThiefTipShown", 1);
                PlayerPrefs.Save();
                Engine.Toast.Show("A thief! Chase him and tap NET to catch him!", 5f);
            }
        }

        private System.Collections.IEnumerator WatchThief(Thief thief)
        {
            while (thief != null && !thief.HasLeftStore && !thief.IsCaught)
                yield return null;
            activeTheft = false;
        }
    }
}
