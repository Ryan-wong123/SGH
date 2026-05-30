using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject[] characterPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    private bool hasSpawned;
    private string targetSceneName;
    private Coroutine spawnRoutine;
    private bool isAttemptingGameplayRejoin;
    private bool isReconnectingToGameplayRoom;
    private static string pendingSceneAfterLeavingRoom;

    private void Start()
    {
        Network.EnsurePersistentUserId();
        PhotonNetwork.AutomaticallySyncScene = true;

        if (PhotonNetwork.InRoom)
        {
            Network.CacheCurrentRoomForRejoin();
            Network.ApplyLocalPlayerTag();
        }
        else if (!string.IsNullOrWhiteSpace(Network.GetCachedRoomName()))
        {
            TryReconnectToGameplayRoom();
        }

        TrySpawnLocalPlayer();
    }

    public override void OnJoinedRoom()
    {
        Network.CacheCurrentRoomForRejoin();
        Network.ApplyLocalPlayerTag();
        isAttemptingGameplayRejoin = false;
        isReconnectingToGameplayRoom = false;
        hasSpawned = false;
        TrySpawnLocalPlayer();
    }

    public override void OnConnectedToMaster()
    {
        Network.ApplyLocalPlayerTag();
        isReconnectingToGameplayRoom = false;

        if (TryRejoinCachedGameplayRoom())
        {
            return;
        }

        PhotonNetwork.JoinLobby();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        hasSpawned = false;
        spawnRoutine = null;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            TryReconnectToGameplayRoom();
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (!isAttemptingGameplayRejoin)
        {
            return;
        }

        Debug.LogWarning($"Gameplay rejoin failed ({returnCode}): {message}");
        isAttemptingGameplayRejoin = false;
        isReconnectingToGameplayRoom = false;
        Network.ClearCachedRoomRejoin();
        SceneManager.LoadScene("Lobby");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Network.CacheCurrentRoomForRejoin();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Network.CacheCurrentRoomForRejoin();

        if (!PhotonNetwork.IsMasterClient || otherPlayer == null || !otherPlayer.IsInactive)
        {
            return;
        }

        PhotonNetwork.DestroyPlayerObjects(otherPlayer);
        Debug.Log($"Destroyed player model for inactive actor {otherPlayer.ActorNumber}.");
    }

    private void TrySpawnLocalPlayer()
    {
        if (!PhotonNetwork.InRoom || hasSpawned || spawnRoutine != null)
        {
            return;
        }

        if (HasLocalPlayerModel())
        {
            hasSpawned = true;
            return;
        }

        spawnRoutine = StartCoroutine(SpawnWhenSceneReady());
    }

    private bool TryRejoinCachedGameplayRoom()
    {
        string cachedRoomName = Network.GetCachedRoomName();
        if (string.IsNullOrWhiteSpace(cachedRoomName) || !PhotonNetwork.IsConnectedAndReady || PhotonNetwork.InRoom)
        {
            return false;
        }

        isAttemptingGameplayRejoin = PhotonNetwork.RejoinRoom(cachedRoomName);
        Debug.Log(isAttemptingGameplayRejoin
            ? $"Attempting to rejoin gameplay room '{cachedRoomName}'."
            : $"Failed to start gameplay rejoin for room '{cachedRoomName}'.");

        return isAttemptingGameplayRejoin;
    }

    private void TryReconnectToGameplayRoom()
    {
        string cachedRoomName = Network.GetCachedRoomName();
        if (string.IsNullOrWhiteSpace(cachedRoomName) || isReconnectingToGameplayRoom || PhotonNetwork.InRoom)
        {
            return;
        }

        isReconnectingToGameplayRoom = true;
        isAttemptingGameplayRejoin = true;

        if (PhotonNetwork.ReconnectAndRejoin())
        {
            Debug.Log($"ReconnectAndRejoin started for gameplay room '{cachedRoomName}'.");
            return;
        }

        Debug.Log($"ReconnectAndRejoin could not start for '{cachedRoomName}'. Reconnecting before rejoin.");
        if (!PhotonNetwork.Reconnect())
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    private IEnumerator SpawnWhenSceneReady()
    {
        yield return new WaitForSeconds(0.5f);
        spawnRoutine = null;

        if (!PhotonNetwork.InRoom || HasLocalPlayerModel())
        {
            hasSpawned = HasLocalPlayerModel();
            yield break;
        }

        int playerIndex = GetPlayerIndex(PhotonNetwork.LocalPlayer);
        Transform spawnPoint = spawnPoints.Length > playerIndex ? spawnPoints[playerIndex] : spawnPoints[0];
        int characterIndex = GetSelectedCharacterIndex();
        PhotonNetwork.Instantiate(characterPrefabs[characterIndex].name, spawnPoint.position, spawnPoint.rotation);
        hasSpawned = true;

        Debug.Log($"Spawned character index {characterIndex} for actor {PhotonNetwork.LocalPlayer.ActorNumber}.");
    }

    private int GetSelectedCharacterIndex()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("CharacterIndex", out object indexObj))
        {
            int characterIndex = (int)indexObj;
            if (characterIndex >= 0 && characterIndex < characterPrefabs.Length)
            {
                return characterIndex;
            }

            Debug.LogWarning("Invalid character index. Defaulting to first prefab.");
            return 0;
        }

        Debug.LogWarning("No character selected. Defaulting to first prefab.");
        return 0;
    }

    private int GetPlayerIndex(Player player)
    {
        Player[] players = PhotonNetwork.PlayerList;
        System.Array.Sort(players, (a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == player)
            {
                return i;
            }
        }

        return 0;
    }

    private bool HasLocalPlayerModel()
    {
        PlayerController[] playerControllers = FindObjectsOfType<PlayerController>();
        foreach (PlayerController controller in playerControllers)
        {
            PhotonView view = controller.GetComponent<PhotonView>();
            if (view != null && view.IsMine)
            {
                return true;
            }
        }

        return false;
    }

    public void RestartLevel(string sceneName)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_LeaveRoom), RpcTarget.All, sceneName);
        }
    }

    [PunRPC]
    private void RPC_LeaveRoom(string sceneName)
    {
        targetSceneName = sceneName;
        PhotonNetwork.LeaveRoom();
    }

    public static void LeaveRoomAndLoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Cannot leave room because no target scene was provided.");
            return;
        }

        pendingSceneAfterLeavingRoom = sceneName;
        GameSessionCsvLogger.Instance?.WriteCsvIfNeeded();
        Network.ClearCachedRoomRejoin();

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public override void OnLeftRoom()
    {
        Network.ClearCachedRoomRejoin();
        hasSpawned = false;
        spawnRoutine = null;
        string sceneToLoad = !string.IsNullOrWhiteSpace(targetSceneName)
            ? targetSceneName
            : pendingSceneAfterLeavingRoom;
        pendingSceneAfterLeavingRoom = null;

        Destroy(gameObject);

        if (!string.IsNullOrWhiteSpace(sceneToLoad))
            SceneManager.LoadScene(sceneToLoad);
    }

    public void QuitGame()
    {
        GameSessionCsvLogger.Instance?.WriteCsvIfNeeded();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
