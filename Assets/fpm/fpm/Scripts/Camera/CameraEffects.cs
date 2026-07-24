using UnityEngine;

namespace FastFPSMovement.CameraSystems
{
    /// <summary>
    /// Handles procedural camera effects: shake and directional "kick" impulses
    /// (jump, land, dash). Applies a local position/rotation offset on top of whatever
    /// FPSCameraController sets each frame, so the two systems never fight over the transform.
    /// </summary>
    public class CameraEffects : MonoBehaviour
    {
        [Header("Shake")]
        [Tooltip("Overall multiplier for procedural shake magnitude.")]
        [SerializeField] private float cameraShake = 1f;

        [Tooltip("How quickly an active shake decays back to zero.")]
        [SerializeField] private float shakeDecay = 4f;

        [Header("Kick")]
        [Tooltip("How quickly a positional kick offset (jump/land/dash) returns to zero.")]
        [SerializeField] private float kickRecoverySpeed = 8f;

        [Tooltip("Vertical camera dip applied on landing, scaled by fall speed.")]
        [SerializeField] private float landDipAmount = 0.12f;

        [Tooltip("Vertical camera rise applied on jumping.")]
        [SerializeField] private float jumpRiseAmount = 0.06f;

        [Tooltip("Forward push amount applied on dash start.")]
        [SerializeField] private float dashPushAmount = 0.15f;

        private float _shakeMagnitude;
        private Vector3 _kickOffset;
        private Vector3 _currentOffset;

        /// <summary>Current frame's procedural local position offset. FPSCameraController adds this after its own logic.</summary>
        public Vector3 CurrentOffset => _currentOffset;

        private void Update()
        {
            float dt = Time.deltaTime;

            // Decay shake magnitude and apply random jitter scaled by it.
            _shakeMagnitude = Mathf.Max(0f, _shakeMagnitude - shakeDecay * dt);
            Vector3 shakeOffset = _shakeMagnitude > 0f
                ? new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * (_shakeMagnitude * cameraShake * 0.05f)
                : Vector3.zero;

            // Smoothly recover the directional kick offset toward zero.
            _kickOffset = Vector3.Lerp(_kickOffset, Vector3.zero, kickRecoverySpeed * dt);

            _currentOffset = shakeOffset + _kickOffset;
        }

        /// <summary>Triggers a generic shake impulse, e.g. on landing hard or taking damage.</summary>
        public void TriggerShake(float magnitude)
        {
            _shakeMagnitude = Mathf.Max(_shakeMagnitude, magnitude);
        }

        /// <summary>Small upward camera pop when leaving the ground under player control.</summary>
        public void TriggerJumpKick()
        {
            _kickOffset += Vector3.up * jumpRiseAmount;
        }

        /// <summary>Downward dip on landing, scaled by how hard the impact was.</summary>
        public void TriggerLandKick(float fallSpeed01)
        {
            _kickOffset -= Vector3.up * (landDipAmount * Mathf.Clamp01(fallSpeed01));
            TriggerShake(fallSpeed01 * 2f);
        }

        /// <summary>Forward push effect used when a dash begins.</summary>
        public void TriggerDashEffect()
        {
            _kickOffset += Vector3.forward * dashPushAmount;
            TriggerShake(0.5f);
        }
    }
}
