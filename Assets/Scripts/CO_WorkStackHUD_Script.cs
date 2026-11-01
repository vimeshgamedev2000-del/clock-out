using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows how many folders the player carries (GDD §9 HUD, §11 #2): the count
/// punches when it changes and turns warmer as the stack gets loud, then clumsy.
/// Read-only: it observes CO_WorkStack_Script and never changes it.
/// </summary>
public class CO_WorkStackHUD_Script : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player's work stack.")]
    [SerializeField] private CO_WorkStack_Script stack;

    [Tooltip("Text showing the count. Defaults to the text on this object.")]
    [SerializeField] private TMP_Text label;

    [Header("Wording")]
    [Tooltip("{0} is the folders carried, {1} the limit.")]
    [SerializeField] private string format = "WORK {0}/{1}";

    [Header("Colour")]
    [SerializeField] private Color calmColour = Color.white;

    [Tooltip("Once footsteps make noise.")]
    [SerializeField] private Color loudColour = new Color(0.95f, 0.8f, 0.2f);

    [Tooltip("Once the stack sways and drops folders.")]
    [SerializeField] private Color swayColour = new Color(1f, 0.35f, 0.3f);

    [Header("Punch")]
    [Tooltip("Scale added at the peak of the punch when folders land.")]
    [SerializeField] private float addPunch = 0.35f;

    [Tooltip("Scale added at the peak of the punch when folders go.")]
    [SerializeField] private float removePunch = 0.15f;

    private void Awake()
    {
        if (label == null)
        {
            label = GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        if (stack != null)
        {
            stack.FoldersChanged += OnFoldersChanged;
        }
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (stack != null)
        {
            stack.FoldersChanged -= OnFoldersChanged;
        }
    }

    private void OnFoldersChanged(int previous, int current)
    {
        Refresh();

        float punch = current > previous ? addPunch : removePunch;
        label.transform.DOComplete();
        label.transform.DOPunchScale(Vector3.one * punch, 0.3f, 6, 0.5f).SetLink(label.gameObject);
    }

    private void Refresh()
    {
        if (label == null || stack == null)
        {
            return;
        }

        int count = stack.Count;
        label.text = string.Format(format, count, stack.MaxFolders);
        label.color = stack.IsSwaying ? swayColour : stack.IsLoud ? loudColour : calmColour;
    }
}
