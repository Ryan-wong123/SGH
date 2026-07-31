using UnityEngine; // Imports Unity engine types such as GameObject, Debug, and Random.
using UnityEngine.UI; // Imports Unity UI components such as Button and Dropdown.
using Photon.Pun; // Imports Photon PUN networking classes.
using Photon.Realtime; // Imports Photon realtime types such as RoomInfo and RoomOptions.
using System.Collections; // Imports IEnumerator for coroutine support.
using TMPro; // Imports TextMeshPro UI components.
using System.Collections.Generic; // Imports generic collections such as Dictionary and List.


public class CreateRoomManager : Network // Defines a room creation manager that also inherits Network behavior.
{ // Starts the CreateRoomManager class body.
    private const string RoomPasswordPropertyKey = "RoomPassword"; // Names the Photon custom room property used for room passwords.
    private const string PasswordCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Defines the allowed characters for generated room passwords.
    private const int PlayerRejoinTtlMs = 600000; // Sets how long disconnected players can rejoin their slot, in milliseconds.
    private const int EmptyRoomTtlMs = 300000; // Sets how long an empty room can remain alive, in milliseconds.

    //room name input field
    [SerializeField] private TMP_InputField roomNameInput; // References the input field where the player enters a room name.
    //dropdown field of maximum players allowed per room
    [SerializeField] private Dropdown maxPlayersDropdown; // References the dropdown used to choose maximum players.
    //button to go to create room panel
    [SerializeField] private Button createRoomButton; // References the button that creates a room.
    //button to toggle between create room panel and roomlist panel
    [SerializeField] private Button togglecreateRoomPanelButton; // References the button that shows or hides the create room panel.
    //create room panel
    [SerializeField] private GameObject createRoomPanel; // References the panel containing create room controls.
    //cache the room list
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>(); // Stores currently available Photon rooms by room name.
    //cache value of room name and max players of that room
    private string cachedRoomName; // Stores the last requested room name for retrying creation.
    private byte cachedMaxPlayers; // Stores the last requested max player count for retrying creation.
    private string cachedRoomPassword; // Stores the generated room password for retrying creation.
    private bool isCreatingRoom; // Tracks whether this client should become the organiser after Photon assigns its room actor number.

    private void Start() // Runs once when Unity starts this component.
    { // Starts the Start method body.
        //button listener to create room
        createRoomButton.onClick.AddListener(CreateRoom); // Registers the create room button click handler.
        //toggle visibility of CreateRoomPanel
        togglecreateRoomPanelButton.onClick.AddListener(() => // Registers the create room panel toggle handler.
        { // Starts the toggle handler body.
            createRoomPanel.SetActive(!createRoomPanel.activeSelf); // Flips the create room panel active state.
        }); // Ends and registers the toggle handler.
    } // Ends the Start method body.
    public override void OnRoomListUpdate(List<RoomInfo> roomList) // Runs when Photon sends an updated room list.
    { // Starts the OnRoomListUpdate callback body.
        base.OnRoomListUpdate(roomList); // Lets the base Photon callback run first.

        foreach (RoomInfo info in roomList) // Loops through every room update received from Photon.
        { // Starts the room update loop body.
            if (info.RemovedFromList || !info.IsOpen || !info.IsVisible) // Checks whether this room should be removed from the local cache.
                cachedRoomList.Remove(info.Name); // Removes unavailable rooms from the cache.
            else // Handles rooms that are still open and visible.
                cachedRoomList[info.Name] = info; // Adds or updates the room in the cache.
        } // Ends the room update loop body.
    } // Ends the OnRoomListUpdate callback body.

    private void CreateRoom() // Validates input and requests Photon room creation.
    { // Starts the CreateRoom method body.
        //get the room name and maximum player from input field and dropdown
        string roomName = roomNameInput.text; // Reads the requested room name from the input field.
        byte maxPlayers = byte.Parse(maxPlayersDropdown.options[maxPlayersDropdown.value].text); // Reads the selected max player count from the dropdown.

        //check if the roomname is empty and update placeholder
        if (string.IsNullOrEmpty(roomName)) // Checks whether the room name was left blank.
        { // Starts the empty-room-name branch.
            // Update placeholder text if room name is empty
            if (roomNameInput.placeholder is TextMeshProUGUI placeholderText) // Checks whether the placeholder is a TextMeshPro label.
            { // Starts the placeholder-update branch.
                placeholderText.text = "Room name required"; // Shows a validation message in the placeholder.
            } // Ends the placeholder-update branch.

            // Clear the input field to show the new placeholder
            roomNameInput.text = ""; // Clears the input so the placeholder message becomes visible.
            return; // Stops room creation until a room name is entered.
        } // Ends the empty-room-name branch.

        //Check if room already exists
        if (cachedRoomList.ContainsKey(roomName)) // Checks whether the requested room name is already in the cached room list.
        { // Starts the duplicate-room-name branch.
            if (roomNameInput.placeholder is TextMeshProUGUI placeholderText) // Checks whether the placeholder is a TextMeshPro label.
                placeholderText.text = "Room name exists"; // Shows a duplicate-name validation message in the placeholder.

            roomNameInput.text = ""; // Clears the input so the placeholder message becomes visible.
            return; // Stops room creation because the room name is already taken.
        } // Ends the duplicate-room-name branch.

        //cache values for retrying to create room if create room fails
        cachedRoomName = roomName; // Caches the requested room name for retry attempts.
        cachedMaxPlayers = maxPlayers; // Caches the requested maximum player count for retry attempts.
        cachedRoomPassword = GenerateRoomPassword(); // Generates and caches the room password for this room.

        //create room with the options
        RoomOptions options = BuildRoomOptions(maxPlayers, cachedRoomPassword); // Builds Photon room options with player limits and password data.
        isCreatingRoom = true; // Defers organiser assignment until this client has joined and has a valid actor number.
        PhotonNetwork.CreateRoom(roomName, options); // Sends the Photon request to create the room.
    } // Ends the CreateRoom method body.

