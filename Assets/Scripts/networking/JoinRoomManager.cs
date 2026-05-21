using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro;

public class JoinRoomManager : Network
{
    private const string RoomPasswordPropertyKey = "RoomPassword";

    //instance of available room to join button
    [SerializeField] private GameObject roomItemPrefab;
    //scroll view of the room list
    [SerializeField] private Transform contentPanel;
    //refresh button to refresh room list
    [SerializeField] private Button refreshButton;
    //button to toggle room list panel
    [SerializeField] private Button toggleRoomListPanelButton;
    //room list panel
    [SerializeField] private GameObject roomListPanel;
    //room panel of joined players
    [SerializeField] private GameObject roomPanel;
    //player count in that room
    [SerializeField] private Text playerCountText;
    //view to store all joined players in that room
    [SerializeField] private Transform playerListContent;
    //joined player tags
    [SerializeField] private GameObject playerListItemPrefab;
    //start game button
    [SerializeField] private Button startGameButton;
    //cache room list 
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    //Loading Screen component
    [SerializeField] private LoadingScreenManager loadingScreen;

    private string selectedRoomName;
    [SerializeField] private TMP_Text roomPasswordDisplayText;
    [SerializeField] private TMP_Text selectedRoomLabelText;
    [SerializeField] private TMP_Text joinRoomFeedbackText;
    [SerializeField] private TMP_InputField roomPasswordInputField;
    [SerializeField] private Button confirmJoinRoomButton;

    private void Start()
    {
        refreshButton.onClick.AddListener(RefreshRoomList);
        startGameButton.onClick.AddListener(StartGame);
        PhotonNetwork.AutomaticallySyncScene = true;

        if (confirmJoinRoomButton != null)
        {
            confirmJoinRoomButton.onClick.AddListener(JoinSelectedRoom);
        }

        if (roomPasswordInputField != null)
        {
            roomPasswordInputField.onSubmit.AddListener(_ => JoinSelectedRoom());
        }

        ClearSelectedRoom();

        //toggle visibility of RoomListPanel 
        toggleRoomListPanelButton.onClick.AddListener(() =>
        {
            roomListPanel.SetActive(!roomListPanel.activeSelf);
        });
    }

    public void setToggleRoomListPanelButtonActive(bool active)
    {
        toggleRoomListPanelButton.gameObject.SetActive(active);
    }

    public void setRoomListPanelActive(bool active)
    {
        roomListPanel.SetActive(active);
    }

