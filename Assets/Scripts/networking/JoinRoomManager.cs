using System.Collections.Generic; // Imports generic collections such as Dictionary and List.
using UnityEngine; // Imports Unity engine types such as GameObject and Transform.
using UnityEngine.UI; // Imports Unity UI components such as Button and Text.
using Photon.Pun; // Imports Photon PUN networking classes and RPC support.
using Photon.Realtime; // Imports Photon realtime types such as RoomInfo and Player.
using ExitGames.Client.Photon; // Imports Photon client property table support.
using TMPro; // Imports TextMeshPro UI components.

public class JoinRoomManager : Network // Defines a room-list manager that also inherits Network behavior.
{ // Starts the JoinRoomManager class body.
    private const string RoomPasswordPropertyKey = "RoomPassword"; // Names the Photon custom room property used for passwords.
    private const string GameStartedPropertyKey = "GameStarted"; // Marks a room whose players have entered GameScene.
    private bool refreshLobbyAfterLeave; // Tracks whether a lobby refresh is waiting for OnLeftLobby.
    private bool isAttemptingListedGameRejoin; // Tracks a rejoin selected from the visible room list.

    //instance of available room to join button
    [SerializeField] private GameObject roomItemPrefab; // References the prefab used for each room list button.
    //scroll view of the room list
    [SerializeField] private Transform contentPanel; // References the UI parent that contains room list items.
    //refresh button to refresh room list
    [SerializeField] private Button refreshButton; // References the button used to refresh available rooms.
    //button to toggle room list panel
    [SerializeField] private Button toggleRoomListPanelButton; // References the button that shows or hides the room list panel.
    //room list panel
    [SerializeField] private GameObject roomListPanel; // References the panel that displays available rooms.
    //room panel of joined players
    [SerializeField] private GameObject roomPanel; // References the panel shown after joining a room.
    //player count in that room
    [SerializeField] private Text playerCountText; // References the text label that displays current room player count.
    //view to store all joined players in that room
    [SerializeField] private Transform playerListContent; // References the UI parent that contains joined player labels.
    //joined player tags
    [SerializeField] private GameObject playerListItemPrefab; // References the prefab used for each player label.
    //start game button
    [SerializeField] private Button startGameButton; // References the button that lets the master client start the game.
    //cache room list 
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>(); // Stores currently available Photon rooms by room name.
    //Loading Screen component
    [SerializeField] private LoadingScreenManager loadingScreen; // References the component that handles loading screen transitions.

    private string selectedRoomName; // Stores the room currently selected in the room list.
    private bool isCreatingFallbackRoom; // Tracks whether this client created a room after random matchmaking failed.
    [SerializeField] private TMP_Text roomPasswordDisplayText; // References the label that displays the joined room password.
    [SerializeField] private TMP_Text selectedRoomLabelText; // References the label that displays the selected room name.
    [SerializeField] private TMP_Text joinRoomFeedbackText; // References the label used for join validation feedback.
    [SerializeField] private TMP_InputField roomPasswordInputField; // References the input field where the player enters a room password.
    [SerializeField] private Button confirmJoinRoomButton; // References the button that confirms joining the selected room.

    private void Start() // Runs once when Unity starts this component.
    { // Starts the Start method body.
        refreshButton.onClick.AddListener(RefreshRoomList); // Registers the refresh button click handler.
        startGameButton.onClick.AddListener(StartGame); // Registers the start game button click handler.
        PhotonNetwork.AutomaticallySyncScene = true; // Enables Photon scene synchronization for all players.

        if (confirmJoinRoomButton != null) // Checks whether the confirm join button is assigned.
        { // Starts the confirm-button branch.
            confirmJoinRoomButton.onClick.AddListener(JoinSelectedRoom); // Registers the selected-room join handler.
        } // Ends the confirm-button branch.

        if (roomPasswordInputField != null) // Checks whether the password input field is assigned.
        { // Starts the password-input branch.
            roomPasswordInputField.onSubmit.AddListener(_ => JoinSelectedRoom()); // Joins the selected room when the password input is submitted.
        } // Ends the password-input branch.

        ClearSelectedRoom(); // Resets selected room UI and feedback.

        //toggle visibility of RoomListPanel 
        toggleRoomListPanelButton.onClick.AddListener(() => // Registers the room list panel toggle handler.
        { // Starts the toggle handler body.
            roomListPanel.SetActive(!roomListPanel.activeSelf); // Flips the room list panel active state.
        }); // Ends and registers the toggle handler.
    } // Ends the Start method body.

