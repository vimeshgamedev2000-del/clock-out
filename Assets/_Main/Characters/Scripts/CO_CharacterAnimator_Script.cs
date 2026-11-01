using UnityEngine;

/// <summary>
/// Plays idle / walk / sprint on a Kenney character from how fast its body moves.
/// Presentation only: it reads movement and never touches gameplay state.
/// </summary>
public class CO_CharacterAnimator_Script : MonoBehaviour
{
    private static readonly int SpeedId = Animator.StringToHash("Speed");

    [Header("References")]
    [Tooltip("Animator on the model. Defaults to the one on this object.")]
    [SerializeField] private Animator animator;
    [Tooltip("Object whose movement is measured. Defaults to the parent.")]
    [SerializeField] private Transform body;

    [Header("Animation")]
    [Tooltip("Seconds to smooth speed changes, so stops and starts blend.")]
    [SerializeField] private float speedDamping = 0.1f;

    [Header("Facing")]
    [Tooltip("Turn the model toward its move direction. Off for guards: CO_EnemyPatrol_Script already turns their body.")]
    [SerializeField] private bool faceMovement;
    [Tooltip("Degrees per second.")]
    [SerializeField] private float turnSpeed = 720f;

    private Vector3 lastPosition;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (body == null)
        {
            body = transform.parent;
        }

        lastPosition = body.position;
    }

    private void LateUpdate()
    {
        Vector3 delta = body.position - lastPosition;
        delta.y = 0f;
        lastPosition = body.position;

        // Measured from position so the player (CharacterController) and guards (NavMeshAgent) share one script.
        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        animator.SetFloat(SpeedId, speed, speedDamping, Time.deltaTime);

        if (faceMovement && delta.sqrMagnitude > 0.000001f)
        {
            Quaternion target = Quaternion.LookRotation(delta);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }
    }
}
