using System; // Imports a namespace used by this script.
using System.Collections.Generic; // Imports a namespace used by this script.
using System.Globalization; // Imports a namespace used by this script.
using System.IO; // Imports a namespace used by this script.
using System.Text; // Imports a namespace used by this script.
using ExitGames.Client.Photon; // Imports a namespace used by this script.
using Photon.Pun; // Imports a namespace used by this script.
using Photon.Realtime; // Imports a namespace used by this script.
using UnityEngine; // Imports a namespace used by this script.

public class GameSessionCsvLogger : MonoBehaviourPunCallbacks, IOnEventCallback // Declares this type.
{ // Starts this code block.
    private const byte GameplayLogEventCode = 41; // Declares a field, property, or member used by this script.
    private const int EventTypeEnter = 0; // Declares a field, property, or member used by this script.
    private const int EventTypeAnswer = 1; // Declares a field, property, or member used by this script.
    private const int EventTypeTimeSpent = 2; // Declares a field, property, or member used by this script.

    public static GameSessionCsvLogger Instance { get; private set; } // Declares a field, property, or member used by this script.

    private readonly Dictionary<string, PuzzlePlayerStats> statsByKey = new(); // Declares a field, property, or member used by this script.
    private readonly Dictionary<string, double> localAttemptStartTimes = new(); // Declares a field, property, or member used by this script.
    private bool hasWrittenCsv; // Declares a field, property, or member used by this script.

