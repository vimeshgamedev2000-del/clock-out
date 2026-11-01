using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Placeholder WASD movement so the player can be driven in and out of the
/// enemy's detection radius. Reads the keyboard directly rather than going
/// through an input action asset, which is enough for a test stand-in.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CO_PlayerController_Script : MonoBehaviour
{
    [Tooltip("Units travelled per second.")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("Downward pull applied each second so the player stays on the floor.")]
    [SerializeField] private float gravity = 20f;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;

        // Normalised so diagonal movement isn't faster than cardinal movement.
        Vector3 move = new Vector3(horizontal, 0f, vertical);
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        // Move rather than writing transform.position, so walls are collided
        // against instead of passed straight through.
        Vector3 velocity = move * moveSpeed;

        // A little constant downward push keeps the controller settled on the
        // floor; without it isGrounded flickers and it can creep up over edges.
        velocity.y = -gravity;

        controller.Move(velocity * Time.deltaTime);
    }
}
