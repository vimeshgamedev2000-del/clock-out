/// <summary>
/// Something the player has to hold the key on for a moment before it is used -
/// the radio, the shredder, the intern's desk (GDD §5.8). CO_PlayerInteractor_Script
/// fills a progress bar while the key stays down and calls Interact once it is
/// full. Letting go, walking out of reach or the object becoming unusable first
/// cancels the hold, and nothing happens.
///
/// The commitment is the point: a distraction is never set off by a stray tap,
/// and the player stands exposed for a beat while starting it.
/// </summary>
public interface CO_IHoldInteractable_Script : CO_IInteractable_Script
{
    /// <summary>Seconds the key must be held before Interact is called.</summary>
    float HoldDuration { get; }
}
