using UnityEngine; // Imports a namespace used by this script.
using TMPro; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.
using System.Collections.Generic; // Imports a namespace used by this script.
using System; // Imports a namespace used by this script.
using System.Globalization; // Imports a namespace used by this script.

public class DiaryPuzzleManager : MonoBehaviour, IPuzzle // Declares this type.
{ // Starts this code block.
    [System.Serializable] // Adds an attribute that changes how Unity or C# treats the next declaration.
    public struct AnswerItem // Declares this type.
    { // Starts this code block.
        public TMP_InputField field;   // The input to check & color
        public int stage;              // 1-based stage index (1,2,3,…)
        public bool caseSensitive;     // Toggle per field
        public string[] accepted;      // One or more accepted answers
    } // Ends this code block.

    [SerializeField] private List<AnswerItem> answers; // Exposes this private field in the Unity Inspector.
    [SerializeField] private GameObject[] stagePanels; // Exposes this private field in the Unity Inspector.

    private int currentStage = 1; // Declares a field, property, or member used by this script.

    [System.Serializable] // Adds an attribute that changes how Unity or C# treats the next declaration.
    public class DateTextEntry // Declares this type.
    { // Starts this code block.
        public TMP_Text dateText; // Declares a field, property, or member used by this script.
        public int daysBack; // Declares a field, property, or member used by this script.
    } // Ends this code block.

    [SerializeField] private List<DateTextEntry> dateEntries; // Exposes this private field in the Unity Inspector.

    public enum DiarySubmitResult // Declares this type.
    { // Starts this code block.
        Incorrect, // Runs this line of puzzle logic.
        Advanced,   // correct, moved to next diary page
        Completed   // correct, final page complete
    } // Ends this code block.

    public int CurrentStage => currentStage; // Declares a field, property, or member used by this script.
    public int MaxStage => stagePanels != null ? stagePanels.Length : 0; // Declares a field, property, or member used by this script.
    public GameObject ActivePanel => currentStage >= 1 && stagePanels != null && currentStage <= stagePanels.Length ? stagePanels[currentStage - 1] : null; // Provides the active diary page for answer snapshots.

    private void OnEnable() // Declares a method used by this script.
    { // Starts this code block.
        ApplyStageVisibility(); // Runs this line of puzzle logic.

        DateTime today = DateTime.Today; // Runs this line of puzzle logic.

        foreach (var entry in dateEntries) // Repeats this block for each item in a collection.
        { // Starts this code block.
            if (entry.dateText != null) // Checks this condition before continuing.
            { // Starts this code block.
                DateTime targetDate = today.AddDays(-entry.daysBack); // Runs this line of puzzle logic.
                entry.dateText.text = targetDate.ToString("dd MMM yyyy"); // Updates this value.
            } // Ends this code block.
        } // Ends this code block.
    } // Ends this code block.

    private static bool ValuesMatch(string user, string expected, bool caseSensitive, float floatEps = 1e-4f) // Declares a method used by this script.
    { // Starts this code block.
        if (user == null) user = string.Empty; // Checks this condition before continuing.
        if (expected == null) expected = string.Empty; // Checks this condition before continuing.

        string u = user.Trim(); // Creates a local value used by the following logic.
        string e = expected.Trim(); // Creates a local value used by the following logic.

        if (!caseSensitive) // Checks this condition before continuing.
        { // Starts this code block.
            u = u.ToLowerInvariant(); // Updates this value.
            e = e.ToLowerInvariant(); // Updates this value.
        } // Ends this code block.

        if (u == e) return true; // Checks this condition before continuing.

        if (float.TryParse(u, NumberStyles.Float, CultureInfo.InvariantCulture, out var uf) && // Checks this condition before continuing.
            float.TryParse(e, NumberStyles.Float, CultureInfo.InvariantCulture, out var ef)) // Runs this line of puzzle logic.
        { // Starts this code block.
            return Math.Abs(uf - ef) <= floatEps; // Returns this result to the caller.
        } // Ends this code block.

        return false; // Returns this result to the caller.
    } // Ends this code block.

