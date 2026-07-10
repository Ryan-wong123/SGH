using System; // Imports StringComparison and related base types.
using System.Collections.Generic; // Imports List collection support.
using System.Linq; // Imports LINQ helpers used to filter and sort score keys.
using ExitGames.Client.Photon;               // Photon hashtable // Imports Photon Hashtable for room custom properties.
using Photon.Pun; // Imports Photon PUN networking APIs and callbacks.
using Photon.Realtime; // Imports Photon Realtime Player and room types.
using TMPro; // Imports TextMeshPro UI text types.
using UnityEngine; // Imports Unity engine types such as MonoBehaviour and GameObject.

public class PuzzleScoreTracker : MonoBehaviourPunCallbacks // Declares the networked tracker for puzzle wrong-attempt scores.
{ // Starts the PuzzleScoreTracker class body.
    public static PuzzleScoreTracker Instance; // Stores the shared PuzzleScoreTracker singleton instance.

    [Header("Optional End Screen UI")] // Groups optional end-screen references in the Unity inspector.
    [SerializeField] private GameObject endScreenPanel;   // assign in Inspector // References the optional panel shown at the end of the game.
    [SerializeField] private TMP_Text endStatsText;       // assign in Inspector // References the optional text field that displays score summary output.

    // Room Custom Properties keys/prefix
    private const string SCORE_PREFIX = "Score_";   // full key looks like "Score_Puzzle5" // Prefixes room-property keys that store puzzle score data.
    private const string KEY_TOTAL = "total";       // int // Names the total wrong-attempt count inside a score table.
    private const string KEY_PLAYERS = "players";   // nested Hashtable: { actorNumber:int => count:int } // Names the per-player wrong-attempt table inside a score table.

    private void Awake() // Runs before Start to initialize the singleton.
    { // Starts the Awake method body.
        if (Instance == null) Instance = this; // Registers this component as the singleton if none exists.
        else { Destroy(gameObject); return; } // Destroys duplicate score trackers and exits setup.
        DontDestroyOnLoad(gameObject); // Keeps the score tracker alive across scene changes.
    } // Ends the Awake method body.

    /// <summary>
    /// Call this when a player submits and the puzzle is NOT solved.
    /// Master updates room properties; others send request to Master.
    /// </summary>
    public void MarkWrong(PuzzleStateManager.PuzzleStage stage) // Records one wrong attempt for the given puzzle stage.
    { // Starts the MarkWrong method body.
        int stageInt = (int)stage; // Converts the puzzle stage enum into an integer stage number.
        int actor = PhotonNetwork.LocalPlayer.ActorNumber; // Reads the local player's Photon actor number.
        if (PhotonNetwork.IsMasterClient) // Checks whether this client can write room score properties directly.
            AddWrongInternal(stageInt, actor); // Adds the wrong attempt immediately on the master client.
        else // Runs when a non-master client needs the master to update shared score state.
            photonView.RPC(nameof(RPC_RequestAddWrong), RpcTarget.MasterClient, stageInt, actor); // Sends the wrong-attempt request to the master client.
    } // Ends the MarkWrong method body.

    [PunRPC] // Marks the next method as callable through Photon RPC.
    private void RPC_RequestAddWrong(int stageInt, int actorNumber) // Receives a non-master client's wrong-attempt request.
    { // Starts the RPC_RequestAddWrong method body.
        if (!PhotonNetwork.IsMasterClient) return; // Ignores the request unless this client is the master.
        AddWrongInternal(stageInt, actorNumber); // Adds the requested wrong attempt to shared score properties.
    } // Ends the RPC_RequestAddWrong method body.