    public void setToggleRoomListPanelButtonActive(bool active) // Sets whether the room-list toggle button is visible.
    { // Starts the setToggleRoomListPanelButtonActive method body.
        toggleRoomListPanelButton.gameObject.SetActive(active); // Applies the requested active state to the toggle button GameObject.
    } // Ends the setToggleRoomListPanelButtonActive method body.

    public void setRoomListPanelActive(bool active) // Sets whether the room list panel is visible.
    { // Starts the setRoomListPanelActive method body.
        roomListPanel.SetActive(active); // Applies the requested active state to the room list panel.
    } // Ends the setRoomListPanelActive method body.

    //update the room list
    public override void OnRoomListUpdate(List<RoomInfo> roomList) // Runs when Photon sends an updated room list.
    { // Starts the OnRoomListUpdate callback body.
        base.OnRoomListUpdate(roomList); // Lets the base Photon callback run first.

        foreach (RoomInfo info in roomList) // Loops through every room update received from Photon.
        { // Starts the room update loop body.
            if (info.RemovedFromList || !info.IsOpen || !info.IsVisible) // Removes rooms that Photon no longer lists as joinable.
            { // Starts the remove-room branch.
                cachedRoomList.Remove(info.Name); // Removes the room from the local cache.
            } // Ends the remove-room branch.
            else // Handles rooms that are still joinable and visible.
            { // Starts the cache-room branch.
                cachedRoomList[info.Name] = info; // Adds or updates this room in the local cache.
            } // Ends the cache-room branch.
        } // Ends the room update loop body.
        PopulateRoomListUI(); // Rebuilds the visible room list from the cache.
    } // Ends the OnRoomListUpdate callback body.

    void RefreshRoomList() // Requests a refreshed room list from Photon.
    { // Starts the RefreshRoomList method body.
        Debug.Log("Refreshing room list"); // Logs that the refresh button was used.

        if (PhotonNetwork.InRoom)
            return;

        if (PhotonNetwork.InLobby)
        {
            refreshLobbyAfterLeave = true;
            PhotonNetwork.LeaveLobby();
            return;
        }

        refreshLobbyAfterLeave = false;
        Network.TryJoinLobby();
    } // Ends the RefreshRoomList method body.

    public override void OnLeftLobby()
    {
        base.OnLeftLobby();

        if (!refreshLobbyAfterLeave)
            return;

        refreshLobbyAfterLeave = false;
        Network.TryJoinLobby();
    }

    void PopulateRoomListUI() // Rebuilds the room list UI from cached rooms.
    { // Starts the PopulateRoomListUI method body.
        //destroy all rooms
        foreach (Transform child in contentPanel) // Loops through every existing room item in the UI.
        { // Starts the clear-room-item loop body.
            Destroy(child.gameObject); // Destroys the old room item GameObject.
        } // Ends the clear-room-item loop body.
        //reconstruct all available room with its room name and number of players in that room
        foreach (RoomInfo info in cachedRoomList.Values) // Loops through every cached available room.
        { // Starts the room item creation loop body.
            GameObject item = Instantiate(roomItemPrefab, contentPanel); // Creates a room item under the content panel.
            item.GetComponentInChildren<Text>().text = HasGameStarted(info)
                ? $"{info.Name} (Rejoin game)"
                : $"{info.Name} ({info.PlayerCount}/{info.MaxPlayers})"; // Displays a rejoin label for an in-progress room.
            item.GetComponent<Button>().onClick.AddListener(() => SelectRoom(info.Name)); // Makes the room item select this room when clicked.
        } // Ends the room item creation loop body.

        if (!string.IsNullOrEmpty(selectedRoomName) && !cachedRoomList.ContainsKey(selectedRoomName)) // Checks whether the selected room disappeared.
        { // Starts the missing-selected-room branch.
            ClearSelectedRoom(); // Clears selection because the selected room is no longer available.
        } // Ends the missing-selected-room branch.
    } // Ends the PopulateRoomListUI method body.

    void SelectRoom(string roomName) // Selects a room from the room list.
    { // Starts the SelectRoom method body.
        selectedRoomName = roomName; // Stores the selected room name.

        if (selectedRoomLabelText != null) // Checks whether the selected room label is assigned.
        { // Starts the selected-label branch.
            selectedRoomLabelText.text = $"Selected Room: {roomName}"; // Displays the selected room name.
        } // Ends the selected-label branch.

        if (joinRoomFeedbackText != null) // Checks whether the feedback label is assigned.
        { // Starts the feedback-label branch.
            joinRoomFeedbackText.text = cachedRoomList.TryGetValue(roomName, out RoomInfo selectedInfo) && HasGameStarted(selectedInfo)
                ? "Only a player returning to this game can rejoin."
                : "Enter the room password to join."; // Explains the selected room's join path.
            joinRoomFeedbackText.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Sets the feedback text to the neutral color.
        } // Ends the feedback-label branch.

        if (roomPasswordInputField != null) // Checks whether the password input field is assigned.
        { // Starts the password-field branch.
            roomPasswordInputField.text = string.Empty; // Clears any previous password input.
            roomPasswordInputField.ActivateInputField(); // Focuses the password input field for typing.
        } // Ends the password-field branch.
    } // Ends the SelectRoom method body.

