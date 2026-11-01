using UnityEngine;

/// <summary>
/// Camera framing numbers: zoom levels, look-ahead and easing. Read by CO_CameraFollow_Script.
/// Level bounds stay on the camera in the scene, because they belong to the level.
/// </summary>
[CreateAssetMenu(fileName = "CO_CameraTuning_Data", menuName = "Clock Out/Camera Tuning")]
public class CO_CameraTuning_Script : ScriptableObject
{
    [Header("Zoom")]
    [Tooltip("Orthographic size in normal play, in metres (half the screen height).")]
    [SerializeField] private float normalSize = 8f;

    [Tooltip("Orthographic size while the survey key is held, in metres. Large enough to show the whole floor.")]
    [SerializeField] private float surveySize = 21f;

    [Tooltip("Roughly how long a zoom change takes, in seconds.")]
    [Min(0f)]
    [SerializeField] private float zoomSmoothTime = 0.3f;

    [Header("Look-ahead")]
    [Tooltip("How far the view leads the player in the direction they move, in metres.")]
    [SerializeField] private float lookAheadDistance = 2f;

    [Tooltip("Roughly how long the lead takes to swing to a new direction, in seconds.")]
    [Min(0f)]
    [SerializeField] private float lookAheadSmoothTime = 0.5f;

    public float NormalSize => normalSize;
    public float SurveySize => surveySize;
    public float ZoomSmoothTime => zoomSmoothTime;
    public float LookAheadDistance => lookAheadDistance;
    public float LookAheadSmoothTime => lookAheadSmoothTime;
}
