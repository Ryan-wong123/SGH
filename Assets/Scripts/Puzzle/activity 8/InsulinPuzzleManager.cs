using UnityEngine; // Imports a namespace used by this script.
using System.Collections.Generic; // Imports a namespace used by this script.
using System.Collections; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.
using TMPro; // Imports a namespace used by this script.

public class InsulinPuzzleManager : MonoBehaviour, IPuzzle // Declares this type.
{ // Starts this code block.
    [SerializeField] private int required; // Exposes this private field in the Unity Inspector.

    [SerializeField] GameObject panel1; // Exposes this private field in the Unity Inspector.
    [SerializeField] GameObject panel2; // Exposes this private field in the Unity Inspector.

    public TMP_Dropdown syringeDropdown; // Declares a field, property, or member used by this script.
    public int correctSyringeIndex = 1;       // The correct option index in dropdown

    public Color correctColor = Color.green; // Declares a field, property, or member used by this script.
    public Color wrongColor = Color.red; // Declares a field, property, or member used by this script.
    public Color defaultColor = Color.white; // Declares a field, property, or member used by this script.
    private Coroutine part1AdvanceRoutine; // Declares a field, property, or member used by this script.

    public void SubmitSyringeAnswer() // Declares a method used by this script.
    { // Starts this code block.
        if (part1AdvanceRoutine != null) // Checks this condition before continuing.
            StopCoroutine(part1AdvanceRoutine); // Stops a Unity coroutine.

        part1AdvanceRoutine = StartCoroutine(CheckCorrectSyringe()); // Updates this value.
    } // Ends this code block.

    public IEnumerator CheckCorrectSyringe() // Declares a method used by this script.
    { // Starts this code block.
        // Get the user's selected option
        int selected = syringeDropdown.value; // Creates a local value used by the following logic.

        // Check correctness
        if (selected == correctSyringeIndex) // Checks this condition before continuing.
        { // Starts this code block.
            syringeDropdown.SetSyncedFeedbackColor(correctColor); // Runs this line of puzzle logic.
            // Optional delay before showing result (remove if not needed)
            yield return new WaitForSeconds(2f); // Pauses this coroutine until the yielded condition finishes.

            if (PuzzleStateManager.Instance != null) // Checks this condition before continuing.
                PuzzleStateManager.Instance.BroadcastInsulinPart2Transition(); // Runs this line of puzzle logic.
            else // Runs when the previous condition was false.
                GoToPart2(); // Runs this line of puzzle logic.
        } // Ends this code block.
           
        else // Runs when the previous condition was false.
            syringeDropdown.SetSyncedFeedbackColor(wrongColor); // Runs this line of puzzle logic.

        part1AdvanceRoutine = null; // Updates this value.
    } // Ends this code block.

    public void GoToPart2() // Declares a method used by this script.
    { // Starts this code block.
        if (panel1 != null) panel1.SetActive(false); // Checks this condition before continuing.
        if (panel2 != null) panel2.SetActive(true); // Checks this condition before continuing.
    } // Ends this code block.


    public bool AreAllPuzzle8CardsCorrect() // Declares a method used by this script.
    { // Starts this code block.
        

        int correct = 0; // Creates a local value used by the following logic.

        // Reset all card visuals
        foreach (DropSlot slot in DropSlot.allDropSlots) // Repeats this block for each item in a collection.
        { // Starts this code block.
            foreach (DraggableCard card in slot.GetCardsInSlot()) // Repeats this block for each item in a collection.
            { // Starts this code block.
                card.ResetSyncedFeedbackColor(); // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.

        // Evaluate placed cards only
        foreach (DropSlot slot in DropSlot.allDropSlots) // Repeats this block for each item in a collection.
        { // Starts this code block.
            foreach (DraggableCard card in slot.GetCardsInSlot()) // Repeats this block for each item in a collection.
            { // Starts this code block.
                bool isCorrect = slot.IsCardCorrect(card); // Creates a local value used by the following logic.

                card.SetSyncedFeedbackColor(isCorrect ? Color.green : Color.red); // Runs this line of puzzle logic.

                if (isCorrect) correct++; // Checks this condition before continuing.
            } // Ends this code block.
        } // Ends this code block.

        StartCoroutine(ResetIncorrectCardColorsAfterDelay(1f)); // Starts a Unity coroutine.

        Debug.Log($"[Puzzle 2] Correct: {correct} / Required: {required}"); // Writes a diagnostic message to the Unity console.

        return correct >= required; // Returns this result to the caller.
    } // Ends this code block.
    public bool IsSolved() // Declares a method used by this script.
    { // Starts this code block.
        return AreAllPuzzle8CardsCorrect(); // Returns this result to the caller.
    } // Ends this code block.
    private IEnumerator ResetIncorrectCardColorsAfterDelay(float delaySeconds) // Declares a method used by this script.
    { // Starts this code block.
        yield return new WaitForSeconds(delaySeconds); // Pauses this coroutine until the yielded condition finishes.

        foreach (Activity6DropSlot slot in Activity6DropSlot.allDropSlots) // Repeats this block for each item in a collection.
        { // Starts this code block.
            foreach (DraggableCard card in slot.GetCardsInSlot()) // Repeats this block for each item in a collection.
            { // Starts this code block.
                Image img = card.GetComponent<Image>(); // Runs this line of puzzle logic.
                if (img != null && img.color == Color.red) // Checks this condition before continuing.
                { // Starts this code block.
                    card.ResetSyncedFeedbackColor(); // Runs this line of puzzle logic.
                } // Ends this code block.
            } // Ends this code block.
        } // Ends this code block.
    } // Ends this code block.

    public void ResetPuzzle() // Declares a method used by this script.
    { // Starts this code block.
        Debug.Log("[InsulinPuzzle] Resetting Puzzle"); // Writes a diagnostic message to the Unity console.

        // -----------------------------
        // 1) Reset syringe dropdown
        // -----------------------------
        if (syringeDropdown != null) // Checks this condition before continuing.
        { // Starts this code block.
            syringeDropdown.value = 0;                   // Reset selection to first option
            syringeDropdown.RefreshShownValue();         // Update UI
            syringeDropdown.SetSyncedFeedbackColor(defaultColor); // Runs this line of puzzle logic.
        } // Ends this code block.

        // -----------------------------
        // 5) Reset panel visibility
        // -----------------------------
        if (panel1 != null) panel1.SetActive(true); // Checks this condition before continuing.
        if (panel2 != null) panel2.SetActive(false); // Checks this condition before continuing.

        Debug.Log("[InsulinPuzzle] RESET COMPLETE"); // Writes a diagnostic message to the Unity console.
    } // Ends this code block.

} // Ends this code block.
