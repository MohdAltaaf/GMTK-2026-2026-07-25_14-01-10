using UnityEngine;

// The bomb - a Throwable with a persistent fuse timer, soft mid-air homing toward
// whoever it's aimed at, and a proximity-based retarget rule that lets movement
// itself become an attack (see GDD section 4, "herding").
//
// Win condition: when the fuse hits zero, whoever is CLOSEST to the bomb loses -
// holding it counts as effectively distance ~0, so no special-casing needed for
// "holding it when it goes off" vs "it landed near you."
public class Bomb : Throwable
{
    [Header("Fuse")]
    [Tooltip("Total time before detonation. Keeps counting through catches, throws, and idle time on the ground - nothing resets it. ~90s for the real match, keep this short (5-10s) while iterating today.")]
    [SerializeField] private float fuseDuration = 90f;

    [Header("Homing")]
    [Tooltip("How aggressively the bomb curves toward its target mid-flight. Higher = snappier turns, lower = gentler arcs that still feel throwable.")]
    [SerializeField] private float homingStrength = 2f;
    [Tooltip("If any entity other than the excluded thrower gets this close mid-flight, the bomb retargets onto them.")]
    [SerializeField] private float retargetRadius = 2.5f;

    [Header("FX")]
    [SerializeField] private ParticleSystem explosionVFX;

    private float fuseTimer;
    private bool hasDetonated;

    private Catcher currentTarget;
    private Catcher ignoredEntity;      // whoever just threw it - excluded from retargeting until they clear the zone once
    private bool ignoredHasLeftZone;

    public float FuseNormalized => Mathf.Clamp01(fuseTimer / fuseDuration);

    public event System.Action<float> OnFuseTick;     // 1 = full fuse, 0 = about to blow - wire this to your countdown UI
    public event System.Action<Catcher> OnRetargeted; // fire your telegraph flash/sound off this
    public event System.Action<Catcher> OnDetonated;  // whoever was closest when it went off - the loser

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

        if (fuseTimer <= 0f)
        {
            Detonate();
        }
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

        // Quick diagnostic - delete once you've confirmed targeting works.
        Debug.Log(currentTarget != null
            ? $"Bomb targeting: {currentTarget.name}"
            : "Bomb has no target after throw - Catcher.All needs 2+ entries (check your test dummy has an enabled Catcher component)");
    }

    private Catcher FindDefaultTarget(Catcher exclude)
    {
        foreach (Catcher c in Catcher.All)
        {
            if (c != exclude) return c; // 1v1: the default target is just "whoever isn't the thrower"
        }
        return null;
    }

    private void UpdateHoming()
    {
        if (currentTarget == null) return;

        Vector3 toTarget = currentTarget.transform.position - transform.position;
        if (toTarget.sqrMagnitude < 0.01f) return;

        Vector3 desiredDir = toTarget.normalized;
        Vector3 currentDir = rb.linearVelocity.sqrMagnitude > 0.01f ? rb.linearVelocity.normalized : desiredDir;
        Vector3 newDir = Vector3.Slerp(currentDir, desiredDir, homingStrength * Time.fixedDeltaTime);

        rb.linearVelocity = newDir * rb.linearVelocity.magnitude; // curve direction, preserve speed
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
                if (!ignoredHasLeftZone) continue; // not eligible yet - this is the self-retarget fix from the GDD
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
        // Holding the bomb puts you at ~0 distance, so "holding when it goes off"
        // and "it landed near you" both fall out of this one comparison - no
        // separate held-vs-thrown branch needed.

        if (explosionVFX != null)
        {
            ParticleSystem fx = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration);
        }

        OnDetonated?.Invoke(loser);
    }

    // Call this from whatever end-game/menu script you build, once you know
    // where (or whether) the bomb should reappear.
    public void Respawn(Vector3 position)
    {
        ForceReset(position);
        fuseTimer = fuseDuration;
        hasDetonated = false;
        currentTarget = null;
        ignoredEntity = null;
    }
}
