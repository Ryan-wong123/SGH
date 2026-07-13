using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.
using System.Collections.Generic; // Imports a namespace used by this script.
using System.Collections; // Imports a namespace used by this script.

public class DeathLapsesManager : MonoBehaviour, IPuzzle // Declares this type.
{ // Starts this code block.
    [SerializeField] private int requiredAnswer1; // Exposes this private field in the Unity Inspector.
    [SerializeField] private int requiredAnswer2; // Exposes this private field in the Unity Inspector.
    [SerializeField] private GameObject answer2Cards; // Exposes this private field in the Unity Inspector.
    [SerializeField] private GameObject answer2Slots; // Exposes this private field in the Unity Inspector.

    private bool stage1Completed = false; // Declares a field, property, or member used by this script.

    public int CurrentStage => stage1Completed ? 2 : 1; // Identifies which Death Lapses part is active for analytics.

    public enum DeathLapsesSubmitResult // Declares this type.
    { // Starts this code block.
        Incorrect, // Runs this line of puzzle logic.
        Advanced, // Runs this line of puzzle logic.
        Completed // Runs this line of puzzle logic.
    } // Ends this code block.

    public DeathLapsesSubmitResult SubmitDeathLapsesStage() // Declares a method used by this script.
    { // Starts this code block.
        if (!stage1Completed) // Checks this condition before continuing.
        { // Starts this code block.
            if (AreStage1CardsCorrect()) // Checks this condition before continuing.
            { // Starts this code block.
                Debug.Log("✅ Stage 1 completed. Submit again to check Stage 2."); // Writes a diagnostic message to the Unity console.
                return DeathLapsesSubmitResult.Advanced; // Returns this result to the caller.
            } // Ends this code block.
            Debug.Log("❌ Stage 1 incorrect."); // Writes a diagnostic message to the Unity console.
            return DeathLapsesSubmitResult.Incorrect; // Returns this result to the caller.
        } // Ends this code block.

        Debug.Log("checking stage 2"); // Writes a diagnostic message to the Unity console.
        return AreStage2CardsCorrect() // Returns this result to the caller.
            ? DeathLapsesSubmitResult.Completed // Runs this line of puzzle logic.
            : DeathLapsesSubmitResult.Incorrect; // Runs this line of puzzle logic.
    } // Ends this code block.

    public bool IsSolved() => SubmitDeathLapsesStage() == DeathLapsesSubmitResult.Completed; // Declares a method used by this script.

    public void GoToStage2() // Declares a method used by this script.
    { // Starts this code block.
        stage1Completed = true; // Updates this value.
        answer2Cards?.SetActive(true); // Runs this line of puzzle logic.
        answer2Slots?.SetActive(true); // Runs this line of puzzle logic.
    } // Ends this code block.

