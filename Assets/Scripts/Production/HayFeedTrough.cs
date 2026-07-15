using UnityEngine;

namespace MiniMart.Production
{
    /// <summary>
    /// Hay Feed Trough near the cow pasture (new map spec §Livestock West).
    /// Cow milk production rate is BOOSTED while the trough has hay in it;
    /// stall drains 1 unit of hay per FeedIntervalSeconds. Player refills by
    /// depositing Wheat at the trough (wheat is used as feed in-game).
    /// </summary>
    public class HayFeedTrough : MonoBehaviour
    {
        [Tooltip("Max wheat/hay units the trough can hold.")]
        public int Capacity = 8;

        [Tooltip("Seconds between hay consumption ticks (cow eats).")]
        public float FeedIntervalSeconds = 4f;

        [Tooltip("Milk speed multiplier while cow is fed (>1 speeds milk timer).")]
        public float FedSpeedBoost = 1.6f;

        public int HayCount { get; private set; }
        public bool IsFed => HayCount > 0;

        private float feedTimer;

        // Simple visual bindings — set by SceneBootstrapper. Cow references this
        // to boost milking; a small badge above the trough shows "n/cap".
        [System.NonSerialized] public CowPen LinkedCow;

        private TextMesh badge;

        private void Start()
        {
            // Live "n/cap" badge above the trough.
            var go = new GameObject("HayBadge");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, 1.3f, 0);
            badge = go.AddComponent<TextMesh>();
            badge.fontSize = 40;
            badge.characterSize = 0.075f;
            badge.anchor = TextAnchor.MiddleCenter;
            badge.alignment = TextAlignment.Center;
            badge.color = Color.white;
            var font = Engine.HUDBuilder.UIFont;
            if (font != null)
            {
                badge.font = font;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = font.material;
            }
            go.AddComponent<Billboard>();
            Refresh();
        }

        /// <summary>Player/farmer deposits hay (wheat units) into the trough.</summary>
        public int AddHay(int amount)
        {
            int fit = Mathf.Clamp(amount, 0, Capacity - HayCount);
            HayCount += fit;
            Refresh();
            return fit;
        }

        private void Update()
        {
            if (HayCount <= 0) return;
            feedTimer += Time.deltaTime;
            if (feedTimer < FeedIntervalSeconds) return;
            feedTimer = 0f;
            HayCount = Mathf.Max(0, HayCount - 1);
            Refresh();
        }

        private void Refresh()
        {
            if (badge != null) badge.text = $"Hay {HayCount}/{Capacity}";
        }
    }
}
