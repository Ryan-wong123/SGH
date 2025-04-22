using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;


public class Network : MonoBehaviourPunCallbacks
{
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
        PhotonNetwork.ConnectUsingSettings();

        //get username and start game 
        goToRoomListButton.onClick.AddListener(() =>
        {
            //check if username field is empty
            string username = usernameInput.text;
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
            //disable home panel and show the room list panel with create room button
            homePanel.SetActive(false);
            joinRoomManager.setRoomListPanelActive(true);
            joinRoomManager.setToggleRoomListPanelButtonActive(true);
        });
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
