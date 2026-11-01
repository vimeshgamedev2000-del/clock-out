using TMPro;
using UnityEngine;

/// <summary>
/// Shows objective progress on screen. Read-only: it observes CO_ObjectiveTracker_Script
/// and never influences it, in the same way CO_EnemyStateIndicator_Script observes CO_EnemyAI_Script.
/// </summary>
public class CO_ObjectiveHUD_Script : MonoBehaviour
{
    [Header("Label")]
    [Tooltip("Text showing the objective. Defaults to the text on this object.")]
    [SerializeField] private TMP_Text label;

    [Header("Wording")]
    [Tooltip("Shown while power sources are still missing. {0} is the number " +
             "collected, {1} the number needed.")]
    [SerializeField] private string progressFormat = "POWER SOURCES {0}/{1}";

    [Tooltip("Shown once every power source has been collected.")]
    [SerializeField] private string completeText = "MAIN DOOR UNLOCKED";

    [Header("Colour")]
    [SerializeField] private Color progressColour = new Color(0.95f, 0.8f, 0.2f);
    [SerializeField] private Color completeColour = new Color(0.3f, 0.9f, 1f);

    private CO_ObjectiveTracker_Script tracker;

    private void Awake()
    {
        if (label == null)
        {
            label = GetComponent<TMP_Text>();
        }

        if (label == null)
        {
            Debug.LogWarning($"{name}: no TMP_Text to write the objective into.", this);
        }
    }

    private void OnEnable()
    {
        // Awake order between separate objects is arbitrary, so the tracker may
        // not have set Instance yet. Try now, and keep trying in Update until it
        // appears, rather than giving up and leaving the label blank forever.
        TryBind();
    }

    private void OnDisable()
    {
        if (tracker != null)
        {
            tracker.ProgressChanged -= Refresh;
            tracker = null;
        }
    }

    private void Update()
    {
        // Cheap null check in the common case: once bound, there is nothing to do
        // here and the HUD runs purely off the event.
        if (tracker == null)
        {
            TryBind();
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
        tracker.ProgressChanged += Refresh;

        // Draw immediately as well as on change, so a HUD that binds after the
        // tracker's opening broadcast still starts on the right numbers.
        Refresh();
    }

    private void Refresh()
    {
        if (label == null || tracker == null)
        {
            return;
        }

        bool complete = tracker.IsComplete;

        label.text = complete
            ? completeText
            : string.Format(progressFormat, tracker.CollectedCount, tracker.RequiredCount);

        label.color = complete ? completeColour : progressColour;
    }
}
