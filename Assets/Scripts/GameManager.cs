using System.Collections; // Imports IEnumerator and coroutine support.
using Photon.Pun; // Imports Photon PUN networking APIs.
using Photon.Realtime; // Imports Photon Realtime types such as Player and DisconnectCause.
using UnityEngine; // Imports Unity engine types such as MonoBehaviour, GameObject, and Transform.
using UnityEngine.SceneManagement; // Imports Unity scene-loading APIs.

public class GameManager : MonoBehaviourPunCallbacks // Declares the gameplay manager that receives Photon callbacks.
{ // Starts the GameManager class body.
    [SerializeField] private GameObject[] characterPrefabs; // Exposes the available network-spawnable character prefabs in the inspector.
    [SerializeField] private Transform[] spawnPoints; // Exposes the possible player spawn locations in the inspector.

    private bool hasSpawned; // Tracks whether this client has already spawned its local player model.
    private string targetSceneName; // Stores the scene requested by a coordinated restart or leave flow.
    private Coroutine spawnRoutine; // Stores the active delayed spawn coroutine so duplicates are avoided.
    private bool isAttemptingGameplayRejoin; // Tracks whether a cached gameplay-room rejoin is currently in progress.
    private bool isReconnectingToGameplayRoom; // Tracks whether this client has already started a reconnect flow.
    private static string pendingSceneAfterLeavingRoom; // Stores a static scene target while Photon finishes leaving a room.

    private void Start() // Runs when Unity starts this GameManager instance.
    { // Starts the Start method body.
        Network.EnsurePersistentUserId(); // Ensures this player has a stable ID for Photon and rejoin behavior.
        PhotonNetwork.AutomaticallySyncScene = true; // Lets the master client's scene changes sync to other Photon clients.

        if (PhotonNetwork.InRoom) // Checks whether this client is already inside a Photon room.
        { // Starts the in-room setup block.
            Network.CacheCurrentRoomForRejoin(); // Saves the current room name so the player can reconnect later.
            Network.ApplyLocalPlayerTag(); // Applies this player's saved tag/custom properties to Photon.
        } // Ends the in-room setup block.
        else if (!string.IsNullOrWhiteSpace(Network.GetCachedRoomName())) // Checks whether a cached room exists while this client is outside a room.
        { // Starts the cached-room reconnect block.
            TryReconnectToGameplayRoom(); // Starts reconnecting to the cached gameplay room.
        } // Ends the cached-room reconnect block.

        TrySpawnLocalPlayer(); // Attempts to spawn the local player model when room state allows it.
    } // Ends the Start method body.

    public override void OnJoinedRoom() // Runs when Photon confirms this client joined a room.
    { // Starts the OnJoinedRoom callback body.
        Network.CacheCurrentRoomForRejoin(); // Stores the joined room so reconnect can use it later.
        Network.ApplyLocalPlayerTag(); // Reapplies the local player's Photon tag/custom properties after joining.
        isAttemptingGameplayRejoin = false; // Clears the active rejoin-attempt flag because the join succeeded.
        isReconnectingToGameplayRoom = false; // Clears the reconnect-in-progress flag because room entry succeeded.
        hasSpawned = false; // Resets spawn tracking for this room entry.
        TrySpawnLocalPlayer(); // Attempts to create the local player model in the joined room.
    } // Ends the OnJoinedRoom callback body.

    public override void OnConnectedToMaster() // Runs when this client connects to Photon Master Server.
    { // Starts the OnConnectedToMaster callback body.
        Network.ApplyLocalPlayerTag(); // Applies the local player's Photon tag/custom properties after reconnecting.
        isReconnectingToGameplayRoom = false; // Clears the reconnect flag because the server connection is ready.

        if (TryRejoinCachedGameplayRoom()) // Attempts to rejoin a cached gameplay room before entering the lobby.
        { // Starts the successful-rejoin-start block.
            return; // Leaves early because the rejoin process has started.
        } // Ends the successful-rejoin-start block.

        Network.TryJoinLobby(); // Joins the Photon lobby only when Photon is ready and not already joining.
    } // Ends the OnConnectedToMaster callback body.

