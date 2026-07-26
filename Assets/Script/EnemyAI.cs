using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Catcher))]
public class EnemyAI : MonoBehaviour
{
    private enum AIState { Wander, Flee, Intercept }

    [Header("References")]
    [SerializeField] private Bomb bomb;

    [Header("Awareness")]
    [SerializeField] private float awarenessRadius = 12f;
    [SerializeField] private float catchAttemptRadius = 4f;

    [Header("Flee")]
    [SerializeField] private float fleeDistance = 8f;
    [SerializeField] private float repathInterval = 0.4f;

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float wanderInterval = 3f;

    [Header("Catch Reaction")]
    [SerializeField] private Vector2 reactionDelayRange = new Vector2(0.1f, 0.35f);

    [Header("Throw Back")]
    [SerializeField] private float throwBackForce = 18f;
    [SerializeField] private Vector2 throwBackDelayRange = new Vector2(0.2f, 0.5f);
    [Tooltip("Degrees to tilt the throw upward from flat - gives it an actual arc instead of a flat roll, and a real airborne window for deflection.")]
    [SerializeField] private float throwBackArcAngle = 25f;
    [Header("Aggression")]
    [Range(0f, 1f)]
    [Tooltip("Chance each flee-repath to instead push toward the player and herd the bomb at them (GDD section 4), instead of pure fleeing.")]
    [SerializeField] private float herdChance = 0.35f;
    [Tooltip("Candidate angles either side of straight-away-from-bomb, tried when picking a flee direction - stops it beelining into the same wall/corner repeatedly.")]
    [SerializeField] private float[] fleeAngleOffsets = { 0f, 45f, -45f, 90f, -90f };

    private NavMeshAgent agent;
    private Catcher catcher;
    private AIState state;

    private float repathTimer;
    private float wanderTimer;
    private float reactionTimer;
    private bool reacting;
    private bool wasCloseAirborne;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        catcher = GetComponent<Catcher>();
    }

    private void OnEnable() => catcher.OnCaught += HandleCaught;
    private void OnDisable() => catcher.OnCaught -= HandleCaught;

    private void Update()
    {
        if (bomb == null) return;

        float distToBomb = Vector3.Distance(transform.position, bomb.transform.position);
        bool bombInFlight = bomb.State == ThrowableState.InFlight;
        bool closeAirborne = bombInFlight && distToBomb <= catchAttemptRadius;
        bool isThreat = distToBomb <= awarenessRadius;

        if (closeAirborne && !wasCloseAirborne)
        {
            reacting = true;
            reactionTimer = Random.Range(reactionDelayRange.x, reactionDelayRange.y);
        }
        wasCloseAirborne = closeAirborne;

        if (reacting)
        {
            reactionTimer -= Time.deltaTime;
            if (reactionTimer <= 0f) reacting = false;
        }

        catcher.IsReadyToCatch = closeAirborne && !reacting;

        state = !isThreat ? AIState.Wander
              : closeAirborne ? AIState.Intercept
              : AIState.Flee;

        switch (state)
        {
            case AIState.Wander: Wander(); break;
            case AIState.Flee: Flee(); break;
            case AIState.Intercept: Intercept(); break;
        }
    }

    private void Wander()
    {
        wanderTimer -= Time.deltaTime;
        if (wanderTimer > 0f) return;
        wanderTimer = wanderInterval;

        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        Vector3 point = transform.position + new Vector3(offset.x, 0f, offset.y);

        if (NavMesh.SamplePosition(point, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void Flee()
    {
        repathTimer -= Time.deltaTime;
        if (repathTimer > 0f) return;
        repathTimer = repathInterval;

        if (Random.value < herdChance)
        {
            Herd();
            return;
        }

        Vector3 awayDir = (transform.position - bomb.transform.position).normalized;
        Vector3 bestPoint = transform.position;
        float bestDist = -1f;

        foreach (float angle in fleeAngleOffsets)
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * awayDir;
            Vector3 candidate = transform.position + dir * fleeDistance;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
            {
                float d = Vector3.Distance(hit.position, bomb.transform.position);
                if (d > bestDist)
                {
                    bestDist = d;
                    bestPoint = hit.position;
                }
            }
        }

        agent.SetDestination(bestPoint);
    }

    private void Intercept()
    {
        agent.ResetPath();
    }

    private void HandleCaught(Throwable item)
    {
        if (item != bomb) return;
        StartCoroutine(ThrowBackAfterDelay());
    }

    private System.Collections.IEnumerator ThrowBackAfterDelay()
    {
        yield return new WaitForSeconds(Random.Range(throwBackDelayRange.x, throwBackDelayRange.y));
        if (bomb.State != ThrowableState.Held) yield break;

        Transform target = FindThrowBackTarget();
        Vector3 dir = target != null ? (target.position - transform.position) : transform.forward;
        dir.y = 0f;
        dir = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;

        // Tilt the flat direction upward - simple lob, not a full ballistic-arc
        // calculation, but plenty for jam purposes.
        float rad = throwBackArcAngle * Mathf.Deg2Rad;
        Vector3 arcedDir = (dir * Mathf.Cos(rad) + Vector3.up * Mathf.Sin(rad)).normalized;

        bomb.Throw(arcedDir * throwBackForce);
    }

    private Transform FindThrowBackTarget()
    {
        foreach (Catcher c in Catcher.All)
            if (c != catcher) return c.transform;
        return null;
    }
    private void Herd()
    {
        Transform player = FindThrowBackTarget(); // reuse - "whoever isn't me"
        if (player == null) return;

        if (NavMesh.SamplePosition(player.position, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}
