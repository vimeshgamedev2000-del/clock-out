using System;
using UnityEngine;

/// <summary>
/// Counts the power sources the player has collected and announces when the
/// objective is complete. The single source of truth for objective progress:
/// pickups report into it, and the door and HUD read back out of it, so neither
/// of those has to know the other exists.
/// </summary>
public class CO_ObjectiveTracker_Script : MonoBehaviour
{
    [Header("Objective")]
    [Tooltip("How many power sources are needed to unlock the main door. " +
             "Left at 0, it counts the power sources present in the scene on start.")]
    [SerializeField] private int requiredCount;

    /// <summary>
    /// The tracker in the scene. Pickups find it through this rather than a
    /// serialized reference, so dropping a new power source into the level needs
    /// no wiring in the inspector.
    /// </summary>
    public static CO_ObjectiveTracker_Script Instance { get; private set; }

    /// <summary>Power sources collected so far.</summary>
    public int CollectedCount { get; private set; }

    /// <summary>Power sources needed in total.</summary>
    public int RequiredCount => requiredCount;

    /// <summary>True once every power source has been collected.</summary>
    public bool IsComplete => CollectedCount >= requiredCount;

    /// <summary>
    /// Raised on every collection and once more when the objective completes, so
    /// a listener that only redraws on change still lands on the final numbers.
    /// </summary>
    public event Action ProgressChanged;

    /// <summary>Raised once, the moment the last power source is collected.</summary>
    public event Action ObjectiveCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Two trackers would split the count between them and neither would
            // ever complete. Keep the first and disable the newcomer loudly.
            Debug.LogWarning($"{name}: another CO_ObjectiveTracker_Script already exists; disabling this one.", this);
            enabled = false;
            return;
        }

        Instance = this;

        // Counting the scene means the required total cannot drift out of sync
        // with the number of pickups actually placed in the level.
        if (requiredCount <= 0)
        {
            requiredCount = FindObjectsByType<CO_PowerSource_Script>(FindObjectsSortMode.None).Length;
        }

        if (requiredCount <= 0)
        {
            Debug.LogWarning($"{name}: no power sources found, so the objective starts complete.", this);
        }
    }

    private void Start()
    {
        // After Awake, so listeners that subscribed in their own Awake see the
        // starting numbers without having to read them separately.
        ProgressChanged?.Invoke();

        // A level with nothing to collect is already done; announce it rather
        // than leaving the door waiting on a collection that will never come.
        if (IsComplete)
        {
            ObjectiveCompleted?.Invoke();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Records one collected power source. Called by CO_PowerSource_Script as it is picked
    /// up; ignored once the objective is already complete, so a stray extra
    /// pickup cannot push the count past the total or re-fire completion.
    /// </summary>
    public void CollectPowerSource(CO_PowerSource_Script source)
    {
        if (IsComplete)
        {
            Debug.LogWarning($"{name}: power source collected after the objective was already complete.", this);
            return;
        }

        CollectedCount++;
        Debug.Log($"{name}: power source {CollectedCount}/{requiredCount} collected.",
            source != null ? source : this);

        ProgressChanged?.Invoke();

        if (IsComplete)
        {
            Debug.Log($"{name}: all power sources collected - main door unlocked.", this);
            ObjectiveCompleted?.Invoke();
        }
    }
}
