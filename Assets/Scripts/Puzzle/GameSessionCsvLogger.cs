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
    private const int EventTypePlayerInput = 3; // A player changed an answer control.

    public static GameSessionCsvLogger Instance { get; private set; } // Declares a field, property, or member used by this script.

    private readonly Dictionary<string, PuzzlePlayerStats> statsByKey = new(); // Individual-player input and time rows.
    private readonly Dictionary<string, PuzzlePlayerStats> teamStatsByKey = new(); // Team submission rows, keyed by the player who submitted.
    private readonly Dictionary<string, double> localAttemptStartTimes = new(); // Declares a field, property, or member used by this script.
    private DateTime sessionPlayedAt; // Stores when this game session started for CSV export.
    private bool hasWrittenCsv; // Declares a field, property, or member used by this script.

    private sealed class PuzzlePlayerStats // Declares a field, property, or member used by this script.
    { // Starts this code block.
        public string RoomName; // Declares a field, property, or member used by this script.
        public int ActorNumber; // Declares a field, property, or member used by this script.
        public string PlayerName; // Declares a field, property, or member used by this script.
        public int PuzzleNumber; // Declares a field, property, or member used by this script.
        public int PuzzlePart; // Identifies a diary page or multi-part section.
        public int TimesEntered; // Declares a field, property, or member used by this script.
        public int CorrectAnswers; // Declares a field, property, or member used by this script.
        public int WrongAnswers; // Declares a field, property, or member used by this script.
        public double TimeTakenSeconds; // Declares a field, property, or member used by this script.
        public List<string> WrongAnswerSelections = new(); // Declares a field, property, or member used by this script.
        public Dictionary<string, string> LatestInputsByControl = new(); // The latest value entered or selected by this player for each control.
        public Dictionary<string, string> SubmittedInputsByControl = new(); // The player's final input values at the moment the team submits.
        public bool HasSubmittedInputSnapshot; // Avoids exporting changes made after submission as part of the prior attempt.
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
        sessionPlayedAt = DateTime.Now; // Captures the date and time the game session began.
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
        if (stage == PuzzleStateManager.PuzzleStage.Puzzle0 || stage == PuzzleStateManager.PuzzleStage.Puzzle2) // Checks this condition before continuing.
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
        if (stage == PuzzleStateManager.PuzzleStage.Puzzle0 || stage == PuzzleStateManager.PuzzleStage.Puzzle2) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        int actorNumber = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1; // Creates a local value used by the following logic.
        string playerName = ResolvePlayerName(actorNumber); // Creates a local value used by the following logic.
        double elapsedSeconds = CaptureElapsedSeconds(actorNumber, stage, keepRunning: true); // Runs this line of puzzle logic.

        if (elapsedSeconds <= 0d) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        SendOrApplyEvent(EventTypeTimeSpent, GetCurrentRoomName(), actorNumber, playerName, (int)stage, 0, elapsedSeconds); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void RecordPuzzleAnswer(PuzzleStateManager.PuzzleStage stage, bool isCorrect, string selectedAnswers = null, int answerActorNumber = -1, string answerPlayerName = null) // Declares a method used by this script.
    { // Starts this code block.
        if (stage == PuzzleStateManager.PuzzleStage.Puzzle0 || stage == PuzzleStateManager.PuzzleStage.Puzzle2) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        int actorNumber = answerActorNumber > 0 ? answerActorNumber : (PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1); // Records the player whose input is being evaluated.
        string playerName = string.IsNullOrWhiteSpace(answerPlayerName) ? ResolvePlayerName(actorNumber) : answerPlayerName; // Keeps the answer associated with its author.
        bool keepPuzzleTimerRunning = !isCorrect || IsMultiPartPuzzle((int)stage); // Multipart puzzles continue timing between their parts.
        double elapsedSeconds = CaptureElapsedSeconds(actorNumber, stage, keepRunning: keepPuzzleTimerRunning); // Runs this line of puzzle logic.

        SendOrApplyEvent(EventTypeAnswer, GetCurrentRoomName(), actorNumber, playerName, (int)stage, isCorrect ? 1 : 0, elapsedSeconds, selectedAnswers); // Runs this line of puzzle logic.
    } // Ends this code block.

    // Records an individual interaction without treating it as a team submission.
    public void RecordLocalPlayerInput(string controlId, string value)
    {
        var stateManager = PuzzleStateManager.Instance;
        if (stateManager == null || string.IsNullOrWhiteSpace(controlId))
            return;

        PuzzleStateManager.PuzzleStage stage = stateManager.CurrentStage;
        if (stage == PuzzleStateManager.PuzzleStage.Puzzle0 || stage == PuzzleStateManager.PuzzleStage.Puzzle2)
            return;

        int actorNumber = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;
        string playerName = ResolvePlayerName(actorNumber);
        string input = $"__PUZZLE_PART={stateManager.GetCurrentPuzzleAnalyticsPart()}__|{controlId}={value ?? string.Empty}";
        SendOrApplyEvent(EventTypePlayerInput, GetCurrentRoomName(), actorNumber, playerName, (int)stage, 0, 0d, input);
    }

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

        var individualRows = new List<PuzzlePlayerStats>(statsByKey.Values);
        var teamRows = new List<PuzzlePlayerStats>(teamStatsByKey.Values);
        individualRows.Sort((a, b) => // Runs this line of puzzle logic.
        { // Starts this code block.
            int puzzleCompare = a.PuzzleNumber.CompareTo(b.PuzzleNumber); // Creates a local value used by the following logic.
            if (puzzleCompare != 0) return puzzleCompare; // Checks this condition before continuing.

            int partCompare = a.PuzzlePart.CompareTo(b.PuzzlePart);
            if (partCompare != 0) return partCompare;

            int playerCompare = string.Compare(a.PlayerName, b.PlayerName, StringComparison.OrdinalIgnoreCase); // Creates a local value used by the following logic.
            return playerCompare != 0 ? playerCompare : a.ActorNumber.CompareTo(b.ActorNumber); // Returns this result to the caller.
        }); // Runs this line of puzzle logic.

        teamRows.Sort((a, b) =>
        {
            int puzzleCompare = a.PuzzleNumber.CompareTo(b.PuzzleNumber);
            return puzzleCompare != 0 ? puzzleCompare : a.PuzzlePart.CompareTo(b.PuzzlePart);
        });

        int maxWrongAnswers = 0; // Creates a local value used by the following logic.
        foreach (var row in teamRows) // Repeats this block for each item in a collection.
            maxWrongAnswers = Math.Max(maxWrongAnswers, GetCompactedWrongAnswers(row).Count); // Updates this value.
        double totalTeamTimeTakenSeconds = 0d;
        foreach (var row in teamRows)
            totalTeamTimeTakenSeconds += Math.Max(0d, row.TimeTakenSeconds);

        int maxIndividualInputs = 0;
        foreach (var row in individualRows)
            maxIndividualInputs = Math.Max(maxIndividualInputs, GetSortedSubmittedInputs(row).Count);

        var csv = new StringBuilder(); // Creates a local value used by the following logic.
        string playedAt = sessionPlayedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture); // Formats the session date and time for analytics.
        csv.AppendLine("INDIVIDUAL_PLAYER_INPUTS");
        csv.Append("date_time_played,room_name,player_name,puzzle_number,puzzle_name");
        for (int i = 1; i <= maxIndividualInputs; i++)
            csv.Append($",selected_or_typed_answer_{i}");
        csv.AppendLine();
        foreach (var row in individualRows)
        {
            List<string> latestInputs = GetSortedSubmittedInputs(row);
            csv.AppendCsv(playedAt).Append(',');
            csv.AppendCsv(row.RoomName).Append(',');
            csv.AppendCsv(row.PlayerName).Append(',');
            csv.Append(row.PuzzleNumber).Append(',');
            csv.AppendCsv(GetPuzzleName(row.PuzzleNumber, row.PuzzlePart));
            for (int i = 0; i < maxIndividualInputs; i++)
            {
                csv.Append(',');
                if (i < latestInputs.Count)
                    csv.AppendCsv(latestInputs[i]);
            }
            csv.AppendLine();
        }

        csv.AppendLine();
        csv.AppendLine("TEAM_SUBMISSIONS");
        csv.Append("date_time_played,room_name,submitted_by,puzzle_number,puzzle_name,total_tries,number_of_times_answer_is_correct,number_of_times_answer_is_wrong");
        for (int i = 1; i <= maxWrongAnswers; i++) // Repeats this block for a counted loop.
            csv.Append($",wrong_answer_{i}"); // Runs this line of puzzle logic.
        csv.AppendLine(",time_taken,total_time_taken_across_all_puzzles"); // Runs this line of puzzle logic.

        for (int teamRowIndex = 0; teamRowIndex < teamRows.Count; teamRowIndex++)
        { // Starts this code block.
            var row = teamRows[teamRowIndex];
            List<string> compactedWrongAnswers = GetCompactedWrongAnswers(row);
            csv.AppendCsv(playedAt).Append(','); // Writes the session date and time at the start of every row.
            csv.AppendCsv(row.RoomName).Append(','); // Runs this line of puzzle logic.
            csv.AppendCsv(row.PlayerName).Append(','); // Runs this line of puzzle logic.
            csv.Append(row.PuzzleNumber).Append(','); // Runs this line of puzzle logic.
            csv.AppendCsv(GetPuzzleName(row.PuzzleNumber, row.PuzzlePart)).Append(','); // Writes the descriptive puzzle name, including the part suffix when applicable.
            csv.Append(row.CorrectAnswers + row.WrongAnswers).Append(','); // Total tries are all correct and wrong answer attempts.
            csv.Append(row.CorrectAnswers).Append(','); // Runs this line of puzzle logic.
            csv.Append(row.WrongAnswers); // Runs this line of puzzle logic.

            for (int i = 0; i < maxWrongAnswers; i++) // Repeats this block for a counted loop.
            { // Starts this code block.
                csv.Append(','); // Runs this line of puzzle logic.
                if (i < compactedWrongAnswers.Count) // Checks this condition before continuing.
                    csv.AppendCsv(compactedWrongAnswers[i]); // Runs this line of puzzle logic.
            } // Ends this code block.

            csv.Append(','); // Runs this line of puzzle logic.
            csv.Append(FormatMinutesAndSeconds(row.TimeTakenSeconds)).Append(',');
            if (teamRowIndex == 0)
                csv.Append(FormatMinutesAndSeconds(totalTeamTimeTakenSeconds));
            csv.AppendLine();
        } // Ends this code block.

        File.WriteAllText(path, csv.ToString(), Encoding.UTF8); // Runs this line of puzzle logic.
        Debug.Log($"Gameplay CSV saved to: {path}"); // Writes a diagnostic message to the Unity console.
    } // Ends this code block.

    private static List<string> GetCompactedWrongAnswers(PuzzlePlayerStats row)
    {
        var compacted = new List<string>();
        if (row == null || row.WrongAnswerSelections == null)
            return compacted;

        foreach (string answer in row.WrongAnswerSelections)
        {
            if (!string.IsNullOrWhiteSpace(answer))
                compacted.Add(answer.Trim());
        }

        return compacted;
    }

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
        if (puzzleNumber == (int)PuzzleStateManager.PuzzleStage.Puzzle2) // Puzzle 2 is intentionally excluded from analytics exports.
            return; // Skips all Puzzle 2 logging events.

        if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        int puzzlePart = 0;
        const string partPrefix = "__PUZZLE_PART=";
        if (!string.IsNullOrWhiteSpace(selectedAnswers) && selectedAnswers.StartsWith(partPrefix, StringComparison.Ordinal))
        {
            int separator = selectedAnswers.IndexOf("__|", partPrefix.Length, StringComparison.Ordinal);
            if (separator > partPrefix.Length && int.TryParse(selectedAnswers.Substring(partPrefix.Length, separator - partPrefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedPart))
            {
                puzzlePart = parsedPart;
                selectedAnswers = selectedAnswers.Substring(separator + 3);
            }
        }

        if (puzzleNumber == (int)PuzzleStateManager.PuzzleStage.Puzzle9) // Puzzle 9 is a single-part puzzle.
            puzzlePart = 0;

        if ((puzzleNumber == (int)PuzzleStateManager.PuzzleStage.Puzzle4 || puzzleNumber == (int)PuzzleStateManager.PuzzleStage.Puzzle6 || puzzleNumber == (int)PuzzleStateManager.PuzzleStage.Puzzle10 || puzzleNumber == (int)PuzzleStateManager.PuzzleStage.Puzzle11) && puzzlePart == 0) // Ignore unassigned events for multi-part puzzles.
            return;

        if (eventType == EventTypeEnter && (puzzleNumber == (int)PuzzleStateManager.PuzzleStage.Puzzle4 || puzzleNumber == (int)PuzzleStateManager.PuzzleStage.Puzzle6 || puzzleNumber == (int)PuzzleStateManager.PuzzleStage.Puzzle10 || puzzleNumber == (int)PuzzleStateManager.PuzzleStage.Puzzle11) && puzzlePart == 0)
            return; // Diary rows are created only for actual pages.

        bool isTeamSubmission = eventType == EventTypeAnswer;
        var stats = GetOrCreateStats(isTeamSubmission ? teamStatsByKey : statsByKey, roomName, actorNumber, playerName, puzzleNumber, puzzlePart); // Creates a local value used by the following logic.

        if (eventType == EventTypePlayerInput)
        {
            StoreLatestInput(stats, selectedAnswers);
            return;
        }

        if (eventType == EventTypeAnswer && puzzlePart > 0)
            EnsureParticipantRowsForPuzzle(roomName, puzzleNumber, puzzlePart); // Creates rows for all players on this diary page.

        if (eventType == EventTypeAnswer)
            FreezeIndividualInputsForSubmission(roomName, puzzleNumber, puzzlePart);

        if (eventType == EventTypeEnter) // Checks this condition before continuing.
        { // Starts this code block.
            EnsureParticipantRowsForPuzzle(roomName, puzzleNumber, puzzlePart); // Creates blank rows for players who do not submit an answer.
            stats.TimesEntered++; // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        if (eventType == EventTypeTimeSpent) // Checks this condition before continuing.
        { // Starts this code block.
            stats.TimeTakenSeconds += Math.Max(0d, elapsedSeconds); // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        if (puzzleNumber == (int)PuzzleStateManager.PuzzleStage.Puzzle9 && stats.CorrectAnswers > 0) // Puzzle 9 records only the first successful answer; later hint-panel actions are ignored.
            return;

        if (answerWasCorrect) // Checks this condition before continuing.
            stats.CorrectAnswers++; // Runs this line of puzzle logic.
        else // Runs when the previous condition was false.
            stats.WrongAnswers++; // Runs this line of puzzle logic.

        if (!answerWasCorrect)
            AddAnswerSelection(stats, puzzleNumber, selectedAnswers); // Stores what was answered incorrectly.
        stats.TimeTakenSeconds += Math.Max(0d, elapsedSeconds); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void EnsureParticipantRowsForPuzzle(string roomName, int puzzleNumber, int puzzlePart = 0)
    {
        if (PhotonNetwork.CurrentRoom == null)
            return;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player == null)
                continue;

            GetOrCreateStats(roomName, player.ActorNumber, player.NickName, puzzleNumber, puzzlePart);
        }
    }

    private static void AddAnswerSelection(PuzzlePlayerStats stats, int puzzleNumber, string selectedAnswers)
    {
        if (stats == null || string.IsNullOrWhiteSpace(selectedAnswers))
            return;

        string normalized = puzzleNumber == 1
            ? NormalizePuzzleOneAnswerSnapshot(selectedAnswers)
            : puzzleNumber == 11 && stats.PuzzlePart == 1
                ? NormalizeValueOnlyAnswerSnapshot(selectedAnswers)
            : puzzleNumber == 10
                ? (stats.PuzzlePart == 1 ? NormalizeValueOnlyAnswerSnapshot(selectedAnswers) : NormalizePuzzleTenCardSnapshot(selectedAnswers))
                : puzzleNumber == 4 && stats.PuzzlePart == 1
                    ? NormalizePuzzleTenCardSnapshot(selectedAnswers)
                : puzzleNumber == 4
                    ? selectedAnswers.Trim()
                : puzzleNumber == 6 && (stats.PuzzlePart == 2 || stats.PuzzlePart == 3)
                    ? selectedAnswers.Trim()
                : puzzleNumber == 7
                    ? selectedAnswers.Trim()
                : IsValueOnlyPuzzle(puzzleNumber)
                    ? NormalizeValueOnlyAnswerSnapshot(selectedAnswers)
                    : selectedAnswers.Trim();

        if (!string.IsNullOrWhiteSpace(normalized))
            stats.WrongAnswerSelections.Add(normalized);
    }

    private static bool IsValueOnlyPuzzle(int puzzleNumber)
    {
        return puzzleNumber == 3 || puzzleNumber == 4 || puzzleNumber == 6 || puzzleNumber == 7 || puzzleNumber == 8;
    }

    private static string NormalizePuzzleTenCardSnapshot(string selectedAnswers)
    {
        var slots = new List<string>();
        string[] entries = selectedAnswers.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string entry in entries)
        {
            string slot = entry.Trim();
            int equalsIndex = slot.IndexOf('=');
            if (equalsIndex < 0)
            {
                if (!string.IsNullOrWhiteSpace(slot))
                    slots.Add(slot);
                continue;
            }

            string value = slot.Substring(equalsIndex + 1).Trim();
            if (value.StartsWith("[", StringComparison.Ordinal) && value.EndsWith("]", StringComparison.Ordinal))
                value = value.Substring(1, value.Length - 2).Trim();

            string slotName = slot.Substring(0, equalsIndex).Trim();
            if (!string.IsNullOrWhiteSpace(slotName) && !string.IsNullOrWhiteSpace(value))
                slots.Add($"{slotName}={value}");
        }

        return slots.Count > 0 ? string.Join(", ", slots) : selectedAnswers.Trim();
    }

    private static string NormalizeValueOnlyAnswerSnapshot(string selectedAnswers)
    {
        var values = new List<string>();
        string[] entries = selectedAnswers.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string entry in entries)
        {
            string value = entry.Trim();
            int equalsIndex = value.IndexOf('=');
            if (equalsIndex >= 0)
                value = value.Substring(equalsIndex + 1).Trim();

            if (value.StartsWith("[", StringComparison.Ordinal) && value.EndsWith("]", StringComparison.Ordinal))
                value = value.Substring(1, value.Length - 2).Trim();

            if (!string.IsNullOrWhiteSpace(value))
                values.Add(value);
        }

        return values.Count > 0 ? string.Join(", ", values) : selectedAnswers.Trim();
    }

    private static string NormalizePuzzleOneAnswerSnapshot(string selectedAnswers)
    {
        var answers = new List<string>();
        string[] entries = selectedAnswers.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string entry in entries)
        {
            int valueStart = entry.IndexOf("=[", StringComparison.Ordinal);
            if (valueStart < 0 || !entry.TrimEnd().EndsWith("]", StringComparison.Ordinal))
                continue;

            string answer = entry.Substring(valueStart + 2).Trim();
            answer = answer.TrimEnd(']').Trim();
            if (!string.IsNullOrWhiteSpace(answer))
                answers.Add(answer);
        }

        return answers.Count > 0 ? string.Join(", ", answers) : selectedAnswers.Trim();
    }

    private PuzzlePlayerStats GetOrCreateStats(Dictionary<string, PuzzlePlayerStats> statsCollection, string roomName, int actorNumber, string playerName, int puzzleNumber, int puzzlePart = 0) // Declares a method used by this script.
    { // Starts this code block.
        string key = $"{roomName}|{actorNumber}|{puzzleNumber}|{puzzlePart}"; // Creates a local value used by the following logic.
        if (statsCollection.TryGetValue(key, out var stats)) // Checks this condition before continuing.
            return stats; // Returns this result to the caller.

        stats = new PuzzlePlayerStats // Updates this value.
        { // Starts this code block.
            RoomName = string.IsNullOrWhiteSpace(roomName) ? "Offline" : roomName, // Updates this value.
            ActorNumber = actorNumber, // Updates this value.
            PlayerName = string.IsNullOrWhiteSpace(playerName) ? $"Player {actorNumber}" : playerName, // Updates this value.
            PuzzleNumber = puzzleNumber, // Updates this value.
            PuzzlePart = puzzlePart // Stores the multi-part section.
        }; // Runs this line of puzzle logic.

        statsCollection[key] = stats; // Updates this value.
        return stats; // Returns this result to the caller.
    } // Ends this code block.

    private PuzzlePlayerStats GetOrCreateStats(string roomName, int actorNumber, string playerName, int puzzleNumber, int puzzlePart = 0)
    {
        return GetOrCreateStats(statsByKey, roomName, actorNumber, playerName, puzzleNumber, puzzlePart);
    }

    private static void StoreLatestInput(PuzzlePlayerStats stats, string input)
    {
        if (stats == null || string.IsNullOrWhiteSpace(input))
            return;

        int equalsIndex = input.IndexOf('=');
        if (equalsIndex <= 0)
            return;

        string controlId = input.Substring(0, equalsIndex).Trim();
        string value = input.Substring(equalsIndex + 1).Trim();
        if (!string.IsNullOrWhiteSpace(controlId))
            stats.LatestInputsByControl[controlId] = value;
    }

    private static List<string> GetSortedSubmittedInputs(PuzzlePlayerStats stats)
    {
        if (stats?.SubmittedInputsByControl == null || !stats.HasSubmittedInputSnapshot || stats.SubmittedInputsByControl.Count == 0)
            return new List<string>();

        var entries = new List<string>();
        foreach (var pair in stats.SubmittedInputsByControl)
        {
            if (string.Equals(pair.Value, "deselected", StringComparison.OrdinalIgnoreCase))
                continue;

            entries.Add(string.Equals(pair.Value, "selected", StringComparison.OrdinalIgnoreCase)
                ? pair.Key
                : $"{pair.Key} = {pair.Value}");
        }
        entries.Sort(StringComparer.OrdinalIgnoreCase);
        return entries;
    }

    private void FreezeIndividualInputsForSubmission(string roomName, int puzzleNumber, int puzzlePart)
    {
        foreach (var stats in statsByKey.Values)
        {
            if (!string.Equals(stats.RoomName, string.IsNullOrWhiteSpace(roomName) ? "Offline" : roomName, StringComparison.Ordinal) ||
                stats.PuzzleNumber != puzzleNumber || stats.PuzzlePart != puzzlePart)
                continue;

            stats.SubmittedInputsByControl.Clear();
            foreach (var input in stats.LatestInputsByControl)
                stats.SubmittedInputsByControl[input.Key] = input.Value;
            stats.HasSubmittedInputSnapshot = true;
        }
    }

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

    private static string GetPuzzleName(int puzzleNumber, int puzzlePart)
    {
        string name = GetPuzzleBaseName(puzzleNumber);
        return puzzlePart > 0 && IsMultiPartPuzzle(puzzleNumber) ? $"{name}_{puzzlePart}" : name;
    }

    private static bool IsMultiPartPuzzle(int puzzleNumber)
    {
        return puzzleNumber == 4 || puzzleNumber == 6 || puzzleNumber == 10 || puzzleNumber == 11;
    }

    private static string GetPuzzleBaseName(int puzzleNumber) // Maps the exported puzzle number to its game puzzle name.
    { // Starts this code block.
        switch (puzzleNumber) // Selects the configured name for the puzzle stage.
        { // Starts this code block.
            case 1: return "Decode the hypoglycemia symptoms"; // Names puzzle 1.
            case 2: return "Align the palette"; // Names puzzle 2.
            case 3: return "Fuel up to break free from the low-sugar trap"; // Names puzzle 3.
            case 4: return "Crack the survival code against hypoglycemia"; // Names puzzle 4.
            case 5: return "Find the hidden drug-drug and drug-disease interactions that trigger hypoglycemia"; // Names puzzle 5.
            case 6: return "Glucose Diary"; // Names puzzle 6.
            case 7: return "With insulin as the highest risk, decode the risk level of other agents."; // Names puzzle 7.
            case 8: return "Crack the code to the Insulatard dose"; // Names puzzle 8.
            case 9: return "Decode the insulin"; // Names puzzle 9.
            case 10: return "Insulatard vial: align the right cards in order"; // Names puzzle 10.
            case 11: return "Uncover the hidden lapses that sealed the patient’s destiny"; // Names puzzle 11.
            default: return $"Puzzle {puzzleNumber}"; // Keeps unexpected stages readable in the export.
        } // Ends this code block.
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

        return $"'{minutes:00}:{seconds:00}"; // Prefixes the duration as text so spreadsheet apps preserve MM:SS.
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