    /// <summary>
    /// Master-only: increments total and per-player wrong counts for a given stage.
    /// Uses nested ExitGames.Hashtable so it replicates to everyone.
    /// </summary>
    private void AddWrongInternal(int stageInt, int actorNumber) // Updates room custom properties with one wrong attempt.
    { // Starts the AddWrongInternal method body.
        var room = PhotonNetwork.CurrentRoom; // Reads the current Photon room.
        if (room == null) return; // Leaves if there is no room to store score data in.

        string roomKey = GetRoomKey(stageInt); // Builds the room-property key for this puzzle stage.

        // Read existing stats for this puzzle
        var scores = room.CustomProperties.ContainsKey(roomKey) // Checks whether this puzzle already has score data.
            ? (Hashtable)room.CustomProperties[roomKey] // Reuses the existing nested score table.
            : new Hashtable(); // Creates a new nested score table for this puzzle.

        int total = scores.ContainsKey(KEY_TOTAL) ? (int)scores[KEY_TOTAL] : 0; // Reads the existing total wrong-attempt count.
        total++; // Increments the total wrong-attempt count.

        var playersHT = scores.ContainsKey(KEY_PLAYERS) // Checks whether per-player score data already exists.
            ? (Hashtable)scores[KEY_PLAYERS] // Reuses the existing per-player score table.
            : new Hashtable(); // Creates a new per-player score table.

        int current = playersHT.ContainsKey(actorNumber) ? (int)playersHT[actorNumber] : 0; // Reads this actor's current wrong-attempt count.
        playersHT[actorNumber] = current + 1; // Stores the incremented wrong-attempt count for this actor.

        scores[KEY_TOTAL] = total; // Writes the updated total count into the nested score table.
        scores[KEY_PLAYERS] = playersHT; // Writes the updated per-player table into the nested score table.

        var setter = new Hashtable { [roomKey] = scores }; // Wraps the nested score table in a room-property update table.
        room.SetCustomProperties(setter);  // replicates to everyone // Publishes the updated score data to all clients through Photon.
    } // Ends the AddWrongInternal method body.

    private static string GetRoomKey(int stageInt) => $"{SCORE_PREFIX}Puzzle{stageInt + 1}"; // Builds the Photon room-property key for a puzzle stage.

