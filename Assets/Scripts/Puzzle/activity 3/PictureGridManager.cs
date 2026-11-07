using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.
using System.Collections.Generic; // Imports a namespace used by this script.
using TMPro; // Imports a namespace used by this script.

public class PictureGridManager : MonoBehaviour, IPuzzle // Declares this type.
{ // Starts this code block.
    [Header("Grid Settings")] // Adds a labeled section in the Unity Inspector.
    public List<Button> pictureButtons; // Assign all 20 buttons
    public List<int> correctIndexes; // e.g. [0, 4, 7, 9, 15, 19]

    private HashSet<int> selectedIndexes = new HashSet<int>(); // Declares a field, property, or member used by this script.
    private readonly HashSet<int> observedSelectedIndexes = new HashSet<int>(); // Reuses a snapshot when checking synced button state.
    private readonly List<PhotonSyncedUIButtonColor> syncedButtons = new List<PhotonSyncedUIButtonColor>(); // Declares a field, property, or member used by this script.

    public Color defaultColor = Color.white; // Declares a field, property, or member used by this script.
    public Color selectedColor = Color.yellow; // Declares a field, property, or member used by this script.
    public Color correctColor = Color.green; // Declares a field, property, or member used by this script.
    public Color wrongColor = Color.red; // Declares a field, property, or member used by this script.

    [SerializeField] GameObject submitButton; // Exposes this private field in the Unity Inspector.
    [SerializeField] private TMP_Text selectedCountText; // Exposes this private field in the Unity Inspector.

    public GameObject SubmitButton => submitButton; // Declares a field, property, or member used by this script.

