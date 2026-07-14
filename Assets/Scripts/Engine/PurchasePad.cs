using UnityEngine;

namespace MiniMart.Engine
{
    /// <summary>
    /// Reference-flow expansion pad: a marked spot on the ground with a big downward
    /// yellow triangle arrow bouncing above it and a rounded green cost pill
    /// underneath. Stand on it and your cash drains into the pad; when fully paid,
    /// the hidden target objects appear in place and the pad vanishes. The whole
    /// store is bought piece by piece this way — the world starts almost empty.
    /// </summary>
    public class PurchasePad : MonoBehaviour
    {
        public float Cost;
        public string Label = "";
        public GameObject[] Targets;
        /// <summary>Store level required before this pad even appears. Progressive
        /// disclosure like the reference game: a fresh player sees 3-4 pads, not 24 —
        /// each level-up "drip-feeds" the next batch, which is the addiction loop.</summary>
        public int MinLevel = 1;

        private float remaining;
        private TextMesh costLabel;
        private TextMesh nameLabel;
        private Transform arrow;
        private float bobT;
        private float dwell;
        private Vector3 lastPlayerPos;
        private bool lockedHidden;

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

        private bool progressApplied;

        private void Start()
        {
            if (!progressApplied) remaining = Cost;

            // Bright green ground disc — reference pad footprint.
            var disc = PrimitiveFactory.Part(PrimitiveType.Cylinder, transform,
                new Vector3(0, 0.03f, 0), new Vector3(1.7f, 0.03f, 1.7f),
                new Color(0.55f, 0.85f, 0.45f, 1f));
            disc.name = "PadDisc";

            // Downward-pointing triangle arrow (approximated: pyramid nose down).
            // We use a Cube stretched thin and rotated so its tip aims down — reads
            // as a big yellow arrow indicator from any camera pitch.
            arrow = new GameObject("ArrowPivot").transform;
            arrow.SetParent(transform, false);
            arrow.localPosition = new Vector3(0, 1.8f, 0);

            var yellow = new Color(1f, 0.85f, 0.10f);
            var stem = PrimitiveFactory.Part(PrimitiveType.Cube, arrow,
                new Vector3(0, 0.35f, 0), new Vector3(0.35f, 0.7f, 0.35f), yellow);
            stem.name = "ArrowStem";
            // Tip: an inverted pyramid (cube rotated 45° on X so a corner points down).
            var tip = PrimitiveFactory.Part(PrimitiveType.Cube, arrow,
                new Vector3(0, -0.15f, 0), new Vector3(0.7f, 0.7f, 0.35f), yellow);
            tip.name = "ArrowTip";
            tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // Name label — small dark text above the arrow.
            var nameGO = new GameObject("PadName");
            nameGO.transform.SetParent(transform, false);
            nameGO.transform.localPosition = new Vector3(0, 2.9f, 0);
            nameLabel = nameGO.AddComponent<TextMesh>();
            nameLabel.text = Label;
            nameLabel.fontSize = 36;
            nameLabel.characterSize = 0.075f;
            nameLabel.anchor = TextAnchor.MiddleCenter;
            nameLabel.alignment = TextAlignment.Center;
            nameLabel.color = new Color(0.15f, 0.15f, 0.15f);
            var font = HUDBuilder.UIFont;
            if (font != null)
            {
                nameLabel.font = font;
                var mr = nameGO.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = font.material;
            }
            nameGO.AddComponent<Billboard>();

            // Green cost pill directly under the arrow — reference cost tag.
            var pillGO = new GameObject("CostPill");
            pillGO.transform.SetParent(transform, false);
            pillGO.transform.localPosition = new Vector3(0, 0.9f, 0);
            pillGO.AddComponent<Billboard>();

            var pillBack = PrimitiveFactory.Part(PrimitiveType.Cube, pillGO.transform,
                Vector3.zero, new Vector3(1.1f, 0.34f, 0.05f), new Color(0.20f, 0.65f, 0.28f));
            pillBack.name = "PillBack";

            var costTextGO = new GameObject("CostText");
            costTextGO.transform.SetParent(pillGO.transform, false);
            costTextGO.transform.localPosition = new Vector3(0, 0, -0.06f);
            costLabel = costTextGO.AddComponent<TextMesh>();
            costLabel.fontSize = 40;
            costLabel.characterSize = 0.075f;
            costLabel.anchor = TextAnchor.MiddleCenter;
            costLabel.alignment = TextAlignment.Center;
            costLabel.color = Color.white;
            if (font != null)
            {
                costLabel.font = font;
                var cmr = costTextGO.GetComponent<MeshRenderer>();
                if (cmr != null) cmr.material = font.material;
            }
            RefreshLabel();

            // Hide every child until the first Update proves this pad is unlocked.
            // Otherwise a level-locked pad renders for one frame before Update can
            // hide it (visible flash on scene load). Unlocked pads reappear on the
            // very next frame — imperceptible — while locked ones simply stay
            // hidden. lockedHidden starts "true" so Update's change-check fires.
            lockedHidden = true;
            foreach (Transform child in transform)
                child.gameObject.SetActive(false);
        }

