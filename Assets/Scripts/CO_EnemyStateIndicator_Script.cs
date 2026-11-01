using TMPro;
using UnityEngine;

/// <summary>
/// Shows the enemy's current state by tinting its renderer and updating a
/// floating label. Read-only: it observes CO_EnemyAI_Script and never influences it.
/// </summary>
[RequireComponent(typeof(CO_EnemyAI_Script))]
public class CO_EnemyStateIndicator_Script : MonoBehaviour
{
    [Header("Colour")]
    [Tooltip("Renderer to tint. Defaults to the renderer on this object.")]
    [SerializeField] private Renderer targetRenderer;

    [SerializeField] private Color patrolColour = new Color(0.25f, 0.8f, 0.3f);

    [Tooltip("Heard a noise but was not the one sent to check it.")]
    [SerializeField] private Color alertColour = new Color(0.3f, 0.75f, 0.95f);

    [Tooltip("Walking over to a noise to look it over.")]
    [SerializeField] private Color investigateColour = new Color(0.65f, 0.4f, 0.95f);

    [SerializeField] private Color chaseColour = new Color(0.95f, 0.8f, 0.2f);
    [SerializeField] private Color attackColour = new Color(0.9f, 0.2f, 0.2f);

    [Header("Label")]
    [Tooltip("World-space text above the enemy. Optional.")]
    [SerializeField] private TextMeshPro label;

    [Tooltip("Keeps the label facing the camera so it stays readable.")]
    [SerializeField] private bool billboardLabel = true;

    private CO_EnemyAI_Script ai;
    private MaterialPropertyBlock propertyBlock;
    private Camera mainCamera;

    // Cached so the state is only pushed to the renderer when it actually changes.
    private EnemyState? displayedState;

    // URP lit shaders use _BaseColor rather than the built-in _Color.
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        ai = GetComponent<CO_EnemyAI_Script>();

        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        propertyBlock = new MaterialPropertyBlock();
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        // LateUpdate so the state shown matches the movement already applied
        // this frame rather than lagging it by one.
        if (displayedState != ai.CurrentState)
        {
            displayedState = ai.CurrentState;
            Apply(ai.CurrentState);
        }

        if (billboardLabel && label != null)
        {
            FaceCamera();
        }
    }

    private void Apply(EnemyState state)
    {
        Color colour = ColourFor(state);

        if (targetRenderer != null)
        {
            // A property block tints this one renderer without creating a material
            // instance, so the shared material is left untouched.
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, colour);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        if (label != null)
        {
            label.text = state.ToString().ToUpperInvariant();
            label.color = colour;
        }
    }

    private Color ColourFor(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Attacking:
                return attackColour;
            case EnemyState.Chasing:
                return chaseColour;
            case EnemyState.Investigating:
                return investigateColour;
            case EnemyState.Alerted:
                return alertColour;
            case EnemyState.Patrolling:
            default:
                return patrolColour;
        }
    }

    private void FaceCamera()
    {
        if (mainCamera == null)
        {
            // Camera.main is null if the camera is untagged or was swapped at runtime.
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        // Rotated to match the camera's facing rather than looking at it, so the
        // text stays upright and does not skew near screen edges.
        label.transform.rotation = mainCamera.transform.rotation;
    }
}
