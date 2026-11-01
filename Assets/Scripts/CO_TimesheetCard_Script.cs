using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// The timesheet card that replaces the win screen (GDD §8, §11 #8). Read-only:
/// CO_LevelFlow_Script hands it the filled-in sheet and it only draws it. The card
/// slides up, the rows tick in one by one and the title stamps down. Everything
/// runs on unscaled time, because the floor has stopped.
/// </summary>
public class CO_TimesheetCard_Script : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Fades the whole screen in. Starts hidden (alpha 0).")]
    [SerializeField] private CanvasGroup group;

    [Tooltip("The card itself, slid up from below.")]
    [SerializeField] private RectTransform card;

    [Tooltip("\"TIMESHEET — <floor>\".")]
    [SerializeField] private TMP_Text header;

    [Tooltip("The rows: clocked out, tasks dodged, tasks taken, reply-all.")]
    [SerializeField] private TMP_Text rows;

    [Tooltip("The title stamp.")]
    [SerializeField] private TMP_Text stamp;

    [Tooltip("The floor's best clock-out time.")]
    [SerializeField] private TMP_Text best;

    [Header("Timing (real seconds)")]
    [SerializeField] private float fadeTime = 0.25f;
    [SerializeField] private float slideTime = 0.45f;

    [Tooltip("Seconds between two rows ticking in.")]
    [SerializeField] private float rowInterval = 0.18f;

    [Tooltip("Seconds the stamp takes to come down.")]
    [SerializeField] private float stampTime = 0.18f;

    [Header("Look")]
    [Tooltip("How far below its place the card starts, in pixels.")]
    [SerializeField] private float slideDistance = 900f;

    [Tooltip("Tilt of the title stamp, in degrees.")]
    [SerializeField] private float stampTilt = -8f;

    private Vector2 cardPosition;

    private void Awake()
    {
        cardPosition = card.anchoredPosition;
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        stamp.alpha = 0f;
    }

    /// <summary>Fills the card in and plays it.</summary>
    public void Show(CO_LevelFlow_Script.Timesheet sheet)
    {
        header.text = $"TIMESHEET — {sheet.FloorName}";
        rows.text =
            $"Clocked out<pos=62%>{CO_GameClock_Script.Format(sheet.ClockOutSeconds)}\n" +
            $"Tasks dodged<pos=62%>{sheet.TasksDodged}\n" +
            $"Tasks taken<pos=62%>{sheet.TasksTaken}\n" +
            $"Reply-all<pos=62%>{(sheet.RepliedAll ? "used" : "—")}";
        stamp.text = sheet.Title;
        best.text = sheet.IsNewBest
            ? "NEW BEST!"
            : $"Best: {CO_GameClock_Script.Format(sheet.BestSeconds)} · {sheet.BestTitle}";
        best.alpha = 0f;

        group.blocksRaycasts = true;
        group.interactable = true;
        rows.maxVisibleLines = 0;
        card.anchoredPosition = cardPosition + Vector2.down * slideDistance;
        stamp.transform.localScale = Vector3.one * 2.5f;
        stamp.transform.localRotation = Quaternion.Euler(0f, 0f, stampTilt);

        int rowCount = rows.text.Split('\n').Length;

        Sequence show = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        show.Append(DOTween.To(() => group.alpha, a => group.alpha = a, 1f, fadeTime));
        show.Join(DOTween.To(() => card.anchoredPosition, p => card.anchoredPosition = p, cardPosition, slideTime)
            .SetEase(Ease.OutBack));
        show.Append(DOTween.To(() => rows.maxVisibleLines, n => rows.maxVisibleLines = n, rowCount, rowInterval * rowCount)
            .SetEase(Ease.Linear));

        // The stamp: drops from big to its size, lands with a punch and a nudge of the card.
        show.Append(DOTween.To(() => stamp.alpha, a => stamp.alpha = a, 1f, stampTime * 0.5f));
        show.Join(stamp.transform.DOScale(1f, stampTime).SetEase(Ease.InQuad));
        show.Append(stamp.transform.DOPunchScale(Vector3.one * 0.15f, 0.25f, 6));
        show.Join(card.DOPunchPosition(new Vector3(0f, -12f, 0f), 0.25f, 8));
        show.Append(DOTween.To(() => best.alpha, a => best.alpha = a, 1f, 0.3f));
    }
}
