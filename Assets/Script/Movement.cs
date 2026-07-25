using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;


[RequireComponent(typeof(CharacterController))]
public class Movement : MonoBehaviour
{
    [Header ("References")]
    [SerializeField] private Transform cam;
    [Header("Move")]
    public float moveSpeed = 9f;
    public float jumpHeight = 1.5f;
    public float gravity = -25f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("Dash")]
    public float dashSpeed = 22f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.4f;
    public int airDashCharges = 1;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 velocity;

    private float coyoteTimer;
    private float jumpBufferTimer;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector3 dashDirection;
    
    private int airDashesRemaining;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        airDashesRemaining = airDashCharges;
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            jumpBufferTimer = jumpBufferTime; 
        }
    }
    void OnDash(InputValue value)
    {
        if (value.isPressed)
        {
            TryDash();
        }
    }



    

    private void Update()
    {
        bool grounded = controller.isGrounded;
 
        // --- Timers ---
        coyoteTimer = grounded ? coyoteTime : coyoteTimer - Time.deltaTime;
        jumpBufferTimer -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;
 
        if (grounded)
        {
            airDashesRemaining = airDashCharges; // refill on landing
            if (velocity.y < 0f) velocity.y = -2f; // small stick-to-ground force, avoids isGrounded flicker
        }
 
        // --- Jump (coyote time + buffer are both easy to strip if they cause weirdness) ---
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f; // prevents double-jumping off the same coyote window
        }
 
        // --- Dash resolution ---
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) isDashing = false;
        }
 
        // --- Horizontal movement ---
        Vector3 horizontalMove;
        if (isDashing)
        {
            horizontalMove = dashDirection * dashSpeed;
        }
        else
        {
            Vector3 wishDir = CameraRelativeDirection();
            horizontalMove = wishDir * moveSpeed;
        }
 
        // --- Gravity ---
        velocity.y += gravity * Time.deltaTime;
 
        // --- Apply everything in one Move() call ---
        Vector3 finalMove = horizontalMove;
        finalMove.y = velocity.y;
        controller.Move(finalMove * Time.deltaTime);
    }
 
    private Vector3 CameraRelativeDirection()
    {
        Vector3 camForward = cam.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight = cam.right; camRight.y = 0f; camRight.Normalize();
 
        Vector3 wishDir = (camForward * moveInput.y) + (camRight * moveInput.x);
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();
        return wishDir;
    }
 
    private void TryDash()
    {
        if (isDashing) return;
        if (dashCooldownTimer > 0f) return;
 
        bool grounded = controller.isGrounded;
        if (!grounded)
        {
            if (airDashesRemaining <= 0) return;
            airDashesRemaining--;
        }
 
        // Dash toward current move input direction, or straight forward if standing still.
        Vector3 dir = CameraRelativeDirection();
        dashDirection = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
 
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
    }

}
