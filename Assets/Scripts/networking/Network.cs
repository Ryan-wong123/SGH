using UnityEngine; // Imports Unity engine types such as MonoBehaviour and GameObject.
using Photon.Pun; // Imports Photon PUN networking classes.
using Photon.Realtime; // Imports Photon realtime classes such as Player and DisconnectCause.
using UnityEngine.UI; // Imports Unity UI components such as Button.
using TMPro; // Imports TextMeshPro UI components.
using ExitGames.Client.Photon; // Imports Photon client authentication and data types.
using System; // Imports core .NET types such as Guid.


public class Network : MonoBehaviourPunCallbacks // Defines a Photon callback-enabled Unity component.
{ // Starts the Network class body.
    private const string PlayerTagPropertyKey = "PlayerTag"; // Names the custom Photon property that stores the player's display tag.
    private const string PhotonUserIdPrefKey = "PhotonUserId"; // Names the PlayerPrefs key used to persist the Photon user ID.
    private const string RejoinRoomPrefKey = "PhotonRejoinRoom"; // Names the PlayerPrefs key used to remember a room for rejoining.
    private const string LegacyPhotonNicknamePrefKey = "PhotonNickname"; // Names the old PlayerPrefs key that should be cleared.

    //game version
    [SerializeField] private string gameVersion = "1.0"; // Exposes the Photon game version in the Unity inspector.
    //maximum number of player in 1 room
    [SerializeField] protected byte maxPlayersPerRoom = 4; // Exposes the maximum room size in the Unity inspector.

    //home panel 
    [SerializeField] private GameObject homePanel; // References the first screen shown before room selection.
    //username input field
    [SerializeField] private TMP_InputField usernameInput; // References the input field where the player types a username.
    //button to go to room list panel
    [SerializeField] private Button goToRoomListButton; // References the button that proceeds to room selection.
    //join room manager component
    [SerializeField] private JoinRoomManager joinRoomManager; // References the component that controls room list UI.
    private enum HomeEntryStep { Username }
    private HomeEntryStep homeEntryStep; // Tracks the active field in the player-name flow.
    private string pendingUsername; // Stores a validated username before entering the room list.
    private string initialInputPlaceholder; // Restores the username prompt before the email-entry step.
    private string initialContinueButtonLabel; // Restores the original button label before the email-entry step.
    private static bool isAttemptingStoredRoomRejoin; // Tracks whether the current join attempt is from saved room data.
    private static bool isReconnectingToMaster; // Tracks whether a reconnect attempt is already in progress.
    private static bool leavingRejectedStoredRoomRejoin; // Tracks whether leaving a rejected rejoin should return to the lobby.

    private void Start() // Runs once when Unity starts this component.
    { // Starts the Start method body.
        EnsurePersistentUserId(); // Makes sure this client has a stable Photon user ID.
        ClearLegacySavedNickname(); // Removes old saved nickname data and clears nickname UI.
        EnsureChatManagerExists(); // Creates a ChatManager if one is not already present.

        //connect to photon server with settings
        Debug.Log("Connecting to Photon..."); // Writes a connection status message to the Unity console.
        PhotonNetwork.AutomaticallySyncScene = true; // Enables Photon scene synchronization for all players.
        PhotonNetwork.GameVersion = gameVersion; // Sets the Photon game version used for matchmaking compatibility.

        // If already connected, don't reconnect (reconnecting can kick you out / reset state)
        if (PhotonNetwork.IsConnected) // Checks whether Photon is already connected.
        { // Starts the already-connected branch.
            Debug.Log("Already connected. Ensuring lobby..."); // Logs that no new connection is needed.
            EnsureLobbyJoined(); // Joins the lobby if the client is ready and not already in one.
        } // Ends the already-connected branch.
        else // Handles the case where Photon is not connected yet.
        { // Starts the fresh-connection branch.
            Debug.Log("Connecting to Photon..."); // Logs that a new Photon connection is starting.
            PhotonNetwork.ConnectUsingSettings(); // Connects to Photon using the configured project settings.
        } // Ends the fresh-connection branch.

        initialInputPlaceholder = (usernameInput.placeholder as TextMeshProUGUI)?.text;
        initialContinueButtonLabel = goToRoomListButton.GetComponentInChildren<TMP_Text>(true)?.text;

        // Collect only the display name; all email settings are held only by the backend.
        goToRoomListButton.onClick.RemoveAllListeners();
        goToRoomListButton.onClick.AddListener(HandleHomePanelContinue);
    } // Ends the Start method body.

