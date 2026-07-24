using UnityEngine;

namespace FastFPSMovement.InputHandling
{
    /// <summary>
    /// Centralizes all raw keyboard/mouse input reading for the movement framework.
    /// No other script reads Input directly - everything goes through this handler,
    /// so input remapping only ever needs to happen in one place.
    /// Uses the legacy Input Manager (UnityEngine.Input) for immediate drop-in compatibility.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Movement Keys")]
        [SerializeField] private KeyCode moveForwardKey = KeyCode.W;
        [SerializeField] private KeyCode moveBackwardKey = KeyCode.S;
        [SerializeField] private KeyCode moveLeftKey = KeyCode.A;
        [SerializeField] private KeyCode moveRightKey = KeyCode.D;

        [Header("Action Keys")]
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode slideKey = KeyCode.LeftControl;
        [SerializeField] private KeyCode dashKey = KeyCode.Q;

        [Header("Mouse Axis Names")]
        [SerializeField] private string mouseXAxis = "Mouse X";
        [SerializeField] private string mouseYAxis = "Mouse Y";

        /// <summary>Raw movement axis, x = strafe (-1..1), y = forward (-1..1). Not normalized.</summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>True on the exact frame the jump key is pressed down.</summary>
        public bool JumpPressedThisFrame { get; private set; }

        /// <summary>True every frame the jump key is held.</summary>
        public bool JumpHeld { get; private set; }

        /// <summary>True on the exact frame the dash key is pressed down.</summary>
        public bool DashPressedThisFrame { get; private set; }

        /// <summary>True on the exact frame the slide key is pressed down.</summary>
        public bool SlidePressedThisFrame { get; private set; }

        /// <summary>True every frame the slide key is held.</summary>
        public bool SlideHeld { get; private set; }

        /// <summary>True every frame the sprint key is held.</summary>
        public bool SprintHeld { get; private set; }

        public float MouseX { get; private set; }
        public float MouseY { get; private set; }

        /// <summary>
        /// Reads all raw input for the current frame. Called explicitly by PlayerMovement
        /// at the very start of its Update, rather than relying on Unity's Update ordering,
        /// so every downstream system always sees this frame's fresh input.
        /// </summary>
        public void Tick()
        {
            float x = 0f;
            float y = 0f;

            if (Input.GetKey(moveForwardKey)) y += 1f;
            if (Input.GetKey(moveBackwardKey)) y -= 1f;
            if (Input.GetKey(moveRightKey)) x += 1f;
            if (Input.GetKey(moveLeftKey)) x -= 1f;

            MoveInput = new Vector2(x, y);

            JumpPressedThisFrame = Input.GetKeyDown(jumpKey);
            JumpHeld = Input.GetKey(jumpKey);

            DashPressedThisFrame = Input.GetKeyDown(dashKey);

            SlidePressedThisFrame = Input.GetKeyDown(slideKey);
            SlideHeld = Input.GetKey(slideKey);

            SprintHeld = Input.GetKey(sprintKey);

            MouseX = Input.GetAxisRaw(mouseXAxis);
            MouseY = Input.GetAxisRaw(mouseYAxis);
        }

        /// <summary>Returns the raw move input direction used for dash direction resolution.</summary>
        public Vector2 GetLastMoveInput()
        {
            return MoveInput;
        }
    }
}
