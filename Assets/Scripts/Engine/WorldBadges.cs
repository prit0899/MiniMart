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