    private void HandleHomePanelContinue()
    {
        switch (homeEntryStep)
        {
            case HomeEntryStep.Username:
                if (string.IsNullOrWhiteSpace(usernameInput.text))
                {
                    ShowEntryError("username is required");
                    return;
                }

                pendingUsername = usernameInput.text.Trim();
                CompleteHomeEntry();
                return;
        }
    }

    private void ShowNextEntryStep(HomeEntryStep nextStep, string placeholder)
    {
        homeEntryStep = nextStep;
        usernameInput.text = string.Empty;
        usernameInput.contentType = TMP_InputField.ContentType.Standard;
        if (usernameInput.placeholder is TextMeshProUGUI inputPlaceholder)
            inputPlaceholder.text = placeholder;
        SetHomePanelButtonLabel("Continue");
        usernameInput.ActivateInputField();
    }

    private void ShowEntryError(string message)
    {
        if (usernameInput.placeholder is TextMeshProUGUI inputPlaceholder)
            inputPlaceholder.text = message;
        usernameInput.text = string.Empty;
    }

    private void CompleteHomeEntry()
    {
        PhotonNetwork.NickName = pendingUsername;
        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            { PlayerTagPropertyKey, pendingUsername }
        });

        homePanel.SetActive(false);
        joinRoomManager.setRoomListPanelActive(true);
        joinRoomManager.setToggleRoomListPanelButtonActive(true);
        EnsureLobbyJoined();
    }

    private void SetHomePanelButtonLabel(string value)
    {
        TMP_Text label = goToRoomListButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = value;
    }

    private static void EnsureChatManagerExists() // Ensures a ChatManager object exists in the scene.
    { // Starts the EnsureChatManagerExists method body.
        if (ChatManager.Instance != null || FindAnyObjectByType<ChatManager>() != null) // Checks for an existing ChatManager singleton or scene object.
        { // Starts the existing-manager branch.
            return; // Leaves because no new ChatManager is needed.
        } // Ends the existing-manager branch.

        new GameObject("ChatManager").AddComponent<ChatManager>(); // Creates a ChatManager GameObject and attaches the component.
    } // Ends the EnsureChatManagerExists method body.

    public static void CacheCurrentRoomForRejoin() // Stores the current room name so the player can rejoin later.
    { // Starts the CacheCurrentRoomForRejoin method body.
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) // Checks whether there is an active Photon room to cache.
        { // Starts the no-room branch.
            return; // Leaves because there is no room to save.
        } // Ends the no-room branch.

        if (!HasOtherActivePlayersInRoom()) // Checks whether any other active player remains in the room.
        { // Starts the no-other-players branch.
            ClearCachedRoomRejoin(); // Clears the saved room because rejoining would not be useful.
            return; // Stops after clearing the cache.
        } // Ends the no-other-players branch.

        PlayerPrefs.SetString(RejoinRoomPrefKey, PhotonNetwork.CurrentRoom.Name); // Saves the current room name in PlayerPrefs.
        PlayerPrefs.Save(); // Flushes PlayerPrefs changes to disk.
    } // Ends the CacheCurrentRoomForRejoin method body.

    public static void ClearCachedRoomRejoin() // Clears saved room rejoin state.
    { // Starts the ClearCachedRoomRejoin method body.
        PlayerPrefs.DeleteKey(RejoinRoomPrefKey); // Removes the saved room name from PlayerPrefs.
        PlayerPrefs.Save(); // Flushes the PlayerPrefs deletion to disk.
        isAttemptingStoredRoomRejoin = false; // Resets the saved-room rejoin attempt flag.
        isReconnectingToMaster = false; // Resets the reconnect-in-progress flag.
        leavingRejectedStoredRoomRejoin = false; // Resets the rejected-rejoin leave flag.
    } // Ends the ClearCachedRoomRejoin method body.

    public static string GetCachedRoomName() // Reads the saved room name from PlayerPrefs.
    { // Starts the GetCachedRoomName method body.
        return PlayerPrefs.GetString(RejoinRoomPrefKey, string.Empty); // Returns the saved room name or an empty string.
    } // Ends the GetCachedRoomName method body.

    public static bool HasOtherActivePlayersInRoom() // Checks whether another active player is still in the current room.
    { // Starts the HasOtherActivePlayersInRoom method body.
        foreach (Player player in PhotonNetwork.PlayerList) // Loops through every player currently listed by Photon.
        { // Starts the player loop body.
            if (player == null || player == PhotonNetwork.LocalPlayer || player.IsInactive) // Skips null, local, or inactive players.
            { // Starts the skip-player branch.
                continue; // Moves to the next player.
            } // Ends the skip-player branch.

            return true; // Reports that another active player exists.
        } // Ends the player loop body.

        return false; // Reports that no other active players were found.
    } // Ends the HasOtherActivePlayersInRoom method body.

    public static void EnsurePersistentUserId() // Ensures Photon has a stable user ID across sessions.
    { // Starts the EnsurePersistentUserId method body.
        string userId = PlayerPrefs.GetString(PhotonUserIdPrefKey, string.Empty); // Reads the saved Photon user ID from PlayerPrefs.
        if (string.IsNullOrWhiteSpace(userId)) // Checks whether a user ID needs to be created.
        { // Starts the new-user-id branch.
            userId = Guid.NewGuid().ToString("N"); // Creates a new compact GUID for Photon authentication.
            PlayerPrefs.SetString(PhotonUserIdPrefKey, userId); // Saves the new Photon user ID in PlayerPrefs.
            PlayerPrefs.Save(); // Flushes the new user ID to disk.
        } // Ends the new-user-id branch.

        PhotonNetwork.AuthValues ??= new AuthenticationValues(); // Creates Photon authentication values if missing.
        PhotonNetwork.AuthValues.UserId = userId; // Applies the persistent user ID to Photon authentication.
    } // Ends the EnsurePersistentUserId method body.

    private void ClearLegacySavedNickname() // Clears old nickname persistence and resets current nickname fields.
    { // Starts the ClearLegacySavedNickname method body.
        if (PlayerPrefs.HasKey(LegacyPhotonNicknamePrefKey)) // Checks whether the old nickname key still exists.
        { // Starts the legacy-key branch.
            PlayerPrefs.DeleteKey(LegacyPhotonNicknamePrefKey); // Deletes the obsolete saved nickname.
            PlayerPrefs.Save(); // Flushes the PlayerPrefs deletion to disk.
        } // Ends the legacy-key branch.

        if (usernameInput != null) // Checks whether the username input reference is assigned.
        { // Starts the input-reset branch.
            usernameInput.text = string.Empty; // Clears any visible username text in the input field.
            usernameInput.contentType = TMP_InputField.ContentType.Standard;
            if (usernameInput.placeholder is TextMeshProUGUI placeholderText && !string.IsNullOrWhiteSpace(initialInputPlaceholder))
                placeholderText.text = initialInputPlaceholder;
        } // Ends the input-reset branch.

        homeEntryStep = HomeEntryStep.Username;
        pendingUsername = string.Empty;
        SessionSmtpCredentials.Clear();
        if (!string.IsNullOrWhiteSpace(initialContinueButtonLabel))
            SetHomePanelButtonLabel(initialContinueButtonLabel);
        PhotonNetwork.NickName = string.Empty; // Clears the Photon nickname for this session.
    } // Ends the ClearLegacySavedNickname method body.

    public static void ApplyLocalPlayerTag() // Applies the Photon nickname to the local player's custom tag.
    { // Starts the ApplyLocalPlayerTag method body.
        if (PhotonNetwork.LocalPlayer == null || string.IsNullOrWhiteSpace(PhotonNetwork.NickName)) // Checks whether a local player and nickname are available.
        { // Starts the cannot-apply branch.
            return; // Leaves because there is no valid tag to apply.
        } // Ends the cannot-apply branch.

        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable // Starts setting the local player's custom Photon properties.
        { // Starts the custom property table.
            { PlayerTagPropertyKey, PhotonNetwork.NickName } // Stores the current Photon nickname as the player tag.
        }); // Finishes applying the custom player properties.
    } // Ends the ApplyLocalPlayerTag method body.

    public static bool TryJoinLobby()
    {
        if (PhotonNetwork.InRoom || PhotonNetwork.InLobby)
            return false;

        if (PhotonNetwork.NetworkClientState != ClientState.ConnectedToMasterServer)
            return false;

        return PhotonNetwork.JoinLobby();
    }

    private bool TryRejoinCachedRoom() // Attempts to rejoin the room saved in PlayerPrefs.
    { // Starts the TryRejoinCachedRoom method body.
        string cachedRoomName = GetCachedRoomName(); // Reads the saved room name.
        if (string.IsNullOrWhiteSpace(cachedRoomName) || !PhotonNetwork.IsConnectedAndReady || PhotonNetwork.InRoom) // Checks whether rejoining is possible now.
        { // Starts the cannot-rejoin branch.
            return false; // Reports that no rejoin attempt was started.
        } // Ends the cannot-rejoin branch.

        isAttemptingStoredRoomRejoin = PhotonNetwork.RejoinRoom(cachedRoomName); // Starts the Photon rejoin attempt and records whether it began.
        Debug.Log(isAttemptingStoredRoomRejoin // Logs the result of trying to start the rejoin.
            ? $"Attempting to rejoin cached room '{cachedRoomName}'." // Message used when Photon starts the rejoin attempt.
            : $"Failed to start rejoin for cached room '{cachedRoomName}'."); // Message used when Photon cannot start the rejoin attempt.

        return isAttemptingStoredRoomRejoin; // Reports whether the rejoin attempt started.
    } // Ends the TryRejoinCachedRoom method body.

    private void EnsureLobbyJoined() // Joins the Photon lobby when the client is ready and not already in a room.
    { // Starts the EnsureLobbyJoined method body.
        // If you're in a room, you cannot join lobby yet
        if (PhotonNetwork.InRoom) return; // Leaves because lobby joining is not allowed while inside a room.

        // If connected to Master but not in lobby, join lobby
        if (!PhotonNetwork.InLobby && PhotonNetwork.IsConnectedAndReady) // Checks whether the client can join the lobby now.
        { // Starts the join-lobby branch.
            TryJoinLobby(); // Requests entry to the Photon lobby only when Photon is ready.
        } // Ends the join-lobby branch.
    } // Ends the EnsureLobbyJoined method body.
    public override void OnConnectedToMaster() // Runs when Photon connects to the master server.
    { // Starts the OnConnectedToMaster callback body.
        Debug.Log("Connected to Photon Master Server"); // Logs successful master server connection.
        isReconnectingToMaster = false; // Clears the reconnect-in-progress flag.
        ApplyLocalPlayerTag(); // Reapplies the local player tag after connection.

        if (TryRejoinCachedRoom()) // Attempts to rejoin a saved room before joining the lobby.
        { // Starts the successful-rejoin-started branch.
            return; // Leaves so Photon can finish the rejoin flow.
        } // Ends the successful-rejoin-started branch.

        TryJoinLobby(); // Joins the lobby only when Photon is ready and not already joining.
    } // Ends the OnConnectedToMaster callback body.

    public override void OnJoinedLobby() // Runs when Photon confirms lobby entry.
    { // Starts the OnJoinedLobby callback body.
        Debug.Log("Joined Lobby."); // Logs successful lobby entry.
    } // Ends the OnJoinedLobby callback body.
   
    public override void OnDisconnected(DisconnectCause cause) // Runs when Photon disconnects from the server.
    { // Starts the OnDisconnected callback body.
        Debug.LogError($"Disconnected from Photon: {cause}"); // Logs the disconnect reason as an error.

        string cachedRoomName = GetCachedRoomName(); // Reads any saved room name for reconnection.
        if (!string.IsNullOrWhiteSpace(cachedRoomName) && !isReconnectingToMaster) // Checks whether a reconnect should be started.
        { // Starts the reconnect branch.
            isReconnectingToMaster = true; // Marks that a reconnect attempt is now in progress.
            bool reconnectStarted = PhotonNetwork.Reconnect(); // Attempts to reconnect to Photon.
            if (reconnectStarted) // Checks whether Photon accepted the reconnect request.
            { // Starts the reconnect-started branch.
                Debug.Log($"Reconnect started for room '{cachedRoomName}'. Room availability will be revalidated after reconnecting."); // Logs that reconnect validation will happen later.
            } // Ends the reconnect-started branch.
            else // Handles failed reconnect startup.
            { // Starts the reconnect-failed branch.
                Debug.LogWarning($"Reconnect could not start for room '{cachedRoomName}'. Starting a fresh connection to revalidate instead."); // Logs the fallback connection path.
                PhotonNetwork.ConnectUsingSettings(); // Starts a fresh Photon connection using configured settings.
            } // Ends the reconnect-failed branch.
        } // Ends the reconnect branch.
    } // Ends the OnDisconnected callback body.

    public override void OnJoinedRoom() // Runs when Photon confirms the local player joined a room.
    { // Starts the OnJoinedRoom callback body.
        CacheCurrentRoomForRejoin(); // Saves the room name if it is worth rejoining later.
        ApplyLocalPlayerTag(); // Applies the local player tag inside the joined room.
        isAttemptingStoredRoomRejoin = false; // Clears the saved-room rejoin attempt flag.
    } // Ends the OnJoinedRoom callback body.

    public override void OnLeftRoom() // Runs when Photon confirms the local player left a room.
    { // Starts the OnLeftRoom callback body.
        bool shouldReturnToLobby = leavingRejectedStoredRoomRejoin; // Captures whether this leave should return to lobby.
        ClearCachedRoomRejoin(); // Clears saved room and reconnect state.

        if (shouldReturnToLobby && PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby) // Checks whether the client should re-enter the lobby.
        { // Starts the return-to-lobby branch.
            TryJoinLobby(); // Requests entry to the Photon lobby only when Photon is ready.
        } // Ends the return-to-lobby branch.
    } // Ends the OnLeftRoom callback body.

    public override void OnJoinRoomFailed(short returnCode, string message) // Runs when Photon fails to join a room.
    { // Starts the OnJoinRoomFailed callback body.
        Debug.LogWarning($"Join room failed ({returnCode}): {message}"); // Logs the failed join result from Photon.

        if (!isAttemptingStoredRoomRejoin) // Checks whether the failure was unrelated to cached-room rejoining.
        { // Starts the non-cached-failure branch.
            return; // Leaves normal join failures for other UI or logic to handle.
        } // Ends the non-cached-failure branch.

        ClearCachedRoomRejoin(); // Clears the invalid saved room after a failed rejoin.
        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby) // Checks whether the client can return to the lobby.
        { // Starts the lobby-return branch.
            TryJoinLobby(); // Requests entry to the Photon lobby only when Photon is ready.
        } // Ends the lobby-return branch.
    } // Ends the OnJoinRoomFailed callback body.
} // Ends the Network class body.
