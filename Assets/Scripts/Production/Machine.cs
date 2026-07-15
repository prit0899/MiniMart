using UnityEngine;
using MiniMart.Catalog;

namespace MiniMart.Production
{
    /// <summary>
    /// One processing machine (Blender/Oven/Mill). Stack capacity and speed scale with
    /// level 1-4 per ProductionCatalog.SharedFourLevelCurve. Converts InputItem -> OutputItem.
    /// </summary>
    public class Machine : MonoBehaviour
    {
        public MachineType Type;
        public int Level = 1;
        public int InputQueued;
        public int OutputReady;
        private float processTimer;
        private UpgradeCurve curve;
        private UpgradeStep step;

        private void InitializeIfNeeded()
        {
            if (curve != null) return;
            curve = Type switch
            {
                MachineType.Blender => ProductionCatalog.BlenderCurve(),
                MachineType.Oven => ProductionCatalog.OvenCurve(),
                MachineType.Mill => ProductionCatalog.MillCurve(),
                MachineType.Dairy => ProductionCatalog.DairyCurve(),
                MachineType.LeafProcessor => ProductionCatalog.LeafProcessorCurve(),
                MachineType.Stove => ProductionCatalog.StoveCurve(),
                MachineType.CornProcessor   => ProductionCatalog.CornProcessorCurve(),
                MachineType.CookieStation   => ProductionCatalog.CookieStationCurve(),
                MachineType.CoffeeDispenser => ProductionCatalog.CoffeeDispenserCurve(),
                _ => ProductionCatalog.BlenderCurve(),
            };
            ApplyLevel(1);
        }

        private void Awake()
        {
            InitializeIfNeeded();
        }

        public void ApplyLevel(int level)
        {
            InitializeIfNeeded();
            Level = Mathf.Clamp(level, 1, curve.MaxLevel);
            step = curve.GetStep(Level);
        }

        /// <summary>Cost of the next level, or -1 at max. Read-only — safe for UI polling.</summary>
        public int NextUpgradeCost
        {
            get
            {
                InitializeIfNeeded();
                return curve.CostForNextLevel(Level);
            }
        }

        public bool TryUpgrade(out int cost)
        {
            InitializeIfNeeded();
            cost = curve.CostForNextLevel(Level);
            if (cost < 0) return false;
            ApplyLevel(Level + 1);
            return true;
        }

        public int StackCapacity
        {
            get
            {
                InitializeIfNeeded();
                return step.stackCapacity;
            }
        }

        /// <summary>Adds raw ingredient to the input queue, capped at the machine's stack capacity.</summary>
        public void LoadInput(int amount)
        {
            InputQueued = Mathf.Min(StackCapacity, InputQueued + amount);
        }

        private ParticleSystem steam;

        /// <summary>White steam puffs while processing — reference-game machine feedback.</summary>
        private void EnsureSteam()
        {
            if (steam != null) return;
            var go = new GameObject("Steam");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, 1.25f, 0);
            steam = go.AddComponent<ParticleSystem>();

            var main = steam.main;
            main.startLifetime = 0.7f;
            main.startSpeed = 0.6f;
            // Softer / smaller puffs so it reads as steam, not blocky cubes.
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
            main.startColor = new Color(1f, 1f, 1f, 0.4f);
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = steam.emission;
            emission.rateOverTime = 3f;

            var shape = steam.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.12f;

            var col = steam.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var renderer = steam.GetComponent<ParticleSystemRenderer>();
            renderer.material = Engine.PrimitiveFactory.NewParticleMaterial();
            steam.Stop();
        }

        /// <summary>Coffee dispenser produces without input — a self-contained "fresh
        /// coffee" appliance in the café. All other machines require input queued
        /// before they can produce.</summary>
        private bool IsAutoProducer => Type == MachineType.CoffeeDispenser;

        private void Update()
        {
            InitializeIfNeeded();
            EnsureSteam();

            bool processing = (IsAutoProducer || InputQueued > 0) && OutputReady < StackCapacity;
            if (processing && !steam.isPlaying) steam.Play();
            else if (!processing && steam.isPlaying) steam.Stop();

            // Non-auto machines need input; auto machines just need output tray space.
            if (!IsAutoProducer && InputQueued <= 0) return;

            // Output tray full: stall processing instead of consuming input and silently
            // DESTROYING the product (OutputReady was clamped, losing one item per cycle).
            if (OutputReady >= StackCapacity) return;

            float baseSeconds = ProductionCatalog.BaseProcessSeconds[Type];
            float secondsPerUnit = baseSeconds / step.speedMultiplier;

            processTimer += Time.deltaTime;
            if (processTimer >= secondsPerUnit)
            {
                processTimer = 0f;
                if (!IsAutoProducer) InputQueued -= 1;
                OutputReady = Mathf.Min(StackCapacity, OutputReady + 1);
            }
        }

        /// <summary>Workers call this to pull finished product out of the machine into their hands/storage.</summary>
        public int CollectFinished()
        {
            int amount = OutputReady;
            OutputReady = 0;
            return amount;
        }
    }
}
