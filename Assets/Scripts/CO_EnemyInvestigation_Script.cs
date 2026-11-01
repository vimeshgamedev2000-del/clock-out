using UnityEngine;

/// <summary>
/// Walks the enemy to a heard noise and has it look the spot over before giving
/// up on it. Owns no decision about *whether* to investigate: CO_EnemyAI_Script starts it
/// and ticks it, the same way it drives CO_EnemyPatrol_Script.
///
/// The inspection is where the distraction pays off - the enemy is stood still,
/// facing away from wherever it came from, for as long as inspectDuration lasts.
/// </summary>
[RequireComponent(typeof(CO_EnemyPatrol_Script))]
public class CO_EnemyInvestigation_Script : MonoBehaviour
{
    /// <summary>How far along an investigation is.</summary>
    private enum Phase
    {
        Travelling,
        Inspecting,
        Finished
    }

    [Header("Approach")]
    [Tooltip("Units travelled per second on the way to the noise. Between patrol " +
             "and chase speed reads as curious rather than alarmed.")]
    [SerializeField] private float investigateSpeed = 2.8f;

    [Tooltip("How close to the noise counts as having reached it. Keep above the " +
             "agent's radius, or the enemy pushes into the object it came to look at.")]
    [SerializeField] private float arriveDistance = 1.5f;

    [Tooltip("Seconds before an unreachable noise is written off, measured from " +
             "setting out. Stops a guard walking at a wall forever.")]
    [SerializeField] private float travelTimeout = 15f;

    [Header("Inspection")]
    [Tooltip("Seconds spent looking around on arrival, unless the source asks for its own.")]
    [SerializeField] private float inspectDuration = 4f;

    [Tooltip("Total width of the look-around sweep in degrees, centred on the noise.")]
    [SerializeField] private float sweepAngle = 100f;

    [Tooltip("Seconds for one full sweep across and back.")]
    [SerializeField] private float sweepPeriod = 2.5f;

    private CO_EnemyPatrol_Script movement;
    private CO_INoiseSource_Script source;
    private Phase phase = Phase.Finished;
    private float travelDeadline;
    private float inspectEndTime;

    // Direction the sweep is centred on: the way the enemy was facing the noise
    // when it arrived, so it looks around the spot rather than around itself.
    private Vector3 sweepCentre = Vector3.forward;

    /// <summary>True once the enemy has looked the spot over, or given up on reaching it.</summary>
    public bool IsComplete => phase == Phase.Finished;

    /// <summary>True while the enemy is stood at the noise looking around.</summary>
    public bool IsInspecting => phase == Phase.Inspecting;

    private void Awake()
    {
        movement = GetComponent<CO_EnemyPatrol_Script>();
    }

    /// <summary>
    /// Starts a fresh investigation. The source is remembered so it can be told
    /// it was found once the inspection finishes.
    /// </summary>
    public void Begin(CO_INoiseSource_Script noiseSource)
    {
        source = noiseSource;
        phase = Phase.Travelling;
        travelDeadline = Time.time + travelTimeout;
    }

    /// <summary>
    /// Runs one frame of the investigation. The position is passed in each frame
    /// rather than stored, so a source that is still making noise keeps pulling
    /// the enemy toward wherever it is now. Ignored once the enemy has arrived.
    /// </summary>
    public void Tick(Vector3 noisePosition)
    {
        switch (phase)
        {
            case Phase.Travelling:
                Travel(noisePosition);
                break;

            case Phase.Inspecting:
                Inspect();
                break;
        }
    }

    /// <summary>
    /// Abandons the investigation without telling the source. Called when the
    /// player is spotted, which outranks anything the enemy merely heard.
    /// </summary>
    public void Cancel()
    {
        source = null;
        phase = Phase.Finished;
    }

    private void Travel(Vector3 noisePosition)
    {
        Vector3 target = movement.FlattenToOwnHeight(noisePosition);

        if (Vector3.Distance(transform.position, target) <= arriveDistance)
        {
            Arrive(target);
            return;
        }

        if (Time.time >= travelDeadline)
        {
            Debug.Log($"{name}: could not reach the noise, giving up.", this);
            Finish(reachedSource: false);
            return;
        }

        movement.MoveTowards(target, investigateSpeed);
    }

    private void Arrive(Vector3 target)
    {
        phase = Phase.Inspecting;
        float duration = source != null && source.InspectDuration > 0f ? source.InspectDuration : inspectDuration;
        inspectEndTime = Time.time + duration;

        // Centre the sweep on the noise itself. Standing exactly on it leaves no
        // direction to measure, so keep the current facing in that case.
        Vector3 toNoise = target - transform.position;
        toNoise.y = 0f;
        sweepCentre = toNoise.sqrMagnitude > 0.0001f ? toNoise.normalized : transform.forward;

        Debug.Log($"{name}: reached the noise, looking around.", this);
    }

    private void Inspect()
    {
        // FaceTowards halts the agent as well as turning, so the enemy stands
        // still for the whole inspection instead of creeping onto the object.
        movement.FaceTowards(SweepLookPoint());

        if (Time.time >= inspectEndTime)
        {
            Finish(reachedSource: true);
        }
    }

    /// <summary>
    /// A point to the left or right of the noise, swinging back and forth. Sine
    /// rather than a ping-pong so the head eases at the ends of the sweep
    /// instead of snapping back.
    /// </summary>
    private Vector3 SweepLookPoint()
    {
        float phaseAngle = sweepPeriod > 0f ? Time.time * Mathf.PI * 2f / sweepPeriod : 0f;
        float offset = Mathf.Sin(phaseAngle) * sweepAngle * 0.5f;

        Vector3 direction = Quaternion.Euler(0f, offset, 0f) * sweepCentre;
        return transform.position + direction;
    }

    private void Finish(bool reachedSource)
    {
        phase = Phase.Finished;

        // Only tell the source it was found if the enemy actually got there; a
        // radio should keep playing for a guard that never arrived.
        if (reachedSource && source != null)
        {
            source.OnInvestigated(gameObject);
        }

        source = null;
    }

    private void OnValidate()
    {
        arriveDistance = Mathf.Max(arriveDistance, 0.1f);
        sweepPeriod = Mathf.Max(sweepPeriod, 0f);
    }
}
