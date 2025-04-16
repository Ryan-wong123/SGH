using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class CreateRoomManager : Network
{
    [SerializeField] private InputField roomNameInput;
    [SerializeField] private Dropdown maxPlayersDropdown;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button togglecreateRoomPanelButton;
    [SerializeField] private GameObject createRoomPanel;
    private string cachedRoomName;
    private byte cachedMaxPlayers;
    private void Start()
    {
        createRoomButton.onClick.AddListener(CreateRoom);
        // Toggle visibility of RoomListPanel and CreateRoomPanel
        togglecreateRoomPanelButton.onClick.AddListener(() =>
        {
            createRoomPanel.SetActive(!createRoomPanel.activeSelf);
        });
    }

    private void CreateRoom()
    {
        string roomName = roomNameInput.text;
        byte maxPlayers = byte.Parse(maxPlayersDropdown.options[maxPlayersDropdown.value].text);

        if (string.IsNullOrEmpty(roomName))
        {
            // Update placeholder text if room name is empty
            if (roomNameInput.placeholder is Text placeholderText)
            {
                placeholderText.text = "Room name is required";
            }

            // Clear the input field to show the new placeholder
            roomNameInput.text = "";
            return;
        }

        // Cache values for retry
        cachedRoomName = roomName;
        cachedMaxPlayers = maxPlayers;

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
        yield return new WaitForSeconds(2f); // Wait 2 seconds before retry

        RoomOptions options = new RoomOptions { MaxPlayers = cachedMaxPlayers };
        PhotonNetwork.CreateRoom(cachedRoomName, options);
    }
}
