using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Routes a noise to the enemies that can hear it and decides which single one
/// goes to look. Exactly one is handed the investigation; the rest glance that
/// way and carry on, which is what makes a distraction pull one guard off its
/// route instead of emptying the whole level onto one spot. Only when nobody can
/// be spared does the rest of the earshot hold its alert.
///
/// Static rather than a scene singleton: it holds no settings of its own, so
/// there is nothing to place or configure. Enemies add themselves on enable.
/// </summary>
public static class CO_NoiseManager_Script
{
    private static readonly List<CO_EnemyHearing_Script> listeners = new List<CO_EnemyHearing_Script>();

    // Reused per emit so a noise going off every second does not allocate.
    private static readonly List<CO_EnemyHearing_Script> audible = new List<CO_EnemyHearing_Script>();

    /// <summary>
    /// Sorts an enemy already busy with a different noise behind the idle ones.
    /// Large enough that no real distance can close the gap, so a free guard is
    /// always preferred over one that is already walking somewhere.
    /// </summary>
    private const float BusyPenalty = 10000f;

    /// <summary>Adds an enemy to the set that noises are offered to.</summary>
    public static void Register(CO_EnemyHearing_Script listener)
    {
        if (listener != null && !listeners.Contains(listener))
        {
            listeners.Add(listener);
        }
    }

    /// <summary>Removes an enemy, on disable or destroy.</summary>
    public static void Unregister(CO_EnemyHearing_Script listener)
    {
        listeners.Remove(listener);
    }

    /// <summary>
    /// Makes a sound at a point in the world. Convenience wrapper for callers
    /// with nothing to leave behind at the other end.
    /// </summary>
    public static void Emit(Vector3 position, float radius)
    {
        Emit(new CO_NoiseEvent_Script(position, radius));
    }

    /// <summary>
    /// Makes a sound: alerts every enemy that can hear it and assigns one of
    /// them to investigate. Safe to call repeatedly - a looping source re-emits
    /// to keep the investigator's target fresh and to catch enemies that have
    /// since wandered into earshot.
    /// </summary>
    public static void Emit(CO_NoiseEvent_Script noise)
    {
        audible.Clear();

        for (int i = 0; i < listeners.Count; i++)
        {
            CO_EnemyHearing_Script listener = listeners[i];

            // Destroyed enemies unregister themselves, but a null slipping
            // through here would take the whole emit down with it.
            if (listener == null || !listener.isActiveAndEnabled)
            {
                continue;
            }

            if (listener.CanHear(noise))
            {
                audible.Add(listener);
            }
        }

        CO_EnemyHearing_Script investigator = ChooseInvestigator(noise);

        for (int i = 0; i < audible.Count; i++)
        {
            CO_EnemyHearing_Script listener = audible[i];

            if (listener == investigator)
            {
                listener.HearNoise(noise, true);
            }
            else if (investigator != null)
            {
                // Somebody is already on their way, so the rest only look that
                // way for a moment before carrying on - which is what makes a
                // distraction cost one guard rather than all of them.
                listener.HearNoiseHandledByAnother(noise);
            }
            else
            {
                // Nobody could be spared: everyone in earshot has eyes on the
                // player. The ones who heard it at least look that way.
                listener.HearNoise(noise, false);
            }
        }
    }

    /// <summary>
    /// Picks the one enemy that walks over to look. Prefers whoever is already
    /// on their way to this same source, then the nearest enemy with nothing
    /// else to check. Returns null when everyone in earshot is busy chasing the
    /// player, in which case the noise only alerts.
    /// </summary>
    private static CO_EnemyHearing_Script ChooseInvestigator(CO_NoiseEvent_Script noise)
    {
        CO_EnemyHearing_Script best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < audible.Count; i++)
        {
            CO_EnemyHearing_Script candidate = audible[i];

            if (!candidate.CanInvestigate)
            {
                continue;
            }

            // Whoever is already walking to this source keeps the job, so a
            // radio pulsing every second does not hand it to a different guard
            // each pulse and leave both of them halfway there.
            if (noise.Source != null && candidate.IsInvestigating(noise.Source))
            {
                return candidate;
            }

            // Straight-line rather than path distance: a guard behind a wall may
            // not truly be closest, but the difference is not worth a path query
            // per enemy per noise.
            float score = Vector3.Distance(candidate.transform.position, noise.Position);

            if (candidate.HasNoise)
            {
                score += BusyPenalty;
            }

            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>
    /// Clears the registry when play mode starts. With domain reloading turned
    /// off the static list survives between plays and would otherwise keep
    /// entries from the previous session.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        listeners.Clear();
        audible.Clear();
    }
}
