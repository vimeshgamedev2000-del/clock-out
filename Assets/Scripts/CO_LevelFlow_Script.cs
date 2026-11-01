using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// How a floor ends. For now only the failure (GDD §3, §5.7): when the work stack
/// hits the limit, time stops, the "You Live Here Now" overlay fades in and the
/// floor reloads. The real ending screen is Checklist item 184; the timesheet win
/// is T-03.
/// </summary>
public class CO_LevelFlow_Script : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player's work stack.")]
    [SerializeField] private CO_WorkStack_Script stack;

    [Tooltip("The \"You Live Here Now\" overlay. Starts hidden (alpha 0).")]
    [SerializeField] private CanvasGroup endingOverlay;

    [Header("Timing")]
    [Tooltip("Seconds the overlay takes to fade in, in real time.")]
    [SerializeField] private float fadeTime = 0.6f;

    [Tooltip("Seconds from the ending to the reload, in real time.")]
    [SerializeField] private float reloadDelay = 3f;

    /// <summary>True once the floor has ended.</summary>
    public bool HasEnded { get; private set; }

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

    private void OnBuried()
    {
        if (HasEnded)
        {
            return;
        }

        HasEnded = true;
        Debug.Log($"{name}: You Live Here Now.", this);

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
