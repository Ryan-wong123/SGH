using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;
using ExitGames.Client.Photon;
using System;


public class Network : MonoBehaviourPunCallbacks
{
    private const string PlayerTagPropertyKey = "PlayerTag";
    private const string PhotonUserIdPrefKey = "PhotonUserId";
    private const string PhotonNicknamePrefKey = "PhotonNickname";
    private const string RejoinRoomPrefKey = "PhotonRejoinRoom";

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
    private static bool isAttemptingStoredRoomRejoin;
    private static bool isReconnectingToMaster;
    private static bool leavingRejectedStoredRoomRejoin;

    private void Start()
    {
        EnsurePersistentUserId();
        RestoreSavedNickname();
        EnsureChatManagerExists();

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
            PlayerPrefs.SetString(PhotonNicknamePrefKey, username);
            PlayerPrefs.Save();
            PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
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

    private static void EnsureChatManagerExists()
    {
        if (ChatManager.Instance != null || FindAnyObjectByType<ChatManager>() != null)
        {
            return;
        }

        new GameObject("ChatManager").AddComponent<ChatManager>();
    }

    public static void CacheCurrentRoomForRejoin()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        PlayerPrefs.SetString(RejoinRoomPrefKey, PhotonNetwork.CurrentRoom.Name);
        PlayerPrefs.Save();
    }

    public static void ClearCachedRoomRejoin()
    {
        PlayerPrefs.DeleteKey(RejoinRoomPrefKey);
        PlayerPrefs.Save();
        isAttemptingStoredRoomRejoin = false;
        isReconnectingToMaster = false;
        leavingRejectedStoredRoomRejoin = false;
    }

    public static string GetCachedRoomName()
    {
        return PlayerPrefs.GetString(RejoinRoomPrefKey, string.Empty);
    }

    public static void EnsurePersistentUserId()
    {
        string userId = PlayerPrefs.GetString(PhotonUserIdPrefKey, string.Empty);
        if (string.IsNullOrWhiteSpace(userId))
        {
            userId = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(PhotonUserIdPrefKey, userId);
            PlayerPrefs.Save();
        }

        PhotonNetwork.AuthValues ??= new AuthenticationValues();
        PhotonNetwork.AuthValues.UserId = userId;
    }

    private void RestoreSavedNickname()
    {
        string savedNickname = PlayerPrefs.GetString(PhotonNicknamePrefKey, string.Empty);
        if (string.IsNullOrWhiteSpace(savedNickname))
        {
            return;
        }

        PhotonNetwork.NickName = savedNickname;

        if (usernameInput != null && string.IsNullOrWhiteSpace(usernameInput.text))
        {
            usernameInput.text = savedNickname;
        }
    }

    public static void ApplyLocalPlayerTag()
    {
        if (PhotonNetwork.LocalPlayer == null || string.IsNullOrWhiteSpace(PhotonNetwork.NickName))
        {
            return;
        }

        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            { PlayerTagPropertyKey, PhotonNetwork.NickName }
        });
    }

    private bool TryRejoinCachedRoom()
    {
        string cachedRoomName = GetCachedRoomName();
        if (string.IsNullOrWhiteSpace(cachedRoomName) || !PhotonNetwork.IsConnectedAndReady || PhotonNetwork.InRoom)
        {
            return false;
        }

        isAttemptingStoredRoomRejoin = PhotonNetwork.RejoinRoom(cachedRoomName);
        Debug.Log(isAttemptingStoredRoomRejoin
            ? $"Attempting to rejoin cached room '{cachedRoomName}'."
            : $"Failed to start rejoin for cached room '{cachedRoomName}'.");

        return isAttemptingStoredRoomRejoin;
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
        isReconnectingToMaster = false;
        ApplyLocalPlayerTag();

        if (TryRejoinCachedRoom())
        {
            return;
        }

        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby.");
    }
   
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"Disconnected from Photon: {cause}");

        string cachedRoomName = GetCachedRoomName();
        if (!string.IsNullOrWhiteSpace(cachedRoomName) && !isReconnectingToMaster)
        {
            isReconnectingToMaster = true;
            bool reconnectStarted = PhotonNetwork.Reconnect();
            if (reconnectStarted)
            {
                Debug.Log($"Reconnect started for room '{cachedRoomName}'. Room availability will be revalidated after reconnecting.");
            }
            else
            {
                Debug.LogWarning($"Reconnect could not start for room '{cachedRoomName}'. Starting a fresh connection to revalidate instead.");
                PhotonNetwork.ConnectUsingSettings();
            }
        }
    }

    public override void OnJoinedRoom()
    {
        CacheCurrentRoomForRejoin();
        ApplyLocalPlayerTag();
        isAttemptingStoredRoomRejoin = false;
    }

    public override void OnLeftRoom()
    {
        bool shouldReturnToLobby = leavingRejectedStoredRoomRejoin;
        ClearCachedRoomRejoin();

        if (shouldReturnToLobby && PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"Join room failed ({returnCode}): {message}");

        if (!isAttemptingStoredRoomRejoin)
        {
            return;
        }

        ClearCachedRoomRejoin();
        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }
}
