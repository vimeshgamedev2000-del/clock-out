using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// How a floor ends (GDD §3, §8). Win: CO_LevelExit_Script calls ClockOut, time
/// stops and the timesheet card shows the clock-out time, tasks and title.
/// Fail: when the work stack hits the limit, the "You Live Here Now" overlay fades
/// in and the floor reloads. Either way the clock stops. The real fail screen is
/// Checklist item 184.
/// </summary>
public class CO_LevelFlow_Script : MonoBehaviour
{
    /// <summary>What the timesheet card shows. Filled once, when the player clocks out.</summary>
    public struct Timesheet
    {
        public string FloorName;
        public float ClockOutSeconds;
        public int TasksDodged;
        public int TasksTaken;
        public bool RepliedAll;
        public string Title;
        public float BestSeconds;
        public string BestTitle;
        public bool IsNewBest;
    }

    [Header("References")]
    [Tooltip("The player's work stack.")]
    [SerializeField] private CO_WorkStack_Script stack;

    [Tooltip("The floor's clock. Stopped when the floor ends.")]
    [SerializeField] private CO_GameClock_Script clock;

    [Tooltip("Counts the chases the player got away from.")]
    [SerializeField] private CO_RunStats_Script stats;

    [Tooltip("The timesheet card shown on clocking out. Optional.")]
    [SerializeField] private CO_TimesheetCard_Script timesheetCard;

    [Tooltip("The \"You Live Here Now\" overlay. Starts hidden (alpha 0).")]
    [SerializeField] private CanvasGroup endingOverlay;

    [Header("Floor")]
    [Tooltip("Name on the timesheet header, e.g. \"Floor 1 · Open Plan\".")]
    [SerializeField] private string floorName = "Test Floor";

    [Header("Timing")]
    [Tooltip("Seconds the overlay takes to fade in, in real time.")]
    [SerializeField] private float fadeTime = 0.6f;

    [Tooltip("Seconds from the ending to the reload, in real time.")]
    [SerializeField] private float reloadDelay = 3f;

    /// <summary>True once the floor has ended.</summary>
    public bool HasEnded { get; private set; }

    /// <summary>True once the player clocked out (the win), not buried.</summary>
    public bool HasClockedOut { get; private set; }

    /// <summary>The card's contents, valid once HasClockedOut.</summary>
    public Timesheet LastTimesheet { get; private set; }

    /// <summary>PlayerPrefs key for this floor's best clock-out time; the title is stored under the same key + ".Title".</summary>
    public string BestKey => $"ClockOut.Best.{SceneManager.GetActiveScene().name}";

    private void Awake()
    {
        endingOverlay.alpha = 0f;
        endingOverlay.blocksRaycasts = false;
    }

    private void OnEnable()
    {
        stack.Buried += OnBuried;
    }

    private void OnDisable()
    {
        stack.Buried -= OnBuried;
    }

    private void OnDestroy()
    {
        // Never leave the next scene (or the Editor) frozen.
        if (HasEnded)
        {
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// The player stepped into the elevator: stops time and the clock, fills in the
    /// timesheet, keeps this floor's best time and shows the card. Once per floor.
    /// </summary>
    public void ClockOut()
    {
        if (HasEnded)
        {
            return;
        }

        HasEnded = true;
        HasClockedOut = true;
        clock.Stop();
        Time.timeScale = 0f;

        Timesheet sheet = new Timesheet
        {
            FloorName = floorName,
            ClockOutSeconds = clock.GameSeconds,
            TasksDodged = stats != null ? stats.TasksDodged : 0,
            TasksTaken = stack.CatchCount,
            RepliedAll = stack.HasRepliedAll,
            Title = ChooseTitle(stack.CatchCount, clock.GameSeconds, clock.Tuning),
        };

        // Earliest clock-out is the best one.
        float best = PlayerPrefs.GetFloat(BestKey, float.PositiveInfinity);
        sheet.IsNewBest = sheet.ClockOutSeconds < best;
        if (sheet.IsNewBest)
        {
            PlayerPrefs.SetFloat(BestKey, sheet.ClockOutSeconds);
            PlayerPrefs.SetString(BestKey + ".Title", sheet.Title);
            PlayerPrefs.Save();
        }

        sheet.BestSeconds = PlayerPrefs.GetFloat(BestKey);
        sheet.BestTitle = PlayerPrefs.GetString(BestKey + ".Title");
        LastTimesheet = sheet;

        Debug.Log($"{name}: clocked out at {CO_GameClock_Script.Format(sheet.ClockOutSeconds)} as {sheet.Title} " +
                  $"({sheet.TasksDodged} dodged, {sheet.TasksTaken} taken).", this);

        if (timesheetCard != null)
        {
            timesheetCard.Show(sheet);
        }
    }

    /// <summary>The timesheet's Retry button: plays the floor again from the start.</summary>
    public void Retry()
    {
        Reload();
    }

    /// <summary>
    /// The title rules of GDD §8, checked top to bottom. "Perfect Exit" belongs to
    /// the tutorial, which doesn't exist yet.
    /// </summary>
    public static string ChooseTitle(int catches, float clockOutSeconds, CO_ClockTuning_Script tuning)
    {
        if (catches == 0)
        {
            return "PROFESSIONAL GHOST";
        }

        if (clockOutSeconds < tuning.QuietQuitterBeforeSeconds)
        {
            return "QUIET QUITTER";
        }

        if (catches >= tuning.TeamPlayerCatches)
        {
            return "TEAM PLAYER";
        }

        if (clockOutSeconds > tuning.OvertimeAfterSeconds)
        {
            return "OVERTIME LEGEND";
        }

        return "NINE-TO-FIVER";
    }

    private void OnBuried()
    {
        if (HasEnded)
        {
            return;
        }

        HasEnded = true;
        Debug.Log($"{name}: You Live Here Now.", this);

        clock.Stop();
        Time.timeScale = 0f;
        endingOverlay.blocksRaycasts = true;

        // Unscaled, because time has just stopped.
        DOTween.To(() => endingOverlay.alpha, a => endingOverlay.alpha = a, 1f, fadeTime)
            .SetUpdate(true)
            .SetTarget(endingOverlay)
            .SetLink(gameObject);
        DOVirtual.DelayedCall(reloadDelay, Reload, true).SetLink(gameObject);
    }

    private void Reload()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
