using TMPro;
using UnityEngine;

/// <summary>
/// The taskbar clock in the HUD (GDD §5.5, §9). Read-only: it shows
/// CO_GameClock_Script and never changes it. Rewrites the label only when the
/// shown minute changes.
/// </summary>
public class CO_ClockHUD_Script : MonoBehaviour
{
    [Tooltip("The floor's clock.")]
    [SerializeField] private CO_GameClock_Script clock;

    [Tooltip("Text showing the time. Defaults to the text on this object.")]
    [SerializeField] private TMP_Text label;

    private int shownMinute = -1;

    private void Awake()
    {
        if (label == null)
        {
            label = GetComponent<TMP_Text>();
        }
    }

    private void LateUpdate()
    {
        int minute = Mathf.FloorToInt(clock.GameSeconds / 60f);
        if (minute == shownMinute)
        {
            return;
        }

        shownMinute = minute;
        label.text = CO_GameClock_Script.Format(clock.GameSeconds);
    }
}