    //update the room list
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        base.OnRoomListUpdate(roomList);

        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList || !info.IsOpen || !info.IsVisible)
            {
                cachedRoomList.Remove(info.Name);
            }
            else
            {
                cachedRoomList[info.Name] = info;
            }
        }
        PopulateRoomListUI();
    }

    void RefreshRoomList()
    {
        Debug.Log("Refreshing room list");
        // Rejoin lobby to refresh list
        PhotonNetwork.LeaveLobby(); 
        PhotonNetwork.JoinLobby();
    }

    void PopulateRoomListUI()
    {
        //destroy all rooms
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }
        //reconstruct all available room with its room name and number of players in that room
        foreach (RoomInfo info in cachedRoomList.Values)
        {
            GameObject item = Instantiate(roomItemPrefab, contentPanel);
            item.GetComponentInChildren<Text>().text = $"{info.Name} ({info.PlayerCount}/{info.MaxPlayers})";
            item.GetComponent<Button>().onClick.AddListener(() => SelectRoom(info.Name));
        }

        if (!string.IsNullOrEmpty(selectedRoomName) && !cachedRoomList.ContainsKey(selectedRoomName))
        {
            ClearSelectedRoom();
        }
    }

    void SelectRoom(string roomName)
    {
        selectedRoomName = roomName;

        if (selectedRoomLabelText != null)
        {
            selectedRoomLabelText.text = $"Selected Room: {roomName}";
        }

        if (joinRoomFeedbackText != null)
        {
            joinRoomFeedbackText.text = "Enter the room password to join.";
            joinRoomFeedbackText.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        }

        if (roomPasswordInputField != null)
        {
            roomPasswordInputField.text = string.Empty;
            roomPasswordInputField.ActivateInputField();
        }
    }

    void JoinSelectedRoom()
    {
        if (string.IsNullOrEmpty(selectedRoomName))
        {
            SetJoinFeedback("Select a room first.", true);
            return;
        }

        if (!cachedRoomList.TryGetValue(selectedRoomName, out RoomInfo roomInfo))
        {
            SetJoinFeedback("That room is no longer available.", true);
            ClearSelectedRoom();
            return;
        }

        string expectedPassword = GetRoomPassword(roomInfo);
        string enteredPassword = roomPasswordInputField != null ? roomPasswordInputField.text.Trim() : string.Empty;

        if (!string.IsNullOrEmpty(expectedPassword) && !string.Equals(expectedPassword, enteredPassword))
        {
            SetJoinFeedback("Incorrect password.", true);
            return;
        }

        SetJoinFeedback("Joining room...", false);
        PhotonNetwork.JoinRoom(selectedRoomName);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"Join Random Room Failed: {message}. Creating new room...");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom });
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log($"Joined Room: {PhotonNetwork.CurrentRoom.Name}");
        //hide room list panel and show current room panel
        toggleRoomListPanelButton.gameObject.SetActive(false);
        roomListPanel.SetActive(false);
        roomPanel.SetActive(true);

        //show the player count and the players in that room
        PopulatePlayerList();
        UpdatePlayerCountUI();
        UpdateRoomPasswordDisplay();
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        ClearSelectedRoom();
    }
    
    //function to show the player count in the room
    private void UpdatePlayerCountUI()
    {
        int current = PhotonNetwork.CurrentRoom.PlayerCount;
        int max = PhotonNetwork.CurrentRoom.MaxPlayers;
        playerCountText.text = $"{current} / {max} Players";
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        PopulatePlayerList();
        UpdatePlayerCountUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        PopulatePlayerList();
        UpdatePlayerCountUI();
    }

    private void PopulatePlayerList()
    {
        //remove all player tags
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        //reconstruct all new player tags
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject item = Instantiate(playerListItemPrefab, playerListContent);
            item.GetComponentInChildren<Text>().text = player.NickName;
        }
    }

    //function to start game
    void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.CurrentRoom.IsOpen = true;

        // Tell everyone to show loading UI + start tracking progress
        photonView.RPC(nameof(RPC_BeginLoading), RpcTarget.All, "GameScene");

        // Master triggers the actual networked scene load (everyone will follow)
        PhotonNetwork.LoadLevel("GameScene");
    }

    [PunRPC]
    private void RPC_BeginLoading(string sceneName)
    {
        loadingScreen.StartFadeAndLoad(sceneName);
    }

    private static string GetRoomPassword(RoomInfo roomInfo)
    {
        if (roomInfo == null || roomInfo.CustomProperties == null)
        {
            return string.Empty;
        }

        if (roomInfo.CustomProperties.TryGetValue(RoomPasswordPropertyKey, out object passwordValue) && passwordValue is string password)
        {
            return password;
        }

        return string.Empty;
    }

    private void UpdateRoomPasswordDisplay()
    {
        if (roomPasswordDisplayText == null)
        {
            return;
        }

        string roomPassword = string.Empty;

        if (PhotonNetwork.CurrentRoom?.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomPasswordPropertyKey, out object passwordValue) &&
            passwordValue is string password)
        {
            roomPassword = password;
        }

        roomPasswordDisplayText.text = string.IsNullOrEmpty(roomPassword)
            ? "Room Password: None"
            : $"Room Password: {roomPassword}";
    }

    private void SetJoinFeedback(string message, bool isError)
    {
        if (joinRoomFeedbackText == null)
        {
            return;
        }

        joinRoomFeedbackText.text = message;
        joinRoomFeedbackText.color = isError
            ? new Color(0.75f, 0.15f, 0.15f, 1f)
            : new Color(0.2f, 0.2f, 0.2f, 1f);
    }

    private void ClearSelectedRoom()
    {
        selectedRoomName = null;

        if (selectedRoomLabelText != null)
        {
            selectedRoomLabelText.text = "Selected Room: None";
        }

        if (roomPasswordInputField != null)
        {
            roomPasswordInputField.text = string.Empty;
        }

        SetJoinFeedback("Select a room to enter its password.", false);
    }

}
