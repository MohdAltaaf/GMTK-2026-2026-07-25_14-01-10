using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Catcher))]
public class PlayerThrowController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Aim direction for throws - typically the player's camera.")]
    [SerializeField] private Transform cam;

    [Header("Pickup")]
    [SerializeField] private float pickupRange = 2f;
    [SerializeField] private LayerMask throwableMask;

    [Header("Throw")]
    [SerializeField] private float throwForce = 18f;

    private Catcher catcher;
    private Throwable heldItem;

    private void Awake()
    {
        catcher = GetComponent<Catcher>();
    }

    private void OnEnable()
    {
        catcher.OnCaught += HandleCaught;
    }

    private void OnDisable()
    {
        catcher.OnCaught -= HandleCaught;
    }

    private void HandleCaught(Throwable item)
    {
        heldItem = item;
    }

    // Holding this button arms the catch (checked inside Throwable.OnCollisionEnter)
    // and, on the initial press, attempts a ground pickup if your hands are empty.
    void OnInteract(InputValue value)
    {
        bool pressed = value.isPressed;
        catcher.IsReadyToCatch = pressed;
        Debug.Log("interact fired, pressed=" + value.isPressed);

        if (pressed && heldItem == null)
        {
            TryPickUpNearby();
        }
    }

    void OnThrow(InputValue value)
    {
        if (!value.isPressed) return;
        if (heldItem == null) return;

        heldItem.Throw(cam.forward * throwForce);
        heldItem = null;
    }

    private void TryPickUpNearby()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRange, throwableMask);
        Throwable closest = null;
        float closestDist = pickupRange;

        foreach (Collider hit in hits)
        {
            Throwable candidate = hit.GetComponentInParent<Throwable>();
            if (candidate == null || !candidate.CanBePickedUp()) continue;

            float dist = Vector3.Distance(transform.position, candidate.transform.position);
            Debug.Log(hits.Length);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = candidate;
            }
        }

        if (closest != null)
        {
            closest.PickUp(catcher.HoldPoint);
            heldItem = closest;
        }
    }
}
