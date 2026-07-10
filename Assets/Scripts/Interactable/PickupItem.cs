using System.Collections; // Imports IEnumerator so pickup syncing can wait across frames with coroutines.
using Photon.Pun; // Imports Photon networking types used for RPC calls and scene object syncing.
using UnityEngine; // Imports Unity engine types such as MonoBehaviour, GameObject, Debug, and LayerMask.
using UnityEngine.Serialization; // Imports FormerlySerializedAs so renamed serialized fields keep old inspector data.

public class PickupItem : MonoBehaviourPun, IInteractable, IHighlightable // Declares a networked pickup that can be interacted with and highlighted.
{ // Starts the PickupItem class body.
    [Header("Pickup Info")] // Adds an inspector header for the pickup configuration fields.
    [SerializeField] private string itemName = "Key"; // Stores the item or clue name that will be added when picked up.
    [SerializeField] private bool isClue = false; // Marks whether this pickup should go into the clue system instead of the inventory.
    [SerializeField] private string notificationMessage = ""; // Stores an optional custom popup message for this pickup.
    [FormerlySerializedAs("sharedCluePanelId")] // Preserves serialized values from the old sharedCluePanelId field name.
    [SerializeField] private string sharedDisplayId = ""; // Stores an optional clue display ID to show to all players after pickup.

    private HighlightEffect highlightEffect; // Caches the highlight component used when the player looks at this pickup.

    private void Awake() // Runs Unity setup when this pickup object is initialized.
    { // Starts the Awake method body.
        gameObject.layer = LayerMask.NameToLayer("Interactable"); // Places the pickup on the Interactable layer for interaction raycasts.
        SetupPhotonView(); // Ensures the pickup has a PhotonView configured for network RPC behavior.
        highlightEffect = GetComponent<HighlightEffect>(); // Finds and stores the optional highlight effect on this object.
    } // Ends the Awake method body.

    public void Highlight() => highlightEffect?.Highlight(); // Turns on the highlight effect if one is available.
    public void Unhighlight() => highlightEffect?.Unhighlight(); // Turns off the highlight effect if one is available.

    void SetupPhotonView() // Configures the PhotonView required for multiplayer pickup synchronization.
    { // Starts the SetupPhotonView method body.
        PhotonView view = GetComponent<PhotonView>(); // Looks for an existing PhotonView on this pickup.
        if (view == null) // Checks whether the pickup is missing a PhotonView.
            view = gameObject.AddComponent<PhotonView>(); // Adds a PhotonView so RPC calls can be sent from this pickup.

        view.OwnershipTransfer = OwnershipOption.Fixed; // Keeps ownership fixed so clients do not transfer control of this scene object.
        view.Synchronization = ViewSynchronization.UnreliableOnChange; // Uses lightweight state synchronization only when values change.

        if (PhotonNetwork.IsConnectedAndReady && view.ViewID == 0) // Registers the view only when connected and the view has no assigned ID.
        { // Starts the Photon registration block.
            PhotonNetwork.RegisterPhotonView(view); // Registers this scene object's PhotonView with the current Photon room.
            Debug.Log($"Registered scene object with ViewID: {view.ViewID}"); // Logs the assigned Photon ViewID for debugging.
        } // Ends the Photon registration block.
    } // Ends the SetupPhotonView method body.

    public void Interact() // Handles the player interaction that picks up this item.
    { // Starts the Interact method body.
        string popupText = notificationMessage != "" ? notificationMessage : $"Picked up: {itemName}"; // Chooses a custom popup message or falls back to the item name.

        CluePopupUI.Instance?.Show(popupText); // Shows the pickup popup immediately for the local player if the popup UI exists.

        photonView.RPC(nameof(SyncItemPickup), RpcTarget.AllBuffered, itemName, isClue); // Tells every player, including late joiners, to add the item or clue.
        photonView.RPC(nameof(ShowPickupPopupRemote), RpcTarget.Others, popupText); // Shows the pickup popup on the other connected clients.

        if (TryGetSharedDisplayId(out string displayId)) // Checks whether this pickup has a shared clue display to show.
            photonView.RPC(nameof(ShowSharedDisplay), RpcTarget.All, displayId); // Tells all players to open the configured shared display.

        photonView.RPC(nameof(RequestDestroy), RpcTarget.MasterClient); // Asks the master client to destroy the picked-up object across the room.
    } // Ends the Interact method body.

    [PunRPC] // Allows Photon to invoke this method remotely.
    void SyncItemPickup(string pickedName, bool pickedIsClue) // Receives the item data that should be synced into local systems.
    { // Starts the SyncItemPickup method body.
        StartCoroutine(SafeSync(pickedName, pickedIsClue)); // Starts a coroutine so missing systems can finish initializing before sync.
    } // Ends the SyncItemPickup method body.

