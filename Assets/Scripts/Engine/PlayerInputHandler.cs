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

                // Analog: partial stick deflection = partial speed, like the reference
                // game — but shaped with a smoothstep so the character commits to full
                // speed by ~70% deflection instead of feeling mushy at mid-stick.
                Vector3 dir = (camForward * v + camRight * h).normalized;
                float raw01 = Mathf.Clamp01(smoothedInput.magnitude);
                float throttle = raw01 * raw01 * (3f - 2f * raw01); // smoothstep
                // Dash skill: short speed burst on the DASH button (see TriggerDash).
                float dash = Time.time < dashUntil ? 1.6f : 1f;
                Vector3 next = player.transform.position + dir * player.CurrentSpeed * throttle * dash * Time.deltaTime;

                // Keep player inside the world. These bounds MUST match the
                // pathfinder grid built in SceneBootstrapper (origin (-50,0,0),
                // 100×82 cells) — a stale clamp from the original tiny map kept
                // the player boxed into x[1,29] z[1,21], which read as "can't
                // walk around the mall". Walls/fences still block via the grid
                // check below; this is only the outer map edge.
                next.x = Mathf.Clamp(next.x, -49f, 49f);
                next.z = Mathf.Clamp(next.z, 1f, 81f);

                // Walls/fences block direct movement too (NPCs already respect the grid
                // via A*). Try the full step, then each axis alone so the player slides
                // along walls instead of stopping dead.
                var pf = MiniMart.Map.GridPathfinder.Instance;
                if (pf != null && !pf.IsWalkableWorld(next))
                {
                    Vector3 cur = player.transform.position;
                    var slideX = new Vector3(next.x, next.y, cur.z);
                    var slideZ = new Vector3(cur.x, next.y, next.z);
                    if (pf.IsWalkableWorld(slideX)) next = slideX;
                    else if (pf.IsWalkableWorld(slideZ)) next = slideZ;
                    else next = cur;
                }

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
        // ── Dash skill (batch 36) ────────────────────────────────────────────
        // Short 1.6x speed burst on demand; 2s duration, 5s cooldown. Gives the
        // movement loop an active verb without complicating steering.
        private float dashUntil = -999f;
        private float dashReadyAt;
        public bool DashReady => Time.time >= dashReadyAt;

        public void TriggerDash()
        {
            if (!DashReady) return;
            dashUntil = Time.time + 2f;
            dashReadyAt = Time.time + 5f;
            AudioFx.Sale(); // light whoosh-ish feedback from the existing bus
        }

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
    /// Greys the DASH button out while the dash skill is on cooldown so its
    /// state is always readable at a glance (batch 36).
    /// </summary>
    public class DashCooldownTint : MonoBehaviour
    {
        public PlayerInputHandler Input;
        public UnityEngine.UI.Image Target;
        private Color readyColor;
        private bool cached;

        private void Update()
        {
            if (Input == null || Target == null) return;
            if (!cached) { readyColor = Target.color; cached = true; }
            var dim = readyColor; dim.a = 0.35f;
            Target.color = Input.DashReady ? readyColor : dim;
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
        }

        private void Start()
        {
            // Hide initially until touched (floating behavior). This must run in Start,
            // not Awake: the HUD builder assigns Background/Handle AFTER AddComponent
            // (which is when Awake fires), so hiding in Awake targeted the wrong rect
            // and the joystick sat visible in the middle of the screen at boot.
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            // Toggle EVERY image under the background — the direction arrows and
            // the blue hand cursor are child images, and skipping them left a
            // ghost "blue blob + 4 dots" floating at screen centre while the
            // ring/knob were hidden (visible in every playtest capture).
            if (Background != null)
                foreach (var img in Background.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    img.enabled = visible;
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            // Show joystick and move it to touch position
            if (Background != null)
            {
                SetVisible(true);

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
            SetVisible(false);
        }
    }
}
