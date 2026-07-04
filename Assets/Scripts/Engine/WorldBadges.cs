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
        public float Height = 2.2f;
        public float RefreshInterval = 0.4f;

        protected TextMesh badge;
        private float timer;

        private void Start()
        {
            var go = new GameObject("Badge");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, Height, 0);
            badge = go.AddComponent<TextMesh>();
            badge.fontSize = 40;
            badge.characterSize = 0.075f;
            badge.anchor = TextAnchor.MiddleCenter;
            badge.alignment = TextAlignment.Center;
            badge.color = Color.white;
            var font = HUDBuilder.UIFont;
            if (font != null)
            {
                badge.font = font;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = font.material;
            }
            go.AddComponent<Billboard>();
            Refresh();
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < RefreshInterval) return;
            timer = 0f;
            Refresh();
        }

        protected abstract void Refresh();
    }

    /// <summary>Lists the store inventory's non-empty item counts above the storage depot.</summary>
    public class StorageBadge : WorldBadge
    {
        protected override void Refresh()
        {
            var inv = GameManager.Instance?.Inventory;
            if (inv == null || badge == null) return;

            var sb = new System.Text.StringBuilder();
            int onLine = 0;
            foreach (var kv in inv.Stocks)
            {
                if (kv.Value.Count <= 0) continue;
                sb.Append(ShortName(kv.Key)).Append(' ').Append(kv.Value.Count).Append("  ");
                if (++onLine % 3 == 0) sb.Append('\n');
            }
            badge.text = sb.Length == 0 ? "STORAGE" : sb.ToString().TrimEnd();
        }

        private static string ShortName(ItemType t) => t switch
        {
            ItemType.TomatoKetchup => "Ketchup",
            ItemType.WheatFlour => "Flour",
            _ => t.ToString(),
        };
    }

    /// <summary>Shows a machine's input queue and finished output ("in 3/4  out 2").</summary>
    public class MachineBadge : WorldBadge
    {
        private Machine machine;
        private void Awake() => machine = GetComponent<Machine>();

        protected override void Refresh()
        {
            if (machine == null || badge == null) return;
            badge.text = $"in {machine.InputQueued}/{machine.StackCapacity}   out {machine.OutputReady}";
        }
    }
}
