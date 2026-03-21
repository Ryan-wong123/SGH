using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;
using ExitGames.Client.Photon;


public class Network : MonoBehaviourPunCallbacks
{
    private const string PlayerTagPropertyKey = "PlayerTag";

    //game version
    [SerializeField] private string gameVersion = "1.0";
    //maximum number of player in 1 room
    [SerializeField] protected byte maxPlayersPerRoom = 4;

    //home panel 
    [SerializeField] private GameObject homePanel;
    //username input field
    [SerializeField] private TMP_InputField usernameInput;
    //button to go to room list panel
    [SerializeField] private Button goToRoomListButton;
    //join room manager component
    [SerializeField] private JoinRoomManager joinRoomManager;

    private void Start()
    {
        //connect to photon server with settings
        Debug.Log("Connecting to Photon...");
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = gameVersion;

        // If already connected, don't reconnect (reconnecting can kick you out / reset state)
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Already connected. Ensuring lobby...");
            EnsureLobbyJoined();
        }
        else
        {
            Debug.Log("Connecting to Photon...");
            PhotonNetwork.ConnectUsingSettings();
        }

        //get username and start game 
        goToRoomListButton.onClick.RemoveAllListeners();
        goToRoomListButton.onClick.AddListener(() =>
        {
            //check if username field is empty
            string username = usernameInput.text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                //update placeholder text if username is empty
                if (usernameInput.placeholder is TextMeshProUGUI placeholderText)
                {
                    placeholderText.text = "username is required";
                }

                //clear the input field to show the new placeholder
                usernameInput.text = "";
                return;
            }

            //set username 
            PhotonNetwork.NickName = username;
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
            {
                { PlayerTagPropertyKey, username }
            });

            //disable home panel and show the room list panel with create room button
            homePanel.SetActive(false);
            joinRoomManager.setRoomListPanelActive(true);
            joinRoomManager.setToggleRoomListPanelButtonActive(true);
            EnsureLobbyJoined();
        });
    }

    private void EnsureLobbyJoined()
    {
        // If you're in a room, you cannot join lobby yet
        if (PhotonNetwork.InRoom) return;

        // If connected to Master but not in lobby, join lobby
        if (!PhotonNetwork.InLobby && PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinLobby();
        }
    }
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby.");
    }
   
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"Disconnected from Photon: {cause}");
    }
}
