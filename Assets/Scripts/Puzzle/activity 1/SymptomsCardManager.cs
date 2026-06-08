using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.
using System.Collections.Generic; // Imports a namespace used by this script.
using System.Collections; // Imports a namespace used by this script.

public class SymptomsCardManager : MonoBehaviour, IPuzzle // Declares this type.
{ // Starts this code block.
    [SerializeField] private int required; // Exposes this private field in the Unity Inspector.


    public bool AreAllPuzzle1CardsCorrect() // Declares a method used by this script.
    { // Starts this code block.
        int totalSlots = DropSlot.allDropSlots.Count; // Creates a local value used by the following logic.
        int filled = 0; // Creates a local value used by the following logic.
        int correct = 0; // Creates a local value used by the following logic.

        // Evaluate each slot
        foreach (DropSlot slot in DropSlot.allDropSlots) // Repeats this block for each item in a collection.
        { // Starts this code block.
            var cards = slot.GetCardsInSlot(); // Creates a local value used by the following logic.
            if (cards.Count > 0) // Checks this condition before continuing.
            { // Starts this code block.
                filled++; // Runs this line of puzzle logic.

                DraggableCard card = cards[0]; // Runs this line of puzzle logic.
                bool isCorrect = slot.IsCardCorrect(card); // Creates a local value used by the following logic.

                card.SetSyncedFeedbackColor(isCorrect ? Color.green : Color.red); // Runs this line of puzzle logic.

                if (isCorrect) correct++; // Checks this condition before continuing.
            } // Ends this code block.
        } // Ends this code block.
        StartCoroutine(ResetIncorrectCardColorsAfterDelay(1f)); // Starts a Unity coroutine.
        Debug.Log($"[Puzzle 1] Filled: {filled}/{totalSlots}, Correct: {correct}/{totalSlots}"); // Writes a diagnostic message to the Unity console.
        return filled == totalSlots && correct == totalSlots; // Returns this result to the caller.
    } // Ends this code block.

    public bool IsSolved() // Declares a method used by this script.
    { // Starts this code block.
        if (AreAllPuzzle1CardsCorrect()) // Checks this condition before continuing.
        { // Starts this code block.
            return true; // Returns this result to the caller.
        } // Ends this code block.
 
        return false; // Returns this result to the caller.
    } // Ends this code block.
    private IEnumerator ResetIncorrectCardColorsAfterDelay(float delaySeconds) // Declares a method used by this script.
    { // Starts this code block.
        yield return new WaitForSeconds(delaySeconds); // Pauses this coroutine until the yielded condition finishes.

        foreach (DropSlot slot in DropSlot.allDropSlots) // Repeats this block for each item in a collection.
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
