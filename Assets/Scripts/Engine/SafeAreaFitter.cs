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

        private void Awake()
        {
            rt = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != lastApplied) Apply();
        }

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            lastApplied = safe;

            Vector2 anchorMin = safe.position;
            Vector2 anchorMax = safe.position + safe.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
