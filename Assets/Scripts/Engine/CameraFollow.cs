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

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }
    }
}
