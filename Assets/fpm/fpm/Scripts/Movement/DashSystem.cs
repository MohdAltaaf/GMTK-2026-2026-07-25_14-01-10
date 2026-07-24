using UnityEngine;

namespace FastFPSMovement.Movement
{
    /// <summary>
    /// Directional dash with charge-based resource management (like a short cooldown "battery"
    /// rather than a single cooldown timer), a curve-driven speed profile, and momentum hand-off
    /// back to MomentumSystem when the dash ends so speed is preserved instead of reset.
    /// </summary>
    public class DashSystem : MonoBehaviour
    {
        [Header("Dash Force")]
        [Tooltip("Peak speed reached during the dash, in units/second.")]
        [SerializeField] private float dashForce = 18f;

        [Tooltip("Duration of a single dash in seconds.")]
        [SerializeField] private float dashDuration = 0.2f;

        [Tooltip("Evaluated 0->1 over dashDuration, scales dashForce. Lets you shape acceleration/deceleration of the dash.")]
        [SerializeField] private AnimationCurve dashCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.4f);

        [Header("Charges")]
        [Tooltip("Number of dashes that can be stored/used before needing to recharge.")]
        [SerializeField] private int dashCharges = 2;

        [Tooltip("Seconds required to regenerate a single dash charge.")]
        [SerializeField] private float dashCooldown = 1.5f;

        [Header("Momentum Hand-off")]
        [Tooltip("Fraction of the dash's final speed kept as momentum once the dash ends (0 = none, 1 = full).")]
        [Range(0f, 1f)]
        [SerializeField] private float momentumPreservation = 0.6f;

        private int _currentCharges;
        private float _rechargeTimer;
        private float _dashTimer;
        private Vector3 _dashDirection;
        private bool _isDashing;

        public bool IsDashing => _isDashing;
        public int CurrentCharges => _currentCharges;
        public int MaxCharges => dashCharges;

        private void Awake()
        {
            _currentCharges = dashCharges;
        }

        private void Update()
        {
            // Recharge one dash charge at a time.
            if (_currentCharges < dashCharges)
            {
                _rechargeTimer += Time.deltaTime;
                if (_rechargeTimer >= dashCooldown)
                {
                    _rechargeTimer = 0f;
                    _currentCharges++;
                }
            }
        }

        /// <summary>
        /// Attempts to start a dash in the given world-space direction (forward/back/left/right or blend).
        /// Returns true if the dash was started.
        /// </summary>
        public bool TryStartDash(Vector3 worldDirection)
        {
            if (_isDashing || _currentCharges <= 0)
            {
                return false;
            }

            if (worldDirection.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            _dashDirection = worldDirection.normalized;
            _dashTimer = 0f;
            _isDashing = true;
            _currentCharges--;
            return true;
        }

        /// <summary>
        /// Advances the dash timer and returns the horizontal velocity to apply this frame while dashing.
        /// Call only while IsDashing is true.
        /// </summary>
        public Vector3 TickDash(float deltaTime)
        {
            _dashTimer += deltaTime;
            float t = Mathf.Clamp01(_dashTimer / dashDuration);
            float curveValue = dashCurve.Evaluate(t);
            Vector3 velocity = _dashDirection * (dashForce * curveValue);

            if (_dashTimer >= dashDuration)
            {
                _isDashing = false;
            }

            return velocity;
        }

        /// <summary>Speed (units/sec) that should be preserved as momentum once the dash finishes.</summary>
        public float GetMomentumHandoffSpeed()
        {
            return dashForce * momentumPreservation;
        }

        public Vector3 GetDashDirection()
        {
            return _dashDirection;
        }

        /// <summary>Forcefully cancels an in-progress dash (e.g. on wall collision).</summary>
        public void CancelDash()
        {
            _isDashing = false;
        }
    }
}
