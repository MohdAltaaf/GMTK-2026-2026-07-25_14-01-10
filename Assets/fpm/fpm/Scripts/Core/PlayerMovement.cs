using UnityEngine;
using FastFPSMovement.InputHandling;
using FastFPSMovement.Movement;
using FastFPSMovement.CameraSystems;

namespace FastFPSMovement.Core
{
    /// <summary>
    /// Top level orchestrator for the movement framework. Owns the CharacterController
    /// and drives every subsystem in a fixed, explicit order each frame:
    /// input -> ground check -> state transitions (dash/slide/wallkick overrides) ->
    /// base ground/air movement -> vertical (jump/gravity) -> momentum bookkeeping ->
    /// CharacterController.Move -> camera feel.
    ///
    /// Every subsystem is self-contained and only exposes small Tick-style methods,
    /// so this class contains no gameplay math itself beyond blending their outputs.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("General Settings")]
        [SerializeField] private MovementSettings settings = new MovementSettings();

        [Header("Subsystem References")]
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private JumpSystem jumpSystem;
        [SerializeField] private DashSystem dashSystem;
        [SerializeField] private SlideSystem slideSystem;
        [SerializeField] private WallKickSystem wallKickSystem;
        [SerializeField] private FPSCameraController cameraController;

        [Header("Camera Relative Transform")]
        [Tooltip("Transform used to determine forward/right for camera-relative movement input. Usually the camera or its parent.")]
        [SerializeField] private Transform cameraTransform;

        private CharacterController _controller;
        private MomentumSystem _momentumSystem;
        private readonly PlayerState _state = new PlayerState();

        public PlayerState State => _state;
        public MomentumSystem Momentum => _momentumSystem;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _momentumSystem = new MomentumSystem(settings.moveSpeed, settings.momentumDecayRate);

            if (slideSystem != null) slideSystem.Initialize(_controller);
            if (wallKickSystem != null) wallKickSystem.Initialize(transform);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (inputHandler == null)
            {
                return;
            }

            inputHandler.Tick();

            UpdateGroundState();
            ResolveCameraRelativeInput();

            // Priority order: dash overrides slide, slide overrides wall kick attempt,
            // and any active override takes full ownership of horizontal velocity this frame.
            bool overrideActive = HandleDash(dt);
            if (!overrideActive) overrideActive = HandleSlide(dt);
            if (!overrideActive) HandleWallKick();

            if (!overrideActive)
            {
                HandleBaseMovement(dt);
            }

            slideSystem?.UpdateControllerHeight(dt);

            HandleVertical(dt);

            _momentumSystem.DecayExcessMomentum(dt);

            Vector3 finalVelocity = _momentumSystem.GetMomentum();
            finalVelocity.y = _state.VerticalVelocity;
            _controller.Move(finalVelocity * dt);

            UpdateHighLevelState(overrideActive);

