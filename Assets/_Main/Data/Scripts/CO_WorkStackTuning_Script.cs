using UnityEngine;

/// <summary>
/// Work stack numbers (GDD §5.7): what each folder costs the player, when the stack
/// gets loud and clumsy, and when it ends the run. Read by CO_WorkStack_Script.
/// How many folders a catch hands over belongs to the coworker, not to this asset.
/// </summary>
[CreateAssetMenu(fileName = "CO_WorkStackTuning_Data", menuName = "Clock Out/Work Stack Tuning")]
public class CO_WorkStackTuning_Script : ScriptableObject
{
    [Header("Limit")]
    [Tooltip("Folders that end the run (\"You Live Here Now\").")]
    [Min(1)]
    [SerializeField] private int maxFolders = 10;

    [Header("Speed")]
    [Tooltip("Move speed lost per folder carried, as a fraction (0.05 = 5%).")]
    [Range(0f, 1f)]
    [SerializeField] private float speedPenaltyPerFolder = 0.05f;

    [Tooltip("Most move speed the stack can take away, as a fraction (0.4 = 40%).")]
    [Range(0f, 1f)]
    [SerializeField] private float maxSpeedPenalty = 0.4f;

    [Header("Footsteps")]
    [Tooltip("Folders from which every step makes a noise.")]
    [Min(1)]
    [SerializeField] private int footstepFolders = 4;

    [Tooltip("Footstep noise radius before folders are added, in metres.")]
    [Min(0f)]
    [SerializeField] private float footstepBaseRadius = 3f;

    [Tooltip("Extra footstep noise radius per folder carried, in metres.")]
    [Min(0f)]
    [SerializeField] private float footstepRadiusPerFolder = 0.5f;

    [Tooltip("Metres walked per step.")]
    [Min(0.1f)]
    [SerializeField] private float stepLength = 0.8f;

    [Header("Sway and drops")]
    [Tooltip("Folders from which the stack sways and a wall bump drops the top one.")]
    [Min(1)]
    [SerializeField] private int swayFolders = 8;

    [Tooltip("Noise radius of a dropped folder, in metres.")]
    [Min(0f)]
    [SerializeField] private float dropNoiseRadius = 6f;

    [Tooltip("Seconds after a drop before another wall bump can drop the next folder.")]
    [Min(0f)]
    [SerializeField] private float dropCooldown = 1f;

    [Header("Catch")]
    [Tooltip("Seconds after a catch during which nobody can hand the player more work.")]
    [Min(0f)]
    [SerializeField] private float graceDuration = 2f;

    public int MaxFolders => maxFolders;
    public float SpeedPenaltyPerFolder => speedPenaltyPerFolder;
    public float MaxSpeedPenalty => maxSpeedPenalty;
    public int FootstepFolders => footstepFolders;
    public float FootstepBaseRadius => footstepBaseRadius;
    public float FootstepRadiusPerFolder => footstepRadiusPerFolder;
    public float StepLength => stepLength;
    public int SwayFolders => swayFolders;
    public float DropNoiseRadius => dropNoiseRadius;
    public float DropCooldown => dropCooldown;
    public float GraceDuration => graceDuration;
}
