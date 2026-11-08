using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Tracks a looping waypoint route and moves the enemy toward a destination,
/// pausing for a random spell each time a waypoint is reached.
/// Owns no decision about *where* to go: CO_EnemyAI_Script drives it each frame, which is
/// what lets the same movement code serve both patrolling and chasing.
///
/// Movement runs through a NavMeshAgent, so the enemy routes around walls rather
/// than pressing into them. Rotation is kept in this script instead of the
/// agent's own, so turnSpeed still governs how fast the vision cone swings.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class CO_EnemyPatrol_Script : MonoBehaviour
{
    [Header("Patrol Route")]
    [Tooltip("Points to visit in order. Needs at least two.")]
    [SerializeField] private Transform[] waypoints;

    [Header("Movement")]
    [Tooltip("This coworker type's numbers: patrol speed, turn speed and waypoint pauses.")]
    [SerializeField] private CO_GuardTuning_Script tuning;

    [Tooltip("How close counts as having reached a waypoint.")]
    [SerializeField] private float arriveDistance = 0.15f;

    private int currentIndex;
    private float waitTimer;
    private NavMeshAgent agent;

    /// <summary>Speed used when following the patrol route.</summary>
    public float PatrolSpeed => tuning.PatrolSpeed;

    /// <summary>True when the route has enough valid points to follow.</summary>
    public bool HasRoute => waypoints != null && waypoints.Length >= 2;

    /// <summary>True while standing at a waypoint waiting out its pause.</summary>
    public bool IsWaiting => waitTimer > 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // This script keeps ownership of facing so turnSpeed stays meaningful and
        // FaceTowards can hold the enemy on the player mid-attack.
        agent.updateRotation = false;

        if (!HasRoute)
        {
            Debug.LogWarning($"{name}: CO_EnemyPatrol_Script needs at least two waypoints.", this);
        }
    }

    /// <summary>
    /// The waypoint currently being walked toward, flattened to the enemy's own
    /// height. Returns the enemy's position if the route is unusable, which keeps
    /// it standing still rather than throwing.
    /// </summary>
    public Vector3 GetCurrentDestination()
    {
        if (!HasRoute)
        {
            return transform.position;
        }

        Transform waypoint = waypoints[currentIndex];
        if (waypoint == null)
        {
            return transform.position;
        }

        return FlattenToOwnHeight(waypoint.position);
    }

    /// <summary>
    /// Runs the route for this frame: counts down a pause if one is open, and
    /// otherwise opens one on reaching the current waypoint. The step to the next
    /// waypoint waits until that pause expires, so the enemy stands at the point
    /// it arrived at rather than at the one it is already heading for.
    /// Called by CO_EnemyAI_Script only while patrolling, so chasing never consumes the route.
    /// </summary>
    public void UpdateRoute()
    {
        if (!HasRoute)
        {
            return;
        }

        if (IsWaiting)
        {
            waitTimer -= Time.deltaTime;

            if (!IsWaiting)
            {
                currentIndex = (currentIndex + 1) % waypoints.Length;
            }

            return;
        }

        if (Vector3.Distance(transform.position, GetCurrentDestination()) <= arriveDistance)
        {
            // Rolled per stop rather than once per enemy, so guards sharing a route
            // drift apart instead of marching in step.
            waitTimer = Random.Range(tuning.MinWaitTime, tuning.MaxWaitTime);

            // With the range left at zero there is no pause to expire, so step on
            // here instead. Otherwise the enemy would park on the waypoint.
            if (!IsWaiting)
            {
                currentIndex = (currentIndex + 1) % waypoints.Length;
            }
        }
    }

    /// <summary>
    /// Drops a pause in progress. Called when the enemy leaves its route, so a
    /// half-spent wait does not freeze it the moment it resumes patrolling.
    /// </summary>
    public void CancelWait()
    {
        waitTimer = 0f;
    }

    /// <summary>
    /// Holds position without turning. The agent still holds the destination from
    /// the last move, so it has to be stopped explicitly or it creeps onward.
    /// </summary>
    public void Halt()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }

    /// <summary>
    /// Steps toward a destination and turns to face it. Shared by every state,
    /// so patrolling and chasing move and rotate identically.
    /// </summary>
    public void MoveTowards(Vector3 destination, float speed)
    {
        if (!agent.isOnNavMesh)
        {
            return;
        }

        agent.speed = speed;
        agent.isStopped = false;
        agent.SetDestination(destination);

        // Face along the path rather than straight at the destination: while
        // rounding a corner the two differ, and steering is what the enemy is
        // actually doing. desiredVelocity goes to zero on arrival, which is also
        // when there is no meaningful direction left to turn toward.
        Vector3 direction = agent.desiredVelocity;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion facing = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, facing, tuning.TurnSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Turns to face a target without moving. Used when the enemy has stopped but
    /// should still track the player.
    /// </summary>
    public void FaceTowards(Vector3 target)
    {
        // Attacking calls this instead of MoveTowards, so the agent still holds
        // the destination from the last chase frame. Halt it, or the enemy keeps
        // creeping forward while it is supposed to be standing and swinging.
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        Vector3 direction = FlattenToOwnHeight(target) - transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion facing = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, facing, tuning.TurnSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Keeps the enemy at its own height so a target above or below the ground
    /// doesn't pull it off the floor.
    /// </summary>
    public Vector3 FlattenToOwnHeight(Vector3 position)
    {
        return new Vector3(position.x, transform.position.y, position.z);
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        Gizmos.color = Color.yellow;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);

            // Connect to the next point, wrapping back to the first to show the loop.
            Transform next = waypoints[(i + 1) % waypoints.Length];
            if (next != null)
            {
                Gizmos.DrawLine(waypoints[i].position, next.position);
            }
        }
    }
}
