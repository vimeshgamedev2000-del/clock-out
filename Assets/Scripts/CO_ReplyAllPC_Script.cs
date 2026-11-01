using UnityEngine;

/// <summary>
/// The panic button for getting rid of work (GDD §5.8). Press E at any lit PC to
/// forward the player's whole stack at once. It works once per floor, whichever
/// PC is used: CO_WorkStack_Script owns that rule, so every PC on the floor goes
/// dark after the first reply-all.
///
/// Sending it makes one loud noise through CO_NoiseManager_Script: one coworker
/// comes to the PC to see who sent it, and everyone else in range only glances up.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CO_ReplyAllPC_Script : MonoBehaviour, CO_IInteractable_Script, CO_INoiseSource_Script
{
    [Header("Interaction")]
    [Tooltip("What the player is told they can do. The folder count is added after it.")]
    [SerializeField] private string prompt = "Reply all";

    [Tooltip("Tag of the player whose work stack it forwards.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Noise")]
    [Tooltip("How far \"they got the email\" carries, in metres. Every enemy inside hears it.")]
    [SerializeField] private float noiseRadius = 20f;

    [Header("Feedback")]
    [Tooltip("Renderer tinted lit while a reply-all is still available this floor, dark after. Optional.")]
    [SerializeField] private Renderer targetRenderer;

    [SerializeField] private Color litColour = new Color(0.45f, 0.75f, 1f);
    [SerializeField] private Color darkColour = new Color(0.15f, 0.15f, 0.18f);

    private MaterialPropertyBlock propertyBlock;
    private CO_WorkStack_Script playerStack;
    private bool shownLit;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    /// <summary>Folders this PC forwarded.</summary>
    public int FoldersForwarded { get; private set; }

    /// <summary>True while the floor's reply-all hasn't been used, on this PC or any other.</summary>
    public bool IsLit => ResolveStack() == null || !playerStack.HasRepliedAll;

    public string InteractionPrompt =>
        ResolveStack() != null
            ? $"{prompt} ({playerStack.Count} {(playerStack.Count == 1 ? "folder" : "folders")})"
            : prompt;

    public bool CanInteract => ResolveStack() != null && playerStack.CanReplyAll;

    public Transform Transform => transform;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        propertyBlock = new MaterialPropertyBlock();
        shownLit = true;
        ApplyColour();
    }

    private void Update()
    {
        // Another PC may have used the floor's reply-all.
        if (IsLit != shownLit)
        {
            shownLit = IsLit;
            ApplyColour();
        }
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract)
        {
            return;
        }

        int forwarded = playerStack.TryReplyAll();
        if (forwarded == 0)
        {
            return;
        }

        FoldersForwarded += forwarded;
        Debug.Log($"{name}: replied all, {forwarded} folder(s) forwarded.", this);

        CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(transform.position, noiseRadius, this));
    }

    /// <summary>The coworker came to see who sent it. The email is already out: nothing to switch off.</summary>
    public void OnInvestigated(GameObject investigator)
    {
    }

    /// <summary>
    /// Finds the player's stack by tag, retrying while it is missing, so a PC
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
        propertyBlock.SetColor(BaseColorId, shownLit ? litColour : darkColour);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnValidate()
    {
        noiseRadius = Mathf.Max(noiseRadius, 0f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.45f, 0.75f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, noiseRadius);
    }
}
