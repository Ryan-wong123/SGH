using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject[] characterPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    private bool hasSpawned = false;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (PhotonNetwork.InRoom && !hasSpawned)
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


    public void RestartLevel()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().name);
        }
    }
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
