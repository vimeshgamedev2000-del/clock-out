using UnityEngine;

/// <summary>
/// One sound made in the world: where it came from, how far it carries and what
/// produced it. Passed to every enemy close enough to hear it.
/// Immutable, because a noise is a moment rather than a thing that changes.
/// </summary>
public readonly struct CO_NoiseEvent_Script
{
    /// <summary>Where the sound was made, in world space.</summary>
    public readonly Vector3 Position;

    /// <summary>How far the sound carries before it is inaudible.</summary>
    public readonly float Radius;

    /// <summary>
    /// What made the sound, so the enemy that walks over to it can act on it.
    /// Null for a one-off noise with nothing to find at the other end.
    /// </summary>
    public readonly CO_INoiseSource_Script Source;

    public CO_NoiseEvent_Script(Vector3 position, float radius, CO_INoiseSource_Script source = null)
    {
        Position = position;
        Radius = radius;
        Source = source;
    }
}
