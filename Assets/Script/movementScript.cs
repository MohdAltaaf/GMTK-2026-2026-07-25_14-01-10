using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class movementScript : MonoBehaviour
{
    private Camera playerCamera;

    public float MouseSensitivity = 0.1f;
    public float minPitch = -85f;
    public float maxPitch = 85f;
    private Vector2 MouseInput;
    private float mouseHorizontal;
    private float mouseVertical;
    private float Xrotation;
    private float Yrotation;
    private float aimInput;
    public bool isAiming;





    void OnLook(InputValue Value)
    {
        MouseInput = Value.Get<Vector2>();
        mouseHorizontal = MouseInput.x;
        mouseVertical = MouseInput.y;
        
        
    }

    void OnAim(InputValue value)
    {
        isAiming = value.isPressed;


        
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
    }

    // Update is called once per frame
    void Update()
    {
        rotation();
        

    }
    void rotation()
    {
        Xrotation -= Mathf.Clamp(mouseVertical*MouseSensitivity, minPitch, maxPitch);
        Yrotation += mouseHorizontal*MouseSensitivity;
        Quaternion desiredRotation = Quaternion.Euler(Xrotation, Yrotation, 0);
        transform.localRotation = desiredRotation;

    }
 
}
