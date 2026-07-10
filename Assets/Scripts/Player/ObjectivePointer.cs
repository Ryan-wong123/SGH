using UnityEngine; // Imports Unity engine types such as MonoBehaviour, Transform, Camera, and Vector3.
using TMPro; // Imports TextMeshPro types if pointer UI text is added later.

public class ObjectivePointer : MonoBehaviour // Declares the component that points the player toward waypoint objectives.
{ // Starts the ObjectivePointer class body.
    public static ObjectivePointer Instance { get; private set; } // Stores the shared ObjectivePointer singleton instance.

    [System.Serializable] // Allows Unity to show waypoint paths in the inspector.
    public class WaypointPath // Defines one named sequence of waypoints.
    { // Starts the WaypointPath class body.
        public string id;              // unique name/key for this path // Stores the unique path ID used by other scripts.
        public Transform[] waypoints;  // ordered list of points // Stores the ordered waypoint transforms for this path.
    } // Ends the WaypointPath class body.

    // -------- Pointer / 3D Arrow --------
    [Header("Pointer References")] // Groups pointer object references in the Unity inspector.
    [SerializeField] private Transform player;        // local player // References the local player's transform.
    [SerializeField] private Transform arrowObject;   // 3D arrow object in world // References the world-space arrow transform.

    [Header("World Arrow Settings")] // Groups world arrow positioning settings in the Unity inspector.
    [SerializeField] private float forwardOffset = 1.5f;  // how far in front of the player // Sets how far in front of the player/camera the arrow appears.
    [SerializeField] private float heightOffset = 1.2f;   // how high above the player // Sets how high above the player/camera the arrow appears.

    // -------- Paths --------
    [Header("Available Paths")] // Groups available waypoint paths in the Unity inspector.
    [SerializeField] private WaypointPath[] paths; // Stores all configured objective waypoint paths.

    private int activePathIndex = -1; // Tracks which path is currently active, or -1 when none is active.
    private int currentWaypointIndex = -1; // Tracks the waypoint index currently being targeted.
    private Transform currentTarget; // Stores the transform the arrow currently points toward.

    public bool IsActive { get; private set; } // Reports whether an objective path is currently active.

    private Camera cam; // kept in case you want later use // Stores an optional camera reference for arrow placement.

    // ========== LIFECYCLE ==========
    private void Awake() // Runs before Start to initialize the singleton.
    { // Starts the Awake method body.
        if (Instance != null && Instance != this) // Checks whether another ObjectivePointer already exists.
        { // Starts the duplicate-instance block.
            Destroy(gameObject); // Destroys this duplicate pointer object.
            return; // Leaves because the singleton is already assigned.
        } // Ends the duplicate-instance block.

        Instance = this; // Registers this component as the singleton instance.
    } // Ends the Awake method body.

    private void Update() // Runs every frame to position and rotate the objective arrow.
    { // Starts the Update method body.
        // Need at least: arrow, target, and either player or cam
        if (arrowObject == null || currentTarget == null || (player == null && cam == null)) // Checks whether required arrow, target, or reference transforms are missing.
        { // Starts the missing-reference block.
            if (arrowObject != null && arrowObject.gameObject.activeSelf) // Checks whether the arrow can be hidden.
                arrowObject.gameObject.SetActive(false); // Hides the arrow when there is no valid target/reference.
            return; // Leaves because the arrow cannot be updated this frame.
        } // Ends the missing-reference block.

        // Use camera as reference if available, otherwise fallback to player
        Transform refTransform = cam != null ? cam.transform : player; // Chooses the camera transform when available, otherwise the player transform.

        // 1) Position arrow in front of the camera/player
        Vector3 basePos = refTransform.position // Starts the arrow base-position calculation from the reference position.
                        + refTransform.forward * forwardOffset // Offsets the arrow forward from the reference transform.
                        + Vector3.up * heightOffset; // Offsets the arrow upward in world space.

        arrowObject.position = basePos; // Moves the arrow to its world-space display position.

        // 2) Compute direction to target in full 3D space so different floors are shown.
        Vector3 toTarget = currentTarget.position - arrowObject.position; // Calculates the direction from the arrow to the current target.

        if (toTarget.sqrMagnitude < 0.001f) // Checks whether the arrow is effectively at the target.
        { // Starts the near-target block.
            arrowObject.gameObject.SetActive(false); // Hides the arrow when no meaningful direction can be shown.
            return; // Leaves because rotation would be unreliable.
        } // Ends the near-target block.

        // 3) Rotate arrow to face target
        Quaternion lookRot = Quaternion.LookRotation(toTarget.normalized, refTransform.up); // Calculates the rotation that points the arrow at the target.
        arrowObject.rotation = lookRot; // Applies the target-facing rotation to the arrow.

        if (!arrowObject.gameObject.activeSelf) // Checks whether the arrow is currently hidden.
            arrowObject.gameObject.SetActive(true); // Shows the arrow once it has a valid target and rotation.
    } // Ends the Update method body.

