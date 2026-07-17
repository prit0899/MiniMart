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

        // Reference has SEPARATE "Stack" and "Speed" upgrade tracks per entity —
        // "Stack – Lvl.3" and "Speed – Lvl.5" show as two independent rows in
        // the Upgrades panel. Previously we had one combined `Level` that drove
        // both, so a Speed L5 shelver was FORCED to also be Stack L5. Split them:
        //   • StackLevel drives CarryCapacity via curve.stackCapacity
        //   • SpeedLevel drives speedMultiplier via curve.speedMultiplier
        // `Level` is kept as max(Stack,Speed) for legacy call sites (unlocks,
        // save/load fallback) — new code should read the split fields directly.
        public int StackLevel = 1;
        public int SpeedLevel = 1;
        public int Level => Mathf.Max(StackLevel, SpeedLevel);

        public int CarryCapacity;
        public int CarryCount;
        public Color CarryColor = Color.white;
        public CharacterState State = CharacterState.Idle;

        /// <summary>Returns exactly what is being carried for mixed-stack visuals.</summary>
        public virtual System.Collections.Generic.List<Core.ItemType> GetCarriedItems() => new System.Collections.Generic.List<Core.ItemType>();

        [Header("Movement")]
        // Batch 36: 2.0 → 3.0. The world grew to 100×82 units (batch 21) but
        // speeds never followed — crossing the store band took ~8s and read as
        // sluggish. 3.0 restores the reference game's bustling pace for every
        // character; the player rides higher via the RoleCatalog speed curve.
        public float baseSpeed = 3.0f;          // world units / second at multiplier 1.0
        public float speedMultiplier = 1.0f;
        protected Vector3 target;
        protected bool hasTarget;

        public UpgradeCurve Curve;

        public float CurrentSpeed => baseSpeed * speedMultiplier;

        /// <summary>True while a SetTarget path is still being walked.</summary>
        public bool HasMoveTarget => hasTarget;

        protected virtual void Awake()
        {
            ApplyLevel(1);
        }

        /// <summary>Legacy path: sets BOTH tracks to the same level (used by save/load
        /// fallback for pre-split saves and by SceneBootstrapper before Configure()).</summary>
        public virtual void ApplyLevel(int level)
        {
            if (Curve == null) return;
            int clamped = Mathf.Clamp(level, 1, Curve.MaxLevel);
            ApplyStackLevel(clamped);
            ApplySpeedLevel(clamped);
        }

        public virtual void ApplyStackLevel(int level)
        {
            if (Curve == null) return;
            StackLevel = Mathf.Clamp(level, 1, Curve.MaxLevel);
            CarryCapacity = Curve.GetStep(StackLevel).stackCapacity;
        }

        public virtual void ApplySpeedLevel(int level)
        {
            if (Curve == null) return;
            SpeedLevel = Mathf.Clamp(level, 1, Curve.MaxLevel);
            speedMultiplier = Curve.GetStep(SpeedLevel).speedMultiplier;
        }

        /// <summary>Legacy combined-upgrade path — bumps both tracks. Prefer the
        /// track-specific TryUpgradeStack/TryUpgradeSpeed for reference-parity UI.</summary>
        public bool TryUpgrade(out int cost)
        {
            cost = Curve?.CostForNextLevel(Level) ?? -1;
            if (cost < 0) return false;
            ApplyLevel(Level + 1);
            return true;
        }

        public int NextStackCost => Curve?.CostForNextLevel(StackLevel) ?? -1;
        public int NextSpeedCost => Curve?.CostForNextLevel(SpeedLevel) ?? -1;

        public bool TryUpgradeStack(out int cost)
        {
            cost = NextStackCost;
            if (cost < 0) return false;
            ApplyStackLevel(StackLevel + 1);
            return true;
        }

        public bool TryUpgradeSpeed(out int cost)
        {
            cost = NextSpeedCost;
            if (cost < 0) return false;
            ApplySpeedLevel(SpeedLevel + 1);
            return true;
        }

        protected System.Collections.Generic.List<Vector3> pathWaypoints = new System.Collections.Generic.List<Vector3>();
        protected int currentWaypointIndex = 0;

        // ── Steering state ───────────────────────────────────────────────────
        /// <summary>Current velocity (XZ). Steering forces accumulate into this,
        /// which is what gives movement its weight and smooth turns.</summary>
        protected Vector3 velocity;

        /// <summary>How hard the agent can change its mind, in units/s². Higher =
        /// snappier, lower = more momentum. Tuned so workers bank into corners.</summary>
        protected float maxForce = 22f;

        /// <summary>Degrees/second the body turns to face its heading.</summary>
        protected float turnSpeed = 540f;

        /// <summary>Every live character — used for agent-vs-agent separation.</summary>
        public static readonly System.Collections.Generic.List<CharacterBase> All =
            new System.Collections.Generic.List<CharacterBase>();

        public void SetTarget(Vector3 worldPos)
        {
            target = new Vector3(worldPos.x, 0, worldPos.z);
            hasTarget = true;
            State = CharacterState.Walking;
            currentWaypointIndex = 0;

            // Preferred: nav-mesh A* + funnel → a handful of REAL corner points, so
            // the agent walks natural diagonals. Falls back to the old grid A* (and
            // finally a straight line) only if the mesh isn't baked.
            var nav = NavMesh.Instance;
            if (nav != null)
            {
                pathWaypoints = nav.FindPath(transform.position, target);
                if (pathWaypoints != null && pathWaypoints.Count > 0) return;
            }

            if (GridPathfinder.Instance != null)
            {
                pathWaypoints = GridPathfinder.Instance.FindPath(transform.position, target);
                if (pathWaypoints != null && pathWaypoints.Count > 0) return;
            }

            pathWaypoints = new System.Collections.Generic.List<Vector3> { target };
        }

        protected virtual void Start()
        {
            if (!All.Contains(this)) All.Add(this);
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterCharacter(this);

            var anim = GetComponentInChildren<Animator>();
            if (anim != null) anim.applyRootMotion = false;
        }

        protected virtual void OnDestroy()
        {
            All.Remove(this);
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
                Stop();
                return;
            }

            Vector3 pos = transform.position; pos.y = 0f;
            Vector3 wp  = pathWaypoints[currentWaypointIndex]; wp.y = 0f;
            bool lastLeg = currentWaypointIndex == pathWaypoints.Count - 1;

            float maxSpeed = CurrentSpeed;

            // ── Steering: blend the behaviours, don't just point-and-shoot ─────
            // Seek the next corner; ease into the FINAL one so we settle instead of
            // overshooting. Separation keeps workers from walking through each other,
            // and the wall feelers round them off obstacles. Summing forces (rather
            // than snapping the heading) is what makes the motion read as natural.
            Vector3 force = lastLeg
                ? Steering.Arrive(pos, velocity, wp, maxSpeed, ArriveRadius)
                : Steering.Seek(pos, velocity, wp, maxSpeed);

            force += Steering.Separation(pos, All, this, SeparationRadius, maxSpeed) * SeparationWeight;
            force += Steering.AvoidWalls(pos, velocity, Map.GridPathfinder.Instance, maxSpeed) * AvoidWeight;

            velocity += Steering.Clamp(force, maxForce) * dt;
            velocity.y = 0f;
            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

            Vector3 next = pos + velocity * dt;
            next.y = 0f;

            // Hard wall rule (map plan): NOBODY crosses an unwalkable cell. The mesh
            // path already stays inside walkable polys, so this only catches the
            // fallback/no-path case. Slide along the wall instead of stopping dead.
            var grid = Map.GridPathfinder.Instance;
            if (grid != null && !grid.IsWalkableWorld(next))
            {
                var slideX = new Vector3(next.x, 0f, pos.z);
                var slideZ = new Vector3(pos.x, 0f, next.z);
                if (grid.IsWalkableWorld(slideX)) { next = slideX; velocity.z = 0f; }
                else if (grid.IsWalkableWorld(slideZ)) { next = slideZ; velocity.x = 0f; }
                else { Stop(); return; }
            }

            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.Move(next - pos);
            else
            {
                var rb = GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic) rb.MovePosition(next);
                else transform.position = next;
            }

            // Turn the body toward where we're actually going, smoothly.
            if (velocity.sqrMagnitude > 0.02f)
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(velocity, Vector3.up),
                    turnSpeed * dt);

            // Advance along the path. Non-final corners can be clipped generously —
            // we're rounding them, not stopping on them.
            float reach = lastLeg ? 0.18f : NavMesh.AgentRadius + 0.15f;
            if ((next - wp).sqrMagnitude <= reach * reach)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= pathWaypoints.Count) Stop();
            }
        }

        /// <summary>Come to rest and fire OnArrived once.</summary>
        private void Stop()
        {
            velocity = Vector3.zero;
            hasTarget = false;
            State = CharacterState.Idle;
            OnArrived();
        }

        /// <summary>Distance at which we start easing to a halt.</summary>
        protected virtual float ArriveRadius => 1.2f;
        protected virtual float SeparationRadius => 1.1f;
        protected virtual float SeparationWeight => 0.9f;
        protected virtual float AvoidWeight => 1.3f;

        protected virtual void OnArrived() { }

        /// <summary>Hard-stop all movement WITHOUT firing OnArrived. Used when an
        /// agent reaches its purpose "well enough" (queue slot, shelf reach) and
        /// must stop orbiting under steering forces.</summary>
        protected void HaltMovement()
        {
            velocity = Vector3.zero;
            hasTarget = false;
            pathWaypoints?.Clear();
            currentWaypointIndex = 0;
            State = CharacterState.Idle;
        }

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