        private void Update()
        {
            var gm = GameManager.Instance;

            // Level lock: keep the whole pad invisible (and payment disabled)
            // until the store reaches MinLevel. Children are toggled, not the
            // root, so this Update keeps running to notice the unlock.
            bool locked = gm != null && gm.StoreLevel < MinLevel;
            if (locked != lockedHidden)
            {
                lockedHidden = locked;
                foreach (Transform child in transform)
                    child.gameObject.SetActive(!locked);
            }
            if (locked) return;

            bobT += Time.deltaTime * 4f;
            if (arrow != null)
                arrow.localPosition = new Vector3(0, 1.8f + Mathf.Abs(Mathf.Sin(bobT)) * 0.35f, 0);

            if (gm == null || gm.Player == null || gm.Economy == null) return;

            Vector3 playerPos = gm.Player.transform.position;
            if (Vector3.Distance(playerPos, transform.position) > 1.6f)
            {
                dwell = 0f;
                return;
            }

            // Only a player who deliberately STOPS on the pad pays — merely running
            // across it must never siphon the wallet (that bug once ate $231 in one
            // sprint through the store). Movement resets the dwell timer.
            if ((playerPos - lastPlayerPos).magnitude > 2.5f * Time.deltaTime)
                dwell = 0f;
            lastPlayerPos = playerPos;

            dwell += Time.deltaTime;
            if (dwell < 0.35f) return;

            float step = Mathf.Min(remaining, Mathf.Max(2f, Cost) * Time.deltaTime / 1.8f, gm.Economy.PlayerCash);
            if (step <= 0f) return;
            gm.Economy.PlayerCash -= step;
            remaining -= step;
            RefreshLabel();

            if (remaining <= 0.01f) Purchase();
        }

        private void RefreshLabel()
        {
            if (costLabel != null) costLabel.text = $"$ {Mathf.CeilToInt(remaining)}";
            if (nameLabel != null) nameLabel.text = Label;
        }

        private void Purchase()
        {
            foreach (var t in Targets)
                if (t != null) t.SetActive(true);
            GameManager.Instance?.MarkPadPurchased(Label);
            AudioFx.Purchase();
            Vfx.Poof(transform.position); // cartoon poof where the new thing appears
            Debug.Log($"[PurchasePad] Purchased: {Label}");
            Destroy(gameObject);
        }

        public void RestorePurchased()
        {
            foreach (var t in Targets)
                if (t != null) t.SetActive(true);
            Destroy(gameObject);
        }

        public float Remaining => remaining;

        public void ApplyProgress(float savedRemaining)
        {
            progressApplied = true;
            remaining = Mathf.Clamp(savedRemaining, 0f, Cost);
            RefreshLabel();
            if (remaining <= 0.01f) Purchase();
        }
    }
}
