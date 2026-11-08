using UnityEngine;

/// <summary>
/// The catch (GDD §5.7): decides when the enemy is close enough to reach the
/// player and, on a cooldown, tries to hand them work. The player's work stack
/// refuses during its grace after the last catch, so the enemy keeps trying
/// until it lands or the player gets away.
/// </summary>
public class CO_EnemyAttack_Script : MonoBehaviour
{
    [Tooltip("This coworker type's numbers: catch ranges, cooldown and folders per catch.")]
    [SerializeField] private CO_GuardTuning_Script tuning;

    private float nextAttackTime;
    private float lastAttackTime = float.NegativeInfinity;

    // Cached per target, so the lookup runs once per player rather than every swing.
    private Transform stackOwner;
    private CO_WorkStack_Script targetStack;

    /// <summary>How long the gizmo stays lit after a swing.</summary>
    private const float AttackFlashDuration = 0.15f;

    /// <summary>Folders handed over per catch.</summary>
    public int FoldersPerCatch => tuning.FoldersPerCatch;

    /// <summary>When this enemy last handed over work, for the catch line. Negative infinity before the first.</summary>
    public float LastHandOffTime { get; private set; } = float.NegativeInfinity;

    /// <summary>
    /// True when the player is close enough to start attacking. Uses the tighter
    /// range, so the enemy must close properly before the first swing.
    /// </summary>
    public bool IsInAttackRange(Vector3 targetPosition)
    {
        return FlatDistanceTo(targetPosition) <= tuning.AttackRange;
    }

    /// <summary>
    /// True when an already-attacking enemy should keep attacking. Uses the wider
    /// range so small movements near the edge do not bounce it back into chasing.
    /// </summary>
    public bool ShouldStayInAttackRange(Vector3 targetPosition)
    {
        return FlatDistanceTo(targetPosition) <= tuning.AttackExitRange;
    }

    /// <summary>
    /// Tries to hand the target work if the cooldown has elapsed. Safe to call
    /// every frame; it paces itself. Returns true on the frame work was handed over.
    /// </summary>
    public bool TryAttack(Transform target)
    {
        if (Time.time < nextAttackTime)
        {
            return false;
        }

        nextAttackTime = Time.time + tuning.AttackCooldown;
        lastAttackTime = Time.time;

        if (stackOwner != target)
        {
            stackOwner = target;
            targetStack = target.GetComponent<CO_WorkStack_Script>();
        }

        if (targetStack == null || !targetStack.TryHandWork(tuning.FoldersPerCatch, gameObject))
        {
            return false;
        }

        LastHandOffTime = Time.time;
        return true;
    }

    /// <summary>
    /// Clears the pending cooldown so the next time the enemy closes in it swings
    /// immediately instead of waiting out a timer from a previous engagement.
    /// </summary>
    public void ResetCooldown()
    {
        nextAttackTime = 0f;
    }

    /// <summary>
    /// Horizontal distance only, so a target at a different height still counts
    /// as being in range.
    /// </summary>
    private float FlatDistanceTo(Vector3 targetPosition)
    {
        Vector3 flattened = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        return Vector3.Distance(transform.position, flattened);
    }

    private void OnDrawGizmosSelected()
    {
        // The base coworker prefab has no type, so nothing to draw.
        if (tuning == null)
        {
            return;
        }

        // Solid red briefly after each swing, outline otherwise.
        bool flashing = Time.time - lastAttackTime < AttackFlashDuration;

        Gizmos.color = flashing ? Color.red : new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, tuning.AttackRange);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, tuning.AttackExitRange);
    }
}
