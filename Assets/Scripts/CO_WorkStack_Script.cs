using System;
using UnityEngine;

/// <summary>
/// The folders the player carries on their head (GDD §5.7). A catch hands work
/// over; the shredder, the intern's desk, a reply-all and wall bumps take it away. The stack
/// slows the player, makes their steps loud from 4 folders, drops a folder on a
/// wall bump from 8, and ends the run at 10.
///
/// It is also the noise source for those steps and drops, so the noise routing
/// keeps one investigator on the player's trail instead of picking a new guard
/// every step.
/// </summary>
public class CO_WorkStack_Script : MonoBehaviour, CO_INoiseSource_Script
{
    [Tooltip("Speed penalty, footsteps, sway and limits.")]
    [SerializeField] private CO_WorkStackTuning_Script tuning;

    [Tooltip("Layers that count as walls for dropping a folder: the level's Obstacle layer.")]
    [SerializeField] private LayerMask wallMask = 1 << 6;

    private Vector3 lastPosition;
    private float distanceSinceStep;
    private float graceEndTime;
    private float nextDropTime;

    /// <summary>A frame's movement above this is a teleport (spawn, respawn), not walking.</summary>
    private const float MaxStepPerFrame = 2f;

    /// <summary>Folders carried now.</summary>
    public int Count { get; private set; }

    /// <summary>Folders that end the run.</summary>
    public int MaxFolders => tuning.MaxFolders;

    /// <summary>True once the stack reached the limit. The run is over.</summary>
    public bool IsBuried { get; private set; }

    /// <summary>True when the stack is heavy enough that every step makes a noise.</summary>
    public bool IsLoud => Count >= tuning.FootstepFolders;

    /// <summary>Folders from which the stack sways and drops folders on a wall bump.</summary>
    public int SwayFolders => tuning.SwayFolders;

    /// <summary>True when the stack is tall enough to sway and drop folders.</summary>
    public bool IsSwaying => Count >= tuning.SwayFolders;

    /// <summary>False during the grace after a catch, so one bad moment can't chain into more.</summary>
    public bool CanBeHandedWork => !IsBuried && Time.time >= graceEndTime;

    /// <summary>Multiplier for the player's move speed, 1 with an empty stack.</summary>
    public float SpeedMultiplier =>
        1f - Mathf.Min(Count * tuning.SpeedPenaltyPerFolder, tuning.MaxSpeedPenalty);

    /// <summary>Folders handed over by catches this floor, for the timesheet.</summary>
    public int FoldersReceived { get; private set; }

    /// <summary>Catches that landed this floor ("tasks taken" on the timesheet). A Boss catch counts once.</summary>
    public int CatchCount { get; private set; }

    /// <summary>True once the player used a reply-all this floor. It works once per floor, and the timesheet marks it.</summary>
    public bool HasRepliedAll { get; private set; }

    /// <summary>True while a reply-all would forward something.</summary>
    public bool CanReplyAll => !HasRepliedAll && !IsBuried && Count > 0;

    /// <summary>Raised with the previous and new count whenever folders are added or removed.</summary>
    public event Action<int, int> FoldersChanged;

    /// <summary>Raised just before the count drops because a wall bump knocked a folder off.</summary>
    public event Action FolderDropped;

    /// <summary>Raised once when the stack reaches the limit.</summary>
    public event Action Buried;

    public Transform Transform => transform;

    private void OnEnable()
    {
        lastPosition = transform.position;
        distanceSinceStep = 0f;
    }

    private void Update()
    {
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;
        delta.y = 0f;

        if (!IsLoud || IsBuried)
        {
            distanceSinceStep = 0f;
            return;
        }

        float moved = delta.magnitude;
        if (moved > MaxStepPerFrame)
        {
            return;
        }

        distanceSinceStep += moved;
        if (distanceSinceStep >= tuning.StepLength)
        {
            distanceSinceStep -= tuning.StepLength;
            float radius = tuning.FootstepBaseRadius + tuning.FootstepRadiusPerFolder * Count;
            CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(transform.position, radius, this));
        }
    }

    /// <summary>
    /// A coworker hands over work. Refused during the grace after the last catch
    /// and once the run is over; returns whether it was taken.
    /// </summary>
    public bool TryHandWork(int folders, GameObject from)
    {
        if (!CanBeHandedWork || folders <= 0)
        {
            return false;
        }

        int previous = Count;
        Count = Mathf.Min(Count + folders, tuning.MaxFolders);
        FoldersReceived += Count - previous;
        CatchCount++;
        graceEndTime = Time.time + tuning.GraceDuration;

        Debug.Log($"{name}: {from.name} handed over {folders} folder(s) ({Count}/{tuning.MaxFolders}).", this);
        FoldersChanged?.Invoke(previous, Count);

        if (Count >= tuning.MaxFolders)
        {
            IsBuried = true;
            Debug.Log($"{name}: buried in work.", this);
            Buried?.Invoke();
        }

        return true;
    }

    /// <summary>Takes the top folder away (shredder, intern). Returns false with an empty stack.</summary>
    public bool TryTakeFolder()
    {
        if (Count == 0 || IsBuried)
        {
            return false;
        }

        RemoveTopFolder();
        return true;
    }

    /// <summary>
    /// Forwards the whole stack at once (reply-all PC). Works once per floor, whichever
    /// PC is used; returns the folders forwarded, 0 when it was refused.
    /// </summary>
    public int TryReplyAll()
    {
        if (!CanReplyAll)
        {
            return 0;
        }

        int previous = Count;
        Count = 0;
        HasRepliedAll = true;

        Debug.Log($"{name}: replied all and forwarded {previous} folder(s).", this);
        FoldersChanged?.Invoke(previous, Count);
        return previous;
    }

    /// <summary>Nobody can switch a stack off: the coworker had a look and goes back.</summary>
    public void OnInvestigated(GameObject investigator)
    {
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!IsSwaying || IsBuried || Time.time < nextDropTime)
        {
            return;
        }

        // Floors and ramps touch the controller every frame; only a side-on hit is a bump.
        if (Mathf.Abs(hit.normal.y) > 0.5f || (wallMask.value & (1 << hit.gameObject.layer)) == 0)
        {
            return;
        }

        nextDropTime = Time.time + tuning.DropCooldown;
        Debug.Log($"{name}: bumped {hit.gameObject.name} and dropped a folder.", this);

        FolderDropped?.Invoke();
        RemoveTopFolder();
        CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(transform.position, tuning.DropNoiseRadius, this));
    }

    private void RemoveTopFolder()
    {
        int previous = Count;
        Count--;
        FoldersChanged?.Invoke(previous, Count);
    }

    private void OnDrawGizmosSelected()
    {
        if (tuning == null || Count < tuning.FootstepFolders)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, tuning.FootstepBaseRadius + tuning.FootstepRadiusPerFolder * Count);
    }
}
