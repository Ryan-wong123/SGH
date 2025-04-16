using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class JoinRoomManager : Network
{
    [SerializeField] private GameObject roomItemPrefab; // UI prefab for a room
    [SerializeField] private Transform contentPanel;    // ScrollView Content parent
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button toggleRoomListPanelButton;
    [SerializeField] private GameObject roomListPanel;
    [SerializeField] private GameObject roomPanel;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    private void Start()
    {
        refreshButton.onClick.AddListener(RefreshRoomList);
        // Toggle visibility of RoomListPanel and CreateRoomPanel
        toggleRoomListPanelButton.onClick.AddListener(() =>
        {
            roomListPanel.SetActive(!roomListPanel.activeSelf);
        });
    }

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
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

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
        toggleRoomListPanelButton.gameObject.SetActive(false);
        roomListPanel.SetActive(false);
        roomPanel.SetActive(true);
    }


}