    // ========== PATH CONTROL ==========

    /// <summary>
    /// Start a path by id (e.g. "ToBedroom").
    /// </summary>
    public void StartPath(string pathId) // Starts an objective path by its configured ID.
    { // Starts the StartPath(string) method body.
        int index = FindPathIndex(pathId); // Looks up the configured path index for the requested ID.
        if (index == -1) // Checks whether the path ID was not found.
        { // Starts the missing-path block.
            Debug.LogWarning($"[ObjectiveNavigator] No path with id '{pathId}'"); // Logs the missing path ID.
            ClearTarget(); // Clears any existing arrow target.
            return; // Leaves because the requested path cannot start.
        } // Ends the missing-path block.

        StartPath(index); // Starts the path by its resolved index.
    } // Ends the StartPath(string) method body.

    /// <summary>
    /// Start a path by index (Inspector order).
    /// </summary>
    public void StartPath(int pathIndex) // Starts an objective path by inspector array index.
    { // Starts the StartPath(int) method body.
        if (paths == null || pathIndex < 0 || pathIndex >= paths.Length) // Checks whether the requested path index is invalid.
        { // Starts the invalid-path-index block.
            Debug.LogWarning($"[ObjectiveNavigator] Invalid path index {pathIndex}"); // Logs the invalid path index.
            ClearTarget(); // Clears any existing arrow target.
            return; // Leaves because the requested path cannot start.
        } // Ends the invalid-path-index block.

        if (paths[pathIndex].waypoints == null || paths[pathIndex].waypoints.Length == 0) // Checks whether the selected path has no waypoints.
        { // Starts the empty-path block.
            Debug.LogWarning($"[ObjectiveNavigator] Path '{paths[pathIndex].id}' has no waypoints"); // Logs the empty path configuration.
            ClearTarget(); // Clears any existing arrow target.
            return; // Leaves because the path has nowhere to point.
        } // Ends the empty-path block.

        activePathIndex = pathIndex; // Stores the active path index.
        currentWaypointIndex = 0; // Starts at the first waypoint in the path.
        IsActive = true; // Marks the objective pointer as active.

        SetTarget(paths[activePathIndex].waypoints[currentWaypointIndex]); // Points the arrow at the first waypoint.
        Debug.Log($"[ObjectiveNavigator] Started path '{paths[activePathIndex].id}', waypoint 0: {currentTarget.name}"); // Logs the started path and first waypoint.
    } // Ends the StartPath(int) method body.

    public void ReachWaypoint(string pathId, int waypointIndex) // Marks a waypoint reached for a specific path ID.
    { // Starts the ReachWaypoint(string, int) method body.
        int expectedPathIndex = FindPathIndex(pathId); // Resolves the path ID to its configured index.

        // Must belong to a valid path
        if (expectedPathIndex == -1) // Checks whether the path ID is invalid.
            return; // Ignores waypoint events for unknown paths.

        // If no active path yet, or a different path is currently active, ignore
        if (activePathIndex != expectedPathIndex) // Checks whether the reached waypoint belongs to the active path.
            return; // Ignores waypoint events for inactive paths.

        UpdateTargetFromReachedWaypoint(waypointIndex); // Advances the objective target from the reached waypoint.
    } // Ends the ReachWaypoint(string, int) method body.

    public void ReachWaypoint(int waypointIndex) // Marks a waypoint reached on the active path.
    { // Starts the ReachWaypoint(int) method body.
        // Overload if you don't care about id and only have one active path
        if (activePathIndex < 0 || activePathIndex >= paths.Length) // Checks whether there is a valid active path.
            return; // Ignores the waypoint event when no active path exists.

        UpdateTargetFromReachedWaypoint(waypointIndex); // Advances the objective target from the reached waypoint.
    } // Ends the ReachWaypoint(int) method body.

