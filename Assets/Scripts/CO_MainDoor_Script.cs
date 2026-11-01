using UnityEngine;

/// <summary>
/// The level's exit. Stays shut until the CO_ObjectiveTracker_Script reports every power
/// source collected, then slides open when the player walks up to it. Listens to
/// the tracker rather than counting anything itself.
/// </summary>
public class CO_MainDoor_Script : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Part of the door that slides. Defaults to this object.")]
    [SerializeField] private Transform movingPart;

    [Tooltip("Where the moving part travels to when open, relative to its closed " +
             "position and in its own local space. Down sinks it into the floor.")]
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 3f, 0f);

    [Tooltip("Units travelled per second while opening.")]
    [SerializeField] private float openSpeed = 2f;

    [Header("Trigger")]
    [Tooltip("How close the player must be for an unlocked door to open.")]
    [SerializeField] private float openDistance = 3f;

    [Tooltip("Tag identifying the player object.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Feedback")]
    [Tooltip("Renderer tinted to show locked and unlocked. Optional.")]
    [SerializeField] private Renderer targetRenderer;

    [SerializeField] private Color lockedColour = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private Color unlockedColour = new Color(0.3f, 0.9f, 1f);

    private CO_ObjectiveTracker_Script tracker;
    private Transform player;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private MaterialPropertyBlock propertyBlock;

    // URP lit shaders use _BaseColor rather than the built-in _Color, matching
    // CO_EnemyStateIndicator_Script.
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    /// <summary>True once the objective is complete and the door will open.</summary>
    public bool IsUnlocked { get; private set; }

    private void Awake()
    {
        if (movingPart == null)
        {
            movingPart = transform;
        }

        closedPosition = movingPart.localPosition;
        openPosition = closedPosition + openOffset;

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        propertyBlock = new MaterialPropertyBlock();

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogWarning($"{name}: no object tagged '{playerTag}', so the door cannot detect an approach.", this);
        }
    }

    private void OnEnable()
    {
        // Awake order between separate objects is arbitrary, so the tracker may
        // not have set Instance yet. Try now, and keep trying in Update until it
        // appears, rather than silently never subscribing and never unlocking.
        TryBind();

        Tint(IsUnlocked ? unlockedColour : lockedColour);
    }

    private void OnDisable()
    {
        if (tracker != null)
        {
            tracker.ObjectiveCompleted -= Unlock;
            tracker = null;
        }
    }

    /// <summary>
    /// Subscribes to the tracker if one exists yet. Safe to call repeatedly; it
    /// does nothing until the tracker is up, then binds exactly once.
    /// </summary>
    private void TryBind()
    {
        CO_ObjectiveTracker_Script found = CO_ObjectiveTracker_Script.Instance;
        if (found == null)
        {
            return;
        }

        tracker = found;
        tracker.ObjectiveCompleted += Unlock;

        // The objective may already have completed before this door bound to the
        // tracker, in which case the event has been and gone.
        if (tracker.IsComplete)
        {
            Unlock();
        }
    }

    private void Update()
    {
        // Cheap null check in the common case: once bound, the door runs purely
        // off the completion event.
        if (tracker == null)
        {
            TryBind();
        }

        // Slide toward whichever position the current state calls for. Driving
        // both directions through one call keeps the door reversible if it is
        // ever locked again mid-motion.
        Vector3 target = ShouldBeOpen ? openPosition : closedPosition;

        movingPart.localPosition = Vector3.MoveTowards(
            movingPart.localPosition, target, openSpeed * Time.deltaTime);
    }

    /// <summary>
    /// True when the door is unlocked and the player is standing close enough.
    /// Flat distance, so a door on a raised threshold still counts the player as
    /// being in front of it.
    /// </summary>
    private bool ShouldBeOpen
    {
        get
        {
            if (!IsUnlocked || player == null)
            {
                return false;
            }

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;

            return toPlayer.sqrMagnitude <= openDistance * openDistance;
        }
    }

    /// <summary>
    /// Marks the door as openable. Idempotent, because it is reachable both from
    /// the completion event and from the catch-up check in OnEnable.
    /// </summary>
    private void Unlock()
    {
        if (IsUnlocked)
        {
            return;
        }

        IsUnlocked = true;
        Debug.Log($"{name}: unlocked.", this);

        Tint(unlockedColour);
    }

    private void Tint(Color colour)
    {
        if (targetRenderer == null)
        {
            return;
        }

        // A property block tints this one renderer without creating a material
        // instance, so the shared material is left untouched.
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, colour);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, openDistance);

        // Show where the door will travel to, so openOffset can be judged
        // without entering play mode. Measured from the closed position rather
        // than the live one, so the preview holds still while the door slides.
        Transform part = movingPart != null ? movingPart : transform;
        Vector3 closedLocal = Application.isPlaying ? closedPosition : part.localPosition;

        Vector3 from = part.parent != null
            ? part.parent.TransformPoint(closedLocal)
            : closedLocal;
        Vector3 to = part.parent != null
            ? part.parent.TransformPoint(closedLocal + openOffset)
            : closedLocal + openOffset;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(from, to);
        Gizmos.DrawWireSphere(to, 0.2f);
    }
}
