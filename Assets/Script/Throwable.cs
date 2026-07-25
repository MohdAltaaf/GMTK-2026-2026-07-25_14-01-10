using UnityEngine;

public enum ThrowableState { Idle, Held, InFlight }

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Throwable : MonoBehaviour
{
    [Header("Throwable")]
    [Tooltip("Local offset from the hold point while carried.")]
    [SerializeField] protected Vector3 holdLocalOffset = new Vector3(0.4f, -0.2f, 0.6f);
    [Tooltip("How long to ignore collision with whoever just threw this, so a point-blank release doesn't immediately register as a self-hit/self-catch.")]
    [SerializeField] private float throwerIgnoreDuration = 0.2f;

    protected Rigidbody rb;
    protected Collider col;

    public ThrowableState State { get; private set; } = ThrowableState.Idle;
    public Transform Holder { get; private set; }
    public Transform CurrentThrower { get; private set; } // last thrower - handy for subclasses (e.g. the bomb's retarget-exclusion logic)

    private Collider ignoredThrowerCollider;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // avoids tunneling through thin colliders at throw speed
    }

    // --- Pickup / Hold ---

    public virtual bool CanBePickedUp() => State == ThrowableState.Idle;

    public virtual void PickUp(Transform holder)
    {
        if (!CanBePickedUp()) return;

        Holder = holder;
        State = ThrowableState.Held;

        rb.isKinematic = true;
        col.enabled = false; // no physics needed while carried - it's parented

        transform.SetParent(holder);
        transform.localPosition = holdLocalOffset;
        transform.localRotation = Quaternion.identity;
    }

    // --- Throw ---

    public virtual void Throw(Vector3 velocity)
    {
        if (State != ThrowableState.Held) return;

        CurrentThrower = Holder;
        Holder = null;
        State = ThrowableState.InFlight;

        transform.SetParent(null);
        col.enabled = true;
        rb.isKinematic = false;
        rb.linearVelocity = velocity; // Unity 6+ naming - use rb.velocity instead on older LTS versions

        Collider throwerCollider = CurrentThrower != null ? CurrentThrower.GetComponentInParent<Collider>() : null;
        if (throwerCollider != null)
        {
            ignoredThrowerCollider = throwerCollider;
            Physics.IgnoreCollision(col, ignoredThrowerCollider, true);
            Invoke(nameof(ReenableThrowerCollision), throwerIgnoreDuration);
        }
    }

    private void ReenableThrowerCollision()
    {
        if (ignoredThrowerCollider == null) return;
        Physics.IgnoreCollision(col, ignoredThrowerCollider, false);
        ignoredThrowerCollider = null;
    }

    // --- Collision handling ---

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (State != ThrowableState.InFlight) return;

        Catcher catcher = collision.collider.GetComponentInParent<Catcher>();
        if (catcher != null && catcher.IsReadyToCatch)
        {
            catcher.ReceiveCatch(this);
            return;
        }

        OnHitEffect(collision);

        // Settle wherever it landed so it can be picked up again -
        // no separate "rolls toward target" pathing, per the GDD.
        State = ThrowableState.Idle;
    }

    // Subclasses override this for their specific effect (explode, freeze, knockback...).
    // Left empty by default so a plain environment hit doesn't need special-casing.
    protected virtual void OnHitEffect(Collision collision) { }
}