    private sealed class PuzzlePlayerStats // Declares a field, property, or member used by this script.
    { // Starts this code block.
        public string RoomName; // Declares a field, property, or member used by this script.
        public int ActorNumber; // Declares a field, property, or member used by this script.
        public string PlayerName; // Declares a field, property, or member used by this script.
        public int PuzzleNumber; // Declares a field, property, or member used by this script.
        public int TimesEntered; // Declares a field, property, or member used by this script.
        public int CorrectAnswers; // Declares a field, property, or member used by this script.
        public int WrongAnswers; // Declares a field, property, or member used by this script.
        public double TimeTakenSeconds; // Declares a field, property, or member used by this script.
        public List<string> AnswerSelections = new(); // Declares a field, property, or member used by this script.
    } // Ends this code block.

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Adds an attribute that changes how Unity or C# treats the next declaration.
    private static void EnsureLoggerExists() // Declares a method used by this script.
    { // Starts this code block.
        if (Instance != null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        var existing = FindObjectOfType<GameSessionCsvLogger>(); // Creates a local value used by the following logic.
        if (existing != null) // Checks this condition before continuing.
        { // Starts this code block.
            Instance = existing; // Updates this value.
            return; // Returns this result to the caller.
        } // Ends this code block.

        var loggerObject = new GameObject(nameof(GameSessionCsvLogger)); // Creates a local value used by the following logic.
        loggerObject.AddComponent<GameSessionCsvLogger>(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void Awake() // Declares a method used by this script.
    { // Starts this code block.
        if (Instance != null && Instance != this) // Checks this condition before continuing.
        { // Starts this code block.
            Destroy(gameObject); // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        Instance = this; // Updates this value.
        DontDestroyOnLoad(gameObject); // Runs this line of puzzle logic.
    } // Ends this code block.

    public override void OnEnable() // Overrides a Unity or Photon callback method.
    { // Starts this code block.
        base.OnEnable(); // Runs this line of puzzle logic.
        PhotonNetwork.AddCallbackTarget(this); // Runs this line of puzzle logic.
    } // Ends this code block.

    public override void OnDisable() // Overrides a Unity or Photon callback method.
    { // Starts this code block.
        PhotonNetwork.RemoveCallbackTarget(this); // Runs this line of puzzle logic.
        base.OnDisable(); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void RecordPuzzleEntered(PuzzleStateManager.PuzzleStage stage, int actorNumber, string playerName) // Declares a method used by this script.
    { // Starts this code block.
        if (stage == PuzzleStateManager.PuzzleStage.Puzzle0) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        string roomName = GetCurrentRoomName(); // Creates a local value used by the following logic.
        double now = PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.realtimeSinceStartupAsDouble; // Runs this line of puzzle logic.
        string attemptKey = BuildAttemptKey(actorNumber, stage); // Creates a local value used by the following logic.

        if (!localAttemptStartTimes.ContainsKey(attemptKey)) // Checks this condition before continuing.
            localAttemptStartTimes[attemptKey] = now; // Updates this value.

        SendOrApplyEvent(EventTypeEnter, roomName, actorNumber, playerName, (int)stage, 0, 0d); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void RecordPuzzleReset(PuzzleStateManager.PuzzleStage stage) // Declares a method used by this script.
    { // Starts this code block.
        if (stage == PuzzleStateManager.PuzzleStage.Puzzle0) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        int actorNumber = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1; // Creates a local value used by the following logic.
        string playerName = ResolvePlayerName(actorNumber); // Creates a local value used by the following logic.
        double elapsedSeconds = CaptureElapsedSeconds(actorNumber, stage, keepRunning: true); // Runs this line of puzzle logic.

        if (elapsedSeconds <= 0d) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        SendOrApplyEvent(EventTypeTimeSpent, GetCurrentRoomName(), actorNumber, playerName, (int)stage, 0, elapsedSeconds); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void RecordPuzzleAnswer(PuzzleStateManager.PuzzleStage stage, bool isCorrect, string selectedAnswers = null) // Declares a method used by this script.
    { // Starts this code block.
        if (stage == PuzzleStateManager.PuzzleStage.Puzzle0) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        int actorNumber = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1; // Creates a local value used by the following logic.
        string playerName = ResolvePlayerName(actorNumber); // Creates a local value used by the following logic.
        double elapsedSeconds = CaptureElapsedSeconds(actorNumber, stage, keepRunning: !isCorrect); // Runs this line of puzzle logic.

        SendOrApplyEvent(EventTypeAnswer, GetCurrentRoomName(), actorNumber, playerName, (int)stage, isCorrect ? 1 : 0, elapsedSeconds, selectedAnswers); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void WriteCsvIfNeeded() // Declares a method used by this script.
    { // Starts this code block.
        if (hasWrittenCsv) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        FlushLocalActiveAttempts(); // Runs this line of puzzle logic.

        if (statsByKey.Count == 0) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        hasWrittenCsv = true; // Updates this value.

        string directory = Path.Combine(Application.persistentDataPath, "GameplayLogs"); // Creates a local value used by the following logic.
        Directory.CreateDirectory(directory); // Runs this line of puzzle logic.

        string roomName = SanitizeFileName(GetCurrentRoomName()); // Creates a local value used by the following logic.
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture); // Creates a local value used by the following logic.
        string path = Path.Combine(directory, $"gameplay_{roomName}_{timestamp}.csv"); // Creates a local value used by the following logic.

        var rows = new List<PuzzlePlayerStats>(statsByKey.Values); // Creates a local value used by the following logic.
        rows.Sort((a, b) => // Runs this line of puzzle logic.
        { // Starts this code block.
            int puzzleCompare = a.PuzzleNumber.CompareTo(b.PuzzleNumber); // Creates a local value used by the following logic.
            if (puzzleCompare != 0) return puzzleCompare; // Checks this condition before continuing.

            int playerCompare = string.Compare(a.PlayerName, b.PlayerName, StringComparison.OrdinalIgnoreCase); // Creates a local value used by the following logic.
            return playerCompare != 0 ? playerCompare : a.ActorNumber.CompareTo(b.ActorNumber); // Returns this result to the caller.
        }); // Runs this line of puzzle logic.

        int maxAnswerTries = 0; // Creates a local value used by the following logic.
        foreach (var row in rows) // Repeats this block for each item in a collection.
            maxAnswerTries = Math.Max(maxAnswerTries, row.AnswerSelections.Count); // Updates this value.

        var csv = new StringBuilder(); // Creates a local value used by the following logic.
        csv.Append("room_name,player_name,puzzle_number,times_entered,correct_answers,wrong_answers"); // Runs this line of puzzle logic.
        for (int i = 1; i <= maxAnswerTries; i++) // Repeats this block for a counted loop.
            csv.Append($",answer_try_{i}"); // Runs this line of puzzle logic.
        csv.AppendLine(",time_taken"); // Runs this line of puzzle logic.

        foreach (var row in rows) // Repeats this block for each item in a collection.
        { // Starts this code block.
            csv.AppendCsv(row.RoomName).Append(','); // Runs this line of puzzle logic.
            csv.AppendCsv(row.PlayerName).Append(','); // Runs this line of puzzle logic.
            csv.Append(row.PuzzleNumber).Append(','); // Runs this line of puzzle logic.
            csv.Append(row.TimesEntered).Append(','); // Runs this line of puzzle logic.
            csv.Append(row.CorrectAnswers).Append(','); // Runs this line of puzzle logic.
            csv.Append(row.WrongAnswers); // Runs this line of puzzle logic.

            for (int i = 0; i < maxAnswerTries; i++) // Repeats this block for a counted loop.
            { // Starts this code block.
                csv.Append(','); // Runs this line of puzzle logic.
                if (i < row.AnswerSelections.Count) // Checks this condition before continuing.
                    csv.AppendCsv(row.AnswerSelections[i]); // Runs this line of puzzle logic.
            } // Ends this code block.

            csv.Append(','); // Runs this line of puzzle logic.
            csv.AppendLine(FormatMinutesAndSeconds(row.TimeTakenSeconds)); // Runs this line of puzzle logic.
        } // Ends this code block.

        File.WriteAllText(path, csv.ToString(), Encoding.UTF8); // Runs this line of puzzle logic.
        Debug.Log($"Gameplay CSV saved to: {path}"); // Writes a diagnostic message to the Unity console.
    } // Ends this code block.

    public override void OnLeftRoom() // Overrides a Unity or Photon callback method.
    { // Starts this code block.
        WriteCsvIfNeeded(); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void OnEvent(EventData photonEvent) // Declares a method used by this script.
    { // Starts this code block.
        if (photonEvent.Code != GameplayLogEventCode || photonEvent.CustomData is not object[] data || data.Length < 7) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        string selectedAnswers = data.Length >= 8 ? data[7] as string : string.Empty; // Creates a local value used by the following logic.

        ApplyEvent( // Runs this line of puzzle logic.
            Convert.ToInt32(data[0], CultureInfo.InvariantCulture), // Runs this line of puzzle logic.
            data[1] as string, // Runs this line of puzzle logic.
            Convert.ToInt32(data[2], CultureInfo.InvariantCulture), // Runs this line of puzzle logic.
            data[3] as string, // Runs this line of puzzle logic.
            Convert.ToInt32(data[4], CultureInfo.InvariantCulture), // Runs this line of puzzle logic.
            Convert.ToInt32(data[5], CultureInfo.InvariantCulture) == 1, // Runs this line of puzzle logic.
            Convert.ToDouble(data[6], CultureInfo.InvariantCulture), // Runs this line of puzzle logic.
            selectedAnswers // Runs this line of puzzle logic.
        ); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void SendOrApplyEvent(int eventType, string roomName, int actorNumber, string playerName, int puzzleNumber, int answerWasCorrect, double elapsedSeconds, string selectedAnswers = null) // Declares a method used by this script.
    { // Starts this code block.
        object[] payload = // Runs this line of puzzle logic.
        { // Starts this code block.
            eventType, // Runs this line of puzzle logic.
            roomName, // Runs this line of puzzle logic.
            actorNumber, // Runs this line of puzzle logic.
            playerName, // Runs this line of puzzle logic.
            puzzleNumber, // Runs this line of puzzle logic.
            answerWasCorrect, // Runs this line of puzzle logic.
            elapsedSeconds, // Runs this line of puzzle logic.
            selectedAnswers ?? string.Empty // Runs this line of puzzle logic.
        }; // Runs this line of puzzle logic.

        if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) // Checks this condition before continuing.
        { // Starts this code block.
            ApplyEvent(eventType, roomName, actorNumber, playerName, puzzleNumber, answerWasCorrect == 1, elapsedSeconds, selectedAnswers); // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        PhotonNetwork.RaiseEvent( // Runs this line of puzzle logic.
            GameplayLogEventCode, // Runs this line of puzzle logic.
            payload, // Runs this line of puzzle logic.
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }, // Runs this line of puzzle logic.
            SendOptions.SendReliable // Runs this line of puzzle logic.
        ); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void ApplyEvent(int eventType, string roomName, int actorNumber, string playerName, int puzzleNumber, bool answerWasCorrect, double elapsedSeconds, string selectedAnswers) // Declares a method used by this script.
    { // Starts this code block.
        if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        var stats = GetOrCreateStats(roomName, actorNumber, playerName, puzzleNumber); // Creates a local value used by the following logic.

        if (eventType == EventTypeEnter) // Checks this condition before continuing.
        { // Starts this code block.
            stats.TimesEntered++; // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        if (eventType == EventTypeTimeSpent) // Checks this condition before continuing.
        { // Starts this code block.
            stats.TimeTakenSeconds += Math.Max(0d, elapsedSeconds); // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        if (answerWasCorrect) // Checks this condition before continuing.
            stats.CorrectAnswers++; // Runs this line of puzzle logic.
        else // Runs when the previous condition was false.
            stats.WrongAnswers++; // Runs this line of puzzle logic.

        AddAnswerSelection(stats, selectedAnswers); // Runs this line of puzzle logic.
        stats.TimeTakenSeconds += Math.Max(0d, elapsedSeconds); // Runs this line of puzzle logic.
    } // Ends this code block.

    private static void AddAnswerSelection(PuzzlePlayerStats stats, string selectedAnswers) // Declares a method used by this script.
    { // Starts this code block.
        if (stats == null || string.IsNullOrWhiteSpace(selectedAnswers)) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        stats.AnswerSelections.Add(selectedAnswers.Trim()); // Runs this line of puzzle logic.
    } // Ends this code block.

    private PuzzlePlayerStats GetOrCreateStats(string roomName, int actorNumber, string playerName, int puzzleNumber) // Declares a method used by this script.
    { // Starts this code block.
        string key = $"{roomName}|{actorNumber}|{puzzleNumber}"; // Creates a local value used by the following logic.
        if (statsByKey.TryGetValue(key, out var stats)) // Checks this condition before continuing.
            return stats; // Returns this result to the caller.

        stats = new PuzzlePlayerStats // Updates this value.
        { // Starts this code block.
            RoomName = string.IsNullOrWhiteSpace(roomName) ? "Offline" : roomName, // Updates this value.
            ActorNumber = actorNumber, // Updates this value.
            PlayerName = string.IsNullOrWhiteSpace(playerName) ? $"Player {actorNumber}" : playerName, // Updates this value.
            PuzzleNumber = puzzleNumber // Updates this value.
        }; // Runs this line of puzzle logic.

        statsByKey[key] = stats; // Updates this value.
        return stats; // Returns this result to the caller.
    } // Ends this code block.

    private double CaptureElapsedSeconds(int actorNumber, PuzzleStateManager.PuzzleStage stage, bool keepRunning) // Declares a method used by this script.
    { // Starts this code block.
        string key = BuildAttemptKey(actorNumber, stage); // Creates a local value used by the following logic.
        double now = PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.realtimeSinceStartupAsDouble; // Runs this line of puzzle logic.

        if (!localAttemptStartTimes.TryGetValue(key, out double startedAt)) // Checks this condition before continuing.
            return 0d; // Returns this result to the caller.

        double elapsedSeconds = Math.Max(0d, now - startedAt); // Runs this line of puzzle logic.

        if (keepRunning) // Checks this condition before continuing.
            localAttemptStartTimes[key] = now; // Updates this value.
        else // Runs when the previous condition was false.
            localAttemptStartTimes.Remove(key); // Runs this line of puzzle logic.

        return elapsedSeconds; // Returns this result to the caller.
    } // Ends this code block.

    private void FlushLocalActiveAttempts() // Declares a method used by this script.
    { // Starts this code block.
        if (localAttemptStartTimes.Count == 0) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        double now = PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.realtimeSinceStartupAsDouble; // Runs this line of puzzle logic.
        var activeAttempts = new List<KeyValuePair<string, double>>(localAttemptStartTimes); // Creates a local value used by the following logic.

        foreach (var attempt in activeAttempts) // Repeats this block for each item in a collection.
        { // Starts this code block.
            if (!TryParseAttemptKey(attempt.Key, out int actorNumber, out PuzzleStateManager.PuzzleStage stage)) // Checks this condition before continuing.
                continue; // Skips to the next loop iteration.

            double elapsedSeconds = Math.Max(0d, now - attempt.Value); // Runs this line of puzzle logic.
            localAttemptStartTimes[attempt.Key] = now; // Updates this value.

            if (elapsedSeconds <= 0d) // Checks this condition before continuing.
                continue; // Skips to the next loop iteration.

            SendOrApplyEvent( // Runs this line of puzzle logic.
                EventTypeTimeSpent, // Runs this line of puzzle logic.
                GetCurrentRoomName(), // Runs this line of puzzle logic.
                actorNumber, // Runs this line of puzzle logic.
                ResolvePlayerName(actorNumber), // Runs this line of puzzle logic.
                (int)stage, // Runs this line of puzzle logic.
                0, // Runs this line of puzzle logic.
                elapsedSeconds // Runs this line of puzzle logic.
            ); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.

    private static string BuildAttemptKey(int actorNumber, PuzzleStateManager.PuzzleStage stage) // Declares a method used by this script.
    { // Starts this code block.
        return $"{actorNumber}|{(int)stage}"; // Returns this result to the caller.
    } // Ends this code block.

    private static bool TryParseAttemptKey(string key, out int actorNumber, out PuzzleStateManager.PuzzleStage stage) // Declares a method used by this script.
    { // Starts this code block.
        actorNumber = -1; // Updates this value.
        stage = PuzzleStateManager.PuzzleStage.Puzzle0; // Updates this value.

        if (string.IsNullOrWhiteSpace(key)) // Checks this condition before continuing.
            return false; // Returns this result to the caller.

        string[] parts = key.Split('|'); // Runs this line of puzzle logic.
        if (parts.Length != 2) // Checks this condition before continuing.
            return false; // Returns this result to the caller.

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out actorNumber)) // Checks this condition before continuing.
            return false; // Returns this result to the caller.

        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int stageInt)) // Checks this condition before continuing.
            return false; // Returns this result to the caller.

        stage = (PuzzleStateManager.PuzzleStage)stageInt; // Updates this value.
        return true; // Returns this result to the caller.
    } // Ends this code block.

    private static string GetCurrentRoomName() // Declares a method used by this script.
    { // Starts this code block.
        if (PhotonNetwork.CurrentRoom != null && !string.IsNullOrWhiteSpace(PhotonNetwork.CurrentRoom.Name)) // Checks this condition before continuing.
            return PhotonNetwork.CurrentRoom.Name; // Returns this result to the caller.

        string cachedRoomName = Network.GetCachedRoomName(); // Creates a local value used by the following logic.
        return string.IsNullOrWhiteSpace(cachedRoomName) ? "Offline" : cachedRoomName; // Returns this result to the caller.
    } // Ends this code block.

    private static string ResolvePlayerName(int actorNumber) // Declares a method used by this script.
    { // Starts this code block.
        Player player = PhotonNetwork.CurrentRoom?.GetPlayer(actorNumber); // Runs this line of puzzle logic.
        if (!string.IsNullOrWhiteSpace(player?.NickName)) // Checks this condition before continuing.
            return player.NickName; // Returns this result to the caller.

        if (!string.IsNullOrWhiteSpace(PhotonNetwork.NickName)) // Checks this condition before continuing.
            return PhotonNetwork.NickName; // Returns this result to the caller.

        return actorNumber > 0 ? $"Player {actorNumber}" : "Offline Player"; // Returns this result to the caller.
    } // Ends this code block.

    private static string SanitizeFileName(string value) // Declares a method used by this script.
    { // Starts this code block.
        if (string.IsNullOrWhiteSpace(value)) // Checks this condition before continuing.
            return "Offline"; // Returns this result to the caller.

        foreach (char invalid in Path.GetInvalidFileNameChars()) // Repeats this block for each item in a collection.
            value = value.Replace(invalid, '_'); // Updates this value.

        return value.Trim(); // Returns this result to the caller.
    } // Ends this code block.

    private static string FormatMinutesAndSeconds(double totalSeconds) // Declares a method used by this script.
    { // Starts this code block.
        int roundedSeconds = Mathf.Max(0, Mathf.RoundToInt((float)totalSeconds)); // Creates a local value used by the following logic.
        int minutes = roundedSeconds / 60; // Creates a local value used by the following logic.
        int seconds = roundedSeconds % 60; // Creates a local value used by the following logic.

        return $"{minutes:00}:{seconds:00}"; // Returns this result to the caller.
    } // Ends this code block.
} // Ends this code block.

internal static class CsvStringBuilderExtensions // Declares this type.
{ // Starts this code block.
    public static StringBuilder AppendCsv(this StringBuilder builder, string value) // Declares a method used by this script.
    { // Starts this code block.
        value ??= string.Empty; // Runs this line of puzzle logic.
        bool mustQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"); // Creates a local value used by the following logic.

        if (!mustQuote) // Checks this condition before continuing.
            return builder.Append(value); // Returns this result to the caller.

        return builder.Append('"').Append(value.Replace("\"", "\"\"")).Append('"'); // Returns this result to the caller.
    } // Ends this code block.
} // Ends this code block.
