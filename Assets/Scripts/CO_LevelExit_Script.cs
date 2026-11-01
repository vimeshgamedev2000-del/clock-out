using UnityEngine;

/// <summary>
/// The elevator cab behind CO_MainDoor_Script. Stepping into it while the door is
/// unlocked clocks the player out (GDD §3, §8). The zone is this object's
/// collider bounds, checked flat, so the player's height doesn't matter and no
/// physics callback is needed.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CO_LevelExit_Script : MonoBehaviour
{
    [Tooltip("The elevator door. The exit only counts while it is unlocked.")]
    [SerializeField] private CO_MainDoor_Script door;

    [Tooltip("Ends the floor and shows the timesheet.")]
    [SerializeField] private CO_LevelFlow_Script levelFlow;

    [Tooltip("Tag identifying the player object.")]
    [SerializeField] private string playerTag = "Player";

    private Collider zone;
    private Transform player;

    private void Awake()
    {
        zone = GetComponent<Collider>();

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogWarning($"{name}: no object tagged '{playerTag}', so nobody can clock out.", this);
        }
    }

    private void Update()
    {
        if (player == null || levelFlow.HasEnded || !door.IsUnlocked)
        {
            return;
        }

        Bounds bounds = zone.bounds;
        Vector3 point = player.position;
        point.y = bounds.center.y;

        if (bounds.Contains(point))
        {
            levelFlow.ClockOut();
        }
    }

    private void OnDrawGizmos()
    {
        Collider box = GetComponent<Collider>();
        if (box == null)
        {
            return;
        }

        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.35f);
        Gizmos.DrawCube(box.bounds.center, box.bounds.size);
    }
}