    public override void OnDisconnected(DisconnectCause cause) // Runs when Photon disconnects this client.
    { // Starts the OnDisconnected callback body.
        hasSpawned = false; // Allows the local player model to be respawned after reconnecting.
        spawnRoutine = null; // Clears any delayed spawn coroutine reference after disconnection.

        if (string.IsNullOrWhiteSpace(targetSceneName)) // Checks whether this disconnect was not caused by a requested scene transition.
        { // Starts the gameplay reconnect block.
            TryReconnectToGameplayRoom(); // Attempts to recover the cached gameplay room connection.
        } // Ends the gameplay reconnect block.
    } // Ends the OnDisconnected callback body.

    public override void OnJoinRoomFailed(short returnCode, string message) // Runs when Photon fails to join or rejoin a room.
    { // Starts the OnJoinRoomFailed callback body.
        if (!isAttemptingGameplayRejoin) // Checks whether this failure belongs to a gameplay rejoin attempt.
        { // Starts the non-rejoin failure block.
            return; // Ignores unrelated join failures here.
        } // Ends the non-rejoin failure block.

        Debug.LogWarning($"Gameplay rejoin failed ({returnCode}): {message}"); // Logs the rejoin failure details for debugging.
        isAttemptingGameplayRejoin = false; // Clears the gameplay rejoin attempt flag.
        isReconnectingToGameplayRoom = false; // Clears the gameplay reconnect flag.
        Network.ClearCachedRoomRejoin(); // Removes the cached room because rejoin failed.
        SceneManager.LoadScene("Lobby"); // Returns the player to the lobby after failed rejoin.
    } // Ends the OnJoinRoomFailed callback body.

    public override void OnPlayerEnteredRoom(Player newPlayer) // Runs when another player enters the current room.
    { // Starts the OnPlayerEnteredRoom callback body.
        Network.CacheCurrentRoomForRejoin(); // Refreshes the cached room name while room membership changes.
    } // Ends the OnPlayerEnteredRoom callback body.

    public override void OnPlayerLeftRoom(Player otherPlayer) // Runs when another player leaves the current room.
    { // Starts the OnPlayerLeftRoom callback body.
        Network.CacheCurrentRoomForRejoin(); // Refreshes the cached room name while room membership changes.

        if (!PhotonNetwork.IsMasterClient || otherPlayer == null || !otherPlayer.IsInactive) // Checks whether this client should clean up an inactive player's objects.
        { // Starts the no-cleanup-needed block.
            return; // Leaves because cleanup is only done by the master client for inactive players.
        } // Ends the no-cleanup-needed block.

        PhotonNetwork.DestroyPlayerObjects(otherPlayer); // Removes networked objects owned by the inactive player.
        Debug.Log($"Destroyed player model for inactive actor {otherPlayer.ActorNumber}."); // Logs which inactive player's objects were removed.
    } // Ends the OnPlayerLeftRoom callback body.

    private void TrySpawnLocalPlayer() // Attempts to start the local player spawn process.
    { // Starts the TrySpawnLocalPlayer method body.
        if (!PhotonNetwork.InRoom || hasSpawned || spawnRoutine != null) // Checks whether spawning is unavailable, already done, or already queued.
        { // Starts the spawn-blocked block.
            return; // Leaves because the local player should not spawn right now.
        } // Ends the spawn-blocked block.

        if (HasLocalPlayerModel()) // Checks whether a local player model already exists in the scene.
        { // Starts the existing-model block.
            hasSpawned = true; // Marks spawning complete because the local model is already present.
            return; // Leaves without creating another player model.
        } // Ends the existing-model block.

        spawnRoutine = StartCoroutine(SpawnWhenSceneReady()); // Starts the delayed spawn routine and stores its handle.
    } // Ends the TrySpawnLocalPlayer method body.