    private bool CheckStage(int stage) // Declares a method used by this script.
    { // Starts this code block.
        bool allCorrect = true; // Creates a local value used by the following logic.

        foreach (var a in answers) // Repeats this block for each item in a collection.
        { // Starts this code block.
            if (a.stage != stage || a.field == null) continue; // Checks this condition before continuing.

            string val = a.field.text ?? string.Empty; // Creates a local value used by the following logic.

            bool ok = false; // Creates a local value used by the following logic.
            if (a.accepted != null && a.accepted.Length > 0) // Checks this condition before continuing.
            { // Starts this code block.
                foreach (var acc in a.accepted) // Repeats this block for each item in a collection.
                { // Starts this code block.
                    if (ValuesMatch(val, acc, a.caseSensitive)) // Checks this condition before continuing.
                    { // Starts this code block.
                        ok = true; // Updates this value.
                        break; // Exits the current loop or switch.
                    } // Ends this code block.
                } // Ends this code block.
            } // Ends this code block.

            a.field.SetSyncedFeedbackColor(ok ? Color.green : Color.red); // Runs this line of puzzle logic.
            if (!ok) allCorrect = false; // Checks this condition before continuing.
        } // Ends this code block.

        return allCorrect; // Returns this result to the caller.
    } // Ends this code block.

    // ✅ New: no panel toggling here, just returns the result
    public DiarySubmitResult SubmitDiaryStage() // Declares a method used by this script.
    { // Starts this code block.
        int maxStage = MaxStage; // Creates a local value used by the following logic.
        if (currentStage < 1 || currentStage > maxStage) // Checks this condition before continuing.
        { // Starts this code block.
            Debug.LogWarning($"Invalid currentStage={currentStage}"); // Writes a warning message to the Unity console.
            return DiarySubmitResult.Incorrect; // Returns this result to the caller.
        } // Ends this code block.

        if (!CheckStage(currentStage)) // Checks this condition before continuing.
        { // Starts this code block.
            Debug.Log($"Diary {currentStage} incorrect. Please fix it."); // Writes a diagnostic message to the Unity console.
            return DiarySubmitResult.Incorrect; // Returns this result to the caller.
        } // Ends this code block.

        if (currentStage < maxStage) // Checks this condition before continuing.
        { // Starts this code block.
            Debug.Log($"Diary {currentStage} correct. Proceed to Diary {currentStage + 1}."); // Writes a diagnostic message to the Unity console.
            return DiarySubmitResult.Advanced; // Returns this result to the caller.
        } // Ends this code block.

        Debug.Log($"Diary {currentStage} correct. All stages complete!"); // Writes a diagnostic message to the Unity console.
        return DiarySubmitResult.Completed; // Returns this result to the caller.
    } // Ends this code block.

    public void GoToNextStage() // Declares a method used by this script.
    { // Starts this code block.
        GoToStage(currentStage + 1); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void GoToStage(int newStage) // Declares a method used by this script.
    { // Starts this code block.
        int maxStage = MaxStage; // Creates a local value used by the following logic.
        if (maxStage <= 0) return; // Checks this condition before continuing.

        newStage = Mathf.Clamp(newStage, 1, maxStage); // Updates this value.
        currentStage = newStage; // Updates this value.
        ApplyStageVisibility(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void ApplyStageVisibility() // Declares a method used by this script.
    { // Starts this code block.
        if (stagePanels == null || stagePanels.Length == 0) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        for (int i = 0; i < stagePanels.Length; i++) // Repeats this block for a counted loop.
            if (stagePanels[i] != null) // Checks this condition before continuing.
                stagePanels[i].SetActive(i == (currentStage - 1)); // Updates this value.
    } // Ends this code block.

    public void ResetPuzzle() // Declares a method used by this script.
    { // Starts this code block.
        foreach (var a in answers) // Repeats this block for each item in a collection.
        { // Starts this code block.
            if (a.field == null) continue; // Checks this condition before continuing.
            a.field.text = string.Empty; // Updates this value.
            a.field.ResetSyncedFeedbackColor(); // Runs this line of puzzle logic.
        } // Ends this code block.

        currentStage = 1; // Updates this value.
        ApplyStageVisibility(); // Runs this line of puzzle logic.
        Debug.Log("Diary puzzle reset."); // Writes a diagnostic message to the Unity console.
    } // Ends this code block.

    // IPuzzle
    public bool IsSolved() // Declares a method used by this script.
    { // Starts this code block.
        // Keep compatibility: only returns true when fully completed
        return SubmitDiaryStage() == DiarySubmitResult.Completed; // Returns this result to the caller.
    } // Ends this code block.
} // Ends this code block.
