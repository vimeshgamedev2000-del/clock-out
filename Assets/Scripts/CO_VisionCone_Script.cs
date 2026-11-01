using UnityEngine;

/// <summary>
/// Draws a guard's vision cone on the floor, cut short by anything that blocks
/// its sight. A yellow band grows outward from the guard as the notice meter
/// fills, and the whole cone turns red once the player is noticed. Read-only:
/// it observes CO_EnemyDetection_Script and never influences it.
/// </summary>
public class CO_VisionCone_Script : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Sight to draw. Defaults to the one on a parent.")]
    [SerializeField] private CO_EnemyDetection_Script detection;

    [Tooltip("The whole cone, drawn at this object's height.")]
    [SerializeField] private MeshFilter baseMesh;

    [Tooltip("The notice band, drawn on top of the base.")]
    [SerializeField] private MeshFilter fillMesh;

    [Header("Shape")]
    [Tooltip("Rays cast across the cone each frame. More rays follow wall edges more closely.")]
    [SerializeField] private int rayCount = 40;

    [Tooltip("Metres the cone sits above the floor under the guard.")]
    [SerializeField] private float floorLift = 0.03f;

    [Tooltip("Metres the notice band sits above the base, so the two never flicker.")]
    [SerializeField] private float fillLift = 0.01f;

    [Header("Colours")]
    // Cool blue rather than white: white disappears on the beige carpet.
    [SerializeField] private Color unawareColour = new Color(0.45f, 0.75f, 1f, 0.3f);
    [SerializeField] private Color fillColour = new Color(1f, 0.8f, 0.1f, 0.5f);
    [SerializeField] private Color noticedColour = new Color(1f, 0.2f, 0.15f, 0.5f);

    [Tooltip("While the coworker ignores the player after handing over work: \"off duty\".")]
    [SerializeField] private Color ignoringColour = new Color(0.55f, 0.55f, 0.55f, 0.25f);

    private static readonly int BaseColourId = Shader.PropertyToID("_BaseColor");

    private Mesh coneShape;
    private Mesh fillShape;
    private MeshRenderer baseRenderer;
    private MeshRenderer fillRenderer;
    private MaterialPropertyBlock block;
    private float[] rayDistances;
    private Vector3[] rayDirections;
    private Vector3[] vertices;
    private int[] triangles;
    private Color currentColour;
    private float floorHeight;

    private void Awake()
    {
        if (detection == null)
        {
            detection = GetComponentInParent<CO_EnemyDetection_Script>();
        }

        rayCount = Mathf.Max(2, rayCount);
        rayDistances = new float[rayCount];
        rayDirections = new Vector3[rayCount];
        vertices = new Vector3[rayCount + 1];
        triangles = new int[(rayCount - 1) * 3];

        // A fan around vertex 0; built once because only the vertices move.
        for (int i = 0; i < rayCount - 1; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        coneShape = CreateMesh("Vision Cone");
        fillShape = CreateMesh("Vision Cone Fill");
        baseMesh.sharedMesh = coneShape;
        fillMesh.sharedMesh = fillShape;

        floorHeight = transform.position.y;

        baseRenderer = baseMesh.GetComponent<MeshRenderer>();
        fillRenderer = fillMesh.GetComponent<MeshRenderer>();
        block = new MaterialPropertyBlock();
        currentColour = unawareColour;
        SetColour(baseRenderer, unawareColour);
        SetColour(fillRenderer, fillColour);
    }

    private void OnDestroy()
    {
        Destroy(coneShape);
        Destroy(fillShape);
    }

    private void LateUpdate()
    {
        CastRays();

        bool noticed = detection.HasNoticedPlayer;
        float fillRadius = noticed ? 0f : detection.NoticeLevel * detection.DetectionRadius;

        UpdateMesh(coneShape, float.MaxValue, 0f);
        UpdateMesh(fillShape, fillRadius, fillLift);
        fillRenderer.enabled = fillRadius > 0f;

        Color colour = detection.IsIgnoringPlayer ? ignoringColour : noticed ? noticedColour : unawareColour;
        if (colour != currentColour)
        {
            currentColour = colour;
            SetColour(baseRenderer, colour);
        }
    }

    /// <summary>
    /// Casts flat rays at eye height across the cone, the same height and mask
    /// the sight test uses, so the cone stops exactly where sight would.
    /// </summary>
    private void CastRays()
    {
        Transform guard = detection.transform;
        Vector3 facing = guard.forward;
        facing.y = 0f;
        facing.Normalize();

        Vector3 eye = guard.position + Vector3.up * detection.EyeHeight;
        float radius = detection.DetectionRadius;

        // The agent's height above the floor differs between edit and play mode,
        // so find the floor rather than trusting a fixed offset. Everything on the
        // level is on the sight mask, the floor included.
        if (Physics.Raycast(eye, Vector3.down, out RaycastHit floor, 5f, detection.ObstacleMask, QueryTriggerInteraction.Ignore))
        {
            floorHeight = floor.point.y;
        }
        float halfAngle = detection.ViewAngle * 0.5f;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / (rayCount - 1));
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * facing;
            rayDirections[i] = direction;

            rayDistances[i] = Physics.Raycast(
                eye, direction, out RaycastHit hit, radius, detection.ObstacleMask, QueryTriggerInteraction.Ignore)
                ? hit.distance
                : radius;
        }
    }

    /// <summary>Rebuilds a fan from the cast rays, each capped at maxDistance.</summary>
    private void UpdateMesh(Mesh mesh, float maxDistance, float lift)
    {
        // Vertices are placed in world space on this object's plane, then moved
        // into local space, so the guard's scale and turning need no special care.
        Vector3 origin = detection.transform.position;
        origin.y = floorHeight + floorLift + lift;

        vertices[0] = transform.InverseTransformPoint(origin);

        for (int i = 0; i < rayCount; i++)
        {
            float distance = Mathf.Min(rayDistances[i], maxDistance);
            vertices[i + 1] = transform.InverseTransformPoint(origin + rayDirections[i] * distance);
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }

    private Mesh CreateMesh(string meshName)
    {
        var mesh = new Mesh { name = meshName };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        return mesh;
    }

    private void SetColour(MeshRenderer target, Color colour)
    {
        target.GetPropertyBlock(block);
        block.SetColor(BaseColourId, colour);
        target.SetPropertyBlock(block);
    }
}
