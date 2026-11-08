using UnityEngine;

/// <summary>
/// One coworker type's numbers (GDD §5.1–§5.3, §6): how it moves, sees, hears,
/// investigates and catches. One asset per type, assigned on its prefab variant
/// and read by the guard components. Technical values (eye height, scan rate,
/// arrive distances) stay on the components.
/// </summary>
[CreateAssetMenu(fileName = "CO_GuardTuning_Data", menuName = "Clock Out/Guard Tuning")]
public class CO_GuardTuning_Script : ScriptableObject
{
    [Header("Movement")]
    [Tooltip("Walking speed on the patrol route, in metres per second.")]
    [Min(0f)]
    [SerializeField] private float patrolSpeed = 2f;

    [Tooltip("Running speed while chasing the player, in metres per second.")]
    [Min(0f)]
    [SerializeField] private float chaseSpeed = 3.5f;

    [Tooltip("Walking speed on the way to a noise, in metres per second.")]
    [Min(0f)]
    [SerializeField] private float investigateSpeed = 2.8f;

    [Tooltip("Degrees turned per second. Sets how fast the vision cone swings.")]
    [Min(0f)]
    [SerializeField] private float turnSpeed = 720f;

    [Header("Waypoint pause")]
    [Tooltip("Shortest pause at a waypoint, in seconds.")]
    [Min(0f)]
    [SerializeField] private float minWaitTime = 1f;

    [Tooltip("Longest pause at a waypoint, in seconds. Every stop rolls a new time between the two.")]
    [Min(0f)]
    [SerializeField] private float maxWaitTime = 1.5f;

    [Header("Sight")]
    [Tooltip("How far the vision cone reaches, in metres.")]
    [Min(0f)]
    [SerializeField] private float detectionRadius = 5f;

    [Tooltip("Total width of the vision cone in degrees.")]
    [Range(0f, 360f)]
    [SerializeField] private float viewAngle = 90f;

    [Tooltip("Seconds of unbroken sight to fill the notice meter. 0 notices on first sight.")]
    [Min(0f)]
    [SerializeField] private float noticeTime = 0.6f;

    [Tooltip("Seconds for a full meter to drain back to empty once the player is out of sight.")]
    [Min(0f)]
    [SerializeField] private float forgetTime = 1.5f;

    [Header("Hearing")]
    [Tooltip("Seconds spent glancing at a noise another coworker was sent to.")]
    [Min(0f)]
    [SerializeField] private float standDownDelay = 3.5f;

    [Tooltip("Seconds spent facing a noise nobody could be sent to.")]
    [Min(0f)]
    [SerializeField] private float alertDuration = 2.5f;

    [Header("Investigation")]
    [Tooltip("Seconds spent looking around at a noise, unless its source asks for its own.")]
    [Min(0f)]
    [SerializeField] private float inspectDuration = 4f;

    [Tooltip("Total width of the look-around sweep in degrees.")]
    [Range(0f, 360f)]
    [SerializeField] private float sweepAngle = 100f;

    [Tooltip("Seconds before an unreachable noise is given up on.")]
    [Min(0f)]
    [SerializeField] private float travelTimeout = 15f;

    [Header("Catch")]
    [Tooltip("Distance at which the coworker stops and hands over work, in metres.")]
    [Min(0f)]
    [SerializeField] private float attackRange = 2f;

    [Tooltip("Distance the player must pass before the chase resumes, in metres. Above attack range so the state doesn't flicker.")]
    [Min(0f)]
    [SerializeField] private float attackExitRange = 2.5f;

    [Tooltip("Seconds between attempts to hand over work.")]
    [Min(0f)]
    [SerializeField] private float attackCooldown = 1f;

    [Tooltip("Folders handed over per catch (GDD §6: Boss 3, the rest 1).")]
    [Min(1)]
    [SerializeField] private int foldersPerCatch = 1;

    [Tooltip("Seconds after a catch during which this coworker ignores the player (\"just delegated\").")]
    [Min(0f)]
    [SerializeField] private float delegatedCooldown = 8f;

    public float PatrolSpeed => patrolSpeed;
    public float ChaseSpeed => chaseSpeed;
    public float InvestigateSpeed => investigateSpeed;
    public float TurnSpeed => turnSpeed;
    public float MinWaitTime => minWaitTime;
    public float MaxWaitTime => maxWaitTime;
    public float DetectionRadius => detectionRadius;
    public float ViewAngle => viewAngle;
    public float NoticeTime => noticeTime;
    public float ForgetTime => forgetTime;
    public float StandDownDelay => standDownDelay;
    public float AlertDuration => alertDuration;
    public float InspectDuration => inspectDuration;
    public float SweepAngle => sweepAngle;
    public float TravelTimeout => travelTimeout;
    public float AttackRange => attackRange;
    public float AttackExitRange => attackExitRange;
    public float AttackCooldown => attackCooldown;
    public int FoldersPerCatch => foldersPerCatch;
    public float DelegatedCooldown => delegatedCooldown;

    private void OnValidate()
    {
        // An inverted range would feed Random.Range values that never pause, and a
        // smaller exit range would bring back the attack/chase flicker.
        maxWaitTime = Mathf.Max(maxWaitTime, minWaitTime);
        attackExitRange = Mathf.Max(attackExitRange, attackRange);
    }
}
