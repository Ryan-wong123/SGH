using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    private const float RejoinRetryDelaySeconds = 2f;

    [SerializeField] private GameObject[] characterPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    private bool hasSpawned = false;
    private string targetSceneName;
    private string rejoinRoomName;
    private bool isTryingToReconnect;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        rejoinRoomName = PhotonNetwork.CurrentRoom?.Name;

        if (PhotonNetwork.InRoom && !hasSpawned)
        {
            hasSpawned = true;
            StartCoroutine(SpawnWhenSceneReady());
        }
    }

    public override void OnJoinedRoom()
    {
        isTryingToReconnect = false;
        rejoinRoomName = PhotonNetwork.CurrentRoom?.Name;

        if (!hasSpawned)
        {
            hasSpawned = true;
            StartCoroutine(SpawnWhenSceneReady());
        }
    }

    private IEnumerator SpawnWhenSceneReady()
    {
        yield return new WaitForSeconds(0.5f);

        int playerIndex = GetPlayerIndex(PhotonNetwork.LocalPlayer);
        Transform spawnPoint = spawnPoints.Length > playerIndex ? spawnPoints[playerIndex] : spawnPoints[0];

        // ✅ Retrieve selected character index
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("CharacterIndex", out object indexObj))
        {
            int characterIndex = (int)indexObj;

            if (characterIndex >= 0 && characterIndex < characterPrefabs.Length)
            {
                PhotonNetwork.Instantiate(characterPrefabs[characterIndex].name, spawnPoint.position, spawnPoint.rotation);
                Debug.Log($"Spawned character index {characterIndex}");
            }
            else
            {
                Debug.LogWarning("Invalid character index. Defaulting to first prefab.");
                PhotonNetwork.Instantiate(characterPrefabs[0].name, spawnPoint.position, spawnPoint.rotation);
            }
        }
        else
        {
            Debug.LogWarning("No character selected. Defaulting to first prefab.");
            PhotonNetwork.Instantiate(characterPrefabs[0].name, spawnPoint.position, spawnPoint.rotation);
        }
    }

    private int GetPlayerIndex(Player player)
    {
        Player[] players = PhotonNetwork.PlayerList;
        System.Array.Sort(players, (a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        for (int i = 0; i < players.Length; i++)
            if (players[i] == player) return i;

        return 0;
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
        targetSceneName = sceneName;   // store scene name locally
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        Destroy(gameObject);
        SceneManager.LoadScene(targetSceneName);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (PhotonNetwork.InRoom)
        {
            return;
        }

        if (string.IsNullOrEmpty(rejoinRoomName))
        {
            rejoinRoomName = PhotonNetwork.CurrentRoom?.Name;
        }

        if (string.IsNullOrEmpty(rejoinRoomName) || isTryingToReconnect)
        {
            return;
        }

        Debug.LogWarning($"Disconnected during gameplay. Attempting reconnect and rejoin. Cause: {cause}");
        isTryingToReconnect = true;
        hasSpawned = false;
        StartCoroutine(ReconnectAndRejoinRoutine());
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Rejoin failed: {message} ({returnCode})");
        isTryingToReconnect = false;
    }

    public override void OnConnectedToMaster()
    {
        if (!isTryingToReconnect || string.IsNullOrEmpty(rejoinRoomName))
        {
            return;
        }

        Debug.Log("Connected to master while recovering. Attempting room rejoin...");
        PhotonNetwork.RejoinRoom(rejoinRoomName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator ReconnectAndRejoinRoutine()
    {
        while (isTryingToReconnect && !PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.NetworkClientState == ClientState.Disconnected)
            {
                bool startedReconnect = PhotonNetwork.ReconnectAndRejoin();

                if (!startedReconnect)
                {
                    PhotonNetwork.Reconnect();
                }
            }

            yield return new WaitForSeconds(RejoinRetryDelaySeconds);
        }
    }
}
