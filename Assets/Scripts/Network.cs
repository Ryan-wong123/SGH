using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public class Network : MonoBehaviourPunCallbacks
{
    [SerializeField] private string gameVersion = "1.0";
    [SerializeField] protected byte maxPlayersPerRoom = 4;

    [SerializeField] private GameObject homePanel;  
    [SerializeField] private InputField usernameInput;
    [SerializeField] private Button goToRoomListButton;

    [SerializeField] private JoinRoomManager joinRoomManager;

    private void Start()
    {
        Debug.Log("Connecting to Photon...");
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();

        goToRoomListButton.onClick.AddListener(() =>
        {
            string username = usernameInput.text;
            if (string.IsNullOrEmpty(username))
            {
                // Update placeholder text if room name is empty
                if (usernameInput.placeholder is Text placeholderText)
                {
                    placeholderText.text = "username is required";
                }

                // Clear the input field to show the new placeholder
                usernameInput.text = "";
                return;
            }

            PhotonNetwork.NickName = username;
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
