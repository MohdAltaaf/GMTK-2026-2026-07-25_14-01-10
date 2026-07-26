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
    public Transform CurrentThrower { get; private set; }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    // --- Pickup / Hold ---

    // "Not currently Held by someone else" - covers ground pickup (Idle) AND
    // mid-flight catch (InFlight) in one check. Idle-only used to silently break every catch.
    public virtual bool CanBePickedUp() => State != ThrowableState.Held;

    public virtual void PickUp(Transform holder)
    {
        if (!CanBePickedUp()) return;

        Holder = holder;
        State = ThrowableState.Held;

        rb.isKinematic = true;
        col.enabled = false;

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
        rb.linearVelocity = velocity;

        Collider throwerCollider = CurrentThrower != null ? CurrentThrower.GetComponentInParent<Collider>() : null;
        if (throwerCollider != null)
        {
            StartCoroutine(IgnoreThrowerBriefly(throwerCollider));
        }
    }

    // Captures its own collider per-call instead of sharing one field - rapid
    // back-and-forth throws (player -> enemy -> player) used to stomp on each
    // other and leave collision permanently disabled for whoever lost the race.
    private System.Collections.IEnumerator IgnoreThrowerBriefly(Collider throwerCollider)
    {
        Physics.IgnoreCollision(col, throwerCollider, true);
        yield return new WaitForSeconds(throwerIgnoreDuration);
        if (throwerCollider != null)
        {
            Physics.IgnoreCollision(col, throwerCollider, false);
        }
    }

    public virtual void ForceReset(Vector3 position)
    {
        StopAllCoroutines(); // clears any pending IgnoreThrowerBriefly re-enable
        transform.SetParent(null);
        transform.position = position;
        transform.rotation = Quaternion.identity;
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        col.enabled = true;
        Holder = null;
        CurrentThrower = null;
        State = ThrowableState.Idle;
    }

    // --- Collision handling ---

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (State != ThrowableState.InFlight) return;

        Catcher catcher = collision.collider.GetComponentInParent<Catcher>();
       if (catcher != null && catcher.IsReadyToCatch && !catcher.IsHoldingSomething)
        {
            catcher.ReceiveCatch(this);
            return;
        }

        OnHitEffect(collision);
        OnMissedHit(collision);
    }

    protected virtual void OnHitEffect(Collision collision) { }

    protected virtual void OnMissedHit(Collision collision)
    {
        State = ThrowableState.Idle;
    }
}