    private bool AreStage1CardsCorrect() // Declares a method used by this script.
    { // Starts this code block.
        int correct = 0; // Creates a local value used by the following logic.

        // ✅ Reset colors first
        foreach (DropSlot baseSlot in DropSlot.allDropSlots) // Repeats this block for each item in a collection.
        { // Starts this code block.
            var slot = baseSlot as Activity9DropSlot; // Creates a local value used by the following logic.
            if (slot == null) continue;                    // skip other puzzles' slots
            if (!slot.CompareTag("Stage1Slot")) continue; // Checks this condition before continuing.

            foreach (var baseCard in slot.GetCardsInSlot()) // Repeats this block for each item in a collection.
            { // Starts this code block.
                var card = baseCard as DraggableCard; // Creates a local value used by the following logic.
                if (card == null) continue;                // skip non-Activity9 cards

                card.ResetSyncedFeedbackColor(); // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.

        // ✅ Assign stage1Answer FIRST then check correctness
        foreach (DropSlot baseSlot in DropSlot.allDropSlots) // Repeats this block for each item in a collection.
        { // Starts this code block.
            var slot = baseSlot as Activity9DropSlot; // Creates a local value used by the following logic.
            if (slot == null) continue; // Checks this condition before continuing.
            if (!slot.CompareTag("Stage1Slot")) continue; // Checks this condition before continuing.

            var cards = slot.GetCardsInSlot(); // Creates a local value used by the following logic.
            if (cards.Count > 0) // Checks this condition before continuing.
            { // Starts this code block.
                var card = cards[0] as DraggableCard; // Creates a local value used by the following logic.
                if (card == null) continue; // Checks this condition before continuing.

                // Save the Stage 1 answer in the slot
                slot.stage1Answer = card.lapsesAnswer.Trim().ToLower(); // Updates this value.

                bool isCorrect = slot.IsCardCorrect(card); // Creates a local value used by the following logic.

                card.SetSyncedFeedbackColor(isCorrect ? Color.green : Color.red); // Runs this line of puzzle logic.

                if (isCorrect) correct++; // Checks this condition before continuing.

                Debug.Log($"[Stage 1] Slot {slot.name} stores '{slot.stage1Answer}' and check = {isCorrect}"); // Writes a diagnostic message to the Unity console.
            } // Ends this code block.
            else // Runs when the previous condition was false.
            { // Starts this code block.
                slot.stage1Answer = ""; // Updates this value.
            } // Ends this code block.
        } // Ends this code block.

        Debug.Log($"Stage 1 correct: {correct}"); // Writes a diagnostic message to the Unity console.
        return correct >= requiredAnswer1; // Returns this result to the caller.
    } // Ends this code block.

    private bool AreStage2CardsCorrect() // Declares a method used by this script.
    { // Starts this code block.
        int correct = 0; // Creates a local value used by the following logic.

        // Reset Stage 2 colors
        foreach (DropSlot baseSlot in DropSlot.allDropSlots) // Repeats this block for each item in a collection.
        { // Starts this code block.
            var slot = baseSlot as Activity9DropSlot; // Creates a local value used by the following logic.
            if (slot == null) continue; // Checks this condition before continuing.
            if (!slot.CompareTag("Stage2Slot")) continue; // Checks this condition before continuing.

            foreach (var baseCard in slot.GetCardsInSlot()) // Repeats this block for each item in a collection.
            { // Starts this code block.
                var card = baseCard as DraggableCard; // Creates a local value used by the following logic.
                if (card == null) continue; // Checks this condition before continuing.

                card.ResetSyncedFeedbackColor(); // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.

        // Check Stage 2 correctness
        foreach (DropSlot baseSlot in DropSlot.allDropSlots) // Repeats this block for each item in a collection.
        { // Starts this code block.
            var slot = baseSlot as Activity9DropSlot; // Creates a local value used by the following logic.
            if (slot == null) continue; // Checks this condition before continuing.
            if (!slot.CompareTag("Stage2Slot")) continue; // Checks this condition before continuing.

            var cards = slot.GetCardsInSlot(); // Creates a local value used by the following logic.
            if (cards.Count > 0) // Checks this condition before continuing.
            { // Starts this code block.
                var card = cards[0] as DraggableCard; // Creates a local value used by the following logic.
                if (card == null) continue; // Checks this condition before continuing.

                bool isCorrect = slot.IsCardCorrect(card); // Creates a local value used by the following logic.

                card.SetSyncedFeedbackColor(isCorrect ? Color.green : Color.red); // Runs this line of puzzle logic.

                if (isCorrect) correct++; // Checks this condition before continuing.
            } // Ends this code block.
        } // Ends this code block.

        Debug.Log($"✅ Stage 2 correct {correct}/{requiredAnswer2}"); // Writes a diagnostic message to the Unity console.
        StartCoroutine(ResetIncorrectCardColorsAfterDelay(1f)); // Starts a Unity coroutine.
        return correct >= requiredAnswer2; // Returns this result to the caller.
    } // Ends this code block.

    private IEnumerator ResetIncorrectCardColorsAfterDelay(float delay) // Declares a method used by this script.
    { // Starts this code block.
        yield return new WaitForSeconds(delay); // Pauses this coroutine until the yielded condition finishes.

        foreach (DropSlot baseSlot in DropSlot.allDropSlots) // Repeats this block for each item in a collection.
        { // Starts this code block.
            var slot = baseSlot as Activity9DropSlot; // Creates a local value used by the following logic.
            if (slot == null) continue; // Checks this condition before continuing.

            foreach (var baseCard in slot.GetCardsInSlot()) // Repeats this block for each item in a collection.
            { // Starts this code block.
                var card = baseCard as DraggableCard; // Creates a local value used by the following logic.
                if (card == null) continue; // Checks this condition before continuing.

                var img = card.GetComponent<Image>(); // Creates a local value used by the following logic.
                if (img != null && img.color == Color.red) // Checks this condition before continuing.
                { // Starts this code block.
                    card.ResetSyncedFeedbackColor(); // Runs this line of puzzle logic.
                } // Ends this code block.
            } // Ends this code block.
        } // Ends this code block.
    } // Ends this code block.

    public void ResetPuzzle() // Declares a method used by this script.
    { // Starts this code block.
        Debug.Log("[DeathLapses] Resetting puzzle."); // Writes a diagnostic message to the Unity console.

        // Reset stage flag
        stage1Completed = false; // Updates this value.

        // Hide Stage 2 UI
        if (answer2Cards != null) answer2Cards.SetActive(false); // Checks this condition before continuing.
        if (answer2Slots != null) answer2Slots.SetActive(false); // Checks this condition before continuing.

        Debug.Log("[DeathLapses] Reset complete."); // Writes a diagnostic message to the Unity console.
    } // Ends this code block.
} // Ends this code block.
