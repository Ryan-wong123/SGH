using Photon.Pun;
using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private bool requiresKey = false;
    [SerializeField] private string keyName = "Key";

    private bool isOpen = false;


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

        // Add PhotonTransformView (optional, if you want to sync movement/rotation)
        if (GetComponent<PhotonTransformView>() == null)
        {
            var transformView = gameObject.AddComponent<PhotonTransformView>();
            view.ObservedComponents.Add(transformView);
        }

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
        if (isLocked)
        {
            Debug.Log("Door is locked.");
            return;
        }

        if (requiresKey && !InventorySystem.Instance.HasItem(keyName))
        {
            Debug.Log($"Door requires a {keyName}");
            return;
        }

        ToggleDoor();
    }

    public string GetInteractionPrompt()
    {
        if (isLocked)
        {
            return "This door is locked.";
        }

        if (requiresKey && !InventorySystem.Instance.HasItem(keyName))
        {
            return $"Door requires a {keyName}";
        }

        return isOpen ? "Press E to close door" : "Press E to open door";
    }

    private void ToggleDoor()
    {
        if (!isOpen)
        {
            transform.eulerAngles = new Vector3(0, 90, 0);
            isOpen = true;
        }
        else
        {
            transform.eulerAngles = new Vector3(0, 0, 0);
            isOpen = false;
        }
    }
}
