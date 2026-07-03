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

            var anim = GetComponentInChildren<Animator>();
            if (anim != null) anim.applyRootMotion = false;
        }

        private void Update()
        {
            if (player == null || player.IsPaused) return;

            if (mainCam == null)
                mainCam = Camera.main ?? Object.FindAnyObjectByType<Camera>();

            HandleKeyboardMovement();
            HandleTapToMove();
            UpdateNetButtonVisibility();
        }

        // GDD 11: the NET button only appears while a thief is actually loose in the store.
        private float netCheckTimer;
        private void UpdateNetButtonVisibility()
        {
            if (ThrowNetButton == null) return;
            netCheckTimer += Time.deltaTime;
            if (netCheckTimer < 0.5f) return;
            netCheckTimer = 0f;

            bool thiefActive = false;
            var thieves = Object.FindObjectsByType<AI.Thief>(FindObjectsInactive.Exclude);
            foreach (var t in thieves)
                if (!t.HasLeftStore && !t.IsCaught) { thiefActive = true; break; }

            if (ThrowNetButton.gameObject.activeSelf != thiefActive)
                ThrowNetButton.gameObject.SetActive(thiefActive);
        }

        // Smoothed analog input: raw joystick values jump frame-to-frame with the finger,
        // which reads as jittery movement. SmoothDamp gives the reference game's glide.
        private Vector2 smoothedInput;
        private Vector2 inputVelocity;
        private const float InputSmoothTime = 0.08f;

        private void HandleKeyboardMovement()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // Add joystick input on top of keyboard.
            if (MobileJoystick != null)
            {
                float jh = MobileJoystick.Horizontal;
                float jv = MobileJoystick.Vertical;
                if (Mathf.Abs(jh) > 0.01f || Mathf.Abs(jv) > 0.01f)
                {
                    h = jh;
                    v = jv;
                }
            }

            var raw = new Vector2(h, v);
            if (raw.sqrMagnitude > 1f) raw.Normalize();
            smoothedInput = Vector2.SmoothDamp(smoothedInput, raw, ref inputVelocity, InputSmoothTime);
            if (raw == Vector2.zero && smoothedInput.sqrMagnitude < 0.0009f)
                smoothedInput = Vector2.zero; // snap to rest so the player doesn't drift
            h = smoothedInput.x;
            v = smoothedInput.y;

            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                if (mainCam == null) return;

                // Direct input takes authority: cancel any tap-to-move path so
                // CharacterBase.Update doesn't drag the player toward a stale target.
                player.StopCurrentTask();

                // Calculate camera-relative forward and right vectors on the XZ plane
                Vector3 camForward = mainCam.transform.forward;
                Vector3 camRight = mainCam.transform.right;
                camForward.y = 0;
                camRight.y = 0;
                camForward.Normalize();
                camRight.Normalize();

                // Analog: partial stick deflection = partial speed, like the reference game.
                Vector3 dir = (camForward * v + camRight * h).normalized;
                float throttle = Mathf.Clamp01(smoothedInput.magnitude);
                Vector3 next = player.transform.position + dir * player.CurrentSpeed * throttle * Time.deltaTime;

                // Keep player clamped to boundaries
                next.x = Mathf.Clamp(next.x, 1f, 29f);
                next.z = Mathf.Clamp(next.z, 1f, 21f);

                // If the clamp cancelled the step (pushing into a map edge), don't play
                // the walk animation — that reads as "moving but stuck".
                bool actuallyMoved = (next - player.transform.position).sqrMagnitude > 0.000001f;

                var cc = player.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.Move(next - player.transform.position);
                }
                else
                {
                    var rb = player.GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic) rb.MovePosition(next);
                    else player.transform.position = next;
                }

                if (dir != Vector3.zero)
                    player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(dir), 14f * Time.deltaTime);

                player.State = actuallyMoved
                    ? MiniMart.Core.CharacterState.Walking
                    : MiniMart.Core.CharacterState.Idle;
            }
            else
            {
                // Only reset to Idle if tap-to-move isn't actively guiding us.
                if (player.State == MiniMart.Core.CharacterState.Walking && !player.HasMoveTarget)
                    player.State = MiniMart.Core.CharacterState.Idle;
            }
        }

        private void HandleTapToMove()
        {
            // While the joystick is steering, taps are joystick drags — never move-orders.
            // if (MobileJoystick != null &&
            //     (Mathf.Abs(MobileJoystick.Horizontal) > 0.01f || Mathf.Abs(MobileJoystick.Vertical) > 0.01f))
            //     return;

            var es = UnityEngine.EventSystems.EventSystem.current;
            bool tapped = false;
            Vector3 screenPos = Vector3.zero;

            if (Input.touchCount > 0)
            {
                // IsPointerOverGameObject() without a fingerId is unreliable for touches —
                // it must be asked per-finger, and only on the frame the touch began.
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began &&
                    (es == null || !es.IsPointerOverGameObject(touch.fingerId)))
                {
                    tapped = true;
                    screenPos = touch.position;
                }
            }
            else if (Input.GetMouseButtonDown(0) &&
                     (es == null || !es.IsPointerOverGameObject()))
            {
                tapped = true;
                screenPos = Input.mousePosition;
            }

            if (!tapped || mainCam == null) return;

            Ray ray = mainCam.ScreenPointToRay(screenPos);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float enter))
                player.SetTarget(ray.GetPoint(enter));
        }

        /// <summary>Called by the runtime HUD builder after this component's Awake has already run,
        /// to attach on-screen controls and register the net button click.</summary>
        public void BindOnScreenControls(Joystick joystick, UnityEngine.UI.Button netButton)
        {
            MobileJoystick = joystick;
            if (netButton != null)
            {
                ThrowNetButton = netButton;
                netButton.onClick.AddListener(OnThrowNetPressed);
            }
        }

        private void OnThrowNetPressed()
        {
            // Find nearest thief and attempt a catch.
            var thieves = Object.FindObjectsByType<AI.Thief>(FindObjectsInactive.Exclude);
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

    /// <summary>
    /// Proper uGUI floating joystick. Touching anywhere in the background moves the joystick
    /// to that touch position, allowing steering from anywhere on the screen.
    /// </summary>
    public class Joystick : MonoBehaviour,
        UnityEngine.EventSystems.IPointerDownHandler,
        UnityEngine.EventSystems.IDragHandler,
        UnityEngine.EventSystems.IPointerUpHandler
    {
        [Tooltip("The circular background RectTransform (the drag bounds).")]
        public RectTransform Background;
        [Tooltip("The movable knob RectTransform, child of Background.")]
        public RectTransform Handle;
        [Tooltip("Max knob travel as a fraction of the background radius.")]
        public float HandleRange = 1f;

        private Vector2 input = Vector2.zero;
        private Canvas canvas;
        private RectTransform rootRect;

        public virtual float Horizontal => input.x;
        public virtual float Vertical => input.y;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            if (Background == null) Background = transform as RectTransform;
            rootRect = canvas?.GetComponent<RectTransform>();
            
            // Hide initially until touched (Floating behavior)
            if (Background != null && Background.GetComponent<UnityEngine.UI.Image>() != null)
                Background.GetComponent<UnityEngine.UI.Image>().enabled = false;
            if (Handle != null && Handle.GetComponent<UnityEngine.UI.Image>() != null)
                Handle.GetComponent<UnityEngine.UI.Image>().enabled = false;
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            // Show joystick and move it to touch position
            if (Background != null)
            {
                if (Background.GetComponent<UnityEngine.UI.Image>() != null)
                    Background.GetComponent<UnityEngine.UI.Image>().enabled = true;
                if (Handle != null && Handle.GetComponent<UnityEngine.UI.Image>() != null)
                    Handle.GetComponent<UnityEngine.UI.Image>().enabled = true;

                Camera cam = null;
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    cam = canvas.worldCamera;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, eventData.position, cam, out Vector2 localPoint))
                {
                    Background.localPosition = localPoint;
                }
            }
            OnDrag(eventData);
        }

        public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (Background == null) return;

            Camera cam = null;
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
                cam = canvas.worldCamera;

            Vector2 radius = Background.sizeDelta * 0.5f;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Background, eventData.position, cam, out Vector2 local))
                return;

            // Normalize to -1..1 relative to the background radius.
            Vector2 normalized = new Vector2(
                radius.x != 0 ? local.x / radius.x : 0f,
                radius.y != 0 ? local.y / radius.y : 0f);

            input = normalized.magnitude > 1f ? normalized.normalized : normalized;

            if (Handle != null)
                Handle.anchoredPosition = input * radius * HandleRange;
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            input = Vector2.zero;
            if (Handle != null) Handle.anchoredPosition = Vector2.zero;
            
            // Hide when released
            if (Background != null && Background.GetComponent<UnityEngine.UI.Image>() != null)
                Background.GetComponent<UnityEngine.UI.Image>().enabled = false;
            if (Handle != null && Handle.GetComponent<UnityEngine.UI.Image>() != null)
                Handle.GetComponent<UnityEngine.UI.Image>().enabled = false;
        }
    }
}
