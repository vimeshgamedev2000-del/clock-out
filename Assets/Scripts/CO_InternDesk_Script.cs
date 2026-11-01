using UnityEngine;

/// <summary>
/// The silent, slow way to get rid of work (GDD §5.8). Each full hold of E for
/// <see cref="holdDuration"/> hands the intern one folder, up to
/// <see cref="capacity"/> per floor. Makes no noise. A full in-tray stops being usable.
///
/// Until the work stack exists (T-04) there are no folders to take: each one is
/// counted and logged.
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

    [Header("Feedback")]
    [Tooltip("Renderer tinted when full. Optional.")]
    [SerializeField] private Renderer targetRenderer;

    [SerializeField] private Color idleColour = new Color(0.85f, 0.85f, 0.75f);
    [SerializeField] private Color fullColour = new Color(0.45f, 0.45f, 0.45f);

    private MaterialPropertyBlock propertyBlock;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    /// <summary>Folders handed over this floor. Stands in for the work stack until T-04.</summary>
    public int FoldersTaken { get; private set; }

    public bool IsFull => FoldersTaken >= capacity;

    public string InteractionPrompt => $"{prompt} ({FoldersTaken}/{capacity})";

    public float HoldDuration => holdDuration;

    public bool CanInteract => !IsFull;

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
        if (IsFull)
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
