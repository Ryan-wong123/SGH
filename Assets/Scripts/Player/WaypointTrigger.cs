using UnityEngine;
using Photon.Pun;

public class WaypointTrigger : MonoBehaviour
{
    [Header("Path and waypoint index")]
    [SerializeField] private string pathId;
    [SerializeField] private int waypointIndex;

    [Header("Filter")]
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        // Only react for the LOCAL player
        PhotonView pv = other.GetComponentInParent<PhotonView>();
        if (pv != null && !pv.IsMine) return;

        if (ObjectivePointer.Instance != null)
        {
            ObjectivePointer.Instance.ReachWaypoint(pathId, waypointIndex);
        }
    }
}
