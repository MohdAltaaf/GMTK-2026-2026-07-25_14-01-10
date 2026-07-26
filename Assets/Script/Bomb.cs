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
    [SerializeField] private ParticleSystem explosionVFX;

    private float fuseTimer;
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
            ParticleSystem fx = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration);
        }

        OnDetonated?.Invoke(loser);
    }

    public void Respawn(Vector3 position)
    {
        ForceReset(position);
        fuseTimer = fuseDuration;
        hasDetonated = false;
        currentTarget = null;
        ignoredEntity = null;
    }
}