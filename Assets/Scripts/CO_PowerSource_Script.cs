using UnityEngine;

/// <summary>
/// A power source the player collects to unlock the main door. Handles its own
/// presentation - bobbing, spinning and disappearing - and reports the pickup to
/// the CO_ObjectiveTracker_Script. It knows nothing about the door or the count.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CO_PowerSource_Script : MonoBehaviour
{
    [Header("Pickup")]
    [Tooltip("Tag identifying the player object.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Idle Motion")]
    [Tooltip("Degrees spun around the up axis per second. Set to 0 to hold still.")]
    [SerializeField] private float spinSpeed = 90f;

    [Tooltip("How far the power source drifts above and below its resting height.")]
    [SerializeField] private float bobHeight = 0.2f;

    [Tooltip("Full bobs per second.")]
    [SerializeField] private float bobSpeed = 1f;

    [Header("Feedback")]
    [Tooltip("Optional effect spawned where the power source was collected.")]
    [SerializeField] private GameObject collectEffect;

    private Vector3 restPosition;
    private bool collected;

    private void Awake()
    {
        // The bob is measured from wherever the power source was placed, so
        // moving one in the editor does not need the script touching.
        restPosition = transform.position;

        Collider pickupCollider = GetComponent<Collider>();

        // Walking into a solid collider would stop the player dead instead of
        // collecting; the trigger is what makes it a pickup.
        if (!pickupCollider.isTrigger)
        {
            Debug.LogWarning($"{name}: collider is not a trigger, so the player cannot collect it.", this);
        }
    }

    private void Update()
    {
        if (spinSpeed != 0f)
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        if (bobHeight != 0f)
        {
            // Sine around the rest height, so the power source returns to exactly
            // where it was placed rather than drifting over time.
            float offset = Mathf.Sin(Time.time * bobSpeed * Mathf.PI * 2f) * bobHeight;
            transform.position = restPosition + Vector3.up * offset;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        Collect();
    }

    /// <summary>
    /// Reports the pickup and removes the power source. Guarded against running
    /// twice: a CharacterController can register more than one trigger enter in
    /// the same frame, and each one would otherwise count separately.
    /// </summary>
    private void Collect()
    {
        if (collected)
        {
            return;
        }

        collected = true;

        if (CO_ObjectiveTracker_Script.Instance != null)
        {
            CO_ObjectiveTracker_Script.Instance.CollectPowerSource(this);
        }
        else
        {
            Debug.LogWarning($"{name}: no CO_ObjectiveTracker_Script in the scene, so this pickup counts for nothing.", this);
        }

        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        // Cyan to read as "powered" next to the yellow patrol route and the
        // enemy's red and orange ranges.
        Gizmos.color = new Color(0.3f, 0.9f, 1f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
