using UnityEngine;

namespace FastFPSMovement.Core
{
    /// <summary>
    /// Serializable settings block for base ground/air movement.
    /// Kept as a plain serializable class (not a ScriptableObject) so every value
    /// is visible and editable directly on the PlayerMovement inspector, per spec.
    /// No gameplay values are hardcoded anywhere in the movement code.
    /// </summary>
    [System.Serializable]
    public class MovementSettings
    {
        [Header("Ground Movement")]
        [Tooltip("Max horizontal ground speed in units/second.")]
        public float moveSpeed = 7f;

        [Tooltip("How fast the player accelerates toward target ground velocity.")]
        public float acceleration = 60f;

        [Tooltip("How fast the player decelerates when no input is given on the ground.")]
        public float deceleration = 50f;

        [Header("Air Movement")]
        [Tooltip("Max speed the player can add to horizontal velocity while airborne.")]
        public float airMoveSpeed = 7f;

        [Tooltip("Acceleration applied to horizontal velocity while airborne.")]
        public float airAcceleration = 20f;

        [Tooltip("0 = no air control, 1 = full ground-like control while airborne.")]
        [Range(0f, 1f)]
        public float airControl = 0.35f;

        [Header("Sprint")]
        [Tooltip("Speed multiplier applied to moveSpeed while the sprint key is held.")]
        public float sprintMultiplier = 1.5f;

        [Tooltip("If true, sprint only kicks in while pressing forward. If false, it boosts speed in any direction.")]
        public bool sprintRequiresForward = true;

        [Header("Momentum")]
        [Tooltip("Natural decay rate applied to excess momentum (speed above moveSpeed) per second.")]
        public float momentumDecayRate = 4f;

        [Header("Ground Detection")]
        [Tooltip("Extra probe distance below the controller used to confirm grounded state.")]
        public float groundCheckDistance = 0.2f;

        [Tooltip("Layers considered ground for detection raycasts/spherecasts.")]
        public LayerMask groundLayers = ~0;
    }
}
