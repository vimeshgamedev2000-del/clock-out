using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Lets the player use things they are standing next to. Picks the nearest
/// usable CO_IInteractable_Script in range each frame and hands it the key press.
/// A CO_IHoldInteractable_Script is only used once the key has been held for its
/// HoldDuration; letting go sooner cancels it.
///
/// Proximity rather than a look ray, because CO_PlayerController_Script never rotates the
/// player - there is no facing to aim with.
/// </summary>
public class CO_PlayerInteractor_Script : MonoBehaviour
{
    [Header("Reach")]
    [Tooltip("How close the player must be to use something.")]
    [SerializeField] private float interactRange = 2.5f;

    [Tooltip("Layers searched for interactables. Leave as Everything to rely on " +
             "the component alone.")]
    [SerializeField] private LayerMask interactMask = ~0;

    [Header("Input")]
    [Tooltip("Key pressed to use the target.")]
    [SerializeField] private Key interactKey = Key.E;

    [Header("Feedback")]
    [Tooltip("Optional screen label naming what is in reach. Hidden when nothing is.")]
    [SerializeField] private TextMeshProUGUI promptLabel;

    [Tooltip("Optional screen-space container holding the prompt and hold meter. Moved each " +
             "frame to sit above the target. Leave empty to keep them where they are on the HUD.")]
    [SerializeField] private RectTransform promptAnchor;

    [Tooltip("Metres above the top of the target's collider that the prompt floats.")]
    [SerializeField] private float promptHeight = 0.4f;

    [Tooltip("Optional hold meter, shown only while holding. Defaults to the fill's own object.")]
    [SerializeField] private GameObject holdMeter;

    [Tooltip("Optional Filled image inside the meter that shows how far a hold has got.")]
    [SerializeField] private Image holdFill;

    [Tooltip("Extra scale the prompt punches by when a hold completes.")]
    [SerializeField] private float completePunch = 0.15f;

    // Reused so the scan does not allocate a new array every frame. Not readonly:
    // a full buffer means the query had more to hand back and dropped the rest
    // arbitrarily, so it is grown and retried rather than quietly missing the
    // one collider that mattered.
    private Collider[] hits = new Collider[12];

    // The hold object the key is being held on, or null.
    private CO_IHoldInteractable_Script heldTarget;
    private float holdElapsed;

    /// <summary>What would be used if the key were pressed now, or null when nothing is in reach.</summary>
    public CO_IInteractable_Script CurrentTarget { get; private set; }

    /// <summary>True while the key is held on a hold object that has not been used yet.</summary>
    public bool IsHolding => heldTarget != null;

    /// <summary>0 to 1: how far the current hold has got. 0 when not holding.</summary>
    public float HoldProgress => heldTarget == null
        ? 0f
        : Mathf.Clamp01(holdElapsed / Mathf.Max(heldTarget.HoldDuration, 0.0001f));

    private void Update()
    {
        CurrentTarget = FindNearestInteractable();

        Keyboard keyboard = Keyboard.current;

        if (heldTarget != null)
        {
            // Any break cancels, so a distraction is never set off for a player
            // who let go or walked off.
            if (keyboard != null && keyboard[interactKey].isPressed
                && ReferenceEquals(CurrentTarget, heldTarget))
            {
                holdElapsed += Time.deltaTime;
                if (holdElapsed >= heldTarget.HoldDuration)
                {
                    CompleteHold();
                }
            }
            else
            {
                CancelHold();
            }
        }
        else if (keyboard != null && CurrentTarget != null && keyboard[interactKey].wasPressedThisFrame)
        {
            if (CurrentTarget is CO_IHoldInteractable_Script holdTarget)
            {
                heldTarget = holdTarget;
                holdElapsed = 0f;
            }
            else
            {
                CurrentTarget.Interact(gameObject);
            }
        }

        UpdatePrompt();
    }

    private void OnDisable()
    {
        CancelHold();
        UpdatePrompt();
    }

    private void CompleteHold()
    {
        CO_IHoldInteractable_Script completed = heldTarget;
        CancelHold();

        // Used once per hold: keeping the key down afterwards does nothing until
        // it is pressed again.
        completed.Interact(gameObject);

        if (promptLabel != null)
        {
            promptLabel.transform.DOComplete();
            promptLabel.transform.DOPunchScale(Vector3.one * completePunch, 0.25f, 6, 0.5f)
                .SetLink(promptLabel.gameObject);
        }
    }

    private void CancelHold()
    {
        heldTarget = null;
        holdElapsed = 0f;
    }

    private CO_IInteractable_Script FindNearestInteractable()
    {
        // Triggers included: a prop can use a trigger collider for its reach
        // without also needing a solid one.
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, interactRange, hits, interactMask, QueryTriggerInteraction.Collide);

        while (count == hits.Length)
        {
            hits = new Collider[hits.Length * 2];
            count = Physics.OverlapSphereNonAlloc(
                transform.position, interactRange, hits, interactMask, QueryTriggerInteraction.Collide);
        }

        CO_IInteractable_Script nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            // InParent so the component can live on the prop's root while the
            // collider sits on a child mesh.
            CO_IInteractable_Script candidate = hits[i].GetComponentInParent<CO_IInteractable_Script>();

            // Unavailable ones are skipped rather than shown greyed out, so a
            // radio that is already playing does not shadow a usable one next to it.
            if (candidate == null || !candidate.CanInteract)
            {
                continue;
            }

            // Stays on the held object while it is in reach, so walking past a
            // nearer one mid-hold does not cut it.
            if (ReferenceEquals(candidate, heldTarget))
            {
                return candidate;
            }

            float distance = Vector3.Distance(transform.position, hits[i].transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private void UpdatePrompt()
    {
        GameObject meter = holdMeter != null ? holdMeter : holdFill != null ? holdFill.gameObject : null;
        if (meter != null)
        {
            meter.SetActive(IsHolding);
        }

        if (holdFill != null)
        {
            holdFill.fillAmount = HoldProgress;
        }

        if (promptLabel == null)
        {
            return;
        }

        // A target used this frame may have just become unusable (a radio now
        // playing), so check again rather than trust the scan from the frame start.
        bool show = isActiveAndEnabled && CurrentTarget != null && CurrentTarget.CanInteract;
        promptLabel.gameObject.SetActive(show);

        if (show)
        {
            string verb = CurrentTarget is CO_IHoldInteractable_Script ? "Hold " : "";
            promptLabel.text = $"[{verb}{interactKey}] {CurrentTarget.InteractionPrompt}";
            PlaceAboveTarget();
        }
    }

    private void PlaceAboveTarget()
    {
        Camera view = Camera.main;
        if (promptAnchor == null || view == null || !(CurrentTarget is Component target))
        {
            return;
        }

        // Above the top of the prop, so the text never covers the thing it names.
        Collider body = target.GetComponentInChildren<Collider>();
        Vector3 top = body != null
            ? new Vector3(body.bounds.center.x, body.bounds.max.y, body.bounds.center.z)
            : target.transform.position;

        // A Screen Space - Overlay canvas works in screen pixels, so the screen
        // point is the position.
        promptAnchor.position = view.WorldToScreenPoint(top + Vector3.up * promptHeight);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
