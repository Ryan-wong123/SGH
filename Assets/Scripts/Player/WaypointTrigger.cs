using UnityEngine; // Gives this script access to Unity engine types like MonoBehaviour, Collider, Header, and SerializeField.
using Photon.Pun; // Gives this script access to PhotonView so the trigger can check local player ownership.

public class WaypointTrigger : MonoBehaviour // Defines a Unity component that marks an objective waypoint as reached when the player enters its trigger.
{ // Starts the WaypointTrigger class body.
    [Header("Path and waypoint index")] // Adds a "Path and waypoint index" section header in the Unity Inspector.
    [SerializeField] private string pathId; // Stores the objective path identifier this waypoint belongs to.
    [SerializeField] private int waypointIndex; // Stores this waypoint's index within the objective path.

    [Header("Filter")] // Adds a "Filter" section header in the Unity Inspector.
    [SerializeField] private string playerTag = "Player"; // Stores the tag required for colliders that should activate this trigger.

    private void OnTriggerEnter(Collider other) // Runs when another collider enters this trigger collider.
    { // Starts the OnTriggerEnter method body.
        if (!other.CompareTag(playerTag)) // Checks whether the entering collider does not have the required player tag.
            return; // Stops because only tagged player colliders should activate this waypoint.

        // Only react for the LOCAL player // Explains why Photon ownership is checked before updating the objective.
        PhotonView pv = other.GetComponentInParent<PhotonView>(); // Gets the PhotonView from the entering collider or one of its parent objects.
        if (pv != null && !pv.IsMine) return; // Stops if the collider belongs to a remote network player.

        if (ObjectivePointer.Instance != null) // Checks whether the objective pointer singleton exists.
        { // Starts the objective-pointer update block.
            ObjectivePointer.Instance.ReachWaypoint(pathId, waypointIndex); // Tells the objective pointer that this waypoint has been reached.
        } // Ends the objective-pointer update block.
    } // Ends the OnTriggerEnter method body.
} // Ends the WaypointTrigger class body.
