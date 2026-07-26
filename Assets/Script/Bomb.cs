using UnityEngine;

public class Bomb : Throwable
{
    [Header("Fuse")]
    [SerializeField] private float fuseDuration = 90f;

    [Header("Homing")]
    [SerializeField] private float homingStrength = 2f;
    [SerializeField] private float retargetRadius = 2.5f;
    [Tooltip("Floor speed once it's rolling on the ground after a miss - stops it freezing once friction kills its velocity.")]
    [SerializeField] private float minGroundHomingSpeed = 4f;
    [Tooltip("Inside this range, skip the gradual turn and aim dead straight at the target - fixes the orbit bug where it circles the target instead of connecting.")]
    [SerializeField] private float snapToDirectRadius = 1.5f;

    [Header("FX")]
    [Tooltip("Prefab whose children are the explosion's particle systems - all get played together.")]
    [SerializeField] private GameObject explosionVFX;

    [Header("Camera Shake")]
    [Tooltip("Player's camera shake system - gets a trauma burst on detonation.")]
    [SerializeField] private movementScript cameraShake;
    [SerializeField] private float shakeTrauma = 1f;

    [Header("Explosion Force")]
    [SerializeField] private float explosionForce = 20f;
    [SerializeField] private float explosionRadius = 15f;
    [SerializeField] private float explosionUpwardsModifier = 1.5f;

    public float fuseTimer;
    private bool hasDetonated;

    private Catcher currentTarget;
    private Catcher ignoredEntity;
    private bool ignoredHasLeftZone;

    public float FuseNormalized => Mathf.Clamp01(fuseTimer / fuseDuration);

    public event System.Action<float> OnFuseTick;
    public event System.Action<Catcher> OnRetargeted;
    public event System.Action<Catcher> OnDetonated;

    protected override void Awake()
    {
        base.Awake();
        fuseTimer = fuseDuration;
    }

    private void Update()
    {
        if (hasDetonated) return;

        fuseTimer -= Time.deltaTime;
        OnFuseTick?.Invoke(FuseNormalized);

        if (fuseTimer <= 0f) Detonate();
    }

    private void FixedUpdate()
    {
        if (hasDetonated) return;
        if (State != ThrowableState.InFlight) return;

        UpdateRetarget();
        UpdateHoming();
    }

    public override void Throw(Vector3 velocity)
    {
        base.Throw(velocity);

        Catcher throwerCatcher = CurrentThrower != null ? CurrentThrower.GetComponentInParent<Catcher>() : null;
        ignoredEntity = throwerCatcher;
        ignoredHasLeftZone = false;

        currentTarget = FindDefaultTarget(throwerCatcher);

        Debug.Log(currentTarget != null
            ? $"Bomb targeting: {currentTarget.name}"
            : "Bomb has no target after throw - Catcher.All needs 2+ entries");
    }

    // Deflect off ANY other throwable it touches - not gated on the other
    // item's State, because that was a race: the other item's own
    // OnCollisionEnter can flip its State to Idle before or after this one
    // runs (Unity doesn't guarantee which collider's callback fires first),
    // so checking otherItem.State == InFlight here was unreliable and often
    // silently failed. A physical collision is proof enough that it touched.
    protected override void OnHitEffect(Collision collision)
    {
        Throwable otherItem = collision.collider.GetComponentInParent<Throwable>();
        if (otherItem == null || otherItem == this) return;
        if (collision.contacts.Length == 0) return;

        Vector3 normal = collision.contacts[0].normal;
        rb.linearVelocity = Vector3.Reflect(rb.linearVelocity, normal);

        Debug.Log($"Bomb deflected off {otherItem.name}");

        // Retarget to whoever ISN'T the deflector - the player who just
        // successfully intercepted it shouldn't still be the one it's chasing.
        Catcher deflector = otherItem.CurrentThrower != null
            ? otherItem.CurrentThrower.GetComponentInParent<Catcher>()
            : null;

        Catcher newTarget = FindDefaultTarget(deflector);

        if (newTarget != null && newTarget != currentTarget)
        {
            currentTarget = newTarget;
            Debug.Log($"Bomb retargeted onto {newTarget.name} (deflect)");
            OnRetargeted?.Invoke(newTarget);
        }
    }
    // Called by anything that needs to strip time off the fuse directly (pocket watch, etc.)
    public void ReduceFuse(float amount)
    {
        fuseTimer = Mathf.Max(0f, fuseTimer - amount);
    }
    // Deflect from an AoE source (windball, etc.) rather than a direct collision -
// same idea as OnHitEffect's reflect, but there's no contact normal here, so
// direction is just "away from the explosion origin" instead of off a surface.
    // Called by AoE sources (windball, etc.) whose OnHitEffect already pushed the
// bomb's rigidbody via the same AddExplosionForce every other object gets -
// this only handles the retarget, since the physical shove is identical to
// anyone else caught in the blast, no special velocity math needed here.
    public void NotifyExplosionHit(Catcher excludeFromRetarget)
    {
        Catcher newTarget = FindDefaultTarget(excludeFromRetarget);
        if (newTarget != null && newTarget != currentTarget)
        {
            currentTarget = newTarget;
            Debug.Log($"Bomb retargeted onto {newTarget.name} (windball blast)");
            OnRetargeted?.Invoke(newTarget);
        }
    }

