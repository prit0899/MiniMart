using UnityEngine;
using MiniMart.Catalog;

namespace MiniMart.Production
{
    /// <summary>
    /// The hen now works like a processing station (owner spec): it EATS TOMATOES and
    /// lays EGGS. Feed it tomatoes (input buffer) and it converts one tomato into one
    /// egg on a timer while it has a tomato to eat and room in the egg tray.
    ///
    /// Two independent capacity tracks, each 4 → 6 → 8:
    ///   • Speed track = tomato (input) buffer   — StationCatalog.HenInput costs
    ///   • Stack track = egg (output) buffer      — StationCatalog.HenOutput costs
    /// </summary>
    public class HenCoop : MonoBehaviour
    {
        public int TomatoQueued;   // input buffer
        public int EggReady;       // output tray

        public int InputCapLevel = 1;   // tomato buffer  ("Speed")
        public int OutputCapLevel = 1;  // egg tray       ("Stack")

        private float timer;

        // Kept so save/load and older callers that read Level don't break.
        public int Level => Mathf.Max(InputCapLevel, OutputCapLevel);

        public int TomatoCapacity => StationCatalog.Cap(InputCapLevel);
        public int EggCapacity    => StationCatalog.Cap(OutputCapLevel);
        public int TomatoRoom     => Mathf.Max(0, TomatoCapacity - TomatoQueued);

        /// <summary>Farmer/player feed the hen tomatoes; returns how many were accepted.</summary>
        public int LoadTomato(int amount)
        {
            int fit = Mathf.Min(amount, TomatoRoom);
            TomatoQueued += fit;
            return fit;
        }

        private void Update()
        {
            if (TomatoQueued <= 0 || EggReady >= EggCapacity) return;

            timer += Time.deltaTime;
            float secondsPerEgg = FarmCatalog.EggGrowSecondsPerUnit; // one egg per tomato eaten
            if (timer >= secondsPerEgg)
            {
                timer -= secondsPerEgg;
                TomatoQueued -= 1;
                EggReady = Mathf.Min(EggCapacity, EggReady + 1);
            }
        }

        public int TotalEggsReady() => EggReady;

        /// <summary>Collect finished eggs (Farmer/player).</summary>
        public int Collect(int amount)
        {
            int taken = Mathf.Min(amount, EggReady);
            EggReady -= taken;
            return taken;
        }

        // ── Two upgrade tracks (matches the character / UpgradeRow API) ───────
        public int StackLevel => OutputCapLevel;   // egg tray
        public int SpeedLevel => InputCapLevel;    // tomato buffer

        public int NextStackCost => OutputCapLevel - 1 >= StationCatalog.HenOutput.Length
            ? -1 : StationCatalog.HenOutput[OutputCapLevel - 1];
        public int NextSpeedCost => InputCapLevel - 1 >= StationCatalog.HenInput.Length
            ? -1 : StationCatalog.HenInput[InputCapLevel - 1];

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

        // Back-compat shim for any old single-track caller (save/load, etc.).
        public int NextUpgradeCost => NextStackCost;
        public bool TryUpgrade(out int cost) => TryUpgradeStack(out cost);
        public void ApplyLevel(int newLevel)
        {
            int lv = Mathf.Clamp(newLevel, 1, StationCatalog.MaxLevel);
            InputCapLevel = lv; OutputCapLevel = lv;
        }
    }
}