    void JoinSelectedRoom() // Attempts to join the currently selected room.
    { // Starts the JoinSelectedRoom method body.
        if (string.IsNullOrEmpty(selectedRoomName)) // Checks whether the player has selected a room.
        { // Starts the no-selected-room branch.
            SetJoinFeedback("Select a room first.", true); // Shows an error telling the player to select a room.
            return; // Stops because there is no room to join.
        } // Ends the no-selected-room branch.

        if (!cachedRoomList.TryGetValue(selectedRoomName, out RoomInfo roomInfo)) // Looks up the selected room in the cache.
        { // Starts the unavailable-room branch.
            SetJoinFeedback("That room is no longer available.", true); // Shows an error for an unavailable room.
            ClearSelectedRoom(); // Clears the stale room selection.
            return; // Stops because the selected room cannot be joined.
        } // Ends the unavailable-room branch.

        if (HasGameStarted(roomInfo))
        {
            if (selectedRoomName != Network.GetCachedRoomName())
            {
                SetJoinFeedback("This game is already in progress.", true);
                return;
            }

            SetJoinFeedback("Rejoining game...", false);
            isAttemptingListedGameRejoin = PhotonNetwork.RejoinRoom(selectedRoomName);
            if (!isAttemptingListedGameRejoin)
                SetJoinFeedback("Could not start rejoining. Try again after reconnecting.", true);
            return;
        }

        string expectedPassword = GetRoomPassword(roomInfo); // Reads the selected room's password from custom properties.
        string enteredPassword = roomPasswordInputField != null ? roomPasswordInputField.text.Trim() : string.Empty; // Reads and trims the entered password.

        if (!string.IsNullOrEmpty(expectedPassword) && !string.Equals(expectedPassword, enteredPassword)) // Checks whether a required password was entered incorrectly.
        { // Starts the wrong-password branch.
            SetJoinFeedback("Incorrect password.", true); // Shows an incorrect password error.
            return; // Stops because the password does not match.
        } // Ends the wrong-password branch.

        SetJoinFeedback("Joining room...", false); // Shows neutral joining feedback.
        PhotonNetwork.JoinRoom(selectedRoomName); // Sends the Photon request to join the selected room.
    } // Ends the JoinSelectedRoom method body.

