using UnityEngine;
using MiniMart.Characters;

namespace MiniMart.Engine
{
    /// <summary>
    /// Isometric follow camera (GDD Section 3: fixed 45 degrees, Cinemachine-style follow, slight
    /// zoom-out as the player's carry stack grows). Uses a fixed world-space offset and smooth
    /// damping in LateUpdate so it never fights the simulation tick.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Transform Target;
        public float FollowSmoothTime = 0.18f;

        [Header("Zoom (orthographic size)")]
        public float BaseOrthoSize = 9f;
        public float MaxOrthoSize = 12f;
        public int MaxCarryForZoom = 7;   // player max stack
        public float ZoomSmoothTime = 0.3f;

        private Camera cam;
        private Vector3 offset;
        private Vector3 followVelocity;
        private float zoomVelocity;
        private CharacterBase targetCarry;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
        }

        public void Initialize(Transform target)
        {
            Target = target;
            if (Target != null)
            {
                targetCarry = Target.GetComponent<CharacterBase>();
                // Preserve the authored isometric offset (camera was placed by the bootstrapper).
                offset = transform.position - Target.position;
            }
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = BaseOrthoSize;
            }
        }

        private void LateUpdate()
        {
            if (Target == null) return;

            Vector3 desired = Target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref followVelocity, FollowSmoothTime);

            if (cam != null && cam.orthographic)
            {
                int carry = targetCarry != null ? targetCarry.CarryCount : 0;
                float t = MaxCarryForZoom > 0 ? Mathf.Clamp01((float)carry / MaxCarryForZoom) : 0f;
                float desiredSize = Mathf.Lerp(BaseOrthoSize, MaxOrthoSize, t);
                cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, desiredSize, ref zoomVelocity, ZoomSmoothTime);
            }
        }
    }
}
