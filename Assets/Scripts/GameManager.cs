using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private bool hasSpawned = false;

    private void Start()
    {
        if (PhotonNetwork.InRoom && !hasSpawned)
        {
            hasSpawned = true;
            StartCoroutine(SpawnWhenSceneReady());
        }
    }


    private IEnumerator SpawnWhenSceneReady()
    {
        // Wait a few frames to ensure the scene is fully loaded
        yield return new WaitForSeconds(0.5f);

        int playerIndex = GetPlayerIndex(PhotonNetwork.LocalPlayer);
        if (playerIndex >= spawnPoints.Length)
        {
            Debug.LogWarning("Not enough spawn points! Spawning at default.");
            playerIndex = 0;
        }

        Transform spawnPoint = spawnPoints[playerIndex];
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
    }

    private int GetPlayerIndex(Player player)
    {
        // Sort players by join order using ActorNumber
        Player[] players = PhotonNetwork.PlayerList;
        System.Array.Sort(players, (a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == player)
                return i;
        }

        return 0; // fallback
    }
}
