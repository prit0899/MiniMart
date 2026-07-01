using UnityEngine;
using MiniMart.Characters;

namespace MiniMart.Engine
{
    /// <summary>
    /// Reads player input (keyboard WASD + Space on desktop; on-screen joystick + buttons on mobile)
    /// and drives the PlayerController. All game-logic decisions stay in PlayerController/GameManager.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Movement")]
        public float tapMoveThreshold = 0.2f; // max seconds between touch-down and touch-up to count as tap

        [Header("On-screen controls (mobile)")]
        public Joystick MobileJoystick;   // assign a FloatingJoystick or FixedJoystick prefab
        public UnityEngine.UI.Button ThrowNetButton;
        public UnityEngine.UI.Button PauseButton;

        private PlayerController player;
        private Camera mainCam;
        private float touchDownTime;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            mainCam = Camera.main;
            ThrowNetButton?.onClick.AddListener(OnThrowNetPressed);
            PauseButton?.onClick.AddListener(OnPausePressed);
        }

        private void Update()
        {
            if (player == null || player.IsPaused) return;
            HandleKeyboardMovement();
            HandleTapToMove();
        }

        private void HandleKeyboardMovement()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // Override with joystick if active.
            if (MobileJoystick != null)
            {
                h = MobileJoystick.Horizontal;
                v = MobileJoystick.Vertical;
            }

            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                // Calculate camera-relative forward and right vectors on the XZ plane
                Vector3 camForward = mainCam.transform.forward;
                camForward.y = 0;
                camForward.Normalize();
                
                Vector3 camRight = mainCam.transform.right;
                camRight.y = 0;
                camRight.Normalize();

                Vector3 dir = (camForward * v + camRight * h).normalized;
                Vector3 next = player.transform.position + dir * player.CurrentSpeed * Time.deltaTime;
                player.transform.position = next;
                
                // Rotate to face movement
                if (dir.sqrMagnitude > 0.001f)
                    player.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }

        private void HandleTapToMove()
        {
            // Mouse click or single touch: move toward tapped world position.
            if (Input.GetMouseButtonDown(0)) touchDownTime = Time.time;
            if (Input.GetMouseButtonUp(0) && (Time.time - touchDownTime) <= tapMoveThreshold)
            {
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                
                if (groundPlane.Raycast(ray, out float enter))
                {
                    Vector3 worldPos = ray.GetPoint(enter);
                    player.SetTarget(worldPos);
                }
            }
        }

        private void OnThrowNetPressed()
        {
            // Find nearest thief and attempt a catch.
            var thieves = FindObjectsOfType<AI.Thief>();
            AI.Thief nearest = null;
            float minDist = float.MaxValue;
            foreach (var t in thieves)
            {
                float d = Vector3.Distance(player.transform.position, t.transform.position);
                if (d < minDist) { minDist = d; nearest = t; }
            }
            if (nearest != null && minDist < 3f)
                player.TryCatchThief(nearest);
            else
                player.Net?.Throw(); // throw anyway for visual feedback
        }

        private void OnPausePressed()
        {
            if (GameManager.Instance == null) return;
            if (player.IsPaused) GameManager.Instance.Resume();
            else GameManager.Instance.Pause();
        }
    }

    /// <summary>Minimal joystick stub so the class compiles without a third-party asset.
    /// Replace with the actual Joystick class from your chosen mobile input package.</summary>
    public abstract class Joystick : MonoBehaviour
    {
        public abstract float Horizontal { get; }
        public abstract float Vertical { get; }
    }
}
