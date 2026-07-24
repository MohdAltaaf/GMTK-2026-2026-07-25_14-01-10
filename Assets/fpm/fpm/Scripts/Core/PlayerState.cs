using UnityEngine;

namespace FastFPSMovement.Core
{
    /// <summary>
    /// High level movement state used for animation, debug, camera effects,
    /// and to let subsystems know what the player is currently doing.
    /// </summary>
    public enum PlayerMovementState
    {
        Grounded,
        Airborne,
        Sliding,
        Dashing,
        WallKicking
    }

    /// <summary>
    /// Plain (non-MonoBehaviour) shared runtime data object.
    /// PlayerMovement owns an instance of this and passes it to every subsystem
    /// so they can read/write shared information without tight coupling to each other.
    /// This keeps every movement system modular and independently testable.
    /// </summary>
    public class PlayerState
    {
        /// <summary>Current high level state, mainly for debug/animation/camera hooks.</summary>
        public PlayerMovementState CurrentState = PlayerMovementState.Airborne;

        /// <summary>True if the CharacterController was grounded on the last check this frame.</summary>
        public bool IsGrounded;

        /// <summary>Was grounded on the previous frame. Used to detect landing events.</summary>
        public bool WasGroundedLastFrame;

        /// <summary>Current combined horizontal velocity (world space, Y ignored).</summary>
        public Vector3 HorizontalVelocity;

        /// <summary>Current vertical velocity (gravity/jump), stored separately since gravity integrates differently.</summary>
        public float VerticalVelocity;

        /// <summary>Normalized move input direction in world space (camera relative), set every frame by PlayerMovement.</summary>
        public Vector3 WorldMoveDirection;

        /// <summary>Raw 2D input axis (x = strafe, y = forward) before camera relative transform.</summary>
        public Vector2 RawInputAxis;

        /// <summary>Convenience flag - true while any override system (dash/slide/wallkick) owns velocity this frame.</summary>
        public bool IsStateLocked;

        /// <summary>Fired (checked) by camera/debug systems - true for exactly one frame on landing.</summary>
        public bool JustLanded;

        /// <summary>Fired (checked) by camera/debug systems - true for exactly one frame on leaving the ground via jump.</summary>
        public bool JustJumped;

        public float CurrentSpeed => HorizontalVelocity.magnitude;
    }
}
