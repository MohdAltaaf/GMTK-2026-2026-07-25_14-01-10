using UnityEngine;
using FastFPSMovement.Core;
using FastFPSMovement.Movement;

namespace FastFPSMovement.Utilities
{
    /// <summary>
    /// On-screen debug overlay for the movement framework. Displays current speed,
    /// movement state, ground status, and dash charges. Purely diagnostic - safe to
    /// disable or remove for a shipping build.
    /// </summary>
    public class MovementDebug : MonoBehaviour
    {
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private DashSystem dashSystem;
        [SerializeField] private bool showDebug = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showDebug = !showDebug;
            }
        }

        private void OnGUI()
        {
            if (!showDebug || playerMovement == null)
            {
                return;
            }

            const int width = 260;
            const int lineHeight = 20;
            int lines = 5;
            GUI.Box(new Rect(10, 10, width, lineHeight * lines + 10), "Movement Debug (F3 to toggle)");

            var state = playerMovement.State;
            float speed = playerMovement.Momentum != null ? playerMovement.Momentum.GetCurrentSpeed() : 0f;

            int y = 30;
            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"State: {state.CurrentState}"); y += lineHeight;
            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"Grounded: {state.IsGrounded}"); y += lineHeight;
            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"Speed: {speed:F2} u/s"); y += lineHeight;
            GUI.Label(new Rect(20, y, width - 20, lineHeight), $"Vertical Vel: {state.VerticalVelocity:F2}"); y += lineHeight;

            if (dashSystem != null)
            {
                GUI.Label(new Rect(20, y, width - 20, lineHeight), $"Dash Charges: {dashSystem.CurrentCharges}/{dashSystem.MaxCharges}");
            }
        }
    }
}