    public override void OnJoinRandomFailed(short returnCode, string message) // Runs when Photon fails to join a random room.
    { // Starts the OnJoinRandomFailed callback body.
        Debug.LogWarning($"Join Random Room Failed: {message}. Creating new room..."); // Logs the random-join failure and fallback.
        var options = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            PlayerTtl = 600000,
            EmptyRoomTtl = 300000,
            PublishUserId = true,
            CustomRoomProperties = new Hashtable { { GameStartedPropertyKey, false } },
            CustomRoomPropertiesForLobby = new[] { GameStartedPropertyKey }
        };
        isCreatingFallbackRoom = true; // Defers organiser assignment until Photon assigns this new room's actor number.
        PhotonNetwork.CreateRoom(null, options); // Creates a fallback room while preserving its organiser identity without exposing an email address.
    } // Ends the OnJoinRandomFailed callback body.

    public override void OnJoinedRoom() // Runs when Photon confirms that the local player joined a room.
    { // Starts the OnJoinedRoom callback body.
        base.OnJoinedRoom(); // Runs the base room-joined handling from Network.
        isAttemptingListedGameRejoin = false;
        SetFallbackRoomOrganizer(); // Records this client only when it created the fallback room.
        Debug.Log($"Joined Room: {PhotonNetwork.CurrentRoom.Name}"); // Logs the joined room name.
        //hide room list panel and show current room panel
        toggleRoomListPanelButton.gameObject.SetActive(false); // Hides the room-list toggle button while in a room.
        roomListPanel.SetActive(false); // Hides the room list panel.
        roomPanel.SetActive(true); // Shows the joined-room panel.

        //show the player count and the players in that room
        PopulatePlayerList(); // Builds the list of players currently in the room.
        UpdatePlayerCountUI(); // Updates the room player count label.
        UpdateRoomPasswordDisplay(); // Updates the room password display label.
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient); // Shows the start button only to the master client.
        ClearSelectedRoom(); // Clears the selected-room UI after joining.
    } // Ends the OnJoinedRoom callback body.

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        bool listedGameRejoinFailed = isAttemptingListedGameRejoin;
        isAttemptingListedGameRejoin = false;
        base.OnJoinRoomFailed(returnCode, message);

        if (listedGameRejoinFailed)
            SetJoinFeedback(returnCode == ErrorCode.JoinFailedFoundActiveJoiner
                ? "Your old connection is still active. Wait a moment, then retry."
                : "Rejoin failed. Your previous player slot may have expired.", true);
    }

    private void SetFallbackRoomOrganizer() // Records a valid organiser actor number after creating a random-match fallback room.
    { // Starts the fallback organiser assignment body.
        if (!isCreatingFallbackRoom || PhotonNetwork.CurrentRoom == null || PhotonNetwork.LocalPlayer == null) // Leaves normally joined rooms unchanged.
            return; // Stops when this client did not create the current room.

        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable // Updates the newly created room with its organiser identity.
        { // Starts the custom room property table.
            { SessionSmtpCredentials.OrganizerActorNumberRoomPropertyKey, PhotonNetwork.LocalPlayer.ActorNumber } // Uses the actor number assigned by the completed room join.
        }); // Sends only the organiser identifier, never SMTP credentials.
        isCreatingFallbackRoom = false; // Prevents overwriting the organiser property on a later rejoin.
        Debug.Log($"Session organiser set to actor {PhotonNetwork.LocalPlayer.ActorNumber}."); // Confirms the fallback room's sender identity was recorded.
    } // Ends the fallback organiser assignment body.
    
    //function to show the player count in the room
    private void UpdatePlayerCountUI() // Updates the text showing how many players are in the room.
    { // Starts the UpdatePlayerCountUI method body.
        int current = PhotonNetwork.CurrentRoom.PlayerCount; // Reads the current player count from the Photon room.
        int max = PhotonNetwork.CurrentRoom.MaxPlayers; // Reads the maximum player count from the Photon room.
        playerCountText.text = $"{current} / {max} Players"; // Displays the current and maximum player counts.
    } // Ends the UpdatePlayerCountUI method body.

    public override void OnPlayerEnteredRoom(Player newPlayer) // Runs when another player joins the room.
    { // Starts the OnPlayerEnteredRoom callback body.
        Network.CacheCurrentRoomForRejoin(); // Updates the cached rejoin room if another active player is present.
        PopulatePlayerList(); // Refreshes the visible player list.
        UpdatePlayerCountUI(); // Refreshes the visible player count.
    } // Ends the OnPlayerEnteredRoom callback body.

    public override void OnPlayerLeftRoom(Player otherPlayer) // Runs when another player leaves the room.
    { // Starts the OnPlayerLeftRoom callback body.
        Network.CacheCurrentRoomForRejoin(); // Updates or clears the cached rejoin room based on remaining players.
        PopulatePlayerList(); // Refreshes the visible player list.
        UpdatePlayerCountUI(); // Refreshes the visible player count.
    } // Ends the OnPlayerLeftRoom callback body.

    private void PopulatePlayerList() // Rebuilds the joined-player list UI.
    { // Starts the PopulatePlayerList method body.
        //remove all player tags
        foreach (Transform child in playerListContent) // Loops through every existing player label.
        { // Starts the clear-player-label loop body.
            Destroy(child.gameObject); // Destroys the old player label GameObject.
        } // Ends the clear-player-label loop body.

        //reconstruct all new player tags
        foreach (Player player in PhotonNetwork.PlayerList) // Loops through every player currently in the Photon room.
        { // Starts the player label creation loop body.
            GameObject item = Instantiate(playerListItemPrefab, playerListContent); // Creates a player list item under the player list content.
            item.GetComponentInChildren<Text>().text = player.NickName; // Displays the player's Photon nickname.
        } // Ends the player label creation loop body.
    } // Ends the PopulatePlayerList method body.

    //function to start game
    void StartGame() // Starts the game scene for every player in the room.
    { // Starts the StartGame method body.
        if (!PhotonNetwork.IsMasterClient) return; // Stops non-master clients from starting the game.

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { GameStartedPropertyKey, true } }); // Identifies the room as an in-progress game in the lobby.
        PhotonNetwork.CurrentRoom.IsVisible = true; // Keeps the room discoverable by its returning players.
        PhotonNetwork.CurrentRoom.IsOpen = true; // Keeps the room open while the scene load begins.

        // Tell everyone to show loading UI + start tracking progress
        photonView.RPC(nameof(RPC_BeginLoading), RpcTarget.All, "GameScene"); // Tells all clients to begin loading UI for the game scene.

        // Master triggers the actual networked scene load (everyone will follow)
        PhotonNetwork.LoadLevel("GameScene"); // Loads the game scene through Photon scene synchronization.
    } // Ends the StartGame method body.

    [PunRPC] // Marks this method as callable through Photon RPC.
    private void RPC_BeginLoading(string sceneName) // Runs on each client when loading should begin.
    { // Starts the RPC_BeginLoading method body.
        loadingScreen.StartFadeAndLoad(sceneName); // Starts the loading screen fade and scene-load tracking.
    } // Ends the RPC_BeginLoading method body.

    private static string GetRoomPassword(RoomInfo roomInfo) // Reads a password from a room's custom properties.
    { // Starts the GetRoomPassword method body.
        if (roomInfo == null || roomInfo.CustomProperties == null) // Checks whether room info or properties are missing.
        { // Starts the missing-room-info branch.
            return string.Empty; // Returns no password when no property data exists.
        } // Ends the missing-room-info branch.

        if (roomInfo.CustomProperties.TryGetValue(RoomPasswordPropertyKey, out object passwordValue) && passwordValue is string password) // Looks for a string password property.
        { // Starts the password-found branch.
            return password; // Returns the room password.
        } // Ends the password-found branch.

        return string.Empty; // Returns no password when the property is missing or not a string.
    } // Ends the GetRoomPassword method body.

    private static bool HasGameStarted(RoomInfo roomInfo)
    {
        return roomInfo?.CustomProperties != null &&
            roomInfo.CustomProperties.TryGetValue(GameStartedPropertyKey, out object started) &&
            started is bool isStarted && isStarted;
    }

    private void UpdateRoomPasswordDisplay() // Updates the UI label that shows the current room password.
    { // Starts the UpdateRoomPasswordDisplay method body.
        if (roomPasswordDisplayText == null) // Checks whether the password display label is assigned.
        { // Starts the no-display-label branch.
            return; // Leaves because there is no label to update.
        } // Ends the no-display-label branch.

        string roomPassword = string.Empty; // Starts with no known room password.

        if (PhotonNetwork.CurrentRoom?.CustomProperties != null && // Checks that the current room has custom properties.
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomPasswordPropertyKey, out object passwordValue) && // Attempts to read the room password property.
            passwordValue is string password) // Confirms the password property is a string.
        { // Starts the current-room-password branch.
            roomPassword = password; // Stores the password found on the current room.
        } // Ends the current-room-password branch.

        roomPasswordDisplayText.text = string.IsNullOrEmpty(roomPassword) // Chooses the display text based on whether a password exists.
            ? "Room Password: None" // Shows that the room has no password.
            : $"Room Password: {roomPassword}"; // Shows the current room password.
    } // Ends the UpdateRoomPasswordDisplay method body.

    private void SetJoinFeedback(string message, bool isError) // Updates the join feedback text and color.
    { // Starts the SetJoinFeedback method body.
        if (joinRoomFeedbackText == null) // Checks whether the feedback label is assigned.
        { // Starts the no-feedback-label branch.
            return; // Leaves because there is no feedback label to update.
        } // Ends the no-feedback-label branch.

        joinRoomFeedbackText.text = message; // Displays the requested feedback message.
        joinRoomFeedbackText.color = isError // Chooses the feedback color based on whether this is an error.
            ? new Color(0.75f, 0.15f, 0.15f, 1f) // Uses a red color for error feedback.
            : new Color(0.2f, 0.2f, 0.2f, 1f); // Uses a neutral dark color for normal feedback.
    } // Ends the SetJoinFeedback method body.

    private void ClearSelectedRoom() // Clears the currently selected room and resets related UI.
    { // Starts the ClearSelectedRoom method body.
        selectedRoomName = null; // Removes the stored selected room name.

        if (selectedRoomLabelText != null) // Checks whether the selected room label is assigned.
        { // Starts the selected-room-label reset branch.
            selectedRoomLabelText.text = "Selected Room: None"; // Displays that no room is selected.
        } // Ends the selected-room-label reset branch.

        if (roomPasswordInputField != null) // Checks whether the password input field is assigned.
        { // Starts the password-input reset branch.
            roomPasswordInputField.text = string.Empty; // Clears any typed password.
        } // Ends the password-input reset branch.

        SetJoinFeedback("Select a room to enter its password.", false); // Restores the default selection prompt.
    } // Ends the ClearSelectedRoom method body.

} // Ends the JoinRoomManager class body.
