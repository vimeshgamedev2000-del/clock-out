using UnityEngine;

/// <summary>
/// The fast, loud way to get rid of work (GDD §5.8). Hold E for
/// <see cref="holdDuration"/> to start it; it then runs on its own for
/// <see cref="runDuration"/>, shredding one folder every <see cref="secondsPerFolder"/>
/// and pulsing a noise into CO_NoiseManager_Script, so one coworker comes over to
/// check. The coworker who finds it switches it off.
///
/// Each folder comes off the player's CO_WorkStack_Script. It can't be started
/// with an empty stack, and stops early once there is nothing left to shred.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CO_Shredder_Script : MonoBehaviour, CO_IHoldInteractable_Script, CO_INoiseSource_Script
{
    [Header("Interaction")]
    [Tooltip("What the player is told they can do, shown by CO_PlayerInteractor_Script.")]
    [SerializeField] private string prompt = "Start shredder";

    [Tooltip("Seconds the player holds the key to start it.")]
    [SerializeField] private float holdDuration = 1f;

    [Tooltip("Seconds it runs once started, unless a coworker switches it off first.")]
    [SerializeField] private float runDuration = 4f;

    [Tooltip("Seconds of running per folder shredded.")]
    [SerializeField] private float secondsPerFolder = 1f;

    [Tooltip("Seconds before it can be started again after stopping.")]
    [SerializeField] private float reuseDelay = 3f;

    [Tooltip("Tag of the player whose work stack it shreds.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Noise")]
    [Tooltip("How far the grinding carries, in metres. Every enemy inside hears it.")]
    [SerializeField] private float noiseRadius = 8f;

    [Tooltip("Seconds between noise pulses while running.")]
    [SerializeField] private float pulseInterval = 1f;

    [Header("Feedback")]
    [Tooltip("Renderer tinted while running. Optional.")]
    [SerializeField] private Renderer targetRenderer;

    [SerializeField] private Color idleColour = new Color(0.35f, 0.35f, 0.4f);
    [SerializeField] private Color activeColour = new Color(0.95f, 0.6f, 0.2f);

    private MaterialPropertyBlock propertyBlock;
    private float progress;
    private float nextPulseTime;
    private float stopTime;
    private float readyTime;
    private CO_WorkStack_Script playerStack;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    /// <summary>True while it is running and making noise.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Folders shredded this floor.</summary>
    public int FoldersShredded { get; private set; }

    public string InteractionPrompt => prompt;

    public float HoldDuration => holdDuration;

    public bool CanInteract =>
        !IsRunning && Time.time >= readyTime && ResolveStack() != null && playerStack.Count > 0;

    public Transform Transform => transform;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        propertyBlock = new MaterialPropertyBlock();
        ApplyColour();
    }

    private void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        if (Time.time >= nextPulseTime)
        {
            nextPulseTime = Time.time + pulseInterval;
            CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(transform.position, noiseRadius, this));
        }

        progress += Time.deltaTime;
        while (progress >= secondsPerFolder)
        {
            progress -= secondsPerFolder;

            if (playerStack == null || !playerStack.TryTakeFolder())
            {
                Stop("nothing left to shred");
                return;
            }

            FoldersShredded++;
            Debug.Log($"{name}: shredded a folder ({FoldersShredded} so far).", this);
        }

        if (Time.time >= stopTime)
        {
            Stop("finished");
        }
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract)
        {
            return;
        }

        IsRunning = true;
        progress = 0f;
        stopTime = Time.time + runDuration;

        // Noise this frame, so starting it is never silent.
        nextPulseTime = Time.time;

        ApplyColour();
        Debug.Log($"{name}: started shredding.", this);
    }

    /// <summary>The coworker came to look and switches it off.</summary>
    public void OnInvestigated(GameObject investigator)
    {
        if (!IsRunning)
        {
            return;
        }

        Debug.Log($"{name}: switched off by {investigator.name}.", this);
        Stop("investigated");
    }

    /// <summary>
    /// Finds the player's stack by tag, retrying while it is missing, so a shredder
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

    private void Stop(string reason)
    {
        IsRunning = false;
        readyTime = Time.time + reuseDelay;
        ApplyColour();
        Debug.Log($"{name}: stopped shredding ({reason}).", this);
    }

    private void ApplyColour()
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, IsRunning ? activeColour : idleColour);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnValidate()
    {
        // Zero would shred an infinite number of folders in one frame.
        secondsPerFolder = Mathf.Max(secondsPerFolder, 0.05f);
        pulseInterval = Mathf.Max(pulseInterval, 0.1f);
        noiseRadius = Mathf.Max(noiseRadius, 0f);
        holdDuration = Mathf.Max(holdDuration, 0f);
        runDuration = Mathf.Max(runDuration, 0f);
        reuseDelay = Mathf.Max(reuseDelay, 0f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsRunning ? new Color(1f, 0.3f, 0.9f) : new Color(1f, 0.3f, 0.9f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, noiseRadius);
    }
}