    private bool TryRejoinCachedGameplayRoom() // Attempts to rejoin the cached gameplay room while connected to Master Server.
    { // Starts the TryRejoinCachedGameplayRoom method body.
        string cachedRoomName = Network.GetCachedRoomName(); // Reads the saved gameplay room name.
        if (string.IsNullOrWhiteSpace(cachedRoomName) || !PhotonNetwork.IsConnectedAndReady || PhotonNetwork.InRoom) // Checks whether rejoin prerequisites are missing.
        { // Starts the cannot-rejoin block.
            return false; // Reports that no rejoin attempt was started.
        } // Ends the cannot-rejoin block.

        isAttemptingGameplayRejoin = PhotonNetwork.RejoinRoom(cachedRoomName); // Starts Photon room rejoin and stores whether it began successfully.
        Debug.Log(isAttemptingGameplayRejoin // Logs the result of trying to start the room rejoin.
            ? $"Attempting to rejoin gameplay room '{cachedRoomName}'." // Message used when rejoin starts.
            : $"Failed to start gameplay rejoin for room '{cachedRoomName}'."); // Message used when rejoin cannot start.

        return isAttemptingGameplayRejoin; // Reports whether the rejoin attempt started.
    } // Ends the TryRejoinCachedGameplayRoom method body.

    private void TryReconnectToGameplayRoom() // Attempts to reconnect and rejoin the cached gameplay room.
    { // Starts the TryReconnectToGameplayRoom method body.
        string cachedRoomName = Network.GetCachedRoomName(); // Reads the saved gameplay room name.
        if (string.IsNullOrWhiteSpace(cachedRoomName) || isReconnectingToGameplayRoom || PhotonNetwork.InRoom) // Checks whether reconnect should be skipped.
        { // Starts the reconnect-skipped block.
            return; // Leaves because reconnect is unavailable, already running, or unnecessary.
        } // Ends the reconnect-skipped block.

        isReconnectingToGameplayRoom = true; // Marks that a gameplay reconnect flow has started.
        isAttemptingGameplayRejoin = true; // Marks that the reconnect flow is intended to rejoin gameplay.

        if (PhotonNetwork.ReconnectAndRejoin()) // Attempts Photon reconnect-and-rejoin in one call.
        { // Starts the reconnect-and-rejoin-started block.
            Debug.Log($"ReconnectAndRejoin started for gameplay room '{cachedRoomName}'."); // Logs that Photon accepted the reconnect-and-rejoin request.
            return; // Leaves because Photon is handling reconnect-and-rejoin.
        } // Ends the reconnect-and-rejoin-started block.

        Debug.Log($"ReconnectAndRejoin could not start for '{cachedRoomName}'. Reconnecting before rejoin."); // Logs the fallback reconnect path.
        if (!PhotonNetwork.Reconnect()) // Attempts a plain Photon reconnect first.
        { // Starts the reconnect-fallback block.
            PhotonNetwork.ConnectUsingSettings(); // Starts a fresh Photon connection when reconnect cannot start.
        } // Ends the reconnect-fallback block.
    } // Ends the TryReconnectToGameplayRoom method body.

