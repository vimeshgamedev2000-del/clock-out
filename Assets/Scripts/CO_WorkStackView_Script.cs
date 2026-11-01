using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Shows the work stack as a pile of folder models on the player's head (GDD §5.7,
/// §11 #2, #4, #5): handed-over folders drop in and squash, shredded ones pop
/// away, a dropped one tumbles to the floor, and from 8 the pile sways.
/// Read-only: it observes CO_WorkStack_Script and never changes it.
/// </summary>
public class CO_WorkStackView_Script : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The stack to show. Defaults to the one on this object.")]
    [SerializeField] private CO_WorkStack_Script stack;

    [Tooltip("Point on top of the head the pile is built on. Swaying tilts it.")]
    [SerializeField] private Transform anchor;

    [Tooltip("One folder model, spawned per folder carried.")]
    [SerializeField] private GameObject folderPrefab;

    [Header("Pile")]
    [Tooltip("Height of one folder in the pile, in metres (anchor space).")]
    [SerializeField] private float folderSpacing = 0.08f;

    [Tooltip("Random turn per folder around the vertical, in degrees, so the pile looks hand-stacked.")]
    [SerializeField] private float jitterAngle = 12f;

    [Header("Landing")]
    [Tooltip("Height above its slot a new folder falls from, in metres.")]
    [SerializeField] private float landHeight = 0.6f;

    [Tooltip("Seconds a new folder takes to land.")]
    [SerializeField] private float landTime = 0.18f;

    [Tooltip("Gap between folders landing in one catch, in seconds.")]
    [SerializeField] private float landStagger = 0.08f;

    [Tooltip("How much the pile squashes when a folder lands.")]
    [SerializeField] private float squash = 0.25f;

    [Header("Removing")]
    [Tooltip("Seconds a shredded or delegated folder takes to pop away.")]
    [SerializeField] private float popTime = 0.2f;

    [Tooltip("How far a dropped folder lands from the player, in metres.")]
    [SerializeField] private float dropDistance = 0.9f;

    [Tooltip("Seconds a dropped folder lies on the floor before it disappears.")]
    [SerializeField] private float dropLinger = 1.5f;

    [Header("Sway")]
    [Tooltip("Tilt of the pile at the first swaying folder, in degrees. Grows by the same amount per extra folder.")]
    [SerializeField] private float swayAngle = 3f;

    [Tooltip("Sway cycles per second.")]
    [SerializeField] private float swaySpeed = 1.6f;

    private readonly List<Transform> folders = new List<Transform>();
    private Vector3 anchorScale;
    private bool nextRemovalIsDrop;

    private void Awake()
    {
        if (stack == null)
        {
            stack = GetComponent<CO_WorkStack_Script>();
        }

        anchorScale = anchor != null ? anchor.localScale : Vector3.one;
    }

    private void OnEnable()
    {
        if (stack != null)
        {
            stack.FoldersChanged += OnFoldersChanged;
            stack.FolderDropped += OnFolderDropped;
        }
    }

    private void OnDisable()
    {
        if (stack != null)
        {
            stack.FoldersChanged -= OnFoldersChanged;
            stack.FolderDropped -= OnFolderDropped;
        }
    }

    private void LateUpdate()
    {
        if (anchor == null || stack == null)
        {
            return;
        }

        float tilt = 0f;
        if (stack.IsSwaying)
        {
            // Taller piles wobble harder.
            float amplitude = swayAngle * (1 + stack.Count - stack.SwayFolders);
            tilt = Mathf.Sin(Time.time * swaySpeed * Mathf.PI * 2f) * amplitude;
        }

        anchor.localRotation = Quaternion.Euler(0f, 0f, tilt);
    }

    private void OnFolderDropped()
    {
        nextRemovalIsDrop = true;
    }

    private void OnFoldersChanged(int previous, int current)
    {
        for (int i = previous; i < current; i++)
        {
            AddFolder(i, (i - previous) * landStagger);
        }

        for (int i = previous; i > current; i--)
        {
            RemoveTopFolder(nextRemovalIsDrop);
            nextRemovalIsDrop = false;
        }
    }

    private void AddFolder(int index, float delay)
    {
        if (folderPrefab == null || anchor == null)
        {
            return;
        }

        Transform folder = Instantiate(folderPrefab, anchor).transform;
        folder.localPosition = new Vector3(0f, index * folderSpacing + landHeight, 0f);
        folder.localRotation = Quaternion.Euler(0f, Random.Range(-jitterAngle, jitterAngle), 0f);
        folders.Add(folder);

        folder.DOLocalMoveY(index * folderSpacing, landTime)
            .SetDelay(delay)
            .SetEase(Ease.InQuad)
            .SetLink(folder.gameObject)
            .OnComplete(Squash);
    }

    private void Squash()
    {
        anchor.DOComplete();
        anchor.localScale = anchorScale;
        anchor.DOPunchScale(new Vector3(squash, -squash, squash), 0.2f, 4, 0.5f).SetLink(gameObject);
    }

    private void RemoveTopFolder(bool dropped)
    {
        if (folders.Count == 0)
        {
            return;
        }

        Transform folder = folders[folders.Count - 1];
        folders.RemoveAt(folders.Count - 1);
        folder.DOKill();

        if (!dropped)
        {
            folder.DOScale(0f, popTime).SetEase(Ease.InBack).SetLink(folder.gameObject)
                .OnComplete(() => Destroy(folder.gameObject));
            return;
        }

        // Off the head and onto the floor beside the player, tumbling as it goes.
        folder.SetParent(null, true);
        Vector2 side = Random.insideUnitCircle.normalized * dropDistance;
        Vector3 landing = new Vector3(transform.position.x + side.x, FloorHeight(), transform.position.z + side.y);

        folder.DOJump(landing, 0.5f, 1, 0.45f).SetLink(folder.gameObject);
        folder.DORotate(new Vector3(0f, Random.Range(0f, 360f), 90f), 0.45f, RotateMode.FastBeyond360)
            .SetLink(folder.gameObject);
        Destroy(folder.gameObject, 0.45f + dropLinger);
    }

    /// <summary>Where the player's feet are, so a dropped folder lies on the floor.</summary>
    private float FloorHeight()
    {
        return Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 5f)
            ? hit.point.y
            : transform.position.y;
    }
}