    // Keeps homing/rolling toward the target instead of going Idle on a miss -
    // a fumbled throw should still be a threat, not a free reset.
    protected override void OnMissedHit(Collision collision)
    {
        // Intentionally empty - do NOT call base (which sets State = Idle).
    }

    private Catcher FindDefaultTarget(Catcher exclude)
    {
        foreach (Catcher c in Catcher.All)
        {
            if (c != exclude) return c;
        }
        return null;
    }

    private void UpdateHoming()
    {
        if (currentTarget == null) return;

        Vector3 toTarget = currentTarget.transform.position - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;
        if (dist < 0.01f) return;

        Vector3 desiredDir = toTarget / dist;

        Vector3 flatVel = rb.linearVelocity; flatVel.y = 0f;
        Vector3 currentDir = flatVel.sqrMagnitude > 0.01f ? flatVel.normalized : desiredDir;

        Vector3 newDir = dist < snapToDirectRadius
            ? desiredDir
            : Vector3.Slerp(currentDir, desiredDir, homingStrength * Time.fixedDeltaTime);

        float speed = Mathf.Max(flatVel.magnitude, minGroundHomingSpeed);
        Vector3 newVel = newDir * speed;
        newVel.y = rb.linearVelocity.y;
        rb.linearVelocity = newVel;
    }

    private void UpdateRetarget()
    {
        Catcher closest = null;
        float closestDist = retargetRadius;

        foreach (Catcher c in Catcher.All)
        {
            float dist = Vector3.Distance(transform.position, c.transform.position);

            if (c == ignoredEntity)
            {
                if (dist > retargetRadius) ignoredHasLeftZone = true;
                if (!ignoredHasLeftZone) continue;
            }

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = c;
            }
        }

        if (closest != null && closest != currentTarget)
        {
            currentTarget = closest;
            Debug.Log($"Bomb retargeted onto {closest.name} (proximity)");
            OnRetargeted?.Invoke(closest);
        }
    }

    private void Detonate()
    {
        hasDetonated = true;

        Catcher loser = null;
        float closestDist = float.MaxValue;

        foreach (Catcher c in Catcher.All)
        {
            float dist = Vector3.Distance(c.transform.position, transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                loser = c;
            }
        }

        if (explosionVFX != null)
        {
            GameObject fx = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            ParticleSystem[] systems = fx.GetComponentsInChildren<ParticleSystem>();

            float longestDuration = 0f;
            foreach (ParticleSystem ps in systems)
            {
                ps.Play();
                longestDuration = Mathf.Max(longestDuration, ps.main.duration + ps.main.startLifetime.constantMax);
            }

            Destroy(fx, longestDuration);
        }

        if (cameraShake != null)
        {
            cameraShake.AddTrauma(shakeTrauma);
        }

        ApplyExplosionForce();

        OnDetonated?.Invoke(loser);

        // Single-round game - the bomb doesn't come back, the explosion replaces it.
        Destroy(gameObject);
    }

    private void ApplyExplosionForce()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            Rigidbody hitRb = hit.attachedRigidbody;
            if (hitRb == null || hitRb == rb) continue; // skip the bomb's own body - it's being destroyed anyway
            hitRb.AddExplosionForce(explosionForce, transform.position, explosionRadius, explosionUpwardsModifier, ForceMode.Impulse);
        }
    }
}