    private IEnumerator SpawnWhenSceneReady() // Delays local player spawning until scene objects are likely ready.
    { // Starts the SpawnWhenSceneReady coroutine body.
        yield return new WaitForSeconds(0.5f); // Waits briefly for scene and Photon state to settle.
        spawnRoutine = null; // Clears the coroutine handle because this spawn attempt is now running.

        if (!PhotonNetwork.InRoom || HasLocalPlayerModel()) // Checks whether the client left the room or already has a local model.
        { // Starts the abort-spawn block.
            hasSpawned = HasLocalPlayerModel(); // Records whether the local model already exists.
            yield break; // Stops the coroutine without spawning a duplicate.
        } // Ends the abort-spawn block.

        int playerIndex = GetPlayerIndex(PhotonNetwork.LocalPlayer); // Calculates this player's index among room players.
        Transform spawnPoint = spawnPoints.Length > playerIndex ? spawnPoints[playerIndex] : spawnPoints[0]; // Chooses a matching spawn point or falls back to the first one.
        int characterIndex = GetSelectedCharacterIndex(); // Reads and validates the selected character index.
        PhotonNetwork.Instantiate(characterPrefabs[characterIndex].name, spawnPoint.position, spawnPoint.rotation); // Creates the selected character as a networked Photon object.
        hasSpawned = true; // Marks this client as spawned after instantiation.

        Debug.Log($"Spawned character index {characterIndex} for actor {PhotonNetwork.LocalPlayer.ActorNumber}."); // Logs which character was spawned for this actor.
    } // Ends the SpawnWhenSceneReady coroutine body.

    private int GetSelectedCharacterIndex() // Gets the selected character index from Photon player properties.
    { // Starts the GetSelectedCharacterIndex method body.
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("CharacterIndex", out object indexObj)) // Checks whether the local player has a saved character selection.
        { // Starts the character-selection-found block.
            int characterIndex = (int)indexObj; // Casts the saved Photon property to an integer index.
            if (characterIndex >= 0 && characterIndex < characterPrefabs.Length) // Validates the selected character index against the prefab list.
            { // Starts the valid-character-index block.
                return characterIndex; // Returns the validated selected character index.
            } // Ends the valid-character-index block.

            Debug.LogWarning("Invalid character index. Defaulting to first prefab."); // Logs that the saved character index is out of range.
            return 0; // Falls back to the first character prefab.
        } // Ends the character-selection-found block.

