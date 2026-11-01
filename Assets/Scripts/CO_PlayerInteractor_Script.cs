using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Lets the player use things they are standing next to. Picks the nearest
/// usable CO_IInteractable_Script in range each frame and hands it the key press.
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

    // Reused so the scan does not allocate a new array every frame. Not readonly:
    // a full buffer means the query had more to hand back and dropped the rest
    // arbitrarily, so it is grown and retried rather than quietly missing the
    // one collider that mattered.
    private Collider[] hits = new Collider[12];

    /// <summary>What would be used if the key were pressed now, or null when nothing is in reach.</summary>
    public CO_IInteractable_Script CurrentTarget { get; private set; }

    private void Update()
    {
        CurrentTarget = FindNearestInteractable();

        UpdatePrompt();

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || CurrentTarget == null)
        {
            return;
        }

        if (keyboard[interactKey].wasPressedThisFrame)
        {
            CurrentTarget.Interact(gameObject);
        }
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
        if (promptLabel == null)
        {
            return;
        }

        bool show = CurrentTarget != null;
        promptLabel.gameObject.SetActive(show);

        if (show)
        {
            promptLabel.text = $"[{interactKey}] {CurrentTarget.InteractionPrompt}";
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
