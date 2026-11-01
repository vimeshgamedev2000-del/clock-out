using UnityEngine;

/// <summary>
/// A prop the player switches on to make noise - a radio, a TV, a generator.
/// The player holds the key on it for <see cref="holdDuration"/> to switch it on.
/// While it is running it pulses a noise into the CO_NoiseManager_Script, which alerts
/// every enemy in earshot and sends exactly one of them over to look.
///
/// Knows nothing about enemies or states: it makes a sound and is told, through
/// CO_INoiseSource_Script, when somebody came and found it.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CO_DistractionDevice_Script : MonoBehaviour, CO_IHoldInteractable_Script, CO_INoiseSource_Script
{
    [Header("Interaction")]
    [Tooltip("What the player is told they can do, shown by CO_PlayerInteractor_Script.")]
    [SerializeField] private string prompt = "Turn on radio";

    [Tooltip("Seconds the player holds the key to switch it on.")]
    [SerializeField] private float holdDuration = 1f;

    [Tooltip("Seconds before the device can be used again after switching off. " +
             "Set high to make each distraction a scarce resource.")]
    [SerializeField] private float reuseDelay = 3f;

    [Tooltip("Uncheck to leave the device spent once it has been switched off.")]
    [SerializeField] private bool reusable = true;

    [Header("Noise")]
    [Tooltip("How far the sound carries. Every enemy standing inside this radius " +
             "hears it, so the gizmo sphere is exactly who reacts.")]
    [SerializeField] private float noiseRadius = 16f;

    [Tooltip("Seconds between noise pulses while running. Each pulse refreshes " +
             "the investigator's target and catches enemies that have wandered in.")]
    [SerializeField] private float pulseInterval = 1f;

    [Tooltip("Seconds the device runs before switching itself off. 0 runs until " +
             "an enemy comes and finds it.")]
    [SerializeField] private float playDuration = 0f;

    [Tooltip("Seconds the investigator spends at it once arrived. 0 uses the " +
             "guard's own inspect time.")]
    [SerializeField] private float inspectDuration = 0f;

    [Header("Feedback")]
    [Tooltip("Optional looping sound played while running.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Renderer tinted to show running and idle. Optional.")]
    [SerializeField] private Renderer targetRenderer;

    [SerializeField] private Color idleColour = new Color(0.45f, 0.45f, 0.5f);
    [SerializeField] private Color activeColour = new Color(0.95f, 0.45f, 0.85f);

    private MaterialPropertyBlock propertyBlock;
    private float nextPulseTime;
    private float stopTime;
    private float readyTime;
    private bool spent;

    // URP lit shaders use _BaseColor rather than the built-in _Color, matching
    // CO_EnemyStateIndicator_Script and CO_MainDoor_Script.
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    /// <summary>True while the device is running and making noise.</summary>
    public bool IsActive { get; private set; }

    public string InteractionPrompt => prompt;

    public float HoldDuration => holdDuration;

    public bool CanInteract => !IsActive && !spent && Time.time >= readyTime;

    public Transform Transform => transform;

    public float InspectDuration => inspectDuration;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        propertyBlock = new MaterialPropertyBlock();
        ApplyColour();
    }

    private void Update()
    {
        if (!IsActive)
        {
            return;
        }

        // Pulsed rather than emitted once, so the sound is something enemies can
        // walk into and keep following, not a single instant they had to be
        // standing in earshot for.
        if (Time.time >= nextPulseTime)
        {
            nextPulseTime = Time.time + pulseInterval;
            CO_NoiseManager_Script.Emit(new CO_NoiseEvent_Script(transform.position, noiseRadius, this));
        }

        if (playDuration > 0f && Time.time >= stopTime)
        {
            Deactivate("ran out");
        }
    }

    /// <summary>
    /// Switches the device on. CO_PlayerInteractor_Script checks CanInteract first, but
    /// this guards anyway so a script calling it directly cannot double-start.
    /// </summary>
    public void Interact(GameObject interactor)
    {
        if (!CanInteract)
        {
            return;
        }

        IsActive = true;
        stopTime = Time.time + playDuration;

        // Fires this frame rather than waiting out an interval, so the player
        // gets feedback on the press instead of a beat later.
        nextPulseTime = Time.time;

        if (audioSource != null)
        {
            audioSource.loop = true;
            audioSource.Play();
        }

        ApplyColour();
        Debug.Log($"{name}: switched on, making noise.", this);
    }

    /// <summary>
    /// An enemy walked over and looked the device over. Switching off here is
    /// what closes the loop: the distraction is used up, and the guard has been
    /// pulled well off its route by the time it happens.
    /// </summary>
    public void OnInvestigated(GameObject investigator)
    {
        if (!IsActive)
        {
            return;
        }

        Debug.Log($"{name}: found by {investigator.name}.", this);
        Deactivate("investigated");
    }

    private void Deactivate(string reason)
    {
        IsActive = false;
        readyTime = Time.time + reuseDelay;
        spent = !reusable;

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        ApplyColour();
        Debug.Log($"{name}: switched off ({reason}).", this);
    }

    private void ApplyColour()
    {
        if (targetRenderer == null)
        {
            return;
        }

        // A property block tints this one renderer without creating a material
        // instance, so the shared material is left untouched.
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, IsActive ? activeColour : idleColour);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnValidate()
    {
        // A zero interval would emit a noise every frame, flooding the manager.
        pulseInterval = Mathf.Max(pulseInterval, 0.1f);
        holdDuration = Mathf.Max(holdDuration, 0f);
        noiseRadius = Mathf.Max(noiseRadius, 0f);
        inspectDuration = Mathf.Max(inspectDuration, 0f);
    }

    private void OnDrawGizmos()
    {
        // Magenta to read as "noise" next to the yellow route, cyan vision cone
        // and blue hearing range. Solid while running, faded while idle.
        Gizmos.color = IsActive
            ? new Color(1f, 0.3f, 0.9f)
            : new Color(1f, 0.3f, 0.9f, 0.35f);

        Gizmos.DrawWireSphere(transform.position, noiseRadius);
    }
}