            if (cameraController != null)
            {
                cameraController.TickLook(dt);
                bool isSliding = slideSystem != null && slideSystem.IsSliding;
                cameraController.TickFeel(_momentumSystem.GetCurrentSpeed(), _state.RawInputAxis.x, isSliding, dt);
            }
        }

        private void UpdateGroundState()
        {
            _state.WasGroundedLastFrame = _state.IsGrounded;

            // CharacterController.isGrounded can be unreliable on the exact same frame as a Move call,
            // so we back it up with a downward SphereCast. Critically, this cast is filtered by the
            // hit surface's normal (must point mostly upward) so a wall right next to the player is
            // never misread as "ground" - a plain CheckSphere would overlap nearby walls and falsely
            // report grounded, which used to reset the wall-kick chain every frame near a wall.
            bool controllerGrounded = _controller.isGrounded;
            bool probeGrounded = false;

            float probeRadius = _controller.radius * 0.9f;
            Vector3 probeOrigin = transform.position + Vector3.up * (probeRadius + 0.05f);
            float probeDistance = settings.groundCheckDistance + 0.1f;

            if (Physics.SphereCast(probeOrigin, probeRadius, Vector3.down, out RaycastHit groundHit,
                    probeDistance, settings.groundLayers, QueryTriggerInteraction.Ignore))
            {
                probeGrounded = groundHit.normal.y > 0.5f;
            }

            _state.IsGrounded = controllerGrounded || probeGrounded;

            _state.JustLanded = _state.IsGrounded && !_state.WasGroundedLastFrame;

            if (_state.IsGrounded)
            {
                wallKickSystem?.NotifyGrounded();
            }
        }

        private void ResolveCameraRelativeInput()
        {
            Vector2 raw = inputHandler.MoveInput;
            _state.RawInputAxis = raw;

            Transform relativeTo = cameraTransform != null ? cameraTransform : transform;

            Vector3 forward = Vector3.ProjectOnPlane(relativeTo.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(relativeTo.right, Vector3.up).normalized;

            _state.WorldMoveDirection = (forward * raw.y + right * raw.x);
            if (_state.WorldMoveDirection.sqrMagnitude > 1f)
            {
                _state.WorldMoveDirection.Normalize();
            }
        }

        private void HandleBaseMovement(float dt)
        {
            Vector3 currentHorizontal = _momentumSystem.GetMomentum();

            bool isSprinting = inputHandler.SprintHeld && _state.IsGrounded &&
                                (!settings.sprintRequiresForward || _state.RawInputAxis.y > 0.1f);
            float speedMultiplier = isSprinting ? settings.sprintMultiplier : 1f;

            Vector3 targetVelocity = _state.WorldMoveDirection * (settings.moveSpeed * speedMultiplier);

            float accel;
            if (_state.IsGrounded)
            {
                accel = _state.WorldMoveDirection.sqrMagnitude > 0.01f ? settings.acceleration : settings.deceleration;
            }
            else
            {
                accel = settings.airAcceleration * Mathf.Lerp(0.1f, 1f, settings.airControl);
                targetVelocity = _state.WorldMoveDirection * settings.airMoveSpeed;
            }

            Vector3 newHorizontal = Vector3.MoveTowards(currentHorizontal, targetVelocity, accel * dt);
            _momentumSystem.SetMomentum(newHorizontal);
        }

        private bool HandleDash(float dt)
        {
            if (dashSystem == null)
            {
                return false;
            }

            if (dashSystem.IsDashing)
            {
                Vector3 dashVelocity = dashSystem.TickDash(dt);
                _momentumSystem.SetMomentum(dashVelocity);

                if (!dashSystem.IsDashing)
                {
                    // Dash just ended this frame - hand off preserved speed as momentum.
                    Vector3 handoff = dashSystem.GetDashDirection() * dashSystem.GetMomentumHandoffSpeed();
                    _momentumSystem.SetMomentum(handoff);
                }
                return true;
            }

            if (inputHandler.DashPressedThisFrame)
            {
                Vector3 dashDir = _state.WorldMoveDirection.sqrMagnitude > 0.01f
                    ? _state.WorldMoveDirection
                    : Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

                if (dashSystem.TryStartDash(dashDir))
                {
                    cameraController?.NotifyDashed();
                    return true;
                }
            }

            return false;
        }

        private bool HandleSlide(float dt)
        {
            if (slideSystem == null)
            {
                return false;
            }

            if (slideSystem.IsSliding)
            {
                Vector3 slideVelocity = slideSystem.TickSlide(dt, inputHandler.SlideHeld);
                _momentumSystem.SetMomentum(slideVelocity);

                if (!slideSystem.IsSliding)
                {
                    Vector3 handoff = slideSystem.GetSlideDirection() * slideSystem.GetExitSpeed();
                    _momentumSystem.SetMomentum(handoff);
                }
                return true;
            }

            if (inputHandler.SlidePressedThisFrame && _state.IsGrounded)
            {
                if (slideSystem.TryStartSlide(_momentumSystem.GetMomentum()))
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleWallKick()
        {
            if (wallKickSystem == null || !wallKickSystem.WallKickEnabled)
            {
                return;
            }

            if (_state.IsGrounded || !inputHandler.JumpPressedThisFrame)
            {
                return;
            }

            if (wallKickSystem.DetectWall(out Vector3 wallNormal))
            {
                // Prefer current movement direction (what the player is actually pressing);
                // fall back to look direction if they're not holding any move input.
                Vector3 playerDirection = _state.WorldMoveDirection.sqrMagnitude > 0.01f
                    ? _state.WorldMoveDirection
                    : Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

                if (wallKickSystem.TryWallKick(wallNormal, playerDirection, out Vector3 resultVelocity))
                {
                    Vector3 horizontal = new Vector3(resultVelocity.x, 0f, resultVelocity.z);
                    _momentumSystem.SetMomentum(horizontal);
                    jumpSystem?.SetVerticalVelocity(resultVelocity.y);
                    cameraController?.NotifyJumped();
                }
            }
        }

        private void HandleVertical(float dt)
        {
            if (jumpSystem == null)
            {
                return;
            }

            bool jumpExecuted = jumpSystem.TickVertical(
                _state.IsGrounded,
                inputHandler.JumpPressedThisFrame,
                inputHandler.JumpHeld,
                dt);

            _state.VerticalVelocity = jumpSystem.VerticalVelocity;
            _state.JustJumped = jumpExecuted;

            if (jumpExecuted)
            {
                // Jumping cancels an active slide so the player doesn't slide mid-air.
                slideSystem?.ForceCancelSlide();
                cameraController?.NotifyJumped();
            }

            if (_state.JustLanded)
            {
                float fallSpeed01 = Mathf.Clamp01(Mathf.Abs(_state.VerticalVelocity) / 20f);
                cameraController?.NotifyLanded(fallSpeed01);
            }
        }

        private void UpdateHighLevelState(bool overrideActive)
        {
            _state.IsStateLocked = overrideActive;

            if (dashSystem != null && dashSystem.IsDashing)
            {
                _state.CurrentState = PlayerMovementState.Dashing;
            }
            else if (slideSystem != null && slideSystem.IsSliding)
            {
                _state.CurrentState = PlayerMovementState.Sliding;
            }
            else if (_state.IsGrounded)
            {
                _state.CurrentState = PlayerMovementState.Grounded;
            }
            else
            {
                _state.CurrentState = PlayerMovementState.Airborne;
            }
        }
    }
}
