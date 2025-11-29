using UnityEngine;
using TMPro;

public class ObjectivePointer : MonoBehaviour
{
    public static ObjectivePointer Instance { get; private set; }

    [System.Serializable]
    public class WaypointPath
    {
        public string id;              // unique name/key for this path
        public Transform[] waypoints;  // ordered list of points
    }

    // -------- Pointer / 3D Arrow --------
    [Header("Pointer References")]
    [SerializeField] private Transform player;        // local player
    [SerializeField] private Transform arrowObject;   // 3D arrow object in world

    [Header("World Arrow Settings")]
    [SerializeField] private float forwardOffset = 1.5f;  // how far in front of the player
    [SerializeField] private float heightOffset = 1.2f;  // how high above the player

    // -------- Paths --------
    [Header("Available Paths")]
    [SerializeField] private WaypointPath[] paths;

    private int activePathIndex = -1;
    private int currentWaypointIndex = -1;
    private Transform currentTarget;

    public bool IsActive { get; private set; }

    private Camera cam; // kept in case you want later use

    // ========== LIFECYCLE ==========
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        // Need at least: arrow, target, and either player or cam
        if (arrowObject == null || currentTarget == null || (player == null && cam == null))
        {
            if (arrowObject != null && arrowObject.gameObject.activeSelf)
                arrowObject.gameObject.SetActive(false);
            return;
        }

        // Use camera as reference if available, otherwise fallback to player
        Transform refTransform = cam != null ? cam.transform : player;

        // 1) Position arrow in front of the camera/player
        Vector3 basePos = refTransform.position
                        + refTransform.forward * forwardOffset
                        + Vector3.up * heightOffset;

        arrowObject.position = basePos;

        // 2) Compute direction to target on horizontal plane
        Vector3 toTarget = currentTarget.position - refTransform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.001f)
        {
            arrowObject.gameObject.SetActive(false);
            return;
        }

        // 3) Rotate arrow to face target
        Quaternion lookRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        arrowObject.rotation = lookRot;

        if (!arrowObject.gameObject.activeSelf)
            arrowObject.gameObject.SetActive(true);
    }


    // ========== PATH CONTROL ==========

    /// <summary>
    /// Start a path by id (e.g. "ToBedroom").
    /// </summary>
    public void StartPath(string pathId)
    {
        int index = FindPathIndex(pathId);
        if (index == -1)
        {
            Debug.LogWarning($"[ObjectiveNavigator] No path with id '{pathId}'");
            ClearTarget();
            return;
        }

        StartPath(index);
    }

    /// <summary>
    /// Start a path by index (Inspector order).
    /// </summary>
    public void StartPath(int pathIndex)
    {
        if (paths == null || pathIndex < 0 || pathIndex >= paths.Length)
        {
            Debug.LogWarning($"[ObjectiveNavigator] Invalid path index {pathIndex}");
            ClearTarget();
            return;
        }

        if (paths[pathIndex].waypoints == null || paths[pathIndex].waypoints.Length == 0)
        {
            Debug.LogWarning($"[ObjectiveNavigator] Path '{paths[pathIndex].id}' has no waypoints");
            ClearTarget();
            return;
        }

        activePathIndex = pathIndex;
        currentWaypointIndex = 0;
        IsActive = true;

        SetTarget(paths[activePathIndex].waypoints[currentWaypointIndex]);
        Debug.Log($"[ObjectiveNavigator] Started path '{paths[activePathIndex].id}', waypoint 0: {currentTarget.name}");
    }

    public void ReachWaypoint(string pathId, int waypointIndex)
    {
        // Only react if this is the active path
        if (!IsActive)
            return;

        int expectedPathIndex = FindPathIndex(pathId);
        if (expectedPathIndex != activePathIndex)
            return;

        if (waypointIndex != currentWaypointIndex)
            return;

        AdvanceWaypoint();
    }

    public void ReachWaypoint(int waypointIndex)
    {
        // Overload if you don't care about id and only have one active path
        if (!IsActive || waypointIndex != currentWaypointIndex)
            return;

        AdvanceWaypoint();
    }

    private void AdvanceWaypoint()
    {
        var path = paths[activePathIndex];

        Debug.Log($"[ObjectiveNavigator] Reached waypoint {currentWaypointIndex} ({path.waypoints[currentWaypointIndex].name})");

        currentWaypointIndex++;

        if (currentWaypointIndex < path.waypoints.Length)
        {
            SetTarget(path.waypoints[currentWaypointIndex]);
            Debug.Log($"[ObjectiveNavigator] Next waypoint {currentWaypointIndex}: {currentTarget.name}");
        }
        else
        {
            Debug.Log($"[ObjectiveNavigator] Completed path '{path.id}'");
            ClearTarget();
            IsActive = false;
        }
    }

    private int FindPathIndex(string id)
    {
        if (paths == null) return -1;
        for (int i = 0; i < paths.Length; i++)
        {
            if (!string.IsNullOrEmpty(paths[i].id) && paths[i].id == id)
                return i;
        }
        return -1;
    }

    public void SetTarget(Transform t)
    {
        currentTarget = t;
        if (t != null)
            Debug.Log($"[ObjectiveNavigator] Target set to {t.name}");
        else
            Debug.Log("[ObjectiveNavigator] Target cleared");
    }

    public void ClearTarget()
    {
        SetTarget(null);
    }

    // ========== EXTERNAL HOOKS ==========

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
        Debug.Log($"[ObjectiveNavigator] Player set to: {newPlayer.name}");
    }

    public void SetCamera(Camera newCam)
    {
        cam = newCam; // still here in case you use it later
    }

    // Optionally, if you ever want to set the arrow from code:
    public void SetArrowObject(Transform newArrow)
    {
        arrowObject = newArrow;
        Debug.Log($"[ObjectiveNavigator] Arrow object set to: {newArrow.name}");
    }
}