    private IEnumerator SafeSync(string pickedName, bool pickedIsClue) // Waits for the correct local system before adding the picked-up content.
    { // Starts the SafeSync coroutine body.
        if (pickedIsClue) // Branches into clue handling when this pickup represents a clue.
        { // Starts the clue pickup block.
            while (ClueSystem.Instance == null) // Waits until the clue system singleton is available.
            { // Starts the clue-system wait loop body.
                Debug.Log("[PickupItem] Waiting for ClueSystem..."); // Logs that the pickup is waiting for clue system initialization.
                yield return null; // Waits one frame before checking for the clue system again.
            } // Ends the clue-system wait loop body.

            ClueSystem.Instance.AddClue(pickedName); // Adds the picked clue name to the clue system.
            ClueSystem.Instance.RefreshUI(); // Refreshes clue UI so the new clue appears.
        } // Ends the clue pickup block.
        else // Branches into inventory handling when this pickup is a normal item.
        { // Starts the inventory pickup block.
            while (InventorySystem.Instance == null || InventoryUIManager.Instance == null) // Waits until both inventory logic and UI are ready.
            { // Starts the inventory-system wait loop body.
                Debug.Log("[PickupItem] Waiting for Inventory systems..."); // Logs that the pickup is waiting for inventory initialization.
                yield return null; // Waits one frame before checking the inventory systems again.
            } // Ends the inventory-system wait loop body.

            InventorySystem.Instance.AddItem(pickedName); // Adds the picked item name to the inventory system.
            InventoryUIManager.Instance.UpdateInventoryUI(); // Refreshes the inventory UI so the new item appears.
        } // Ends the inventory pickup block.
    } // Ends the SafeSync coroutine body.

    [PunRPC] // Allows Photon to invoke this method remotely.
    void ShowPickupPopupRemote(string text) // Shows the pickup popup on clients that did not initiate the pickup.
    { // Starts the ShowPickupPopupRemote method body.
        CluePopupUI.Instance?.Show(text); // Displays the popup text if the clue popup UI is available.
    } // Ends the ShowPickupPopupRemote method body.

    [PunRPC] // Allows Photon to invoke this method remotely.
    void ShowSharedDisplay(string displayId) // Requests that a shared clue display be shown for a specific ID.
    { // Starts the ShowSharedDisplay method body.
        if (string.IsNullOrWhiteSpace(displayId)) // Checks whether the display ID is missing or only whitespace.
            return; // Stops because there is no valid shared display to show.

        StartCoroutine(ShowSharedDisplaySafe(displayId)); // Starts a coroutine that waits for clue UI systems before showing the display.
    } // Ends the ShowSharedDisplay method body.

    private IEnumerator ShowSharedDisplaySafe(string displayId) // Waits for available clue display systems and tries to show the requested ID.
    { // Starts the ShowSharedDisplaySafe coroutine body.
        while (ClueUI.Instance == null && ClueSystem.Instance == null) // Waits until at least one clue display system is available.
            yield return null; // Waits one frame before checking clue display systems again.

        if (ClueUI.Instance != null && ClueUI.Instance.ShowById(displayId)) // Tries to show the display through the clue UI manager first.
            yield break; // Stops the coroutine because the clue UI successfully showed the display.

        if (ClueSystem.Instance != null && ClueSystem.Instance.TryShowClueImageById(displayId)) // Falls back to showing the display through the clue system.
            yield break; // Stops the coroutine because the clue system successfully showed the display.

        Debug.LogWarning($"[PickupItem] No shared display found for id '{displayId}'."); // Warns when no system can resolve the shared display ID.
    } // Ends the ShowSharedDisplaySafe coroutine body.

    [PunRPC] // Allows Photon to invoke this method remotely.
    void RequestDestroy() // Handles the networked destroy request for the picked-up object.
    { // Starts the RequestDestroy method body.
        if (PhotonNetwork.IsMasterClient) // Ensures only the master client destroys the networked object.
            PhotonNetwork.Destroy(gameObject); // Destroys the pickup object across the Photon room.
    } // Ends the RequestDestroy method body.

    public string GetInteractionPrompt() // Provides the text shown when the player can pick up this item.
    { // Starts the GetInteractionPrompt method body.
        return $"Press E to pick up {itemName}"; // Returns the interaction prompt using this pickup's configured item name.
    } // Ends the GetInteractionPrompt method body.

    private bool TryGetSharedDisplayId(out string displayId) // Tries to provide a trimmed shared display ID when one is configured.
    { // Starts the TryGetSharedDisplayId method body.
        if (!string.IsNullOrWhiteSpace(sharedDisplayId)) // Checks whether the serialized shared display ID has useful text.
        { // Starts the valid-display-ID block.
            displayId = sharedDisplayId.Trim(); // Removes surrounding whitespace from the configured shared display ID.
            return true; // Reports that a valid shared display ID is available.
        } // Ends the valid-display-ID block.

        displayId = null; // Outputs null when no shared display ID is configured.
        return false; // Reports that there is no shared display ID to use.
    } // Ends the TryGetSharedDisplayId method body.
} // Ends the PickupItem class body.