        Debug.LogWarning("No character selected. Defaulting to first prefab."); // Logs that no saved character selection was found.
        return 0; // Falls back to the first character prefab.
    } // Ends the GetSelectedCharacterIndex method body.

    private int GetPlayerIndex(Player player) // Calculates a stable player index based on Photon actor numbers.
    { // Starts the GetPlayerIndex method body.
        Player[] players = PhotonNetwork.PlayerList; // Copies the current Photon player list.
        System.Array.Sort(players, (a, b) => a.ActorNumber.CompareTo(b.ActorNumber)); // Sorts players by actor number for stable spawn ordering.

        for (int i = 0; i < players.Length; i++) // Loops through each sorted player.
        { // Starts the player-search loop body.
            if (players[i] == player) // Checks whether the current sorted player is the requested player.
            { // Starts the matching-player block.
                return i; // Returns the requested player's sorted index.
            } // Ends the matching-player block.
        } // Ends the player-search loop body.

        return 0; // Falls back to the first spawn index if the player is not found.
    } // Ends the GetPlayerIndex method body.

    private bool HasLocalPlayerModel() // Checks whether this client already owns a spawned player model.
    { // Starts the HasLocalPlayerModel method body.
        PlayerController[] playerControllers = FindObjectsOfType<PlayerController>(); // Finds all player controller instances in the scene.
        foreach (PlayerController controller in playerControllers) // Loops through every discovered player controller.
        { // Starts the player-controller loop body.
            PhotonView view = controller.GetComponent<PhotonView>(); // Reads the PhotonView attached to this player controller.
            if (view != null && view.IsMine) // Checks whether this controller belongs to the local client.
            { // Starts the local-model-found block.
                return true; // Reports that the local player model already exists.
            } // Ends the local-model-found block.
        } // Ends the player-controller loop body.

        return false; // Reports that no local player model was found.
    } // Ends the HasLocalPlayerModel method body.

    public void RestartLevel(string sceneName) // Requests a synchronized room leave and scene restart.
    { // Starts the RestartLevel method body.
        if (PhotonNetwork.IsMasterClient) // Checks whether this client is allowed to initiate the restart for everyone.
        { // Starts the master-client restart block.
            photonView.RPC(nameof(RPC_LeaveRoom), RpcTarget.All, sceneName); // Tells all clients to leave the room and remember the target scene.
        } // Ends the master-client restart block.
    } // Ends the RestartLevel method body.

    [PunRPC] // Marks the next method as callable through Photon RPC.
    private void RPC_LeaveRoom(string sceneName) // Runs on clients when a restart/leave RPC is received.
    { // Starts the RPC_LeaveRoom method body.
        targetSceneName = sceneName; // Stores the scene that should load after leaving the Photon room.
        PhotonNetwork.LeaveRoom(); // Starts leaving the current Photon room.
    } // Ends the RPC_LeaveRoom method body.

    public static void LeaveRoomAndLoadScene(string sceneName) // Leaves the current room and loads a requested scene afterward.
    { // Starts the LeaveRoomAndLoadScene method body.
        if (string.IsNullOrWhiteSpace(sceneName)) // Checks whether the caller provided a valid target scene name.
        { // Starts the invalid-scene block.
            Debug.LogWarning("Cannot leave room because no target scene was provided."); // Logs that the leave request is missing a scene.
            return; // Leaves because there is no scene to load.
        } // Ends the invalid-scene block.

        pendingSceneAfterLeavingRoom = sceneName; // Stores the target scene for the later OnLeftRoom callback.
        GameSessionCsvLogger.Instance?.WriteCsvIfNeeded(); // Writes the game session CSV before leaving if logging is available.
        Network.ClearCachedRoomRejoin(); // Clears cached room rejoin state because this is an intentional room leave.

        if (PhotonNetwork.InRoom) // Checks whether Photon must leave a room before loading the target scene.
        { // Starts the in-room leave block.
            PhotonNetwork.LeaveRoom(); // Starts leaving the current Photon room.
            return; // Waits for OnLeftRoom to load the target scene.
        } // Ends the in-room leave block.

        SceneManager.LoadScene(sceneName); // Loads the target scene immediately when already outside a room.
    } // Ends the LeaveRoomAndLoadScene method body.

    public override void OnLeftRoom() // Runs when Photon confirms this client left the room.
    { // Starts the OnLeftRoom callback body.
        Network.ClearCachedRoomRejoin(); // Clears cached rejoin data after an intentional room leave.
        hasSpawned = false; // Resets local spawn tracking because the room has ended for this client.
        spawnRoutine = null; // Clears any delayed spawn coroutine reference.
        string sceneToLoad = !string.IsNullOrWhiteSpace(targetSceneName) // Chooses a scene requested by RPC when one exists.
            ? targetSceneName // Uses the instance target scene from a restart/leave RPC.
            : pendingSceneAfterLeavingRoom; // Otherwise uses the static target scene from LeaveRoomAndLoadScene.
        pendingSceneAfterLeavingRoom = null; // Clears the static pending scene after reading it.

        Destroy(gameObject); // Destroys this GameManager because it belongs to the old room/session.

        if (!string.IsNullOrWhiteSpace(sceneToLoad)) // Checks whether there is a scene to load after leaving.
            SceneManager.LoadScene(sceneToLoad); // Loads the chosen scene after the room leave completes.
    } // Ends the OnLeftRoom callback body.

    public void QuitGame() // Quits play mode or the built game after saving session data.
    { // Starts the QuitGame method body.
        GameSessionCsvLogger.Instance?.WriteCsvIfNeeded(); // Writes the game session CSV before quitting if logging is available.

#if UNITY_EDITOR // Compiles the editor-only quit behavior when running inside Unity Editor.
        UnityEditor.EditorApplication.isPlaying = false; // Stops play mode in the Unity Editor.
#else // Compiles the player-build quit behavior outside Unity Editor.
        Application.Quit(); // Quits the built application.
#endif // Ends the editor/player compile-time branch.
    } // Ends the QuitGame method body.
} // Ends the GameManager class body.
