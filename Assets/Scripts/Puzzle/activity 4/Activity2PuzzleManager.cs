using UnityEngine; // Imports a namespace used by this script.
using System.Collections; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.
using TMPro; // Imports a namespace used by this script.
using WebSocketSharp; // Imports a namespace used by this script.

public class Activity2PuzzleManager : MonoBehaviour,IPuzzle // Declares this type.
{ // Starts this code block.
    [SerializeField] private int required; // Exposes this private field in the Unity Inspector.
    [SerializeField] private TMP_InputField input1; // Exposes this private field in the Unity Inspector.
    [SerializeField] private TMP_InputField input2; // Exposes this private field in the Unity Inspector.
    [SerializeField] private GameObject panel1; // Exposes this private field in the Unity Inspector.
    [SerializeField] private GameObject panel2; // Exposes this private field in the Unity Inspector.

    public enum Activity2SubmitResult // Declares this type.
    { // Starts this code block.
        Incorrect, // Runs this line of puzzle logic.
        Advanced, // Runs this line of puzzle logic.
        Completed // Runs this line of puzzle logic.
    } // Ends this code block.

    public bool IsOnPart2 => panel2 != null && panel2.activeSelf; // Declares a field, property, or member used by this script.
    public int CurrentPart => IsOnPart2 ? 2 : 1; // Identifies the active Puzzle 4 part for analytics.
    public GameObject ActivePanel => CurrentPart == 2 ? panel2 : panel1; // Provides the active Puzzle 4 panel for answer snapshots.


    public bool AreAllPuzzle4CardsCorrect() // Declares a method used by this script.
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

        Debug.Log($"[Puzzle 4] Correct: {correct} / Required: {required}"); // Writes a diagnostic message to the Unity console.

        return correct >= required; // Returns this result to the caller.
    } // Ends this code block.

    public Activity2SubmitResult SubmitActivity2Stage() // Declares a method used by this script.
    { // Starts this code block.
        if (!IsOnPart2) // Checks this condition before continuing.
        { // Starts this code block.
            return AreAllPuzzle4CardsCorrect() // Returns this result to the caller.
                ? Activity2SubmitResult.Advanced // Runs this line of puzzle logic.
                : Activity2SubmitResult.Incorrect; // Runs this line of puzzle logic.
        } // Ends this code block.

        return CheckActivity2Part2Correct() // Returns this result to the caller.
            ? Activity2SubmitResult.Completed // Runs this line of puzzle logic.
            : Activity2SubmitResult.Incorrect; // Runs this line of puzzle logic.
    } // Ends this code block.

    public void GoToPart2() // Declares a method used by this script.
    { // Starts this code block.
        if (panel1 != null) panel1.SetActive(false); // Checks this condition before continuing.
        if (panel2 != null) panel2.SetActive(true); // Checks this condition before continuing.
    } // Ends this code block.

    public bool IsSolved() // Declares a method used by this script.
    { // Starts this code block.
        return SubmitActivity2Stage() == Activity2SubmitResult.Completed; // Returns this result to the caller.
    } // Ends this code block.

    public bool CheckActivity2Part2Correct() // Declares a method used by this script.
    { // Starts this code block.
        if (input1.text.IsNullOrEmpty() || input2.text.IsNullOrEmpty()) // Checks this condition before continuing.
        { // Starts this code block.
            return false; // Returns this result to the caller.
        } // Ends this code block.
        input1.SetSyncedFeedbackColor(input1.text == "15" ? Color.green : Color.red); // Updates this value.
        input2.SetSyncedFeedbackColor(input2.text == "4" ? Color.green : Color.red); // Updates this value.
        return input1.text == "15" && input2.text == "4"; // Returns this result to the caller.
  
    } // Ends this code block.

    public void ResetActivity2Part2() // Declares a method used by this script.
    { // Starts this code block.
        input1.ResetSyncedFeedbackColor(); // Runs this line of puzzle logic.
        input2.ResetSyncedFeedbackColor(); // Runs this line of puzzle logic.
        input1.text = ""; // Updates this value.
        input2.text = ""; // Updates this value.
    } // Ends this code block.

    private IEnumerator ResetIncorrectCardColorsAfterDelay(float delaySeconds) // Declares a method used by this script.
    { // Starts this code block.
        yield return new WaitForSeconds(delaySeconds); // Pauses this coroutine until the yielded condition finishes.

        foreach (Activity2DropSlot slot in Activity2DropSlot.allDropSlots) // Repeats this block for each item in a collection.
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
} // Ends this code block.
