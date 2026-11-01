using UnityEngine;

/// <summary>
/// The silent, slow way to get rid of work (GDD §5.8). Each full hold of E for
/// <see cref="holdDuration"/> hands the intern one folder, up to
/// <see cref="capacity"/> per floor, taken off the player's CO_WorkStack_Script.
/// Makes no noise. A full in-tray, or an empty stack, stops it being usable.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CO_InternDesk_Script : MonoBehaviour, CO_IHoldInteractable_Script
{
    [Header("Interaction")]
    [Tooltip("What the player is told they can do. The in-tray count is added after it.")]
    [SerializeField] private string prompt = "Hand a folder to the intern";

    [Tooltip("Seconds the player holds the key per folder handed over.")]
    [SerializeField] private float holdDuration = 1.5f;

    [Tooltip("Folders the intern takes per floor before the in-tray is full.")]
    [SerializeField] private int capacity = 3;

    [Tooltip("Tag of the player whose work stack it takes from.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Feedback")]
    [Tooltip("Renderer tinted when full. Optional.")]
    [SerializeField] private Renderer targetRenderer;

    [SerializeField] private Color idleColour = new Color(0.85f, 0.85f, 0.75f);
    [SerializeField] private Color fullColour = new Color(0.45f, 0.45f, 0.45f);

    private MaterialPropertyBlock propertyBlock;
    private CO_WorkStack_Script playerStack;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    /// <summary>Folders handed over this floor.</summary>
    public int FoldersTaken { get; private set; }

    public bool IsFull => FoldersTaken >= capacity;

    public string InteractionPrompt => $"{prompt} ({FoldersTaken}/{capacity})";

    public float HoldDuration => holdDuration;

    public bool CanInteract => !IsFull && ResolveStack() != null && playerStack.Count > 0;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        propertyBlock = new MaterialPropertyBlock();
        ApplyColour();
    }

    public void Interact(GameObject interactor)
    {
        if (IsFull || ResolveStack() == null || !playerStack.TryTakeFolder())
        {
            return;
        }

        FoldersTaken++;
        Debug.Log($"{name}: the intern took a folder ({FoldersTaken}/{capacity}).", this);

        if (IsFull)
        {
            Debug.Log($"{name}: the in-tray is full.", this);
            ApplyColour();
        }
    }

    /// <summary>
    /// Finds the player's stack by tag, retrying while it is missing, so a desk
    /// placed before the player spawns still finds it.
    /// </summary>
    private CO_WorkStack_Script ResolveStack()
    {
        if (playerStack == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            playerStack = player != null ? player.GetComponent<CO_WorkStack_Script>() : null;
        }

        return playerStack;
    }

    private void ApplyColour()
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, IsFull ? fullColour : idleColour);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnValidate()
    {
        holdDuration = Mathf.Max(holdDuration, 0f);
        capacity = Mathf.Max(capacity, 0);
    }
}
