using UnityEngine;

/// <summary>
/// The enemy's ear. Receives noises from the CO_NoiseManager_Script and remembers the last
/// one until it has been dealt with. Holds state only: CO_EnemyAI_Script reads it to pick a
/// state, and CO_EnemyInvestigation_Script does the walking.
///
/// Hearing is deliberately blunt - standing inside a noise's radius means you
/// heard it. Loudness belongs to whatever made the sound, so the radius drawn
/// around a radio in the scene is exactly who reacts to it.
/// </summary>
public class CO_EnemyHearing_Script : MonoBehaviour
{
    // The glance at a sound another enemy was sent to is standDownDelay; after it
    // that sound is ignored while it keeps going, so a radio left playing costs
    // one glance. alertDuration is for a sound nobody could be sent to at all.
    [Header("Reaction")]
    [Tooltip("This coworker type's numbers: the glance (stand-down) and alert times.")]
    [SerializeField] private CO_GuardTuning_Script tuning;

    /// <summary>
    /// How long a dismissed sound stays dismissed once it has gone quiet. The
    /// memory is refreshed while the sound keeps going, so it only lapses after
    /// the radio actually stops - at which point switching it on again is news
    /// worth looking up for.
    /// </summary>
    private const float DismissalMemory = 3f;

    private CO_EnemyDetection_Script detection;
    private float alertExpiryTime;

    // What this enemy has already looked at and left to somebody else, so the
    // next pulse of the same sound can pass without it stopping again.
    private CO_INoiseSource_Script dismissedSource;
    private float dismissedMemoryExpiry;

    // True while the outstanding noise is a glance at something another enemy
    // is dealing with, rather than an alert nobody was sent to.
    private bool standingDown;

    /// <summary>True while a heard noise is still outstanding.</summary>
    public bool HasNoise { get; private set; }

    /// <summary>True when this enemy is the one going to look at the noise.</summary>
    public bool IsInvestigator { get; private set; }

    /// <summary>Where the outstanding noise was last heard.</summary>
    public Vector3 NoisePosition { get; private set; }

    /// <summary>What made the outstanding noise. Null for a sound with no object behind it.</summary>
    public CO_INoiseSource_Script NoiseSource { get; private set; }

    /// <summary>
    /// False while the enemy has the player in sight: a guard already chasing
    /// someone is not going to wander off to check a radio.
    /// </summary>
    public bool CanInvestigate => detection == null || !detection.HasDetectedPlayer;

    private void Awake()
    {
        detection = GetComponent<CO_EnemyDetection_Script>();
    }

    private void OnEnable()
    {
        CO_NoiseManager_Script.Register(this);
    }

    private void OnDisable()
    {
        CO_NoiseManager_Script.Unregister(this);
        ClearNoise();
    }

    private void Update()
    {
        // Only the short alert glance expires on a timer. An investigation ends
        // when the enemy has actually been over to look, which CO_EnemyInvestigation_Script
        // reports by way of CO_EnemyAI_Script.
        if (HasNoise && !IsInvestigator && Time.time >= alertExpiryTime)
        {
            // Remember a sound that was somebody else's to deal with, so the
            // rest of its pulses go unnoticed. An alert nobody was sent to is
            // not remembered: that one is still unexplained.
            if (standingDown)
            {
                dismissedSource = NoiseSource;
                dismissedMemoryExpiry = Time.time + DismissalMemory;
            }

            ClearNoise();
        }
    }

    /// <summary>
    /// True when this noise reaches the enemy. The audible range is the smaller
    /// of the noise's own radius and this enemy's hearing, cut further when a
    /// wall stands between the two.
    /// </summary>
    public bool CanHear(CO_NoiseEvent_Script noise)
    {
        return Vector3.Distance(transform.position, noise.Position) <= noise.Radius;
    }

    /// <summary>True when the outstanding investigation is of this exact source.</summary>
    public bool IsInvestigating(CO_INoiseSource_Script source)
    {
        return HasNoise && IsInvestigator && NoiseSource == source;
    }