    private void UpdateTargetFromReachedWaypoint(int reachedWaypointIndex) // Updates the arrow target after a waypoint is reached.
    { // Starts the UpdateTargetFromReachedWaypoint method body.
        var path = paths[activePathIndex]; // Reads the currently active waypoint path.

        if (path.waypoints == null || path.waypoints.Length == 0) // Checks whether the active path has valid waypoints.
            return; // Leaves because there is no waypoint data to advance.

        if (reachedWaypointIndex < 0 || reachedWaypointIndex >= path.waypoints.Length) // Checks whether the reached waypoint index is valid.
            return; // Ignores invalid waypoint indices.

        Debug.Log($"[ObjectiveNavigator] Reached waypoint {reachedWaypointIndex} ({path.waypoints[reachedWaypointIndex].name})"); // Logs the reached waypoint.

        // If player reached the FINAL waypoint, complete the path and hide the arrow
        if (reachedWaypointIndex >= path.waypoints.Length - 1) // Checks whether the final waypoint was reached.
        { // Starts the path-complete block.
            currentWaypointIndex = path.waypoints.Length - 1; // Stores the final waypoint index.
            IsActive = false; // Marks the objective path as complete.
            ClearTarget();   // this makes Update() hide the arrow // Clears the target so the arrow hides.
            Debug.Log($"[ObjectiveNavigator] Final waypoint reached. Path '{path.id}' completed."); // Logs path completion.
            return; // Leaves because there is no next waypoint.
        } // Ends the path-complete block.

        // Otherwise point to the next waypoint
        currentWaypointIndex = reachedWaypointIndex + 1; // Advances to the next waypoint index.
        IsActive = true; // Keeps the objective pointer active.

        SetTarget(path.waypoints[currentWaypointIndex]); // Points the arrow at the next waypoint.
        Debug.Log($"[ObjectiveNavigator] Next waypoint {currentWaypointIndex}: {currentTarget.name}"); // Logs the next waypoint target.
    } // Ends the UpdateTargetFromReachedWaypoint method body.

    private int FindPathIndex(string id) // Finds the configured path index for a path ID.
    { // Starts the FindPathIndex method body.
        if (paths == null) return -1; // Reports no match when no paths are configured.
        for (int i = 0; i < paths.Length; i++) // Loops through each configured path.
        { // Starts the path search loop body.
            if (!string.IsNullOrEmpty(paths[i].id) && paths[i].id == id) // Checks whether this path ID matches the requested ID.
                return i; // Returns the matching path index.
        } // Ends the path search loop body.
        return -1; // Reports that no matching path ID was found.
    } // Ends the FindPathIndex method body.

    public void SetTarget(Transform t) // Sets the current objective target transform.
    { // Starts the SetTarget method body.
        currentTarget = t; // Stores the new arrow target.
        if (t != null) // Checks whether a target was provided.
            Debug.Log($"[ObjectiveNavigator] Target set to {t.name}"); // Logs the new target name.
        else // Runs when the target is cleared.
            Debug.Log("[ObjectiveNavigator] Target cleared"); // Logs that the objective target was cleared.
    } // Ends the SetTarget method body.

    public void ClearTarget() // Clears the current objective target.
    { // Starts the ClearTarget method body.
        SetTarget(null); // Sets the target to null so the arrow hides on update.
    } // Ends the ClearTarget method body.

    // ========== EXTERNAL HOOKS ==========

    public void SetPlayer(Transform newPlayer) // Assigns the local player transform used as an arrow reference.
    { // Starts the SetPlayer method body.
        player = newPlayer; // Stores the new player reference.
        Debug.Log($"[ObjectiveNavigator] Player set to: {newPlayer.name}"); // Logs the assigned player transform.
    } // Ends the SetPlayer method body.

    public void SetCamera(Camera newCam) // Assigns the camera used as the arrow reference when available.
    { // Starts the SetCamera method body.
        cam = newCam; // still here in case you use it later // Stores the camera reference for arrow placement.
    } // Ends the SetCamera method body.

    // Optionally, if you ever want to set the arrow from code:
    public void SetArrowObject(Transform newArrow) // Assigns the world-space arrow object from code.
    { // Starts the SetArrowObject method body.
        arrowObject = newArrow; // Stores the new arrow transform reference.
        Debug.Log($"[ObjectiveNavigator] Arrow object set to: {newArrow.name}"); // Logs the assigned arrow object.
    } // Ends the SetArrowObject method body.
} // Ends the ObjectivePointer class body.
