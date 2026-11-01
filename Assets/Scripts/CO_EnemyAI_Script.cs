using UnityEngine;

/// <summary>What the enemy is currently doing, in order of escalation.</summary>
public enum EnemyState
{
    Patrolling,
    Alerted,
    Investigating,
    Chasing,
    Attacking
}

/// <summary>
/// Decides what the enemy should be doing each frame and drives the movement,
/// detection, hearing and attack components accordingly. Those components own
/// their own behaviour; this one only chooses between them.
/// </summary>
[RequireComponent(typeof(CO_EnemyPatrol_Script))]
[RequireComponent(typeof(CO_EnemyDetection_Script))]
[RequireComponent(typeof(CO_EnemyAttack_Script))]
[RequireComponent(typeof(CO_EnemyHearing_Script))]
[RequireComponent(typeof(CO_EnemyInvestigation_Script))]
public class CO_EnemyAI_Script : MonoBehaviour
{
    [Header("Chase")]
    [Tooltip("Units travelled per second while chasing. Usually above patrol speed.")]
    [SerializeField] private float chaseSpeed = 3.5f;

    private CO_EnemyPatrol_Script movement;
    private CO_EnemyDetection_Script detection;
    private CO_EnemyAttack_Script attack;
    private CO_EnemyHearing_Script hearing;
    private CO_EnemyInvestigation_Script investigation;

    /// <summary>Current behaviour, read by the state indicator.</summary>
    public EnemyState CurrentState { get; private set; } = EnemyState.Patrolling;

    private void Awake()
    {
        movement = GetComponent<CO_EnemyPatrol_Script>();
        detection = GetComponent<CO_EnemyDetection_Script>();
        attack = GetComponent<CO_EnemyAttack_Script>();
        hearing = GetComponent<CO_EnemyHearing_Script>();
        investigation = GetComponent<CO_EnemyInvestigation_Script>();
    }

    private void Update()
    {
        UpdateState();

        switch (CurrentState)
        {
            case EnemyState.Attacking:
                Attack();
                break;

            case EnemyState.Chasing:
                Chase();
                break;

            case EnemyState.Investigating:
                Investigate();
                break;

            case EnemyState.Alerted:
                Alert();
                break;

            case EnemyState.Patrolling:
            default:
                Patrol();
                break;
        }
    }

    /// <summary>
    /// Picks the state for this frame and logs transitions, so the state machine
    /// is readable in the console without extra instrumentation.
    /// </summary>
    private void UpdateState()
    {
        EnemyState previous = CurrentState;
        CurrentState = DecideState();

        if (CurrentState == previous)
        {
            return;
        }

        // Arriving fresh from a chase should swing immediately rather than
        // waiting out a cooldown left over from a previous engagement.
        if (CurrentState == EnemyState.Attacking)
        {
            attack.ResetCooldown();
        }

        // Spotting the player cuts a waypoint pause short, so the enemy does
        // not stand around once it goes back to patrolling.
        if (CurrentState != EnemyState.Patrolling)
        {
            movement.CancelWait();
        }

        // Seeing the player outranks having heard something: drop the noise
        // rather than wandering back to it once the chase ends.
        if (CurrentState == EnemyState.Chasing || CurrentState == EnemyState.Attacking)
        {
            investigation.Cancel();
            hearing.ClearNoise();
        }

        // Started here rather than in Investigate(), so the walk begins once per
        // investigation instead of restarting its timeout every frame.
        if (CurrentState == EnemyState.Investigating)
        {
            investigation.Begin(hearing.NoiseSource);
        }

        Debug.Log($"{name}: {previous} -> {CurrentState}", this);
    }

    private EnemyState DecideState()
    {
        // A glimpse only fills the notice meter; the enemy reacts once it is full.
        if (detection.HasNoticedPlayer)
        {
            Vector3 playerPosition = detection.DetectedPlayer.position;

            // Already attacking uses the wider exit range, so hovering near the
            // boundary does not bounce between attacking and chasing every frame.
            bool inRange = CurrentState == EnemyState.Attacking
                ? attack.ShouldStayInAttackRange(playerPosition)
                : attack.IsInAttackRange(playerPosition);

            return inRange ? EnemyState.Attacking : EnemyState.Chasing;
        }

        // The CO_NoiseManager_Script hands a noise to exactly one enemy, and stands the
        // rest down. Being left holding one without the walk only happens when
        // nobody could be spared to go.
        if (hearing.HasNoise)
        {
            return hearing.IsInvestigator ? EnemyState.Investigating : EnemyState.Alerted;
        }

        return EnemyState.Patrolling;
    }

    private void Patrol()
    {
        // Run the route first: arriving this frame opens the pause below, so the
        // enemy stops on the waypoint instead of overshooting it by a frame.
        movement.UpdateRoute();

        if (movement.IsWaiting)
        {
            movement.Halt();
            return;
        }

        movement.MoveTowards(movement.GetCurrentDestination(), movement.PatrolSpeed);
    }

    /// <summary>
    /// Heard the noise but is not the one going to it: stop and look that way
    /// for a moment. CO_EnemyHearing_Script times the moment out, which drops the enemy
    /// back onto its route - briefly when another enemy took the noise, for
    /// longer when nobody could be sent to it at all.
    /// </summary>
    private void Alert()
    {
        movement.FaceTowards(hearing.NoisePosition);
    }

    /// <summary>
    /// Walks to the noise and looks it over. The position comes from hearing
    /// each frame rather than being captured once, so a source still making
    /// noise keeps drawing the enemy to where it is now.
    /// </summary>
    private void Investigate()
    {
        investigation.Tick(hearing.NoisePosition);

        if (investigation.IsComplete)
        {
            hearing.ClearNoise();
        }
    }

    private void Chase()
    {
        // Flattened so the enemy drives at the player's feet rather than tilting
        // toward the centre of their collider.
        Vector3 target = movement.FlattenToOwnHeight(detection.DetectedPlayer.position);
        movement.MoveTowards(target, chaseSpeed);
    }

    private void Attack()
    {
        // Hold position but keep facing the player, so they cannot slip around
        // the side while the enemy is swinging.
        movement.FaceTowards(detection.DetectedPlayer.position);
        attack.TryAttack();
    }
}
