using UnityEngine;
using MiniMart.AI;
using MiniMart.Core;

namespace MiniMart.Engine
{
    /// <summary>
    /// Reference-style thought bubble above a buyer: a colored item chip plus "collected/wanted"
    /// while shopping, and a "$" chip while queueing at the till.
    /// </summary>
    public class ThoughtBubble : MonoBehaviour
    {
        private Buyer buyer;
        private GameObject root;
        private MeshRenderer iconRenderer;
        private TextMesh label;
        private float timer;

        private void Start()
        {
            buyer = GetComponent<Buyer>();

            root = new GameObject("ThoughtBubble");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0.55f, 2.0f, 0);
            root.AddComponent<Billboard>();

            // Backboard.
            var back = PrimitiveFactory.Part(PrimitiveType.Cube, root.transform,
                Vector3.zero, new Vector3(1.0f, 0.55f, 0.05f), new Color(1f, 1f, 1f, 1f));
            back.name = "Back";

            // Item chip.
            var icon = PrimitiveFactory.Part(PrimitiveType.Cube, root.transform,
                new Vector3(-0.28f, 0, -0.06f), new Vector3(0.3f, 0.3f, 0.05f), Color.white);
            icon.name = "Icon";
            iconRenderer = icon.GetComponent<MeshRenderer>();

            // Progress text.
            var textGO = new GameObject("Progress");
            textGO.transform.SetParent(root.transform, false);
            textGO.transform.localPosition = new Vector3(0.18f, 0, -0.06f);
            label = textGO.AddComponent<TextMesh>();
            label.fontSize = 40;
            label.characterSize = 0.05f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.15f, 0.15f, 0.15f);
            var font = HUDBuilder.UIFont;
            if (font != null)
            {
                label.font = font;
                var mr = textGO.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = font.material;
            }
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < 0.25f) return;
            timer = 0f;
            if (buyer == null || root == null) return;

            if (buyer.HasCheckedOut)
            {
                root.SetActive(false);
                return;
            }

            if (buyer.InQueue)
            {
                root.SetActive(true);
                iconRenderer.material.color = new Color(0.3f, 0.8f, 0.35f);
                label.text = "$";
                return;
            }

            // First outstanding wish: colored chip + collected/wanted.
            foreach (var kv in buyer.Basket)
            {
                if (kv.Value <= 0) continue;
                ItemType item = kv.Key;
                int want = buyer.OriginalWant.TryGetValue(item, out int w) ? w : kv.Value;
                int have = want - kv.Value;
                root.SetActive(true);
                iconRenderer.material.color = PrimitiveFactory.ItemColor(item);
                label.text = $"{have}/{want}";
                return;
            }

            root.SetActive(false); // nothing left to wish for
        }
    }
}
