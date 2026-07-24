using UnityEngine;

namespace FastFPSMovement.Movement
{
    /// <summary>
    /// Speed-based slide: entering a slide keeps the player's current horizontal speed
    /// (rather than snapping to a fixed value), then bleeds it off with friction over
    /// slideDuration. Also handles smooth CharacterController height/center transitions
    /// so the player visually crouches during the slide.
    /// </summary>
    public class SlideSystem : MonoBehaviour
    {
        [Header("Slide Speed")]
        [Tooltip("Minimum speed the player must be moving at to start a slide.")]
        [SerializeField] private float minSpeedToSlide = 2f;

        [Tooltip("Extra burst added on top of current speed when the slide starts, for a satisfying kick.")]
        [SerializeField] private float slideSpeedBoost = 2f;

        [Header("Slide Duration & Friction")]
        [Tooltip("Maximum duration of a slide in seconds, unless ended early by friction or the key being released.")]
        [SerializeField] private float slideDuration = 0.85f;

        [Tooltip("Deceleration applied to slide speed per second (units/sec^2).")]
        [SerializeField] private float slideFriction = 6f;

        [Header("Character Controller Height")]
        [Tooltip("CharacterController height while sliding.")]
        [SerializeField] private float slideHeight = 0.9f;

        [Tooltip("CharacterController height while standing/normal.")]
        [SerializeField] private float standingHeight = 1.8f;

        [Tooltip("Seconds to smoothly transition CharacterController height in/out of the slide.")]
        [SerializeField] private float heightTransitionSpeed = 10f;

        private CharacterController _controller;
        private Vector3 _slideDirection;
        private float _slideSpeed;
        private float _slideTimer;
        private bool _isSliding;

        public bool IsSliding => _isSliding;

        public void Initialize(CharacterController controller)
        {
            _controller = controller;
        }

        /// <summary>Starts a slide using the player's current horizontal velocity as the base speed.</summary>
        public bool TryStartSlide(Vector3 currentHorizontalVelocity)
        {
            float currentSpeed = currentHorizontalVelocity.magnitude;
            if (_isSliding || currentSpeed < minSpeedToSlide)
            {
                return false;
            }

            _slideDirection = currentHorizontalVelocity.normalized;
            _slideSpeed = currentSpeed + slideSpeedBoost;
            _slideTimer = 0f;
            _isSliding = true;
            return true;
        }

        /// <summary>
        /// Advances the slide for one frame and returns the horizontal velocity to apply.
        /// Call only while IsSliding is true. Ends the slide automatically on timeout,
        /// low speed, or when slideKeyHeld becomes false.
        /// </summary>
        public Vector3 TickSlide(float deltaTime, bool slideKeyHeld)
        {
            _slideTimer += deltaTime;
            _slideSpeed = Mathf.Max(0f, _slideSpeed - slideFriction * deltaTime);

            Vector3 velocity = _slideDirection * _slideSpeed;

            bool shouldEnd = _slideTimer >= slideDuration || _slideSpeed <= 0.1f || !slideKeyHeld;
            if (shouldEnd)
            {
                _isSliding = false;
            }

            return velocity;
        }

        /// <summary>Speed remaining when the slide ends, to hand back to MomentumSystem.</summary>
        public float GetExitSpeed()
        {
            return _slideSpeed;
        }

        public Vector3 GetSlideDirection()
        {
            return _slideDirection;
        }

        /// <summary>
        /// Smoothly interpolates the CharacterController's height/center toward the slide or
        /// standing height. Call every frame regardless of slide state for a smooth crouch transition.
        /// </summary>
        public void UpdateControllerHeight(float deltaTime)
        {
            if (_controller == null)
            {
                return;
            }

            float targetHeight = _isSliding ? slideHeight : standingHeight;
            float newHeight = Mathf.Lerp(_controller.height, targetHeight, heightTransitionSpeed * deltaTime);
            _controller.height = newHeight;
            _controller.center = new Vector3(0f, newHeight * 0.5f, 0f);
        }

        public void ForceCancelSlide()
        {
            _isSliding = false;
        }
    }
}
