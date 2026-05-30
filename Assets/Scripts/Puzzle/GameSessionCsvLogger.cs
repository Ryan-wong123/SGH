using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class GameSessionCsvLogger : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private const byte GameplayLogEventCode = 41;
    private const int EventTypeEnter = 0;
    private const int EventTypeAnswer = 1;
    private const int EventTypeTimeSpent = 2;

    public static GameSessionCsvLogger Instance { get; private set; }

    private readonly Dictionary<string, PuzzlePlayerStats> statsByKey = new();
    private readonly Dictionary<string, double> localAttemptStartTimes = new();
    private bool hasWrittenCsv;

    private sealed class PuzzlePlayerStats
    {
        public string RoomName;
        public int ActorNumber;
        public string PlayerName;
        public int PuzzleNumber;
        public int TimesEntered;
        public int CorrectAnswers;
        public int WrongAnswers;
        public double TimeTakenSeconds;
        public List<string> AnswerSelections = new();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureLoggerExists()
    {
        if (Instance != null)
            return;

        var existing = FindObjectOfType<GameSessionCsvLogger>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        var loggerObject = new GameObject(nameof(GameSessionCsvLogger));
        loggerObject.AddComponent<GameSessionCsvLogger>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        base.OnDisable();
    }

    public void RecordPuzzleEntered(PuzzleStateManager.PuzzleStage stage, int actorNumber, string playerName)
    {
        if (stage == PuzzleStateManager.PuzzleStage.Puzzle0)
            return;

        string roomName = GetCurrentRoomName();
        double now = PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.realtimeSinceStartupAsDouble;
        string attemptKey = BuildAttemptKey(actorNumber, stage);

        if (!localAttemptStartTimes.ContainsKey(attemptKey))
            localAttemptStartTimes[attemptKey] = now;

        SendOrApplyEvent(EventTypeEnter, roomName, actorNumber, playerName, (int)stage, 0, 0d);
    }

    public void RecordPuzzleReset(PuzzleStateManager.PuzzleStage stage)
    {
        if (stage == PuzzleStateManager.PuzzleStage.Puzzle0)
            return;

        int actorNumber = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;
        string playerName = ResolvePlayerName(actorNumber);
        double elapsedSeconds = CaptureElapsedSeconds(actorNumber, stage, keepRunning: true);

        if (elapsedSeconds <= 0d)
            return;

        SendOrApplyEvent(EventTypeTimeSpent, GetCurrentRoomName(), actorNumber, playerName, (int)stage, 0, elapsedSeconds);
    }

    public void RecordPuzzleAnswer(PuzzleStateManager.PuzzleStage stage, bool isCorrect, string selectedAnswers = null)
    {
        if (stage == PuzzleStateManager.PuzzleStage.Puzzle0)
            return;

        int actorNumber = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;
        string playerName = ResolvePlayerName(actorNumber);
        double elapsedSeconds = CaptureElapsedSeconds(actorNumber, stage, keepRunning: !isCorrect);

        SendOrApplyEvent(EventTypeAnswer, GetCurrentRoomName(), actorNumber, playerName, (int)stage, isCorrect ? 1 : 0, elapsedSeconds, selectedAnswers);
    }

    public void WriteCsvIfNeeded()
    {
        if (hasWrittenCsv)
            return;

        FlushLocalActiveAttempts();

        if (statsByKey.Count == 0)
            return;

        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
            return;

        hasWrittenCsv = true;

        string directory = Path.Combine(Application.persistentDataPath, "GameplayLogs");
        Directory.CreateDirectory(directory);

        string roomName = SanitizeFileName(GetCurrentRoomName());
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        string path = Path.Combine(directory, $"gameplay_{roomName}_{timestamp}.csv");

        var rows = new List<PuzzlePlayerStats>(statsByKey.Values);
        rows.Sort((a, b) =>
        {
            int puzzleCompare = a.PuzzleNumber.CompareTo(b.PuzzleNumber);
            if (puzzleCompare != 0) return puzzleCompare;

            int playerCompare = string.Compare(a.PlayerName, b.PlayerName, StringComparison.OrdinalIgnoreCase);
            return playerCompare != 0 ? playerCompare : a.ActorNumber.CompareTo(b.ActorNumber);
        });

        int maxAnswerTries = 0;
        foreach (var row in rows)
            maxAnswerTries = Math.Max(maxAnswerTries, row.AnswerSelections.Count);

        var csv = new StringBuilder();
        csv.Append("room_name,player_name,puzzle_number,times_entered,correct_answers,wrong_answers");
        for (int i = 1; i <= maxAnswerTries; i++)
            csv.Append($",answer_try_{i}");
        csv.AppendLine(",time_taken");

        foreach (var row in rows)
        {
            csv.AppendCsv(row.RoomName).Append(',');
            csv.AppendCsv(row.PlayerName).Append(',');
            csv.Append(row.PuzzleNumber).Append(',');
            csv.Append(row.TimesEntered).Append(',');
            csv.Append(row.CorrectAnswers).Append(',');
            csv.Append(row.WrongAnswers);

            for (int i = 0; i < maxAnswerTries; i++)
            {
                csv.Append(',');
                if (i < row.AnswerSelections.Count)
                    csv.AppendCsv(row.AnswerSelections[i]);
            }

            csv.Append(',');
            csv.AppendLine(FormatMinutesAndSeconds(row.TimeTakenSeconds));
        }

        File.WriteAllText(path, csv.ToString(), Encoding.UTF8);
        Debug.Log($"Gameplay CSV saved to: {path}");
    }

    public override void OnLeftRoom()
    {
        WriteCsvIfNeeded();
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != GameplayLogEventCode || photonEvent.CustomData is not object[] data || data.Length < 7)
            return;

        string selectedAnswers = data.Length >= 8 ? data[7] as string : string.Empty;

        ApplyEvent(
            Convert.ToInt32(data[0], CultureInfo.InvariantCulture),
            data[1] as string,
            Convert.ToInt32(data[2], CultureInfo.InvariantCulture),
            data[3] as string,
            Convert.ToInt32(data[4], CultureInfo.InvariantCulture),
            Convert.ToInt32(data[5], CultureInfo.InvariantCulture) == 1,
            Convert.ToDouble(data[6], CultureInfo.InvariantCulture),
            selectedAnswers
        );
    }

    private void SendOrApplyEvent(int eventType, string roomName, int actorNumber, string playerName, int puzzleNumber, int answerWasCorrect, double elapsedSeconds, string selectedAnswers = null)
    {
        object[] payload =
        {
            eventType,
            roomName,
            actorNumber,
            playerName,
            puzzleNumber,
            answerWasCorrect,
            elapsedSeconds,
            selectedAnswers ?? string.Empty
        };

        if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient)
        {
            ApplyEvent(eventType, roomName, actorNumber, playerName, puzzleNumber, answerWasCorrect == 1, elapsedSeconds, selectedAnswers);
            return;
        }

        PhotonNetwork.RaiseEvent(
            GameplayLogEventCode,
            payload,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            SendOptions.SendReliable
        );
    }

    private void ApplyEvent(int eventType, string roomName, int actorNumber, string playerName, int puzzleNumber, bool answerWasCorrect, double elapsedSeconds, string selectedAnswers)
    {
        if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            return;

        var stats = GetOrCreateStats(roomName, actorNumber, playerName, puzzleNumber);

        if (eventType == EventTypeEnter)
        {
            stats.TimesEntered++;
            return;
        }

        if (eventType == EventTypeTimeSpent)
        {
            stats.TimeTakenSeconds += Math.Max(0d, elapsedSeconds);
            return;
        }

        if (answerWasCorrect)
            stats.CorrectAnswers++;
        else
            stats.WrongAnswers++;

        AddAnswerSelection(stats, selectedAnswers);
        stats.TimeTakenSeconds += Math.Max(0d, elapsedSeconds);
    }

    private static void AddAnswerSelection(PuzzlePlayerStats stats, string selectedAnswers)
    {
        if (stats == null || string.IsNullOrWhiteSpace(selectedAnswers))
            return;

        stats.AnswerSelections.Add(selectedAnswers.Trim());
    }

    private PuzzlePlayerStats GetOrCreateStats(string roomName, int actorNumber, string playerName, int puzzleNumber)
    {
        string key = $"{roomName}|{actorNumber}|{puzzleNumber}";
        if (statsByKey.TryGetValue(key, out var stats))
            return stats;

        stats = new PuzzlePlayerStats
        {
            RoomName = string.IsNullOrWhiteSpace(roomName) ? "Offline" : roomName,
            ActorNumber = actorNumber,
            PlayerName = string.IsNullOrWhiteSpace(playerName) ? $"Player {actorNumber}" : playerName,
            PuzzleNumber = puzzleNumber
        };

        statsByKey[key] = stats;
        return stats;
    }

    private double CaptureElapsedSeconds(int actorNumber, PuzzleStateManager.PuzzleStage stage, bool keepRunning)
    {
        string key = BuildAttemptKey(actorNumber, stage);
        double now = PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.realtimeSinceStartupAsDouble;

        if (!localAttemptStartTimes.TryGetValue(key, out double startedAt))
            return 0d;

        double elapsedSeconds = Math.Max(0d, now - startedAt);

        if (keepRunning)
            localAttemptStartTimes[key] = now;
        else
            localAttemptStartTimes.Remove(key);

        return elapsedSeconds;
    }

    private void FlushLocalActiveAttempts()
    {
        if (localAttemptStartTimes.Count == 0)
            return;

        double now = PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.realtimeSinceStartupAsDouble;
        var activeAttempts = new List<KeyValuePair<string, double>>(localAttemptStartTimes);

        foreach (var attempt in activeAttempts)
        {
            if (!TryParseAttemptKey(attempt.Key, out int actorNumber, out PuzzleStateManager.PuzzleStage stage))
                continue;

            double elapsedSeconds = Math.Max(0d, now - attempt.Value);
            localAttemptStartTimes[attempt.Key] = now;

            if (elapsedSeconds <= 0d)
                continue;

            SendOrApplyEvent(
                EventTypeTimeSpent,
                GetCurrentRoomName(),
                actorNumber,
                ResolvePlayerName(actorNumber),
                (int)stage,
                0,
                elapsedSeconds
            );
        }
    }

    private static string BuildAttemptKey(int actorNumber, PuzzleStateManager.PuzzleStage stage)
    {
        return $"{actorNumber}|{(int)stage}";
    }

    private static bool TryParseAttemptKey(string key, out int actorNumber, out PuzzleStateManager.PuzzleStage stage)
    {
        actorNumber = -1;
        stage = PuzzleStateManager.PuzzleStage.Puzzle0;

        if (string.IsNullOrWhiteSpace(key))
            return false;

        string[] parts = key.Split('|');
        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out actorNumber))
            return false;

        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int stageInt))
            return false;

        stage = (PuzzleStateManager.PuzzleStage)stageInt;
        return true;
    }

    private static string GetCurrentRoomName()
    {
        if (PhotonNetwork.CurrentRoom != null && !string.IsNullOrWhiteSpace(PhotonNetwork.CurrentRoom.Name))
            return PhotonNetwork.CurrentRoom.Name;

        string cachedRoomName = Network.GetCachedRoomName();
        return string.IsNullOrWhiteSpace(cachedRoomName) ? "Offline" : cachedRoomName;
    }

    private static string ResolvePlayerName(int actorNumber)
    {
        Player player = PhotonNetwork.CurrentRoom?.GetPlayer(actorNumber);
        if (!string.IsNullOrWhiteSpace(player?.NickName))
            return player.NickName;

        if (!string.IsNullOrWhiteSpace(PhotonNetwork.NickName))
            return PhotonNetwork.NickName;

        return actorNumber > 0 ? $"Player {actorNumber}" : "Offline Player";
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Offline";

        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return value.Trim();
    }

    private static string FormatMinutesAndSeconds(double totalSeconds)
    {
        int roundedSeconds = Mathf.Max(0, Mathf.RoundToInt((float)totalSeconds));
        int minutes = roundedSeconds / 60;
        int seconds = roundedSeconds % 60;

        return $"{minutes:00}:{seconds:00}";
    }
}

internal static class CsvStringBuilderExtensions
{
    public static StringBuilder AppendCsv(this StringBuilder builder, string value)
    {
        value ??= string.Empty;
        bool mustQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");

        if (!mustQuote)
            return builder.Append(value);

        return builder.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
    }
}
