using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The "?" over a guard's head that fills with its notice meter and pops to "!"
/// once the player is noticed. Read-only: it observes CO_EnemyDetection_Script
/// and never influences it.
/// </summary>
public class CO_NoticeIndicator_Script : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Sight to read. Defaults to the one on a parent.")]
    [SerializeField] private CO_EnemyDetection_Script detection;

    [Tooltip("Fades the whole badge in and out.")]
    [SerializeField] private CanvasGroup group;

    [Tooltip("Scaled for the appear and pop animations.")]
    [SerializeField] private RectTransform badge;

    [SerializeField] private Image background;

    [Tooltip("Radial-filled image that follows the notice meter.")]
    [SerializeField] private Image fill;

    [SerializeField] private TMP_Text glyph;

    [Header("Look")]
    [SerializeField] private Sprite watchingBackground;
    [SerializeField] private Sprite noticedBackground;
    [SerializeField] private Color watchingBackgroundColour = new Color(0.12f, 0.14f, 0.2f, 0.85f);
    [SerializeField] private Color noticedBackgroundColour = Color.white;

    [Header("Animation")]
    [Tooltip("Seconds for the badge to grow in when the meter starts filling.")]
    [SerializeField] private float appearTime = 0.2f;

    [Tooltip("Seconds to fade out once the meter is empty.")]
    [SerializeField] private float hideTime = 0.25f;

    [Tooltip("Extra scale added at the peak of the \"!\" pop.")]
    [SerializeField] private float popStrength = 0.6f;

    [Tooltip("Seconds the \"!\" pop lasts.")]
    [SerializeField] private float popTime = 0.35f;

    [Tooltip("Keeps the badge facing the camera so it stays readable.")]
    [SerializeField] private bool billboard = true;

    private Camera mainCamera;
    private bool isShown;
    private bool isNoticed;

    private void Awake()
    {
        if (detection == null)
        {
            detection = GetComponentInParent<CO_EnemyDetection_Script>();
        }

        mainCamera = Camera.main;

        group.alpha = 0f;
        badge.localScale = Vector3.zero;
        ShowWatching();
    }

    private void LateUpdate()
    {
        // A full meter with the player out of view is draining, not noticing.
        bool noticed = detection.HasNoticedPlayer;
        bool shouldShow = noticed || detection.NoticeLevel > 0f;

        if (shouldShow && !isShown)
        {
            Show();
        }
        else if (!shouldShow && isShown)
        {
            Hide();
        }

        if (noticed && !isNoticed)
        {
            ShowNoticed();
        }
        else if (!noticed && isNoticed)
        {
            ShowWatching();
        }

        isNoticed = noticed;
        fill.fillAmount = detection.NoticeLevel;

        if (billboard && isShown)
        {
            FaceCamera();
        }
    }

    private void Show()
    {
        isShown = true;

        // Grows from small with a slight overshoot: the anticipation before a possible "!".
        group.DOKill();
        badge.DOKill();
        DOTween.To(() => group.alpha, a => group.alpha = a, 1f, appearTime).SetTarget(group).SetLink(gameObject);
        badge.localScale = Vector3.one * 0.4f;
        badge.DOScale(1f, appearTime).SetEase(Ease.OutBack).SetLink(gameObject);
    }

    private void Hide()
    {
        isShown = false;

        group.DOKill();
        DOTween.To(() => group.alpha, a => group.alpha = a, 0f, hideTime).SetTarget(group).SetLink(gameObject);
    }

    private void ShowWatching()
    {
        background.sprite = watchingBackground;
        background.color = watchingBackgroundColour;
        fill.enabled = true;
        glyph.text = "?";
    }

    private void ShowNoticed()
    {
        background.sprite = noticedBackground;
        background.color = noticedBackgroundColour;
        fill.enabled = false;
        glyph.text = "!";

        // Finish any appear tween first so the punch starts from full size.
        badge.DOKill(true);
        badge.DOPunchScale(Vector3.one * popStrength, popTime, 6, 0.5f).SetLink(gameObject);
    }

    private void FaceCamera()
    {
        if (mainCamera == null)
        {
            // Camera.main is null if the camera is untagged or was swapped at runtime.
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        // Matches the camera's facing rather than looking at it, like the state label.
        transform.rotation = mainCamera.transform.rotation;
    }
}