    /// <summary>
    /// Takes in a noise the enemy can hear. Called by the CO_NoiseManager_Script, which
    /// has already decided which single listener investigates.
    /// </summary>
    public void HearNoise(CO_NoiseEvent_Script noise, bool investigate)
    {
        // Already walking to something else, and this noise was handed to another
        // guard. Taking it anyway would overwrite the destination without giving
        // up the investigator role, which puts two enemies on the same spot and
        // leaves the first noise with nobody going to look at it.
        if (IsInvestigator && !investigate)
        {
            return;
        }

        bool isNew = !HasNoise;
        bool promoted = investigate && !IsInvestigator;

        HasNoise = true;
        NoisePosition = noise.Position;
        NoiseSource = noise.Source;

        // Being sent to a sound, or left alert by one nobody could be sent to,
        // both outrank having dismissed it earlier.
        standingDown = false;
        dismissedSource = null;

        // Never demoted by a later pulse: an enemy mid-walk keeps the job until
        // it finishes or sees the player, which is what ClearNoise is for.
        IsInvestigator |= investigate;

        // Refreshed on every pulse, so a guard left alert beside a radio nobody
        // could be sent to does not drift back to patrolling underneath it.
        alertExpiryTime = Time.time + tuning.AlertDuration;

        if (isNew || promoted)
        {
            string role = IsInvestigator ? "investigating" : "alerted";
            Debug.Log($"{name}: heard a noise, {role}.", this);
        }
    }

    /// <summary>
    /// Takes in a noise another enemy has already been sent to. This one looks
    /// that way for a moment and then gets on with its route: a distraction is
    /// meant to cost one guard, not every guard who happened to hear it.
    /// </summary>
    public void HearNoiseHandledByAnother(CO_NoiseEvent_Script noise)
    {
        // Already walking to a noise of its own, which outranks glancing at this.
        if (IsInvestigator)
        {
            return;
        }

        // Already had a look at this one and left it to somebody else. A radio
        // pulsing away is the same sound it just dismissed, not a fresh one, so
        // it walks on rather than stopping every second until the radio is found.
        if (HasDismissed(noise.Source))
        {
            dismissedMemoryExpiry = Time.time + DismissalMemory;
            return;
        }

        float deadline = Time.time + tuning.StandDownDelay;

        if (!HasNoise)
        {
            HasNoise = true;
            NoisePosition = noise.Position;
            NoiseSource = noise.Source;
            alertExpiryTime = deadline;
            standingDown = true;

            Debug.Log($"{name}: heard a noise, another enemy is checking it.", this);
            return;
        }

        // Holding a different sound: that one is still worth standing up for.
        if (NoiseSource != noise.Source)
        {
            return;
        }

        // Shortened but never extended. A guard left standing because nobody
        // could be sent gets released as soon as somebody is, and a radio that
        // keeps pulsing cannot hold anyone who is not going to it.
        alertExpiryTime = Mathf.Min(alertExpiryTime, deadline);
    }

    /// <summary>
    /// Drops the outstanding noise. Called when the alert glance expires, when
    /// the investigation finishes, and when the player is spotted - seeing
    /// someone outranks having heard something.
    /// </summary>
    public void ClearNoise()
    {
        HasNoise = false;
        IsInvestigator = false;
        NoiseSource = null;
        standingDown = false;
    }

    /// <summary>
    /// True for a sound this enemy has already looked at and left to another,
    /// for as long as that sound keeps going.
    /// </summary>
    private bool HasDismissed(CO_INoiseSource_Script source)
    {
        return source != null
            && ReferenceEquals(source, dismissedSource)
            && Time.time < dismissedMemoryExpiry;
    }

    private void OnDrawGizmosSelected()
    {
        // No range ring to draw: the noise owns the radius, and the source draws
        // it. This only shows what the enemy is currently holding.
        if (HasNoise)
        {
            Gizmos.color = IsInvestigator ? new Color(0.7f, 0.4f, 1f) : new Color(0.3f, 0.8f, 1f);
            Gizmos.DrawLine(transform.position + Vector3.up, NoisePosition);
            Gizmos.DrawWireSphere(NoisePosition, 0.4f);
        }
    }
}
