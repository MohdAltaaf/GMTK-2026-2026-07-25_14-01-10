using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Catcher))]
public class PlayerThrowController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cam;
    public PlayerAudio playerAudio;

    [Header("Pickup")]
    [SerializeField] private float pickupRange = 2f;
    [SerializeField] private LayerMask throwableMask;

    [Header("Throw")]
    [SerializeField] private float throwForce = 18f;

    private Catcher catcher;

    private void Awake()
    {
        catcher = GetComponent<Catcher>();
        playerAudio = GetComponent<PlayerAudio>();
    }

    void OnInteract(InputValue value)
    {
        bool pressed = value.isPressed;
        catcher.IsReadyToCatch = pressed;

        if (pressed && !catcher.IsHoldingSomething)
        {
            TryPickUpNearby();
        }
    }

    void OnThrow(InputValue value)
    {
        if (!value.isPressed) return;
        if (!catcher.IsHoldingSomething) return;

        Throwable item = catcher.HeldItem;

        Vector3 aimPoint = GetAimPoint();
        Vector3 throwDir = (aimPoint - item.transform.position).normalized;

        item.Throw(throwDir * throwForce);
        playerAudio.PlayThrowSound();

        catcher.NotifyThrown();
    }

    private Vector3 GetAimPoint()
    {
        const float maxDistance = 100f;
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, maxDistance))
        {
            return hit.point;
        }
        return cam.position + cam.forward * maxDistance; // nothing in the way - aim far down the sightline
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
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = candidate;
            }
        }

        if (closest != null)
        {
            catcher.ReceiveCatch(closest); // routed through Catcher, not a direct PickUp
        }
    }
}