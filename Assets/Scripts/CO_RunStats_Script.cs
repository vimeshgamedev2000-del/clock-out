using UnityEngine;

/// <summary>
/// Counts "tasks dodged" for the timesheet (GDD §8): +1 each time a coworker's
/// chase ends without a catch. Chasing and Attacking count as one chase, so the
/// flicker at the attack range isn't a dodge. Read-only: it watches
/// CO_EnemyAI_Script.CurrentState and CO_EnemyAttack_Script.LastHandOffTime and
/// never changes them.
/// </summary>
public class CO_RunStats_Script : MonoBehaviour
{
    private CO_EnemyAI_Script[] guards;
    private CO_EnemyAttack_Script[] attacks;
    private bool[] wasChasing;
    private float[] chaseStartTimes;

    /// <summary>Chases this floor that ended without the guard handing over work.</summary>
    public int TasksDodged { get; private set; }

    private void Start()
    {
        // Guards are placed in the level, never spawned, so one look is enough.
        guards = FindObjectsByType<CO_EnemyAI_Script>(FindObjectsSortMode.None);
        attacks = new CO_EnemyAttack_Script[guards.Length];
        wasChasing = new bool[guards.Length];
        chaseStartTimes = new float[guards.Length];

        for (int i = 0; i < guards.Length; i++)
        {
            attacks[i] = guards[i].GetComponent<CO_EnemyAttack_Script>();
        }
    }

    // LateUpdate, so every guard has picked this frame's state.
    private void LateUpdate()
    {
        for (int i = 0; i < guards.Length; i++)
        {
            if (guards[i] == null)
            {
                continue;
            }

            EnemyState state = guards[i].CurrentState;
            bool isChasing = state == EnemyState.Chasing || state == EnemyState.Attacking;

            if (isChasing && !wasChasing[i])
            {
                chaseStartTimes[i] = Time.time;
            }
            else if (!isChasing && wasChasing[i] && attacks[i].LastHandOffTime < chaseStartTimes[i])
            {
                TasksDodged++;
                Debug.Log($"{name}: got away from {guards[i].name} ({TasksDodged} dodged).", this);
            }

            wasChasing[i] = isChasing;
        }
    }
}
