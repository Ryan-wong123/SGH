using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class JoinRoomManager : Network
{
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
    private void Start()
    {
        refreshButton.onClick.AddListener(RefreshRoomList);
        startGameButton.onClick.AddListener(StartGame);

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
        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList)
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
            item.GetComponent<Button>().onClick.AddListener(() => JoinSelectedRoom(info.Name));
        }
    }

    void JoinSelectedRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"Join Random Room Failed: {message}. Creating new room...");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined Room: {PhotonNetwork.CurrentRoom.Name}");
        //hide room list panel and show current room panel
        toggleRoomListPanelButton.gameObject.SetActive(false);
        roomListPanel.SetActive(false);
        roomPanel.SetActive(true);

        //show the player count and the players in that room
        PopulatePlayerList();
        UpdatePlayerCountUI();
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
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
        if (PhotonNetwork.IsMasterClient)
        {
            //prevent anymore player from joining
            PhotonNetwork.CurrentRoom.IsOpen = false;
            loadingScreen.StartFadeAndLoad("GameScene");
        }
    }

}
