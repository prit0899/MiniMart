using UnityEngine;

namespace MiniMart.Engine
{
    /// <summary>
    /// Shrinks this RectTransform to Screen.safeArea so HUD elements never sit under a
    /// notch, dynamic island, or rounded corner. Re-applies when resolution/orientation changes.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rt;
        private Rect lastApplied;
        private int lastW, lastH;

        private void Awake()
        {
            rt = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            // Re-apply when the safe area OR the view size changes (window resize,
            // aspect switch in the editor Game view, device rotation).
            if (Screen.safeArea != lastApplied || Screen.width != lastW || Screen.height != lastH)
                Apply();
        }

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            lastApplied = safe;
            lastW = Screen.width;
            lastH = Screen.height;

            Vector2 anchorMin = safe.position;
            Vector2 anchorMax = safe.position + safe.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // Guard against bogus safe-area reports (seen in the editor Game view:
            // stale rects during a resize, or points vs. pixels mismatches). A real
            // device notch never eats more than ~15% of an edge, so anything wilder
            // means "bad data — use the full view".
            bool bogus =
                anchorMin.x < 0f || anchorMin.y < 0f ||
                anchorMax.x > 1.0001f || anchorMax.y > 1.0001f ||
                anchorMax.x - anchorMin.x < 0.7f ||
                anchorMax.y - anchorMin.y < 0.7f;
#if UNITY_EDITOR
            // The editor Game view has no notch; always use the full view so desktop
            // playtesting matches the on-device layout.
            bogus = true;
#endif
            if (bogus)
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
            }

            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
