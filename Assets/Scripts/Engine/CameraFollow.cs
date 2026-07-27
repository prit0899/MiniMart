using UnityEngine;

namespace MiniMart.Engine
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 15f, -10f); // Adjust for isometric
        // Batch 36: 10 → 14. With the faster player speeds the old damping let
        // the camera trail visibly, which read as movement lag.
        public float smoothSpeed = 14f;

        // Playtest B1: without a clamp, walking to a map edge left the store in a
        // corner with ~60% empty grass on screen. Clamp the FOCUS point (what the
        // camera centres on) to the store's bounds so the framed area never drifts
        // off the building. A small dead-zone avoids micro-jitter at the edges.
        public bool useBounds;
        public Vector2 minBounds;   // world (x, z) minimum for the focus point
        public Vector2 maxBounds;   // world (x, z) maximum for the focus point

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 focus = target.position;
            if (useBounds)
            {
                focus.x = Mathf.Clamp(focus.x, minBounds.x, maxBounds.x);
                focus.z = Mathf.Clamp(focus.z, minBounds.y, maxBounds.y);
            }

            Vector3 desiredPosition = focus + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }
    }
}
