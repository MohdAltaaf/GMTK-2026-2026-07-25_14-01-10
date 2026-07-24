using UnityEngine;
using FastFPSMovement.InputHandling;

namespace FastFPSMovement.CameraSystems
{
    /// <summary>
    /// Professional FPS camera: smooth mouse look with adjustable sensitivity,
    /// speed-based FOV changes, strafe tilt, and hooks (via CameraEffects) for
    /// jump/land/dash kick impulses. Rotates the player body horizontally and
    /// the camera transform vertically, which is the standard FPS setup.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FPSCameraController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Player body transform to rotate horizontally (yaw). Usually the parent CharacterController object.")]
        [SerializeField] private Transform playerBody;

        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private CameraEffects cameraEffects;

        [Header("Look")]
        [Tooltip("Mouse look sensitivity multiplier.")]
        [SerializeField] private float mouseSensitivity = 2.5f;

        [Tooltip("Higher = snappier rotation, lower = smoother/laggier look.")]
        [SerializeField] private float lookSmoothing = 25f;

        [Tooltip("Clamp for vertical look angle (up/down), in degrees.")]
        [SerializeField] private float pitchClamp = 89f;

        [Header("Field of View")]
        [Tooltip("Base FOV while standing still.")]
        [SerializeField] private float normalFOV = 90f;

        [Tooltip("FOV reached at/above the speed threshold while moving fast.")]
        [SerializeField] private float speedFOV = 100f;

        [Tooltip("Horizontal speed at which speedFOV is fully reached.")]
        [SerializeField] private float speedForMaxFOV = 14f;

        [Tooltip("How quickly FOV interpolates toward its target.")]
        [SerializeField] private float fovSmooth = 8f;

        [Header("Strafe Tilt")]
        [Tooltip("Max camera roll angle applied while strafing, in degrees.")]
        [SerializeField] private float cameraTilt = 3.5f;

        [Tooltip("How quickly tilt interpolates toward its target.")]
        [SerializeField] private float tiltSmooth = 10f;

        [Header("Slide")]
        [Tooltip("How far the camera lowers (local units) while the player is sliding.")]
        [SerializeField] private float slideCameraDrop = 0.5f;

        [Tooltip("How quickly the camera moves in/out of the slide-lowered position.")]
        [SerializeField] private float slideCameraSmooth = 10f;

        private Camera _camera;
        private float _pitch;
        private float _yaw;
        private float _currentTilt;
        private float _currentSlideOffset;
        private Vector3 _baseLocalPosition;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            // Remember the camera's authored local position (eye height) so procedural
            // shake/kick effects can be added on TOP of it instead of replacing it.
            _baseLocalPosition = transform.localPosition;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _camera.fieldOfView = normalFOV;
        }

        /// <summary>
        /// Drives mouse look. Called every frame by PlayerMovement (or Update, if used standalone)
        /// so ordering relative to body movement is explicit and controllable.
        /// </summary>
        public void TickLook(float deltaTime)
        {
            if (inputHandler == null || playerBody == null)
            {
                return;
            }

            float mouseX = inputHandler.MouseX * mouseSensitivity;
            float mouseY = inputHandler.MouseY * mouseSensitivity;

            _yaw += mouseX;
            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, -pitchClamp, pitchClamp);

            Quaternion targetBodyRotation = Quaternion.Euler(0f, _yaw, 0f);
            playerBody.rotation = Quaternion.Slerp(playerBody.rotation, targetBodyRotation, lookSmoothing * deltaTime);

            Quaternion targetCamRotation = Quaternion.Euler(_pitch, _yaw, _currentTilt);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetCamRotation, lookSmoothing * deltaTime);
        }

        /// <summary>
        /// Updates FOV, strafe tilt, and slide crouch height based on current horizontal speed,
        /// strafe input, and slide state. Also applies the additive offset from CameraEffects (shake/kick).
        /// </summary>
        public void TickFeel(float currentHorizontalSpeed, float strafeInput, bool isSliding, float deltaTime)
        {
            float speedT = Mathf.Clamp01(currentHorizontalSpeed / Mathf.Max(0.01f, speedForMaxFOV));
            float targetFOV = Mathf.Lerp(normalFOV, speedFOV, speedT);
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFOV, fovSmooth * deltaTime);

            float targetTilt = -strafeInput * cameraTilt;
            _currentTilt = Mathf.Lerp(_currentTilt, targetTilt, tiltSmooth * deltaTime);

            float targetSlideOffset = isSliding ? -slideCameraDrop : 0f;
            _currentSlideOffset = Mathf.Lerp(_currentSlideOffset, targetSlideOffset, slideCameraSmooth * deltaTime);

            Vector3 kickOffset = cameraEffects != null ? cameraEffects.CurrentOffset : Vector3.zero;
            transform.localPosition = _baseLocalPosition + Vector3.up * _currentSlideOffset + kickOffset;
        }

        public void NotifyJumped()
        {
            if (cameraEffects != null) cameraEffects.TriggerJumpKick();
        }

        public void NotifyLanded(float fallSpeed01)
        {
            if (cameraEffects != null) cameraEffects.TriggerLandKick(fallSpeed01);
        }

        public void NotifyDashed()
        {
            if (cameraEffects != null) cameraEffects.TriggerDashEffect();
        }
    }
}
