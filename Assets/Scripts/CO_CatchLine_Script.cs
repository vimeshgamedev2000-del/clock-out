using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// The coworker's line when they hand over work ("Got a sec?", GDD §5.7, §11 #2):
/// a speech label pops over their head, holds, and fades. Read-only: it watches
/// CO_EnemyAttack_Script.LastHandOffTime and never changes the coworker.
/// </summary>
public class CO_CatchLine_Script : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The coworker's attack, which records each hand-off.")]
    [SerializeField] private CO_EnemyAttack_Script attack;

    [Tooltip("Fades the whole bubble.")]
    [SerializeField] private CanvasGroup group;

    [Tooltip("The part that pops in.")]
    [SerializeField] private RectTransform bubble;

    [SerializeField] private TMP_Text label;

    [Header("Line")]
    [Tooltip("What this coworker says when handing over work (GDD §6).")]
    [SerializeField] private string line = "Got a sec?";

    [Header("Timing")]
    [Tooltip("Seconds the pop-in takes.")]
    [SerializeField] private float appearTime = 0.2f;

    [Tooltip("Seconds the line stays up after popping in.")]
    [SerializeField] private float holdTime = 1.4f;

    [Tooltip("Seconds the fade-out takes.")]
    [SerializeField] private float hideTime = 0.3f;

    private float shownHandOff = float.NegativeInfinity;
    private Camera viewCamera;

    private void Awake()
    {
        group.alpha = 0f;
        label.text = line;
        viewCamera = Camera.main;
    }

    private void LateUpdate()
    {
        // Billboard like the state label, so the line reads square-on to the camera.
        if (viewCamera != null)
        {
            transform.rotation = viewCamera.transform.rotation;
        }

        if (attack.LastHandOffTime > shownHandOff)
        {
            shownHandOff = attack.LastHandOffTime;
            Show();
        }
    }

    private void Show()
    {
        group.DOKill();
        bubble.DOKill();

        group.alpha = 1f;
        bubble.localScale = Vector3.zero;
        bubble.DOScale(1f, appearTime).SetEase(Ease.OutBack).SetLink(gameObject);

        DOTween.To(() => group.alpha, a => group.alpha = a, 0f, hideTime)
            .SetDelay(appearTime + holdTime)
            .SetTarget(group)
            .SetLink(gameObject);
    }
}
