using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniMart.Engine
{
    /// <summary>
    /// Travel pad that lets the player move between MiniMart and MegaMart scenes.
    /// Stand on it for 1 second, then the game saves and loads the target scene.
    /// Works exactly like PurchasePad's dwell mechanic but triggers a scene load
    /// instead of a purchase.
    /// </summary>
    public class SceneTransition : MonoBehaviour
    {
        public string TargetScene = "MegaMart";
        public string Label = "Travel to MegaMart";

        private float dwell;
        private Vector3 lastPlayerPos;
        private TextMesh nameLabel;
        private TextMesh actionLabel;
        private Transform arrow;
        private float bobT;
        private bool transitioning;

        public static SceneTransition Create(Vector3 position, string targetScene, string label)
        {
            var go = new GameObject($"TravelPad_{label}");
            go.transform.position = position;
            var st = go.AddComponent<SceneTransition>();
            st.TargetScene = targetScene;
            st.Label = label;
            return st;
        }

        private void Start()
        {
            // Green-blue ground disc — distinct from purchase pads (green).
            var disc = PrimitiveFactory.Part(PrimitiveType.Cylinder, transform,
                new Vector3(0, 0.03f, 0), new Vector3(2.0f, 0.03f, 2.0f),
                new Color(0.30f, 0.70f, 0.85f, 1f));
            disc.name = "TravelDisc";

            // Upward-pointing arrow (inviting "go" feel).
            arrow = new GameObject("ArrowPivot").transform;
            arrow.SetParent(transform, false);
            arrow.localPosition = new Vector3(0, 1.8f, 0);

            var arrowColor = new Color(0.30f, 0.85f, 0.55f);
            var stem = PrimitiveFactory.Part(PrimitiveType.Cube, arrow,
                new Vector3(0, 0.35f, 0), new Vector3(0.35f, 0.7f, 0.35f), arrowColor);
            stem.name = "ArrowStem";
            var tip = PrimitiveFactory.Part(PrimitiveType.Cube, arrow,
                new Vector3(0, 0.85f, 0), new Vector3(0.7f, 0.7f, 0.35f), arrowColor);
            tip.name = "ArrowTip";
            tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // Name label
            var nameGO = new GameObject("TravelName");
            nameGO.transform.SetParent(transform, false);
            nameGO.transform.localPosition = new Vector3(0, 3.2f, 0);
            nameLabel = nameGO.AddComponent<TextMesh>();
            nameLabel.text = Label;
            nameLabel.fontSize = 36;
            nameLabel.characterSize = 0.08f;
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

            // Action hint label
            var actionGO = new GameObject("ActionHint");
            actionGO.transform.SetParent(transform, false);
            actionGO.transform.localPosition = new Vector3(0, 0.9f, 0);
            actionLabel = actionGO.AddComponent<TextMesh>();
            actionLabel.text = "Stand here to travel";
            actionLabel.fontSize = 32;
            actionLabel.characterSize = 0.06f;
            actionLabel.anchor = TextAnchor.MiddleCenter;
            actionLabel.alignment = TextAlignment.Center;
            actionLabel.color = Color.white;
            if (font != null)
            {
                actionLabel.font = font;
                var amr = actionGO.GetComponent<MeshRenderer>();
                if (amr != null) amr.material = font.material;
            }
            actionGO.AddComponent<Billboard>();
        }

        private void Update()
        {
            if (transitioning) return;

            bobT += Time.deltaTime * 3f;
            if (arrow != null)
                arrow.localPosition = new Vector3(0, 1.8f + Mathf.Abs(Mathf.Sin(bobT)) * 0.4f, 0);

            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null) return;

            Vector3 playerPos = gm.Player.transform.position;
            if (Vector3.Distance(playerPos, transform.position) > 2.0f)
            {
                dwell = 0f;
                return;
            }

            // Must stop moving before travel triggers.
            if ((playerPos - lastPlayerPos).magnitude > 2.5f * Time.deltaTime)
                dwell = 0f;
            lastPlayerPos = playerPos;

            dwell += Time.deltaTime;
            if (dwell < 1.5f) return; // must stand still for 1.5 seconds

            // Save and travel!
            transitioning = true;
            Debug.Log($"[SceneTransition] Travelling to {TargetScene}...");

            // Force-drop carried items before scene switch.
            var pi = gm.Player.GetComponent<Characters.PlayerInteraction>();
            if (pi != null)
            {
                // Clear carried items so they don't get lost between scenes.
                gm.Player.CarryCount = 0;
            }

            // Trigger save.
            gm.SendMessage("DoSave", SendMessageOptions.DontRequireReceiver);

            // Load target scene.
            SceneManager.LoadScene(TargetScene);
        }
    }
}
