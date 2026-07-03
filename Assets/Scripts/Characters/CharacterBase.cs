using UnityEngine;
using MiniMart.Core;
using MiniMart.Catalog;
using MiniMart.Map;

namespace MiniMart.Characters
{
    /// <summary>
    /// Common fields/behaviour for every character per Architecture Spec Section 6.
    /// Movement uses simple lane/grid waypoints (no rigid-body physics) per Section 5.
    /// </summary>
    /// </summary>
    public abstract class CharacterBase : MonoBehaviour
    {
        public RoleType Role;
        public int Level = 1;
        public int CarryCapacity;
        public int CarryCount;
        public CharacterState State = CharacterState.Idle;

        [Header("Movement")]
        public float baseSpeed = 2.0f;          // world units / second at multiplier 1.0
        public float speedMultiplier = 1.0f;
        protected Vector3 target;
        protected bool hasTarget;

        public UpgradeCurve Curve;

        public float CurrentSpeed => baseSpeed * speedMultiplier;

        /// <summary>True while a SetTarget path is still being walked.</summary>
        public bool HasMoveTarget => hasTarget;

        protected virtual void Awake()
        {
            ApplyLevel(Level);
        }

        /// <summary>Re-reads capacity/speed from the upgrade curve for the given level.</summary>
        public virtual void ApplyLevel(int level)
        {
            if (Curve == null) return;
            Level = Mathf.Clamp(level, 1, Curve.MaxLevel);
            var step = Curve.GetStep(Level);
            CarryCapacity = step.stackCapacity;
            speedMultiplier = step.speedMultiplier;
        }

        public bool TryUpgrade(out int cost)
        {
            cost = Curve?.CostForNextLevel(Level) ?? -1;
            if (cost < 0) return false;
            ApplyLevel(Level + 1);
            return true;
        }

        protected System.Collections.Generic.List<Vector3> pathWaypoints = new System.Collections.Generic.List<Vector3>();
        protected int currentWaypointIndex = 0;

        public void SetTarget(Vector3 worldPos)
        {
            // Lock Y to 0 for flat ground movement
            target = new Vector3(worldPos.x, 0, worldPos.z);
            hasTarget = true;
            State = CharacterState.Walking;

            if (GridPathfinder.Instance != null)
            {
                pathWaypoints = GridPathfinder.Instance.FindPath(transform.position, target);
                currentWaypointIndex = 0;
                // If path is empty, just add target directly
                if (pathWaypoints == null || pathWaypoints.Count == 0)
                {
                    pathWaypoints = new System.Collections.Generic.List<Vector3> { target };
                }
            }
            else
            {
                pathWaypoints = new System.Collections.Generic.List<Vector3> { target };
                currentWaypointIndex = 0;
            }
        }

        protected virtual void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterCharacter(this);

            var anim = GetComponentInChildren<Animator>();
            if (anim != null) anim.applyRootMotion = false;
        }

        protected virtual void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.UnregisterCharacter(this);
        }

        /// <summary>Called every simulation tick (not every frame) by GameManager.
        /// Handles decision-making and logic updates.</summary>
        public virtual void Tick(float dt)
        {
            // Decision making logic can go here in subclasses
        }

        protected virtual void Update()
        {
            if (hasTarget) MoveTowardsTarget(Time.deltaTime);
        }

        protected virtual void MoveTowardsTarget(float dt)
        {
            if (pathWaypoints == null || pathWaypoints.Count == 0 || currentWaypointIndex >= pathWaypoints.Count)
            {
                hasTarget = false;
                State = CharacterState.Idle;
                OnArrived();
                return;
            }

            Vector3 nextTarget = pathWaypoints[currentWaypointIndex];
            nextTarget.y = 0;

            Vector3 pos = transform.position;
            pos.y = 0;
            Vector3 next = Vector3.MoveTowards(pos, nextTarget, CurrentSpeed * dt);
            
            // Keep grounded
            next.y = 0;
            
            var cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.Move(next - pos);
            }
            else
            {
                var rb = GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic) rb.MovePosition(next);
                else transform.position = next;
            }
            
            // Optional: Rotate character to face movement direction
            if ((next - pos).sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(next - pos, Vector3.up);
            }
            
            if (Vector3.Distance(next, nextTarget) < 0.15f)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= pathWaypoints.Count)
                {
                    hasTarget = false;
                    State = CharacterState.Idle;
                    OnArrived();
                }
            }
        }

        protected virtual void OnArrived() { }

        public bool CanCarryMore(int amount = 1) => CarryCount + amount <= CarryCapacity;

        public bool TryPickUp(int amount = 1)
        {
            if (!CanCarryMore(amount)) return false;
            CarryCount += amount;
            State = CharacterState.Carrying;
            return true;
        }

        public void DropAll() => CarryCount = 0;
    }
}
