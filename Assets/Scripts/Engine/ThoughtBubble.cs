using UnityEngine;
using MiniMart.AI;
using MiniMart.Core;

namespace MiniMart.Engine
{
    /// <summary>
    /// Reference-parity thought bubble: a white rounded speech bubble with just a
    /// colored item icon inside — no text. Reference "My Mini Mart" bubbles are
    /// icon-only (a tomato chip means 'wants tomatoes'). While queueing at the
    /// till the bubble swaps the icon for a laptop/cash symbol.
    /// </summary>
    public class ThoughtBubble : MonoBehaviour
    {
        private Buyer buyer;
        private GameObject root;
        private MeshRenderer iconRenderer;
        private MeshRenderer queueIconRenderer;
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

            // Item chip — square colored icon centered in the bubble.
            var icon = PrimitiveFactory.Part(PrimitiveType.Cube, root.transform,
                new Vector3(0f, 0f, -0.06f), new Vector3(0.38f, 0.38f, 0.05f), Color.white);
            icon.name = "Icon";
            iconRenderer = icon.GetComponent<MeshRenderer>();

            // Alternate icon for queueing: dark 'laptop/register' rectangle. Kept
            // as a separate object so we can toggle without recoloring the same mesh.
            var qIcon = PrimitiveFactory.Part(PrimitiveType.Cube, root.transform,
                new Vector3(0f, 0f, -0.06f), new Vector3(0.42f, 0.28f, 0.05f), new Color(0.18f, 0.20f, 0.24f));
            qIcon.name = "QueueIcon";
            queueIconRenderer = qIcon.GetComponent<MeshRenderer>();
            qIcon.SetActive(false);
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

            // Queueing: swap to the laptop/register icon (reference behavior).
            if (buyer.InQueue)
            {
                root.SetActive(true);
                if (iconRenderer != null) iconRenderer.gameObject.SetActive(false);
                if (queueIconRenderer != null) queueIconRenderer.gameObject.SetActive(true);
                return;
            }

            // First outstanding wish → colored item chip only, no text.
            foreach (var kv in buyer.Basket)
            {
                if (kv.Value <= 0) continue;
                ItemType item = kv.Key;
                root.SetActive(true);
                if (queueIconRenderer != null) queueIconRenderer.gameObject.SetActive(false);
                if (iconRenderer != null)
                {
                    iconRenderer.gameObject.SetActive(true);
                    iconRenderer.material.color = PrimitiveFactory.ItemColor(item);
                }
                return;
            }

            // Wish list complete, but not queued yet — hide the bubble entirely
            // (reference doesn't show anything during the short walk to the till).
            root.SetActive(false);
        }
    }
}