    public void Start() // Declares a method used by this script.
    { // Starts this code block.
        for (int i = 0; i < pictureButtons.Count; i++) // Repeats this block for a counted loop.
        { // Starts this code block.
            var syncedButton = pictureButtons[i].GetComponent<PhotonSyncedUIButtonColor>(); // Creates a local value used by the following logic.
            if (syncedButton != null) // Checks this condition before continuing.
            { // Starts this code block.
                syncedButton.StateChanged += HandleSyncedButtonStateChanged; // Runs this line of puzzle logic.
                syncedButtons.Add(syncedButton); // Runs this line of puzzle logic.
                continue; // Skips to the next loop iteration.
            } // Ends this code block.

            int index = i; // Needed to avoid closure issues
            pictureButtons[i].onClick.AddListener(() => OnPictureClick(index)); // Updates this value.
        } // Ends this code block.

        RebuildSelectedIndexesFromButtons(); // Runs this line of puzzle logic.
        RefreshSubmitButtonVisibility(); // Runs this line of puzzle logic.
        UpdateSelectedCountText(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void OnDestroy() // Declares a method used by this script.
    { // Starts this code block.
        foreach (var syncedButton in syncedButtons) // Repeats this block for each item in a collection.
        { // Starts this code block.
            if (syncedButton != null) // Checks this condition before continuing.
                syncedButton.StateChanged -= HandleSyncedButtonStateChanged; // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.

    private void LateUpdate()
    {
        // A missed state-change callback must not leave the count or Submit button behind the visible cards.
        if (!RebuildSelectedIndexesFromButtons()) return;

        RefreshSubmitButtonVisibility();
        UpdateSelectedCountText();
    }

    void OnPictureClick(int index) // Declares a method used by this script.
    { // Starts this code block.
        if (selectedIndexes.Contains(index)) // Checks this condition before continuing.
        { // Starts this code block.
            selectedIndexes.Remove(index); // Runs this line of puzzle logic.
            SetButtonColor(index, defaultColor); // Runs this line of puzzle logic.
        } // Ends this code block.
        else // Runs when the previous condition was false.
        { // Starts this code block.
            selectedIndexes.Add(index); // Runs this line of puzzle logic.
            SetButtonColor(index, selectedColor); // Runs this line of puzzle logic.
        } // Ends this code block.

        UpdateSelectedCountText(); // Runs this line of puzzle logic.

        if (selectedIndexes.Count == correctIndexes.Count) // Checks this condition before continuing.
            Debug.Log($"{correctIndexes.Count} buttons selected. Ready to check answers!"); // Writes a diagnostic message to the Unity console.

        RefreshSubmitButtonVisibility(); // Runs this line of puzzle logic.
    } // Ends this code block.

    void UpdateSelectedCountText() // Declares a method used by this script.
    { // Starts this code block.
        if (selectedCountText != null) // Checks this condition before continuing.
        { // Starts this code block.
            selectedCountText.text = $"Selected: {selectedIndexes.Count}/{correctIndexes.Count}"; // Updates this value.
        } // Ends this code block.
    } // Ends this code block.

    private void HandleSyncedButtonStateChanged(PhotonSyncedUIButtonColor _) // Declares a method used by this script.
    { // Starts this code block.
        RebuildSelectedIndexesFromButtons(); // Runs this line of puzzle logic.
        RefreshSubmitButtonVisibility(); // Runs this line of puzzle logic.
        UpdateSelectedCountText(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private bool RebuildSelectedIndexesFromButtons() // Declares a method used by this script.
    { // Starts this code block.
        observedSelectedIndexes.Clear(); // Collects the current synced state without disturbing the displayed count yet.

        for (int i = 0; i < pictureButtons.Count; i++) // Repeats this block for a counted loop.
        { // Starts this code block.
            var syncedButton = pictureButtons[i].GetComponent<PhotonSyncedUIButtonColor>(); // Creates a local value used by the following logic.
            if (syncedButton != null) // Checks this condition before continuing.
            { // Starts this code block.
                if (syncedButton.IsSelected) // Checks this condition before continuing.
                    observedSelectedIndexes.Add(i); // Runs this line of puzzle logic.

                continue; // Skips to the next loop iteration.
            } // Ends this code block.

            var image = pictureButtons[i].GetComponent<Image>(); // Creates a local value used by the following logic.
            if (image != null && (image.color == selectedColor || image.color == correctColor || image.color == wrongColor)) // Checks this condition before continuing.
                observedSelectedIndexes.Add(i); // Runs this line of puzzle logic.
        } // Ends this code block.

        if (selectedIndexes.SetEquals(observedSelectedIndexes)) return false;

        selectedIndexes.Clear();
        selectedIndexes.UnionWith(observedSelectedIndexes);
        return true;
    } // Ends this code block.

    // ✅ FULL FIX IS HERE:
    // Instead of directly setting Image.color (local only),
    // we translate the color intention into a synced button-state call.
    void SetButtonColor(int index, Color color) // Declares a method used by this script.
    { // Starts this code block.
        if (index < 0 || index >= pictureButtons.Count) return; // Checks this condition before continuing.

        var btnGO = pictureButtons[index].gameObject; // Creates a local value used by the following logic.
        var synced = btnGO.GetComponent<PhotonSyncedUIButtonColor>(); // Creates a local value used by the following logic.

        // If this button doesn't have the sync component, fallback to local color (keeps your logic usable)
        if (synced == null) // Checks this condition before continuing.
        { // Starts this code block.
            var img = btnGO.GetComponent<Image>(); // Creates a local value used by the following logic.
            if (img != null) img.color = color; // Checks this condition before continuing.
            return; // Returns this result to the caller.
        } // Ends this code block.

        // Map your color usage to synced states:
        // - defaultColor => clear result (back to normal)
        // - selectedColor => selected state (yellow)
        // - correctColor => correct state (green)
        // - wrongColor => wrong state (red)
        if (color == defaultColor) // Checks this condition before continuing.
        { // Starts this code block.
            // back to normal (not selected, not correct/wrong)
            synced.ResetStateAndSync(); // Runs this line of puzzle logic.
        } // Ends this code block.
        else if (color == selectedColor) // Runs when the previous condition was false.
        { // Starts this code block.
            // selected highlight (yellow)
            synced.MarkSelectedAndSync(); // Runs this line of puzzle logic.
        } // Ends this code block.
        else if (color == correctColor) // Runs when the previous condition was false.
        { // Starts this code block.
            synced.MarkCorrectAndSync(); // Runs this line of puzzle logic.
        } // Ends this code block.
        else if (color == wrongColor) // Runs when the previous condition was false.
        { // Starts this code block.
            synced.MarkWrongAndSync(); // Runs this line of puzzle logic.
        } // Ends this code block.
        else // Runs when the previous condition was false.
        { // Starts this code block.
            // If you pass a custom color, just treat it as selected
            // (or you can add SetCustomColorAndSync to your button script if needed)
            synced.MarkSelectedAndSync(); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.

    public bool IsWrongPictureButton(GameObject pictureButtonObject)
    {
        if (pictureButtonObject == null)
            return false;

        for (int index = 0; index < pictureButtons.Count; index++)
        {
            Button pictureButton = pictureButtons[index];
            if (pictureButton != null && pictureButton.gameObject == pictureButtonObject)
                return correctIndexes == null || !correctIndexes.Contains(index);
        }

        return false;
    }

    public bool CheckAnswers() // Declares a method used by this script.
    { // Starts this code block.
        RebuildSelectedIndexesFromButtons(); // Runs this line of puzzle logic.

        bool allCorrect = true; // Creates a local value used by the following logic.

        foreach (int index in selectedIndexes) // Repeats this block for each item in a collection.
        { // Starts this code block.
            if (correctIndexes.Contains(index)) // Checks this condition before continuing.
            { // Starts this code block.
                SetButtonColor(index, correctColor); // Runs this line of puzzle logic.
            } // Ends this code block.
            else // Runs when the previous condition was false.
            { // Starts this code block.
                SetButtonColor(index, wrongColor); // Runs this line of puzzle logic.
                allCorrect = false; // Found a wrong answer
            } // Ends this code block.
        } // Ends this code block.

        // Also ensure that the user didn't *miss* selecting correct answers
        if (selectedIndexes.Count != correctIndexes.Count) // Checks this condition before continuing.
        { // Starts this code block.
            allCorrect = false; // Updates this value.
        } // Ends this code block.

        return allCorrect; // Returns this result to the caller.
    } // Ends this code block.

    public bool IsSolved() // Declares a method used by this script.
    { // Starts this code block.
        return CheckAnswers(); // Returns this result to the caller.
    } // Ends this code block.

    public void ResetGrid() // Declares a method used by this script.
    { // Starts this code block.
        selectedIndexes.Clear(); // Runs this line of puzzle logic.
        for (int i = 0; i < pictureButtons.Count; i++) // Repeats this block for a counted loop.
        { // Starts this code block.
            pictureButtons[i].interactable = true; // Updates this value.
            SetButtonColor(i, defaultColor); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.

    public void ResetPuzzle() // Declares a method used by this script.
    { // Starts this code block.
        selectedIndexes.Clear(); // Runs this line of puzzle logic.

        for (int i = 0; i < pictureButtons.Count; i++) // Repeats this block for a counted loop.
        { // Starts this code block.
            ResetSelection(i); // Handles reset logic per button
        } // Ends this code block.

        RefreshSubmitButtonVisibility(); // Runs this line of puzzle logic.
        UpdateSelectedCountText(); // Runs this line of puzzle logic.

        Debug.Log("Picture Grid Puzzle reset."); // Writes a diagnostic message to the Unity console.
    } // Ends this code block.

    public void RefreshSubmitButtonVisibility() // Declares a method used by this script.
    { // Starts this code block.
        if (submitButton == null) return; // Checks this condition before continuing.

        bool hasRequiredSelectionCount = selectedIndexes.Count == correctIndexes.Count; // Creates a local value used by the following logic.
        bool canLocalPlayerSubmit = PuzzleStateManager.Instance == null || // Creates a local value used by the following logic.
                                    PuzzleStateManager.Instance.CanLocalPlayerSubmitCurrentAttempt(); // Runs this line of puzzle logic.

        submitButton.SetActive(canLocalPlayerSubmit && hasRequiredSelectionCount); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void ResetSelection(int index) // Declares a method used by this script.
    { // Starts this code block.
        if (index < 0 || index >= pictureButtons.Count) return; // Checks this condition before continuing.

        Button btn = pictureButtons[index]; // Runs this line of puzzle logic.
        btn.interactable = true; // Updates this value.

        // Use your existing reset flow, but route through SetButtonColor so it syncs
        SetButtonColor(index, defaultColor); // Runs this line of puzzle logic.
    } // Ends this code block.
} // Ends this code block.
