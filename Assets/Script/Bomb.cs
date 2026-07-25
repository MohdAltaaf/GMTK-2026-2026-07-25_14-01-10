using System.Collections.Generic;
using UnityEngine;

// The bomb - a Throwable with a persistent fuse timer, soft mid-air homing toward
// whoever it's aimed at, and a proximity-based retarget rule that lets movement
// itself become an attack (see GDD section 4, "herding").
public class Bomb : Throwable
{
    [Header("Fuse")]
    [Tooltip("Total time before detonation. Keeps counting through catches, throws, and idle time on the ground - nothing resets it.")]
    [SerializeField] private float fuseDuration = 12f;
    [Tooltip("If the fuse hits zero while the bomb is mid-air or sitting on the ground (not held), anyone within this radius is caught in the blast.")]
    [SerializeField] private float detonationRadius = 3f;

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

    public event System.Action<float> OnFuseTick;          // 1 = full fuse, 0 = about to blow - wire this to your countdown UI
    public event System.Action<Catcher> OnRetargeted;      // fire your telegraph flash/sound off this
    public event System.Action<List<Catcher>> OnDetonated; // whoever got caught in the blast

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
            return;
        }

        if (State == ThrowableState.InFlight)
        {
            UpdateHoming();
            UpdateRetarget();
        }
    }

    public override void Throw(Vector3 velocity)
    {
        base.Throw(velocity);

        Catcher throwerCatcher = CurrentThrower != null ? CurrentThrower.GetComponentInParent<Catcher>() : null;
        ignoredEntity = throwerCatcher;
        ignoredHasLeftZone = false;

        currentTarget = FindDefaultTarget(throwerCatcher);
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
        Vector3 newDir = Vector3.Slerp(currentDir, desiredDir, homingStrength * Time.deltaTime);

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

        List<Catcher> victims = new List<Catcher>();

        if (State == ThrowableState.Held && Holder != null)
        {
            Catcher holderCatcher = Holder.GetComponentInParent<Catcher>();
            if (holderCatcher != null) victims.Add(holderCatcher);
        }
        else
        {
            foreach (Catcher c in Catcher.All)
            {
                if (Vector3.Distance(c.transform.position, transform.position) <= detonationRadius)
                    victims.Add(c);
            }
        }

        if (explosionVFX != null)
        {
            ParticleSystem fx = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration);
        }

        OnDetonated?.Invoke(victims);
    }

    // Call this from whatever round-management script you build next, once you
    // know where the bomb should reappear. Detonate() deliberately doesn't call
    // this itself - what happens after a blast (respawn point, scoring, delay)
    // is round logic, not bomb logic.
    public void Respawn(Vector3 position)
    {
        ForceReset(position);
        fuseTimer = fuseDuration;
        hasDetonated = false;
        currentTarget = null;
        ignoredEntity = null;
    }
}