using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;


public class CreateRoomManager : Network
{
    //room name input field
    [SerializeField] private TMP_InputField roomNameInput;
    //dropdown field of maximum players allowed per room
    [SerializeField] private Dropdown maxPlayersDropdown;
    //button to go to create room panel
    [SerializeField] private Button createRoomButton;
    //button to toggle between create room panel and roomlist panel
    [SerializeField] private Button togglecreateRoomPanelButton;
    //create room panel
    [SerializeField] private GameObject createRoomPanel;

    //cache value of room name and max players of that room
    private string cachedRoomName;
    private byte cachedMaxPlayers;
    private void Start()
    {
        //button listener to create room
        createRoomButton.onClick.AddListener(CreateRoom);
        //toggle visibility of CreateRoomPanel
        togglecreateRoomPanelButton.onClick.AddListener(() =>
        {
            createRoomPanel.SetActive(!createRoomPanel.activeSelf);
        });
    }

    private void CreateRoom()
    {
        //get the room name and maximum player from input field and dropdown
        string roomName = roomNameInput.text;
        byte maxPlayers = byte.Parse(maxPlayersDropdown.options[maxPlayersDropdown.value].text);

        //check if the roomname is empty and update placeholder
        if (string.IsNullOrEmpty(roomName))
        {
            // Update placeholder text if room name is empty
            if (roomNameInput.placeholder is TextMeshProUGUI placeholderText)
            {
                placeholderText.text = "Room name is required";
            }

            // Clear the input field to show the new placeholder
            roomNameInput.text = "";
            return;
        }

        //cache values for retrying to create room if create room fails
        cachedRoomName = roomName;
        cachedMaxPlayers = maxPlayers;

        //create room with the options
        RoomOptions options = new RoomOptions { MaxPlayers = maxPlayers };
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"Room Created: {cachedRoomName}");
        createRoomPanel.SetActive(false);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Failed to create room: " + message + " trying to create room again....");
        StartCoroutine(RetryCreateRoom());
    }

    private IEnumerator RetryCreateRoom()
    {
        //wait 2 seconds before retry create room again with cached options
        yield return new WaitForSeconds(2f);
        RoomOptions options = new RoomOptions { MaxPlayers = cachedMaxPlayers };
        PhotonNetwork.CreateRoom(cachedRoomName, options);
    }
}
