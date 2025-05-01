using UnityEngine;
using Photon.Pun;

public class PickupItem : MonoBehaviourPun, IInteractable
{
    [Header("Pickup Info")]
    [SerializeField] private string itemName = "Key";
    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        SetupPhotonView();
    }

    void SetupPhotonView()
    {
        // Ensure PhotonView exists
        PhotonView view = GetComponent<PhotonView>();
        if (view == null)
        {
            view = gameObject.AddComponent<PhotonView>();
        }

        // Set Ownership Transfer to Fixed (scene object)
        view.OwnershipTransfer = OwnershipOption.Fixed;

        // Set Synchronization method (choose one)
        view.Synchronization = ViewSynchronization.UnreliableOnChange; // or .ReliableOnChange

        // Ensure this scene object is registered at runtime
        if (PhotonNetwork.IsConnectedAndReady && view.ViewID == 0)
        {
            PhotonNetwork.RegisterPhotonView(view);
            Debug.Log($"Registered scene object with ViewID: {view.ViewID}");
        }
    }

    public void Interact()
    {
        photonView.RPC(nameof(SyncItemPickup), RpcTarget.AllBuffered, itemName);
        photonView.RPC(nameof(RequestDestroy), RpcTarget.MasterClient);

    }

    [PunRPC]
    void SyncItemPickup(string itemName)
    {
        StartCoroutine(SafeSync(itemName));
    }

    System.Collections.IEnumerator SafeSync(string itemName)
    {
        while (InventorySystem.Instance == null || InventoryUIManager.Instance == null)
        {
            Debug.Log("[Sync] Still null...");
            yield return null;
        }

        Debug.Log($"Adding {itemName}");

        InventorySystem.Instance.AddItem(itemName);
        InventoryUIManager.Instance.UpdateInventoryUI();
    }

    [PunRPC]
    void RequestDestroy()
    {
        // Only MasterClient will execute this
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
    public string GetInteractionPrompt()
    {
        return $"Press E to pick up {itemName}";
    }
}
