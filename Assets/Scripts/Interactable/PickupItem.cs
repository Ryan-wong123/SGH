using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

public class PickupItem : MonoBehaviourPun, IInteractable, IHighlightable
{
    [Header("Pickup Info")]
    [SerializeField] private string itemName = "Key";
    [SerializeField] private bool isClue = false;
    [SerializeField] private string notificationMessage = "";
    [FormerlySerializedAs("sharedCluePanelId")]
    [SerializeField] private string sharedDisplayId = "";

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
        if (view == null)
            view = gameObject.AddComponent<PhotonView>();

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
        string popupText = notificationMessage != "" ? notificationMessage : $"Picked up: {itemName}";

        CluePopupUI.Instance?.Show(popupText);

        photonView.RPC(nameof(SyncItemPickup), RpcTarget.AllBuffered, itemName, isClue);
        photonView.RPC(nameof(ShowPickupPopupRemote), RpcTarget.Others, popupText);

        if (TryGetSharedDisplayId(out string displayId))
            photonView.RPC(nameof(ShowSharedDisplay), RpcTarget.All, displayId);

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
            ClueSystem.Instance.RefreshUI();
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
        CluePopupUI.Instance?.Show(text);
    }

    [PunRPC]
    void ShowSharedDisplay(string displayId)
    {
        if (string.IsNullOrWhiteSpace(displayId))
            return;

        StartCoroutine(ShowSharedDisplaySafe(displayId));
    }

    private IEnumerator ShowSharedDisplaySafe(string displayId)
    {
        while (ClueUI.Instance == null && ClueSystem.Instance == null)
            yield return null;

        if (ClueUI.Instance != null && ClueUI.Instance.ShowById(displayId))
            yield break;

        if (ClueSystem.Instance != null && ClueSystem.Instance.TryShowClueImageById(displayId))
            yield break;

        Debug.LogWarning($"[PickupItem] No shared display found for id '{displayId}'.");
    }

    [PunRPC]
    void RequestDestroy()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
    }

    public string GetInteractionPrompt()
    {
        return $"Press E to pick up {itemName}";
    }

    private bool TryGetSharedDisplayId(out string displayId)
    {
        if (!string.IsNullOrWhiteSpace(sharedDisplayId))
        {
            displayId = sharedDisplayId.Trim();
            return true;
        }

        displayId = null;
        return false;
    }
}
