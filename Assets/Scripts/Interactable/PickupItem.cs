using UnityEngine;
using Photon.Pun;

public class PickupItem : MonoBehaviourPun, IInteractable
{
    [Header("Pickup Info")]
    public string itemName = "Key";
    private void Awake()
    {
        // Attach PhotonView if missing
        if (GetComponent<PhotonView>() == null)
        {
            gameObject.AddComponent<PhotonView>();
        }
    }

    public void Interact()
    {
        Debug.Log($"Picked up: {itemName}");
        InventorySystem.AddItem(itemName);

        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    public string GetInteractionPrompt()
    {
        return $"Press E to pick up {itemName}";
    }
}
