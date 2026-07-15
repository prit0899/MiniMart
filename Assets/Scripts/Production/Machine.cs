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

        // ── Split-capacity model (owner spec, Mart 1) ────────────────────────
        // When SplitCapacity is on, the machine has INDEPENDENT input and output
        // buffers, each starting at 4 and upgraded to 6 then 8 on its own track.
        // When off (MegaMart, untouched), input == output == the old curve's
        // stackCapacity, so behaviour is exactly as before.
        public bool SplitCapacity;
        public int InputCapLevel = 1;    // "Speed" track  (feed buffer)
        public int OutputCapLevel = 1;   // "Stack" track  (product buffer)
        public int[] InputUpgradeCosts;  // cost 1->2, 2->3  (set by bootstrapper)
        public int[] OutputUpgradeCosts;

        // Optional SECOND input (only the Oven: Bread = Egg + Wheat Flour).
        public Core.ItemType? InputItem2;
        public int InputQueued2;

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

        /// <summary>Input-buffer capacity. In split mode this is the "Speed" (feed) track.</summary>
        public int InputCapacity
        {
            get
            {
                InitializeIfNeeded();
                return SplitCapacity ? StationCatalog.Cap(InputCapLevel) : step.stackCapacity;
            }
        }

        /// <summary>Output-tray capacity. In split mode this is the "Stack" (product) track.</summary>
        public int OutputCapacity
        {
            get
            {
                InitializeIfNeeded();
                return SplitCapacity ? StationCatalog.Cap(OutputCapLevel) : step.stackCapacity;
            }
        }

        /// <summary>Back-compat alias: existing load-guards ("InputQueued &lt; StackCapacity")
        /// still mean the INPUT buffer. Output clamping now uses OutputCapacity.</summary>
        public int StackCapacity => InputCapacity;

        /// <summary>Adds raw ingredient to the (first) input queue, capped at input capacity.</summary>
        public void LoadInput(int amount)
        {
            InputQueued = Mathf.Min(InputCapacity, InputQueued + amount);
        }

        /// <summary>Adds the SECOND ingredient (Oven only), capped at input capacity.</summary>
        public void LoadInput2(int amount)
        {
            InputQueued2 = Mathf.Min(InputCapacity, InputQueued2 + amount);
        }

        // ── Two upgrade tracks (matches the character/UpgradeRow API) ─────────
        //   Stack track = OUTPUT buffer,  Speed track = INPUT buffer.
        public int StackLevel => OutputCapLevel;
        public int SpeedLevel => InputCapLevel;

        public int NextStackCost => (!SplitCapacity || OutputUpgradeCosts == null
            || OutputCapLevel - 1 >= OutputUpgradeCosts.Length) ? -1 : OutputUpgradeCosts[OutputCapLevel - 1];
        public int NextSpeedCost => (!SplitCapacity || InputUpgradeCosts == null
            || InputCapLevel - 1 >= InputUpgradeCosts.Length) ? -1 : InputUpgradeCosts[InputCapLevel - 1];

        public bool TryUpgradeStack(out int cost)
        {
            cost = NextStackCost;
            if (cost < 0) return false;
            OutputCapLevel = Mathf.Min(StationCatalog.MaxLevel, OutputCapLevel + 1);
            return true;
        }

        public bool TryUpgradeSpeed(out int cost)
        {
            cost = NextSpeedCost;
            if (cost < 0) return false;
            InputCapLevel = Mathf.Min(StationCatalog.MaxLevel, InputCapLevel + 1);
            return true;
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

            // A two-input machine (Oven) needs BOTH ingredients queued.
            bool hasInputs = IsAutoProducer
                || (InputItem2.HasValue ? (InputQueued > 0 && InputQueued2 > 0) : InputQueued > 0);

            bool processing = hasInputs && OutputReady < OutputCapacity;
            if (processing && !steam.isPlaying) steam.Play();
            else if (!processing && steam.isPlaying) steam.Stop();

            if (!hasInputs) return;

            // Output tray full: stall instead of consuming input and silently
            // DESTROYING the product.
            if (OutputReady >= OutputCapacity) return;

            float baseSeconds = ProductionCatalog.BaseProcessSeconds[Type];
            float secondsPerUnit = baseSeconds / step.speedMultiplier;

            processTimer += Time.deltaTime;
            if (processTimer >= secondsPerUnit)
            {
                processTimer = 0f;
                if (!IsAutoProducer)
                {
                    InputQueued -= 1;
                    if (InputItem2.HasValue) InputQueued2 -= 1;
                }
                OutputReady = Mathf.Min(OutputCapacity, OutputReady + 1);
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
