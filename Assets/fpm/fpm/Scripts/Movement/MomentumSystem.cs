using UnityEngine;

namespace FastFPSMovement.Movement
{
    /// <summary>
    /// Owns the player's persistent horizontal momentum vector.
    /// This is intentionally a plain C# class (not a MonoBehaviour) - it is pure state
    /// and math, driven every frame by PlayerMovement, and is shared/read by
    /// Dash, Slide and Camera systems for speed-based effects (e.g. FOV).
    /// Momentum never resets instantly; it decays smoothly over time above baseline speed.
    /// </summary>
    public class MomentumSystem
    {
        private Vector3 _currentMomentum;
        private readonly float _baselineSpeed;
        private readonly float _decayRate;

        /// <param name="baselineSpeed">Normal move speed - momentum above this value decays over time.</param>
        /// <param name="decayRate">Units/second^2 that excess speed bleeds off by.</param>
        public MomentumSystem(float baselineSpeed, float decayRate)
        {
            _baselineSpeed = baselineSpeed;
            _decayRate = decayRate;
            _currentMomentum = Vector3.zero;
        }

        /// <summary>Adds an external impulse to the current momentum (e.g. from a dash or slide boost).</summary>
        public void AddMomentum(Vector3 amount)
        {
            _currentMomentum += amount;
        }

        /// <summary>Directly overwrites the current momentum vector (used when systems hand off horizontal velocity).</summary>
        public void SetMomentum(Vector3 value)
        {
            _currentMomentum = value;
        }

        /// <summary>Returns the current horizontal momentum vector.</summary>
        public Vector3 GetMomentum()
        {
            return _currentMomentum;
        }

        /// <summary>Returns the current horizontal speed (magnitude of momentum).</summary>
        public float GetCurrentSpeed()
        {
            return _currentMomentum.magnitude;
        }

        /// <summary>Immediately zeroes momentum. Used sparingly (e.g. respawn/teleport), never for normal gameplay stops.</summary>
        public void ResetMomentum()
        {
            _currentMomentum = Vector3.zero;
        }

        /// <summary>
        /// Applies natural decay to any speed above the baseline move speed.
        /// Speed below baseline is left untouched here; normal acceleration/deceleration
        /// in PlayerMovement handles regular ground/air speed control.
        /// </summary>
        public void DecayExcessMomentum(float deltaTime)
        {
            float speed = _currentMomentum.magnitude;
            if (speed <= _baselineSpeed || speed <= 0.0001f)
            {
                return;
            }

            float excess = speed - _baselineSpeed;
            float decayAmount = _decayRate * deltaTime;
            float newExcess = Mathf.Max(0f, excess - decayAmount);
            float newSpeed = _baselineSpeed + newExcess;

            _currentMomentum = _currentMomentum.normalized * newSpeed;
        }
    }
}
