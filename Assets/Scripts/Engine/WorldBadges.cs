using UnityEngine;
using MiniMart.Core;
using MiniMart.Production;

namespace MiniMart.Engine
{
    /// <summary>
    /// Floating billboarded count label over a world object (same style as shelf badges),
    /// so storage and machines visibly show what they hold.
    /// </summary>
    public abstract class WorldBadge : MonoBehaviour
    {
        public float Height = 1.9f;
        public float RefreshInterval = 0.4f;

        protected TextMesh badge;
        protected GameObject chip;         // dark tag backboard
        private float timer;

        private void Start()
        {
            // Compact reference-style pill: a small dark chip with tiny white text
            // sitting just above the object. Way less noisy than the tall floating
            // labels that dominated the earlier render.
            chip = new GameObject("Chip");
            chip.transform.SetParent(transform, false);
            chip.transform.localPosition = new Vector3(0, Height, 0);
            chip.AddComponent<Billboard>();

            // Chip background — dark rounded pill.
            PrimitiveFactory.Part(PrimitiveType.Cube, chip.transform,
                Vector3.zero, new Vector3(0.55f, 0.22f, 0.02f),
                new Color(0.18f, 0.18f, 0.22f, 1f));

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(chip.transform, false);
            textGO.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            badge = textGO.AddComponent<TextMesh>();
            badge.fontSize = 48;
            badge.characterSize = 0.03f;
            badge.anchor = TextAnchor.MiddleCenter;
            badge.alignment = TextAlignment.Center;
            badge.color = Color.white;
            var font = HUDBuilder.UIFont;
            if (font != null)
            {
                badge.font = font;
                var mr = textGO.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = font.material;
            }
            chip.SetActive(false); // start hidden — subclasses toggle it
            Refresh();
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < RefreshInterval) return;
            timer = 0f;
            Refresh();
        }

        /// <summary>Show or hide the whole chip in one call from subclasses.</summary>
        protected void Show(bool visible)
        {
            if (chip != null && chip.activeSelf != visible) chip.SetActive(visible);
        }

        protected abstract void Refresh();
    }

    /// <summary>Depot inventory chip — shown only while it holds anything.</summary>
    public class StorageBadge : WorldBadge
    {
        protected override void Refresh()
        {
            var inv = GameManager.Instance?.Inventory;
            if (inv == null || badge == null) return;

            int total = 0;
            foreach (var kv in inv.Stocks) total += kv.Value.Count;
            if (total == 0) { Show(false); return; }
            badge.text = $"S {total}";
            Show(true);
        }
    }

    /// <summary>Farm chip — owner: show growth progress on every farm. A "ripe/cap"
    /// readout plus a small fill bar under the chip, so you can see how full a farm is
    /// and when it's worth harvesting. Driven by a getter set at creation so no farm
    /// class needs editing (they have divergent internals).</summary>
    public class FarmBadge : WorldBadge
    {
        public System.Func<int> Ripe;   // current ripe/harvestable units
        public int Capacity = 1;        // max the farm can hold

        private Transform barFill;
        private const float BarW = 0.5f;

        protected void EnsureBar()
        {
            if (barFill != null || chip == null) return;
            // Track + green fill just under the count chip.
            PrimitiveFactory.Part(PrimitiveType.Cube, chip.transform,
                new Vector3(0f, -0.17f, -0.01f), new Vector3(BarW, 0.07f, 0.02f),
                new Color(0.20f, 0.22f, 0.26f));
            barFill = PrimitiveFactory.Part(PrimitiveType.Cube, chip.transform,
                new Vector3(0f, -0.17f, -0.03f), new Vector3(BarW, 0.06f, 0.03f),
                new Color(0.40f, 0.85f, 0.35f)).transform;
        }

        protected override void Refresh()
        {
            if (Ripe == null || badge == null) return;
            int r = Ripe();
            int cap = Mathf.Max(1, Capacity);
            badge.text = $"{r}/{cap}";
            Show(true);            // farms are always present — always show the readout
            EnsureBar();
            if (barFill != null)
            {
                float frac = Mathf.Clamp01((float)r / cap);
                barFill.localScale = new Vector3(Mathf.Max(0.0001f, BarW * frac), 0.06f, 0.03f);
                barFill.localPosition = new Vector3(-BarW * 0.5f + BarW * frac * 0.5f, -0.17f, -0.03f);
            }
        }
    }

    /// <summary>Hen coop chip — owner found the hen confusing ("how many tomatoes
    /// given vs eggs got?"). Show both counts explicitly, side by side, whenever the
    /// coop holds either: tomatoes eaten-in and eggs ready-out.</summary>
    public class HenBadge : WorldBadge
    {
        private HenCoop coop;
        private void Awake() => coop = GetComponent<HenCoop>();

        protected override void Refresh()
        {
            if (coop == null || badge == null) return;
            if (coop.TomatoQueued <= 0 && coop.EggReady <= 0) { Show(false); return; }
            // "Tom N  Egg M" — plainly separates the input (tomatoes fed) from the
            // output (eggs laid) so the conversion is legible at a glance.
            badge.text = $"Tom {coop.TomatoQueued}  Egg {coop.EggReady}";
            Show(true);
        }
    }

    /// <summary>Machine chip — hidden when idle-empty, shown as "IN n" while working
    /// or "OUT n" when output is ready to collect. Never the reference's "in 0/4 out 0"
    /// clutter that stayed visible over every idle appliance.</summary>
    public class MachineBadge : WorldBadge
    {
        private Machine machine;
        private void Awake() => machine = GetComponent<Machine>();

        protected override void Refresh()
        {
            if (machine == null || badge == null) return;
            if (machine.OutputReady > 0)
            {
                badge.text = $"{machine.OutputReady}";
                Show(true);
            }
            else if (machine.InputQueued > 0)
            {
                badge.text = $"{machine.InputQueued}/{machine.StackCapacity}";
                Show(true);
            }
            else
            {
                Show(false);
            }
        }
    }
}
