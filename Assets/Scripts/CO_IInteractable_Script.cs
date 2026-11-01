using UnityEngine;

/// <summary>
/// Something the player can walk up to and use. CO_PlayerInteractor_Script finds these by
/// collider, so the component may sit on the collider or on any parent of it.
/// </summary>
public interface CO_IInteractable_Script
{
    /// <summary>Short line shown to the player, e.g. "Turn on radio".</summary>
    string InteractionPrompt { get; }

    /// <summary>
    /// False while the object cannot be used - already running, on cooldown or
    /// spent. Unavailable objects are skipped when picking a target, so a dead
    /// one does not block a live one standing next to it.
    /// </summary>
    bool CanInteract { get; }

    /// <summary>Uses the object. Only called when <see cref="CanInteract"/> is true.</summary>
    void Interact(GameObject interactor);
}
