using UnityEngine;
using UnityEngine.InputSystem;

public class movementScript : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The camera's own transform (or a pivot holding it). This gets pitch + all the juice. The root object only ever yaws, so the CharacterController's capsule never tilts.")]
    [SerializeField] private Transform cameraTransform;
    [Tooltip("Used to read strafe input for tilt, and to subscribe to dash/jump/land for shake.")]
    [SerializeField] private Movememnt playerMovement;

    [Header("Look")]
    public float MouseSensitivity = 0.1f;
    public float minPitch = -85f;
    public float maxPitch = 85f;

    [Header("Strafe Tilt")]
    [SerializeField] private float tiltAmount = 3f;   // degrees of roll at full strafe
    [SerializeField] private float tiltSpeed = 8f;    // how quickly it eases toward the target tilt

    [Header("Head Bob")]
    [SerializeField] private float bobFrequency = 10f;
    [SerializeField] private float bobAmount = 0.05f;
    [SerializeField] private float bobSmoothing = 10f;

    [Header("Shake")]
    [Tooltip("Trauma added per event, 0-1 range. Shake magnitude scales with trauma^2 for a punchy hit that fades fast.")]
    [SerializeField] private float dashShake = 0.4f;
    [SerializeField] private float jumpShake = 0.15f;
    [SerializeField] private float landShake = 0.25f;
    [SerializeField] private float shakeDecay = 2f;
    [SerializeField] private float maxShakeOffset = 0.15f;
    [SerializeField] private float maxShakeRoll = 4f;

    private Vector2 mouseInput;
    private float xRotation;
    private float yRotation;
    

    private float currentTilt;
    private float bobTimer;
    private Vector3 bobOffset;

    private float trauma; // 0-1, decays each frame
    private Vector3 shakeOffset;
    private float shakeRoll;

    private Vector3 cameraRestPosition; 

    void OnLook(InputValue value)
    {
        mouseInput = value.Get<Vector2>();
    }

    

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cameraRestPosition = cameraTransform.localPosition;
    }

    void OnEnable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnDashStarted += HandleDash;
            playerMovement.OnJumped += HandleJump;
            playerMovement.OnLanded += HandleLand;
        }
    }

    void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnDashStarted -= HandleDash;
            playerMovement.OnJumped -= HandleJump;
            playerMovement.OnLanded -= HandleLand;
        }
    }

    private void HandleDash() => AddTrauma(dashShake);
    private void HandleJump() => AddTrauma(jumpShake);
    private void HandleLand() => AddTrauma(landShake);

    void Update()
    {
        Look();
        Tilt();
        HeadBob();
        Shake();


        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, currentTilt + shakeRoll);
        cameraTransform.localPosition = cameraRestPosition + bobOffset + shakeOffset;
    }

    private void Look()
    {
        xRotation -= mouseInput.y * MouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch); 
        yRotation += mouseInput.x * MouseSensitivity;

        
        transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    private void Tilt()
    {
        float strafe = playerMovement != null ? playerMovement.MoveInput.x : 0f;
        float targetTilt = -strafe * tiltAmount; 
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);
    }

    private void HeadBob()
    {
        bool moving = playerMovement != null
            && playerMovement.MoveInput.sqrMagnitude > 0.01f
            && playerMovement.IsGrounded;

        Vector3 targetBob = Vector3.zero;

        if (moving)
        {
            bobTimer += Time.deltaTime * bobFrequency;
            targetBob = new Vector3(
                Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.5f, 
                Mathf.Sin(bobTimer) * bobAmount,
                0f
            );
        }
        else
        {
            bobTimer = 0f;
        }

        bobOffset = Vector3.Lerp(bobOffset, targetBob, Time.deltaTime * bobSmoothing);
    }

    private void Shake()
    {
        trauma = Mathf.Max(0f, trauma - shakeDecay * Time.deltaTime);
        float shakeAmount = trauma * trauma;

        shakeOffset = new Vector3(
            (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * 2f * maxShakeOffset * shakeAmount,
            (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * 2f * maxShakeOffset * shakeAmount,
            0f
        );
        shakeRoll = (Mathf.PerlinNoise(Time.time * 15f, 5f) - 0.5f) * 2f * maxShakeRoll * shakeAmount;
    }

    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }
}