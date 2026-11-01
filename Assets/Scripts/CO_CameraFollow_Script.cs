using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Keeps the camera trailing the player instead of sitting still over the level.
/// The camera holds a fixed offset from its target and eases toward it, so quick
/// direction changes read as the camera catching up rather than snapping.
///
/// It also leads the player in the direction they move, never shows past the
/// level's edges, and zooms out over the whole floor while the survey key is held.
///
/// Rotation is left alone by default: the scene's angled orthographic framing is
/// authored in the editor, and re-aiming it every frame would only fight that.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CO_CameraFollow_Script : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Transform to follow. Left empty, the object tagged below is used instead.")]
    [SerializeField] private Transform target;

    [Tooltip("Tag used to find the target when none is assigned.")]
    [SerializeField] private string targetTag = "Player";

    [Header("Framing")]
    [Tooltip("Where the camera sits relative to the target. Ignored while the offset is taken from the scene.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 20f, -6.5f);

    [Tooltip("Take the offset from how the camera is already placed in the scene, so the framing set up in the editor is kept.")]
    [SerializeField] private bool useSceneFraming = true;

    [Tooltip("Zoom levels, look-ahead and easing. Left empty, the scene's zoom is kept and there is no look-ahead.")]
    [SerializeField] private CO_CameraTuning_Script tuning;

    [Header("Level bounds")]
    [Tooltip("South-west corner of the area the view must stay inside (x, z in metres). Equal corners mean no limit.")]
    [SerializeField] private Vector2 boundsMin;

    [Tooltip("North-east corner of the area the view must stay inside (x, z in metres).")]
    [SerializeField] private Vector2 boundsMax;

    [Header("Survey")]
    [Tooltip("Hold to zoom out over the whole floor.")]
    [SerializeField] private Key surveyKey = Key.Tab;

    [Header("Smoothing")]
    [Tooltip("Roughly how long the camera takes to close the gap, in seconds. Smaller is tighter, larger drifts further behind.")]
    [Min(0f)]
    [SerializeField] private float smoothTime = 0.25f;

    [Tooltip("Cap on how fast the camera may travel, in units per second. Zero means uncapped.")]
    [Min(0f)]
    [SerializeField] private float maxSpeed = 0f;

    [Tooltip("Turn the camera to face the target each frame instead of keeping the rotation set in the scene.")]
    [SerializeField] private bool lookAtTarget;

    private Camera cam;

    // Owned by SmoothDamp, which carries the easing between frames through them.
    private Vector3 followVelocity;
    private float zoomVelocity;
    private Vector3 lookAheadVelocity;

    private Vector3 lookAhead;
    private Vector3 lastTargetPosition;

    private bool HasBounds => boundsMax.x > boundsMin.x && boundsMax.y > boundsMin.y;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        if (target == null && !string.IsNullOrEmpty(targetTag))
        {
            GameObject tagged = GameObject.FindGameObjectWithTag(targetTag);
            if (tagged != null)
            {
                target = tagged.transform;
            }
        }

        if (target == null)
        {
            Debug.LogWarning($"{name}: CO_CameraFollow_Script has no target and found nothing tagged '{targetTag}'.", this);
            return;
        }

        if (useSceneFraming)
        {
            offset = transform.position - target.position;
        }

        // Start already framed, otherwise the first seconds of play are spent
        // sliding over from wherever the camera happened to be parked.
        SnapToTarget();
    }

    // Late, because the player moves in Update: following here uses this frame's
    // position rather than last frame's, which is what stops the image juddering.
    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        bool isSurveying = Keyboard.current != null && Keyboard.current[surveyKey].isPressed;
        UpdateZoom(isSurveying);
        UpdateLookAhead(isSurveying);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            FramedPosition(target.position + lookAhead),
            ref followVelocity,
            smoothTime,
            maxSpeed > 0f ? maxSpeed : Mathf.Infinity,
            Time.deltaTime);

        if (lookAtTarget)
        {
            transform.LookAt(target);
        }
    }

    /// <summary>
    /// Follow someone else from now on, keeping the current offset. Passing null
    /// leaves the camera where it is rather than dropping it to the origin.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// Jump straight to the framed position with no easing. Worth calling after a
    /// teleport or respawn, where a smooth glide across the level looks wrong.
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        if (tuning != null)
        {
            cam.orthographicSize = tuning.NormalSize;
        }

        lookAhead = Vector3.zero;
        lastTargetPosition = target.position;
        transform.position = FramedPosition(target.position);
        followVelocity = Vector3.zero;
        zoomVelocity = 0f;
        lookAheadVelocity = Vector3.zero;

        if (lookAtTarget)
        {
            transform.LookAt(target);
        }
    }

    /// <summary>
    /// Moves a ground focus point so a view of this size never shows past the bounds.
    /// A level smaller than the view on an axis is centred on that axis instead.
    /// </summary>
    public static Vector3 ClampFocus(Vector3 focus, float orthographicSize, float aspect, float pitchDegrees, Vector2 min, Vector2 max)
    {
        float halfWidth = orthographicSize * aspect;
        // Pitched down, the screen's height covers more ground than it would looking straight down.
        float halfDepth = orthographicSize / Mathf.Sin(pitchDegrees * Mathf.Deg2Rad);
        focus.x = ClampAxis(focus.x, min.x, max.x, halfWidth);
        focus.z = ClampAxis(focus.z, min.y, max.y, halfDepth);
        return focus;
    }

    private static float ClampAxis(float value, float min, float max, float halfView)
    {
        if (max - min <= halfView * 2f)
        {
            return (min + max) * 0.5f;
        }

        return Mathf.Clamp(value, min + halfView, max - halfView);
    }

    private Vector3 FramedPosition(Vector3 focus)
    {
        if (HasBounds && cam.orthographic)
        {
            focus = ClampFocus(focus, cam.orthographicSize, cam.aspect, transform.eulerAngles.x, boundsMin, boundsMax);
        }

        return focus + offset;
    }

    private void UpdateZoom(bool isSurveying)
    {
        if (tuning == null)
        {
            return;
        }

        float wanted = isSurveying ? tuning.SurveySize : tuning.NormalSize;
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, wanted, ref zoomVelocity, tuning.ZoomSmoothTime, Mathf.Infinity, Time.deltaTime);
    }

    private void UpdateLookAhead(bool isSurveying)
    {
        Vector3 moved = target.position - lastTargetPosition;
        moved.y = 0f;
        lastTargetPosition = target.position;

        if (tuning == null)
        {
            return;
        }

        // Settles back on the player when they stop or survey, so standing still is calm.
        Vector3 wanted = !isSurveying && moved.sqrMagnitude > 0.000001f
            ? moved.normalized * tuning.LookAheadDistance
            : Vector3.zero;
        lookAhead = Vector3.SmoothDamp(lookAhead, wanted, ref lookAheadVelocity, tuning.LookAheadSmoothTime, Mathf.Infinity, Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (!HasBounds)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Vector3 centre = new Vector3((boundsMin.x + boundsMax.x) * 0.5f, 0f, (boundsMin.y + boundsMax.y) * 0.5f);
        Vector3 size = new Vector3(boundsMax.x - boundsMin.x, 0f, boundsMax.y - boundsMin.y);
        Gizmos.DrawWireCube(centre, size);
    }
}
