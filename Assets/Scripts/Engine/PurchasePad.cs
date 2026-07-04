using UnityEngine;

namespace MiniMart.Engine
{
    /// <summary>
    /// Reference-flow expansion mechanic: a marked spot on the ground with a bouncing arrow
    /// and a coin cost. Stand on it and your cash drains into it; when fully paid, the
    /// hidden target objects appear in place and the pad vanishes. The whole store is
    /// bought piece by piece this way — the world starts almost empty.
    /// </summary>
    public class PurchasePad : MonoBehaviour
    {
        public float Cost;
        public string Label = "";
        public GameObject[] Targets;

        private float remaining;
        private TextMesh label;
        private Transform arrow;
        private float bobT;

        public static PurchasePad Create(Vector3 position, float cost, string label, params GameObject[] targets)
        {
            var go = new GameObject($"Pad_{label}");
            go.transform.position = position;
            var pad = go.AddComponent<PurchasePad>();
            pad.Cost = cost;
            pad.Label = label;
            pad.Targets = targets;
            return pad;
        }

        private void Start()
        {
            remaining = Cost;

            // Ground disc.
            var disc = PrimitiveFactory.Part(PrimitiveType.Cylinder, transform,
                new Vector3(0, 0.03f, 0), new Vector3(1.6f, 0.03f, 1.6f),
                new Color(0.55f, 0.85f, 0.45f, 1f));
            disc.name = "PadDisc";

            // Bouncing arrow (a slim yellow cube standing on its corner reads as a pointer).
            var arrowGO = PrimitiveFactory.Part(PrimitiveType.Cube, transform,
                new Vector3(0, 1.6f, 0), new Vector3(0.28f, 0.28f, 0.28f),
                new Color(1f, 0.85f, 0.1f));
            arrowGO.name = "PadArrow";
            arrowGO.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            arrow = arrowGO.transform;

            // Cost + name badge.
            var badgeGO = new GameObject("PadLabel");
            badgeGO.transform.SetParent(transform, false);
            badgeGO.transform.localPosition = new Vector3(0, 1.0f, 0);
            label = badgeGO.AddComponent<TextMesh>();
            label.fontSize = 40;
            label.characterSize = 0.075f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;
            var font = HUDBuilder.UIFont;
            if (font != null)
            {
                label.font = font;
                var mr = badgeGO.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = font.material;
            }
            badgeGO.AddComponent<Billboard>();
            RefreshLabel();
        }

        private void Update()
        {
            bobT += Time.deltaTime * 4f;
            if (arrow != null)
                arrow.localPosition = new Vector3(0, 1.6f + Mathf.Abs(Mathf.Sin(bobT)) * 0.25f, 0);

            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null || gm.Economy == null) return;

            if (Vector3.Distance(gm.Player.transform.position, transform.position) > 1.5f) return;

            // Drain payment while the player stands here (reference: cost counts down).
            float step = Mathf.Min(remaining, Mathf.Max(2f, Cost) * Time.deltaTime / 1.2f, gm.Economy.PlayerCash);
            if (step <= 0f) return;
            gm.Economy.PlayerCash -= step;
            remaining -= step;
            RefreshLabel();

            if (remaining <= 0.01f) Purchase();
        }

        private void RefreshLabel()
        {
            if (label != null) label.text = $"{Label}\n$ {Mathf.CeilToInt(remaining)}";
        }

        private void Purchase()
        {
            foreach (var t in Targets)
                if (t != null) t.SetActive(true);
            GameManager.Instance?.MarkPadPurchased(Label);
            Debug.Log($"[PurchasePad] Purchased: {Label}");
            Destroy(gameObject);
        }

        /// <summary>Applied on load for pads that were already bought in a previous session —
        /// activates the targets for free and removes the pad.</summary>
        public void RestorePurchased()
        {
            foreach (var t in Targets)
                if (t != null) t.SetActive(true);
            Destroy(gameObject);
        }
    }
}
