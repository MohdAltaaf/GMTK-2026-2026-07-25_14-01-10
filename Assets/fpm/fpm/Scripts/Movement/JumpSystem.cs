using UnityEngine;

namespace FastFPSMovement.Movement
{
    /// <summary>
    /// Handles vertical movement: gravity integration, jumping, coyote time,
    /// jump buffering and variable jump height. PlayerMovement calls TickVertical
    /// once per frame and applies the returned vertical velocity to the CharacterController.
    /// </summary>
    public class JumpSystem : MonoBehaviour
    {
        [Header("Jump")]
        [Tooltip("Initial upward speed applied when a jump is executed.")]
        [SerializeField] private float jumpForce = 8f;

        [Header("Gravity")]
        [Tooltip("Downward acceleration applied every frame while airborne.")]
        [SerializeField] private float gravity = 18f;

        [Tooltip("Multiplier applied to gravity while falling (moving downward) for a snappier arc.")]
        [SerializeField] private float fallMultiplier = 1.35f;

        [Tooltip("Multiplier applied to gravity while ascending but the jump key has been released (variable height).")]
        [SerializeField] private float lowJumpMultiplier = 1.8f;

        [Header("Assists")]
        [Tooltip("Seconds after leaving the ground during which a jump is still allowed.")]
        [SerializeField] private float coyoteTime = 0.12f;

        [Tooltip("Seconds a jump input is remembered before landing, so pressing jump slightly early still works.")]
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Tooltip("Clamp applied to vertical velocity when landing, to avoid crushing large fall speeds instantly.")]
        [SerializeField] private float maxFallSpeed = 26f;

        private float _verticalVelocity;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _jumpHeldLastFrame;

        public float VerticalVelocity => _verticalVelocity;
        public float GravityValue => gravity;

        /// <summary>
        /// Advances the vertical velocity state machine for one frame.
        /// Must be called exactly once per frame from PlayerMovement.
        /// </summary>
        /// <param name="isGrounded">Current ground state from the CharacterController.</param>
        /// <param name="jumpPressedThisFrame">True if the jump key was pressed down this frame.</param>
        /// <param name="jumpHeld">True while the jump key is held (used for variable height).</param>
        /// <param name="deltaTime">Frame delta time.</param>
        /// <returns>True if a jump was actually executed this frame.</returns>
        public bool TickVertical(bool isGrounded, bool jumpPressedThisFrame, bool jumpHeld, float deltaTime)
        {
            bool jumpExecuted = false;

            // Coyote time bookkeeping.
            if (isGrounded)
            {
                _coyoteTimer = coyoteTime;
            }
            else
            {
                _coyoteTimer -= deltaTime;
            }

            // Jump buffer bookkeeping.
            if (jumpPressedThisFrame)
            {
                _jumpBufferTimer = jumpBufferTime;
            }
            else
            {
                _jumpBufferTimer -= deltaTime;
            }

            // Stick to ground with a small negative value instead of snapping to zero,
            // which keeps CharacterController grounded checks stable.
            if (isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            // Execute jump if buffered input exists and coyote window is open.
            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
            {
                _verticalVelocity = jumpForce;
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                jumpExecuted = true;
            }
            else
            {
                // Gravity integration with fall/low-jump multipliers for a punchier feel.
                float appliedGravity = gravity;

                if (_verticalVelocity < 0f)
                {
                    appliedGravity *= fallMultiplier;
                }
                else if (_verticalVelocity > 0f && !jumpHeld)
                {
                    appliedGravity *= lowJumpMultiplier;
                }

                _verticalVelocity -= appliedGravity * deltaTime;
                _verticalVelocity = Mathf.Max(_verticalVelocity, -maxFallSpeed);
            }

            _jumpHeldLastFrame = jumpHeld;
            return jumpExecuted;
        }

        /// <summary>Directly overrides vertical velocity. Used by WallKickSystem to inject an upward kick.</summary>
        public void SetVerticalVelocity(float value)
        {
            _verticalVelocity = value;
        }

        /// <summary>Resets vertical velocity to a small grounded value, used when entering slide/dash overrides.</summary>
        public void ResetVerticalVelocityToGround()
        {
            _verticalVelocity = -2f;
        }
    }
}
