using UnityEngine;
using MiniMart.AI;
using MiniMart.Core;

namespace MiniMart.Engine
{
    /// <summary>
    /// Reference-parity thought bubble: a white rounded speech bubble with the
    /// current wanted item's colored icon, a "have/want" count, and a progress bar
    /// under the bubble (owner: show 4/5 while a buyer waits for the 5th). When the
    /// current item is fully collected the bubble advances to the next wish; while
    /// queueing at the till it swaps to a register icon.
    /// </summary>
    public class ThoughtBubble : MonoBehaviour
    {
        private const float BarW = 0.62f;

        private Buyer buyer;
        private GameObject root;
        private MeshRenderer iconRenderer;
        private MeshRenderer queueIconRenderer;
        private TextMesh countText;
        private GameObject barBg;
        private GameObject barFill;
        private float timer;

        private void Start()
        {
            buyer = GetComponent<Buyer>();

            root = new GameObject("ThoughtBubble");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0.45f, 1.9f, 0);
            root.AddComponent<Billboard>();

            // Round white backboard — reference bubble is a circle, not a rectangle.
            var back = PrimitiveFactory.Part(PrimitiveType.Sphere, root.transform,
                Vector3.zero, new Vector3(0.7f, 0.7f, 0.08f), new Color(1f, 1f, 1f, 1f));
            back.name = "Back";

            // Tiny "tail" (small sphere pointing down toward the buyer).
            var tail = PrimitiveFactory.Part(PrimitiveType.Sphere, root.transform,
                new Vector3(-0.22f, -0.35f, 0f), new Vector3(0.18f, 0.18f, 0.05f), new Color(1f, 1f, 1f, 1f));
            tail.name = "Tail";

            // Item chip — square colored icon, upper half of the bubble.
            var icon = PrimitiveFactory.Part(PrimitiveType.Cube, root.transform,
                new Vector3(0f, 0.12f, -0.06f), new Vector3(0.34f, 0.34f, 0.05f), Color.white);
            icon.name = "Icon";
            iconRenderer = icon.GetComponent<MeshRenderer>();

            // "have/want" count under the icon.
            var countGO = new GameObject("Count");
            countGO.transform.SetParent(root.transform, false);
            countGO.transform.localPosition = new Vector3(0f, -0.14f, -0.07f);
            countText = countGO.AddComponent<TextMesh>();
            countText.fontSize = 60;
            countText.characterSize = 0.021f;
            countText.anchor = TextAnchor.MiddleCenter;
            countText.alignment = TextAlignment.Center;
            countText.color = new Color(0.15f, 0.15f, 0.15f);
            var font = HUDBuilder.UIFont;
            if (font != null)
            {
                countText.font = font;
                var mr = countGO.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = font.material;
            }

            // Progress bar just under the bubble: dark track + green fill.
            barBg = PrimitiveFactory.Part(PrimitiveType.Cube, root.transform,
                new Vector3(0f, -0.5f, -0.06f), new Vector3(BarW, 0.12f, 0.04f),
                new Color(0.20f, 0.22f, 0.26f));
            barBg.name = "BarBg";
            barFill = PrimitiveFactory.Part(PrimitiveType.Cube, root.transform,
                new Vector3(0f, -0.5f, -0.08f), new Vector3(BarW, 0.10f, 0.05f),
                new Color(0.40f, 0.85f, 0.35f));
            barFill.name = "BarFill";

            // Alternate icon for queueing: dark 'register' rectangle.
            var qIcon = PrimitiveFactory.Part(PrimitiveType.Cube, root.transform,
                new Vector3(0f, 0.05f, -0.06f), new Vector3(0.42f, 0.28f, 0.05f), new Color(0.18f, 0.20f, 0.24f));
            qIcon.name = "QueueIcon";
            queueIconRenderer = qIcon.GetComponent<MeshRenderer>();
            qIcon.SetActive(false);
        }

        private void SetProgressVisible(bool on)
        {
            if (countText != null) countText.gameObject.SetActive(on);
            if (barBg != null) barBg.SetActive(on);
            if (barFill != null) barFill.SetActive(on);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < 0.2f) return;
            timer = 0f;
            if (buyer == null || root == null) return;

            if (buyer.HasCheckedOut)
            {
                root.SetActive(false);
                return;
            }

            // Queueing: swap to the register icon, hide the shopping progress.
            if (buyer.InQueue)
            {
                root.SetActive(true);
                if (iconRenderer != null) iconRenderer.gameObject.SetActive(false);
                if (queueIconRenderer != null) queueIconRenderer.gameObject.SetActive(true);
                SetProgressVisible(false);
                return;
            }

            // Current outstanding wish → colored chip + "have/want" + progress bar.
            foreach (var kv in buyer.Basket)
            {
                if (kv.Value <= 0) continue;
                ItemType item = kv.Key;

                int total = buyer.OriginalWant.TryGetValue(item, out int w) ? w : kv.Value;
                int have = buyer.Collected.TryGetValue(item, out int c) ? c : 0;
                if (total < 1) total = 1;
                float frac = Mathf.Clamp01((float)have / total);

                root.SetActive(true);
                if (queueIconRenderer != null) queueIconRenderer.gameObject.SetActive(false);
                if (iconRenderer != null)
                {
                    iconRenderer.gameObject.SetActive(true);
                    iconRenderer.material.color = PrimitiveFactory.ItemColor(item);
                }
                SetProgressVisible(true);
                if (countText != null) countText.text = $"{have}/{total}";
                if (barFill != null)
                {
                    barFill.transform.localScale = new Vector3(Mathf.Max(0.0001f, BarW * frac), 0.10f, 0.05f);
                    barFill.transform.localPosition = new Vector3(-BarW * 0.5f + BarW * frac * 0.5f, -0.5f, -0.08f);
                }
                return;
            }

            // Wish list complete, not queued yet — hide the bubble during the walk to the till.
            root.SetActive(false);
        }
    }
}