    public override void OnCreatedRoom() // Runs when Photon confirms the room was created.
    { // Starts the OnCreatedRoom callback body.
        Debug.Log($"Room Created: {cachedRoomName}"); // Logs the newly created room name.
        createRoomPanel.SetActive(false); // Hides the create room panel after success.
    } // Ends the OnCreatedRoom callback body.

    public override void OnJoinedRoom() // Runs when Photon has assigned this client an actor number in the new room.
    { // Starts the joined-room callback body.
        base.OnJoinedRoom(); // Lets the base networking callback apply the local player tag.

        if (!isCreatingRoom || PhotonNetwork.CurrentRoom == null || PhotonNetwork.LocalPlayer == null) // Ensures only this room's creator sets the organiser property.
            return; // Leaves joined existing rooms unchanged.

        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable // Stores the valid actor number after Photon assigns it.
        { // Starts the custom room property table.
            { SessionSmtpCredentials.OrganizerActorNumberRoomPropertyKey, PhotonNetwork.LocalPlayer.ActorNumber } // Marks this room creator as the only SMTP sender.
        }); // Sends the organiser identity to Photon without exposing email credentials.
        isCreatingRoom = false; // Prevents this client from overwriting the organiser property on a later rejoin.
        Debug.Log($"Session organiser set to actor {PhotonNetwork.LocalPlayer.ActorNumber}."); // Confirms the sender identity was recorded.
    } // Ends the joined-room callback body.

    public override void OnCreateRoomFailed(short returnCode, string message) // Runs when Photon fails to create the requested room.
    { // Starts the OnCreateRoomFailed callback body.
        Debug.LogError("Failed to create room: " + message + " trying to create room again...."); // Logs the failure and planned retry.
        StartCoroutine(RetryCreateRoom()); // Starts a coroutine that retries room creation after a delay.
    } // Ends the OnCreateRoomFailed callback body.

    private IEnumerator RetryCreateRoom() // Waits briefly and retries the most recent room creation request.
    { // Starts the RetryCreateRoom coroutine body.
        //wait 2 seconds before retry create room again with cached options
        yield return new WaitForSeconds(2f); // Pauses the coroutine for two seconds before retrying.
        RoomOptions options = BuildRoomOptions(cachedMaxPlayers, cachedRoomPassword); // Rebuilds room options from cached creation values.
        isCreatingRoom = true; // Ensures a successful retry records this client as organiser after joining.
        PhotonNetwork.CreateRoom(cachedRoomName, options); // Sends another Photon request to create the cached room.
    } // Ends the RetryCreateRoom coroutine body.

    private static RoomOptions BuildRoomOptions(byte maxPlayers, string roomPassword) // Creates Photon room options for a new room.
    { // Starts the BuildRoomOptions method body.
        return new RoomOptions // Returns a configured Photon RoomOptions object.
        { // Starts the RoomOptions initializer.
            MaxPlayers = maxPlayers, // Sets the maximum number of players allowed in the room.
            PlayerTtl = PlayerRejoinTtlMs, // Allows disconnected players to reclaim their player slot within the configured time.
            EmptyRoomTtl = EmptyRoomTtlMs, // Keeps an empty room alive for the configured time before Photon removes it.
            PublishUserId = true, // Publishes user IDs so rejoin and user tracking can work.
            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable // Creates the custom room properties table.
            { // Starts the custom room properties initializer.
                { RoomPasswordPropertyKey, roomPassword } // Stores the room password in Photon custom room properties.
            }, // Ends the custom room properties initializer.
            CustomRoomPropertiesForLobby = new[] { RoomPasswordPropertyKey } // Makes the password property available in lobby room info.
        }; // Ends and returns the RoomOptions initializer.
    } // Ends the BuildRoomOptions method body.

    private static string GenerateRoomPassword() // Generates a random six-character room password.
    { // Starts the GenerateRoomPassword method body.
        char[] password = new char[6]; // Allocates a six-character password buffer.

        for (int i = 0; i < password.Length; i++) // Loops through each password character position.
        { // Starts the password generation loop body.
            password[i] = PasswordCharacters[Random.Range(0, PasswordCharacters.Length)]; // Picks a random allowed character for this position.
        } // Ends the password generation loop body.

        return new string(password); // Converts the character buffer into a string password.
    } // Ends the GenerateRoomPassword method body.
} // Ends the CreateRoomManager class body.
