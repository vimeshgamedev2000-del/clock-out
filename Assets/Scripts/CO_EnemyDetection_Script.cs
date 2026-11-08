using UnityEngine;

/// <summary>
/// Scans for the player inside a vision cone and fills a notice meter while they
/// stay in it. Detection only: it logs changes and exposes the result, but does
/// not alter how the enemy moves.
/// </summary>
public class CO_EnemyDetection_Script : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("This coworker type's numbers: cone radius and angle, notice and forget time.")]
    [SerializeField] private CO_GuardTuning_Script tuning;

    [Header("Line of Sight")]
    [Tooltip("Layers that block sight. Walls belong here; the player and the enemy do not.")]
    [SerializeField] private LayerMask obstacleMask;

    [Tooltip("Height above the enemy's origin the sight line is cast from.")]
    [SerializeField] private float eyeHeight = 1f;

    [Tooltip("Height above the player's origin the sight line is aimed at.")]
    [SerializeField] private float targetHeight = 1f;

    [Tooltip("Tag identifying the player object.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Seconds between scans. Detection does not need to run every frame.")]
    [SerializeField] private float scanInterval = 0.1f;

    /// <summary>The player while they are inside the vision cone, otherwise null.</summary>
    public Transform DetectedPlayer { get; private set; }

    /// <summary>True while the player is inside the vision cone, noticed or not.</summary>
    public bool HasDetectedPlayer => DetectedPlayer != null;

    /// <summary>How far the notice meter has filled, from 0 (unaware) to 1 (noticed).</summary>
    public float NoticeLevel { get; private set; }

    /// <summary>True while the player is in view and the meter is full: the enemy acts on what it sees.</summary>
    public bool HasNoticedPlayer => HasDetectedPlayer && NoticeLevel >= 1f && !IsIgnoringPlayer;

    /// <summary>
    /// True while the enemy has been told to ignore the player (just handed over work):
    /// the meter stays empty, so the cone and badge don't read as a threat.
    /// </summary>
    public bool IsIgnoringPlayer { get; private set; }

    // Read by the in-game vision cone so it draws exactly the area this script tests.
    public float DetectionRadius => tuning.DetectionRadius;
    public float ViewAngle => tuning.ViewAngle;
    public LayerMask ObstacleMask => obstacleMask;
    public float EyeHeight => eyeHeight;

    // Looked up once by tag and kept, so each scan is three cheap checks rather
    // than a physics query. An overlap query was the obvious way to write this,
    // but it has to be given a fixed buffer, and a crowded corner fills that
    // buffer with walls and floor before the player is ever reached - at which
    // point the enemy stops seeing them entirely.
    private Transform player;
    private float nextScanTime;

    private void Update()
    {
        // Scan first so the meter fills on the same frame the player comes into view.
        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + scanInterval;
            Scan();
        }

        UpdateNoticeMeter();
    }

    /// <summary>
    /// Set by CO_EnemyAI_Script during its "just delegated" cooldown. Starting to
    /// ignore empties the meter at once, so the red cone and "!" drop with the catch.
    /// </summary>
    public void IgnorePlayer(bool ignore)
    {
        if (ignore && !IsIgnoringPlayer)
        {
            NoticeLevel = 0f;
        }

        IsIgnoringPlayer = ignore;
    }

    private void UpdateNoticeMeter()
    {
        bool wasNoticed = NoticeLevel >= 1f;
        float noticeTime = tuning.NoticeTime;
        float forgetTime = tuning.ForgetTime;

        if (HasDetectedPlayer && !IsIgnoringPlayer)
        {
            NoticeLevel = noticeTime <= 0f ? 1f : Mathf.Min(1f, NoticeLevel + Time.deltaTime / noticeTime);
        }
        else
        {
            NoticeLevel = forgetTime <= 0f ? 0f : Mathf.Max(0f, NoticeLevel - Time.deltaTime / forgetTime);
        }

        if (NoticeLevel >= 1f && !wasNoticed)
        {
            Debug.Log($"{name}: noticed the player.", this);
        }
    }

    private void Scan()
    {
        Transform previous = DetectedPlayer;
        DetectedPlayer = FindVisiblePlayer();

        if (DetectedPlayer != null && previous == null)
        {
            Debug.Log($"{name}: player entered view cone.", this);
        }
        else if (DetectedPlayer == null && previous != null)
        {
            Debug.Log($"{name}: player left view cone.", this);
        }
    }

    private Transform FindVisiblePlayer()
    {
        Transform candidate = ResolvePlayer();

        if (candidate == null)
        {
            return null;
        }

        // Range, then angle, then the raycast: each test is dearer than the one
        // before it, so the common case of a player somewhere else in the level
        // costs a single distance check.
        if (Vector3.Distance(transform.position, candidate.position) > DetectionRadius)
        {
            return null;
        }

        if (!IsWithinViewAngle(candidate.position) || !HasLineOfSight(candidate.position))
        {
            return null;
        }

        return candidate;
    }

    /// <summary>
    /// The player's transform, found by tag and cached. Re-resolved while it is
    /// missing, so an enemy in the scene before the player spawns still finds
    /// them, and one whose target was destroyed picks up a replacement.
    /// </summary>
    private Transform ResolvePlayer()
    {
        if (player != null)
        {
            return player;
        }

        GameObject found = GameObject.FindGameObjectWithTag(playerTag);
        player = found != null ? found.transform : null;

        return player;
    }

    /// <summary>
    /// True when the target sits inside the cone centred on the enemy's facing.
    /// Measured flat on the XZ plane, matching how the enemy turns and moves, so a
    /// target standing higher or lower is not pushed out of view by height alone.
    /// </summary>
    private bool IsWithinViewAngle(Vector3 target)
    {
        Vector3 toTarget = target - transform.position;
        toTarget.y = 0f;

        // Standing exactly on top of the enemy leaves no direction to measure;
        // count that as visible rather than letting Angle return something arbitrary.
        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            return true;
        }

        Vector3 facing = transform.forward;
        facing.y = 0f;

        // viewAngle is the full width of the cone, so each side spans half of it.
        return Vector3.Angle(facing, toTarget) <= ViewAngle * 0.5f;
    }

    /// <summary>
    /// True when nothing on the obstacle mask stands between the enemy's eyes and
    /// the target. Cast between chest heights rather than origins, so a low crate
    /// the enemy can plainly see over does not cut the sight line at the floor.
    /// </summary>
    private bool HasLineOfSight(Vector3 target)
    {
        Vector3 origin = EyePosition;
        Vector3 aim = target + Vector3.up * targetHeight;

        Vector3 toTarget = aim - origin;
        float distance = toTarget.magnitude;

        // Nothing to block at zero length, and Raycast needs a real direction.
        if (distance <= 0.0001f)
        {
            return true;
        }

        // Any hit is a blocker: the mask holds obstacles only, so the player is
        // not on it and cannot occlude themselves.
        return !Physics.Raycast(
            origin, toTarget / distance, distance, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    /// <summary>Where the sight line starts: chest height rather than the feet.</summary>
    private Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;

    // Editor-only debug drawing. Gizmos never render in a build, so this is safe
    // to leave in; the in-game cone visual is a separate concern.
    private void OnDrawGizmos()
    {
        // The base coworker prefab has no type, so nothing to draw.
        if (tuning == null)
        {
            return;
        }

        // Red once the player is noticed, yellow while the meter fills, cyan while idle.
        Gizmos.color = HasNoticedPlayer ? Color.red : HasDetectedPlayer ? Color.yellow : Color.cyan;

        DrawViewCone();

        // Show the sight line being tested, so a detection that fails on a wall
        // is visibly different from one that fails on the angle.
        if (DetectedPlayer != null)
        {
            Gizmos.DrawLine(EyePosition, DetectedPlayer.position + Vector3.up * targetHeight);
        }
    }

    /// <summary>
    /// Draws the cone at eye height as two edge lines plus an arc, matching the
    /// flat XZ test in IsWithinViewAngle rather than a true 3D cone.
    /// </summary>
    private void DrawViewCone()
    {
        Vector3 origin = EyePosition;

        Vector3 facing = transform.forward;
        facing.y = 0f;

        if (facing.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        facing.Normalize();
        float viewAngle = ViewAngle;
        float detectionRadius = DetectionRadius;
        float halfAngle = viewAngle * 0.5f;

        // The two outer edges of the cone.
        Vector3 leftEdge = Quaternion.Euler(0f, -halfAngle, 0f) * facing;
        Vector3 rightEdge = Quaternion.Euler(0f, halfAngle, 0f) * facing;

        Gizmos.DrawLine(origin, origin + leftEdge * detectionRadius);
        Gizmos.DrawLine(origin, origin + rightEdge * detectionRadius);

        // Arc across the far end, stepped finely enough to read as a curve at
        // any angle without drawing more segments than a debug view needs.
        const int segments = 24;
        Vector3 previous = origin + leftEdge * detectionRadius;

        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 direction = Quaternion.Euler(0f, -halfAngle + viewAngle * t, 0f) * facing;
            Vector3 current = origin + direction * detectionRadius;

            Gizmos.DrawLine(previous, current);
            previous = current;
        }
    }
}
