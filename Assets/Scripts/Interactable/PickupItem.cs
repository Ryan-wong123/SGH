using UnityEngine;
using Photon.Pun;
using System.Collections;

public class PickupItem : MonoBehaviourPun, IInteractable, IHighlightable
{
    [Header("Pickup Info")]
    [SerializeField] private string itemName = "Key";
    [SerializeField] private bool isClue = false;

    private HighlightEffect highlightEffect;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        SetupPhotonView();
        highlightEffect = GetComponent<HighlightEffect>();
    }

    public void Highlight() => highlightEffect?.Highlight();
    public void Unhighlight() => highlightEffect?.Unhighlight();

    void SetupPhotonView()
    {
        PhotonView view = GetComponent<PhotonView>();
        if (view == null) view = gameObject.AddComponent<PhotonView>();

        view.OwnershipTransfer = OwnershipOption.Fixed;
        view.Synchronization = ViewSynchronization.UnreliableOnChange;

        if (PhotonNetwork.IsConnectedAndReady && view.ViewID == 0)
        {
            PhotonNetwork.RegisterPhotonView(view);
            Debug.Log($"Registered scene object with ViewID: {view.ViewID}");
        }
    }

    public void Interact()
    {
        // ------ 1) POPUP (LOCAL) ------
        string popupText = isClue ? $"Clue found: {itemName}" : $"Picked up: {itemName}";
        CluePopupUI.Instance?.Show(popupText);

        // ------ 2) SYNC STATE (BUFFERED) ------
        photonView.RPC(nameof(SyncItemPickup), RpcTarget.AllBuffered, itemName, isClue);

        // ------ 3) POPUP (OTHERS, NON-BUFFERED) ------
        photonView.RPC(nameof(ShowPickupPopupRemote), RpcTarget.Others, popupText);

        // ------ 4) DESTROY VIA MASTER ------
        photonView.RPC(nameof(RequestDestroy), RpcTarget.MasterClient);
    }

    [PunRPC]
    void SyncItemPickup(string pickedName, bool pickedIsClue)
    {
        StartCoroutine(SafeSync(pickedName, pickedIsClue));
    }

    private IEnumerator SafeSync(string pickedName, bool pickedIsClue)
    {
        if (pickedIsClue)
        {
            while (ClueSystem.Instance == null)
            {
                Debug.Log("[PickupItem] Waiting for ClueSystem...");
                yield return null;
            }

            ClueSystem.Instance.AddClue(pickedName);
            ClueSystem.Instance.RefreshUI(); // omit if ClueSystem auto-updates
        }
        else
        {
            while (InventorySystem.Instance == null || InventoryUIManager.Instance == null)
            {
                Debug.Log("[PickupItem] Waiting for Inventory systems...");
                yield return null;
            }

            InventorySystem.Instance.AddItem(pickedName);
            InventoryUIManager.Instance.UpdateInventoryUI();
        }
    }

    [PunRPC]
    void ShowPickupPopupRemote(string text)
    {
        // Non-buffered: only current peers see it (no replay for late joiners)
        CluePopupUI.Instance?.Show(text);
    }

    [PunRPC]
    void RequestDestroy()
    {
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
