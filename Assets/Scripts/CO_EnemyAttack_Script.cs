using UnityEngine;

/// <summary>
/// Placeholder attack: decides when the enemy is close enough to strike and
/// paces the strikes on a cooldown. Deals no damage - it logs and flashes a
/// gizmo, which is enough to demonstrate the timing.
/// </summary>
public class CO_EnemyAttack_Script : MonoBehaviour
{
    [Header("Range")]
    [Tooltip("Distance at which the enemy stops and starts attacking.")]
    [SerializeField] private float attackRange = 2f;

    [Tooltip("Distance the player must exceed before the enemy resumes chasing. " +
             "Kept above attack range so the state does not flicker at the boundary.")]
    [SerializeField] private float attackExitRange = 2.5f;

    [Header("Timing")]
    [Tooltip("Seconds between attacks.")]
    [SerializeField] private float attackCooldown = 1f;

    private float nextAttackTime;
    private float lastAttackTime = float.NegativeInfinity;

    /// <summary>How long the gizmo stays lit after a swing.</summary>
    private const float AttackFlashDuration = 0.15f;

    private void OnValidate()
    {
        // A smaller exit range than attack range would invert the hysteresis and
        // reintroduce the flicker this is meant to prevent.
        if (attackExitRange < attackRange)
        {
            attackExitRange = attackRange;
        }
    }

    /// <summary>
    /// True when the player is close enough to start attacking. Uses the tighter
    /// range, so the enemy must close properly before the first swing.
    /// </summary>
    public bool IsInAttackRange(Vector3 targetPosition)
    {
        return FlatDistanceTo(targetPosition) <= attackRange;
    }

    /// <summary>
    /// True when an already-attacking enemy should keep attacking. Uses the wider
    /// range so small movements near the edge do not bounce it back into chasing.
    /// </summary>
    public bool ShouldStayInAttackRange(Vector3 targetPosition)
    {
        return FlatDistanceTo(targetPosition) <= attackExitRange;
    }

    /// <summary>
    /// Attacks if the cooldown has elapsed. Safe to call every frame; it paces
    /// itself.
    /// </summary>
    public void TryAttack()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        lastAttackTime = Time.time;

        // Placeholder for a real attack: swap for damage, animation and VFX.
        Debug.Log($"{name}: attack!", this);
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
        // Solid red briefly after each swing, outline otherwise.
        bool flashing = Time.time - lastAttackTime < AttackFlashDuration;

        Gizmos.color = flashing ? Color.red : new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, attackExitRange);
    }
}
