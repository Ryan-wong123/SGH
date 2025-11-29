using UnityEngine;
// using Photon.Pun; // uncomment if you want Photon checks

public class WaypointTrigger : MonoBehaviour
{
    [Header("Path and waypoint index")]
    [SerializeField] private string pathId;      // must match WaypointPath.id
    [SerializeField] private int waypointIndex;  // index in that path

    [Header("Filter")]
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        // Photon filtering (optional) can go here

        if (ObjectivePointer.Instance != null)
        {
            ObjectivePointer.Instance.ReachWaypoint(pathId, waypointIndex);
        }
    }
}
