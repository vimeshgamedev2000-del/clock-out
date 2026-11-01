using UnityEngine;

/// <summary>
/// Clock and timesheet numbers (GDD §5.5, §8): when a floor starts, how fast its
/// clock runs, and the rules that pick the timesheet title. Read by
/// CO_GameClock_Script and CO_LevelFlow_Script. Times are on a 24-hour clock.
/// </summary>
[CreateAssetMenu(fileName = "CO_ClockTuning_Data", menuName = "Clock Out/Clock Tuning")]
public class CO_ClockTuning_Script : ScriptableObject
{
    [Header("Clock")]
    [Tooltip("Hour the floor starts at, 24-hour (17 = 5 PM).")]
    [Range(0, 23)]
    [SerializeField] private int startHour = 17;

    [Range(0, 59)]
    [SerializeField] private int startMinute = 0;

    [Tooltip("Game seconds that pass per real second (10 = a 3-minute floor ends around 5:30).")]
    [Min(0f)]
    [SerializeField] private float gameSecondsPerRealSecond = 10f;

    [Header("Titles, checked top to bottom")]
    [Tooltip("\"Quiet Quitter\": left before this hour, 24-hour.")]
    [Range(0, 23)]
    [SerializeField] private int quietQuitterHour = 17;

    [Range(0, 59)]
    [SerializeField] private int quietQuitterMinute = 15;

    [Tooltip("\"Team Player\": caught at least this many times.")]
    [Min(1)]
    [SerializeField] private int teamPlayerCatches = 3;

    [Tooltip("\"Overtime Legend\": left after this hour, 24-hour.")]
    [Range(0, 23)]
    [SerializeField] private int overtimeHour = 18;

    [Range(0, 59)]
    [SerializeField] private int overtimeMinute = 0;

    /// <summary>Clock time the floor starts at, in seconds after midnight.</summary>
    public float StartSeconds => ToSeconds(startHour, startMinute);

    public float GameSecondsPerRealSecond => gameSecondsPerRealSecond;

    /// <summary>Leaving before this (seconds after midnight) is "Quiet Quitter".</summary>
    public float QuietQuitterBeforeSeconds => ToSeconds(quietQuitterHour, quietQuitterMinute);

    public int TeamPlayerCatches => teamPlayerCatches;

    /// <summary>Leaving after this (seconds after midnight) is "Overtime Legend".</summary>
    public float OvertimeAfterSeconds => ToSeconds(overtimeHour, overtimeMinute);

    private static float ToSeconds(int hour, int minute)
    {
        return hour * 3600f + minute * 60f;
    }
}
