using UnityEngine;

/// <summary>
/// Something in the level that makes noise and can be found at the other end of
/// it - a radio, an alarm, a dropped bottle. Implemented by whatever the
/// investigating enemy should be able to interact with when it arrives.
/// </summary>
public interface CO_INoiseSource_Script
{
    /// <summary>The object making the noise, for distance and facing checks.</summary>
    Transform Transform { get; }

    /// <summary>
    /// Seconds the investigator spends at it before finishing. 0 keeps the
    /// guard's own inspect time; a desk phone keeps them talking longer.
    /// </summary>
    float InspectDuration => 0f;

    /// <summary>
    /// Called once by the enemy that walked over and finished inspecting it.
    /// The source decides what that means: a radio switches itself off.
    /// </summary>
    void OnInvestigated(GameObject investigator);
}
