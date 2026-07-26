using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Catcher))]
public class EnemyAI : MonoBehaviour
{
    private enum AIState { Wander, Flee, Intercept }

    [Header("References")]
    [Tooltip("Drag the bomb in the scene here.")]
    [SerializeField] private Bomb bomb;

    [Header("Awareness")]
    [Tooltip("Bomb closer than this = enemy reacts at all. Farther away = idle wander.")]
    [SerializeField] private float awarenessRadius = 12f;
    [Tooltip("Bomb closer than this while airborne = attempt a catch instead of fleeing.")]
    [SerializeField] private float catchAttemptRadius = 4f;

    [Header("Flee")]
    [SerializeField] private float fleeDistance = 8f;
    [SerializeField] private float repathInterval = 0.4f;

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float wanderInterval = 3f;

    [Header("Catch Reaction")]
    [Tooltip("Random human-like delay before the catch is actually armed once in range - keeps it beatable instead of a perfect wall.")]
    [SerializeField] private Vector2 reactionDelayRange = new Vector2(0.1f, 0.35f);

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

        Vector3 away = (transform.position - bomb.transform.position).normalized;
        Vector3 fleePoint = transform.position + away * fleeDistance;

        if (NavMesh.SamplePosition(fleePoint, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void Intercept()
    {
        // Hold ground and let Throwable's catch check do its job - moving toward
        // an incoming bomb is more likely to help than hurt, but keep it simple:
        // just stop running so it doesn't accidentally dodge the catch.
        agent.ResetPath();
    }
}