    /// <summary>
    /// Builds a string summary from room properties and optionally shows it on the assigned end screen UI.
    /// </summary>
    public string BuildStatsSummary() // Builds a human-readable wrong-attempt summary from Photon room properties.
    { // Starts the BuildStatsSummary method body.
        var room = PhotonNetwork.CurrentRoom; // Reads the current Photon room.
        if (room == null) return "No room."; // Returns a fallback message when no Photon room exists.

        // Gather all SCORE_* keys
        var allKeys = room.CustomProperties.Keys // Reads all custom property keys from the current room.
            .OfType<string>() // Keeps only string keys.
            .Where(k => k.StartsWith(SCORE_PREFIX, StringComparison.Ordinal)) // Keeps only score-related room-property keys.
            .OrderBy(k => k, StringComparer.Ordinal) // Sorts score keys consistently by name.
            .ToList(); // Materializes the score keys into a list.

        if (allKeys.Count == 0) return "No wrong attempts recorded."; // Returns a fallback message when no scores exist.

        List<string> lines = new(); // Stores formatted summary lines for each puzzle.

        foreach (var key in allKeys) // Loops through each score property key.
        { // Starts the score-key loop body.
            // Key format: Score_PuzzleX  → get human-friendly name
            string puzzleLabel = key.Replace(SCORE_PREFIX, string.Empty); // "Puzzle5" // Removes the score prefix to get a readable puzzle label.
            var entry = (Hashtable)room.CustomProperties[key]; // Reads the nested score table for this puzzle.

            int total = entry.ContainsKey(KEY_TOTAL) ? (int)entry[KEY_TOTAL] : 0; // Reads this puzzle's total wrong-attempt count.
            var playersHT = entry.ContainsKey(KEY_PLAYERS) ? (Hashtable)entry[KEY_PLAYERS] : new Hashtable(); // Reads the per-player score table.

            // Per-player lines
            List<string> playerLines = new(); // Stores formatted per-player score lines.

            foreach (var pk in playersHT.Keys) // Loops through each actor number stored in the per-player table.
            { // Starts the per-player loop body.
                int actorNumber = (int)pk; // Converts the hashtable key into a Photon actor number.
                int count = (int)playersHT[pk]; // Reads the wrong-attempt count for this actor.

                Player p = PhotonNetwork.CurrentRoom?.GetPlayer(actorNumber); // Looks up the Photon player for this actor number.
                string name = !string.IsNullOrEmpty(p?.NickName) ? p.NickName : $"Player {actorNumber}"; // Uses nickname when available, otherwise falls back to actor number.
                playerLines.Add($"{name} × {count}"); // Adds this player's formatted wrong-attempt line.
            } // Ends the per-player loop body.

            // Sort player lines by count desc
            playerLines = playerLines // Replaces the list with a sorted copy.
                .OrderByDescending(s => ParseCount(s)) // Sorts players by wrong-attempt count descending.
                .ThenBy(s => s, StringComparer.Ordinal) // Sorts tied entries alphabetically for stable output.
                .ToList(); // Materializes the sorted player lines into a list.

            lines.Add($"• {puzzleLabel}: {total} wrong\n    - " + (playerLines.Count > 0 ? string.Join("\n    - ", playerLines) : "No player errors")); // Adds the formatted puzzle summary line.
        } // Ends the score-key loop body.

        return string.Join("\n", lines); // Joins all puzzle summary lines into one display string.

        static int ParseCount(string s) // Extracts the numeric count from a formatted player summary line.
        { // Starts the ParseCount local function body.
            // expects "... × N"
            int idx = s.LastIndexOf('×'); // Finds the multiplication symbol before the count.
            if (idx < 0) return 0; // Returns zero if the count separator is missing.
            if (int.TryParse(s[(idx + 1)..].Trim(), out int n)) return n; // Parses and returns the count after the separator.
            return 0; // Falls back to zero when parsing fails.
        } // Ends the ParseCount local function body.
    } // Ends the BuildStatsSummary method body.

    /// <summary>
    /// Convenience: show the end panel using the assigned UI refs.
    /// </summary>
    public void ShowEndScreen() // Shows the optional end-screen UI with the score summary.
    { // Starts the ShowEndScreen method body.
        string summary = BuildStatsSummary(); // Builds the latest wrong-attempt summary text.

        if (endStatsText != null) endStatsText.text = summary; // Writes the summary into the assigned text field when present.
        if (endScreenPanel != null) endScreenPanel.SetActive(true); // Shows the assigned end screen panel when present.
        else Debug.Log(summary); // fallback to console if no UI assigned // Logs the summary when no panel is assigned.
    } // Ends the ShowEndScreen method body.

    // (Optional) Allow you to call this if you want to reset all stats between playthroughs.
    public void ResetAllScores() // Clears all score-related room properties for a new playthrough.
    { // Starts the ResetAllScores method body.
        if (!PhotonNetwork.IsMasterClient) return; // Allows only the master client to clear shared score properties.

        var room = PhotonNetwork.CurrentRoom; // Reads the current Photon room.
        if (room == null) return; // Leaves if there is no room to clear.

        List<object> toClear = room.CustomProperties.Keys // Reads all custom property keys from the room.
            .OfType<string>() // Keeps only string keys.
            .Where(k => k.StartsWith(SCORE_PREFIX, StringComparison.Ordinal)) // Keeps only score-related keys.
            .Cast<object>() // Converts keys to object type for hashtable storage.
            .ToList(); // Materializes the keys that should be removed.

        var ht = new Hashtable(); // Creates a property update table for removing score keys.
        foreach (var key in toClear) ht[(string)key] = null; // removing keys // Marks this score key for removal by assigning null.
        room.SetCustomProperties(ht); // Publishes the score-clearing update to the room.
    } // Ends the ResetAllScores method body.
} // Ends the PuzzleScoreTracker class body.
