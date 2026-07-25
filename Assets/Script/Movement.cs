using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Movememnt : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player's camera transform - used to make movement/dash direction camera-relative.")]
    [SerializeField] private Transform cam;

    [Header("Move")]
    [Tooltip("This IS the sprint speed - GDD calls for one fast base speed, no separate sprint toggle.")]
    [SerializeField] private float moveSpeed = 9f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.5f;
    [Tooltip("Stronger than Unity's default gravity for a snappier, less floaty fall.")]
    [SerializeField] private float gravity = -25f;
    [Tooltip("Grace period after walking off a ledge where you can still jump.")]
    [SerializeField] private float coyoteTime = 0.12f;
    [Tooltip("Grace period where a jump press just before landing still fires.")]
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 22f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.4f;
    [Tooltip("Extra dashes usable in the air before touching ground again. Set to 0 for grounded-only dashing.")]
    [SerializeField] private int airDashCharges = 1;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 velocity; // accumulated vertical velocity (gravity/jump)

    private float coyoteTimer;
    private float jumpBufferTimer;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector3 dashDirection;
    private int airDashesRemaining;


    public Vector2 MoveInput => moveInput;
    public bool IsGrounded { get; private set; }



    public event System.Action OnDashStarted;
    public event System.Action OnJumped;
    public event System.Action OnLanded;

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
        if (grounded && !IsGrounded) OnLanded?.Invoke();
        IsGrounded = grounded;

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
            OnJumped?.Invoke();
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

        
        Vector3 dir = CameraRelativeDirection();
        dashDirection = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        OnDashStarted?.Invoke();
    }
}