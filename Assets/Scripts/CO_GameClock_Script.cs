using UnityEngine;

/// <summary>
/// The floor's clock (GDD §5.5). Starts at the tuning's start time and runs at
/// gameSecondsPerRealSecond on scaled time, so it stops with Time.timeScale.
/// CO_LevelFlow_Script stops it when the floor ends; the HUD and the timesheet
/// only read it.
/// </summary>
public class CO_GameClock_Script : MonoBehaviour
{
    [Tooltip("Start time, clock speed and the timesheet title rules.")]
    [SerializeField] private CO_ClockTuning_Script tuning;

    // A float near 61200 (5 PM) only has 1/128 s steps, so adding each frame's
    // small slice to it rounds the same way every frame and the clock runs slow.
    private double gameSeconds;

    /// <summary>Clock time now, in game seconds after midnight.</summary>
    public float GameSeconds => (float)gameSeconds;

    /// <summary>False once the floor has ended.</summary>
    public bool IsRunning { get; private set; } = true;

    public CO_ClockTuning_Script Tuning => tuning;

    private void Awake()
    {
        gameSeconds = tuning.StartSeconds;
    }

    private void Update()
    {
        if (IsRunning)
        {
            gameSeconds += (double)Time.deltaTime * tuning.GameSecondsPerRealSecond;
        }
    }

    /// <summary>Freezes the clock at the current time. The floor is over.</summary>
    public void Stop()
    {
        IsRunning = false;
    }

    /// <summary>A clock time as the taskbar shows it, e.g. "5:07 PM".</summary>
    public static string Format(float gameSeconds)
    {
        int totalMinutes = Mathf.FloorToInt(gameSeconds / 60f);
        int hour24 = totalMinutes / 60 % 24;
        int minute = totalMinutes % 60;
        int hour12 = hour24 % 12 == 0 ? 12 : hour24 % 12;

        return $"{hour12}:{minute:00} {(hour24 < 12 ? "AM" : "PM")}";
    }
}
