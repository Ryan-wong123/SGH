using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;               // Photon hashtable
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class PuzzleScoreTracker : MonoBehaviourPunCallbacks
{
    public static PuzzleScoreTracker Instance;

    [Header("Optional End Screen UI")]
    [SerializeField] private GameObject endScreenPanel;   // assign in Inspector
    [SerializeField] private TMP_Text endStatsText;       // assign in Inspector

    // Room Custom Properties keys/prefix
    private const string SCORE_PREFIX = "Score_";   // full key looks like "Score_Puzzle5"
    private const string KEY_TOTAL = "total";       // int
    private const string KEY_PLAYERS = "players";   // nested Hashtable: { actorNumber:int => count:int }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Call this when a player submits and the puzzle is NOT solved.
    /// Master updates room properties; others send request to Master.
    /// </summary>
    public void MarkWrong(PuzzleStateManager.PuzzleStage stage)
    {
        int stageInt = (int)stage;
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        if (PhotonNetwork.IsMasterClient)
            AddWrongInternal(stageInt, actor);
        else
            photonView.RPC(nameof(RPC_RequestAddWrong), RpcTarget.MasterClient, stageInt, actor);
    }

    [PunRPC]
    private void RPC_RequestAddWrong(int stageInt, int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        AddWrongInternal(stageInt, actorNumber);
    }

    /// <summary>
    /// Master-only: increments total and per-player wrong counts for a given stage.
    /// Uses nested ExitGames.Hashtable so it replicates to everyone.
    /// </summary>
    private void AddWrongInternal(int stageInt, int actorNumber)
    {
        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        string roomKey = GetRoomKey(stageInt);

        // Read existing stats for this puzzle
        var scores = room.CustomProperties.ContainsKey(roomKey)
            ? (Hashtable)room.CustomProperties[roomKey]
            : new Hashtable();

        int total = scores.ContainsKey(KEY_TOTAL) ? (int)scores[KEY_TOTAL] : 0;
        total++;

        var playersHT = scores.ContainsKey(KEY_PLAYERS)
            ? (Hashtable)scores[KEY_PLAYERS]
            : new Hashtable();

        int current = playersHT.ContainsKey(actorNumber) ? (int)playersHT[actorNumber] : 0;
        playersHT[actorNumber] = current + 1;

        scores[KEY_TOTAL] = total;
        scores[KEY_PLAYERS] = playersHT;

        var setter = new Hashtable { [roomKey] = scores };
        room.SetCustomProperties(setter);  // replicates to everyone
    }

    private static string GetRoomKey(int stageInt) => $"{SCORE_PREFIX}Puzzle{stageInt + 1}";

    /// <summary>
    /// Builds a string summary from room properties and optionally shows it on the assigned end screen UI.
    /// </summary>
    public string BuildStatsSummary()
    {
        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return "No room.";

        // Gather all SCORE_* keys
        var allKeys = room.CustomProperties.Keys
            .OfType<string>()
            .Where(k => k.StartsWith(SCORE_PREFIX, StringComparison.Ordinal))
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        if (allKeys.Count == 0) return "No wrong attempts recorded.";

        List<string> lines = new();

        foreach (var key in allKeys)
        {
            // Key format: Score_PuzzleX  → get human-friendly name
            string puzzleLabel = key.Replace(SCORE_PREFIX, string.Empty); // "Puzzle5"
            var entry = (Hashtable)room.CustomProperties[key];

            int total = entry.ContainsKey(KEY_TOTAL) ? (int)entry[KEY_TOTAL] : 0;
            var playersHT = entry.ContainsKey(KEY_PLAYERS) ? (Hashtable)entry[KEY_PLAYERS] : new Hashtable();

            // Per-player lines
            List<string> playerLines = new();

            foreach (var pk in playersHT.Keys)
            {
                int actorNumber = (int)pk;
                int count = (int)playersHT[pk];

                Player p = PhotonNetwork.CurrentRoom?.GetPlayer(actorNumber);
                string name = !string.IsNullOrEmpty(p?.NickName) ? p.NickName : $"Player {actorNumber}";
                playerLines.Add($"{name} × {count}");
            }

            // Sort player lines by count desc
            playerLines = playerLines
                .OrderByDescending(s => ParseCount(s))
                .ThenBy(s => s, StringComparer.Ordinal)
                .ToList();

            lines.Add($"• {puzzleLabel}: {total} wrong\n    - " + (playerLines.Count > 0 ? string.Join("\n    - ", playerLines) : "No player errors"));
        }

        return string.Join("\n", lines);

        static int ParseCount(string s)
        {
            // expects "... × N"
            int idx = s.LastIndexOf('×');
            if (idx < 0) return 0;
            if (int.TryParse(s[(idx + 1)..].Trim(), out int n)) return n;
            return 0;
        }
    }

    /// <summary>
    /// Convenience: show the end panel using the assigned UI refs.
    /// </summary>
    public void ShowEndScreen()
    {
        string summary = BuildStatsSummary();

        if (endStatsText != null) endStatsText.text = summary;
        if (endScreenPanel != null) endScreenPanel.SetActive(true);
        else Debug.Log(summary); // fallback to console if no UI assigned
    }

    // (Optional) Allow you to call this if you want to reset all stats between playthroughs.
    public void ResetAllScores()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        List<object> toClear = room.CustomProperties.Keys
            .OfType<string>()
            .Where(k => k.StartsWith(SCORE_PREFIX, StringComparison.Ordinal))
            .Cast<object>()
            .ToList();

        var ht = new Hashtable();
        foreach (var key in toClear) ht[(string)key] = null; // removing keys
        room.SetCustomProperties(ht);
    }
}
