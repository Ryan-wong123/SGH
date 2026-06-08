using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.EventSystems; // Imports a namespace used by this script.
using System.Collections.Generic; // Imports a namespace used by this script.
using static PuzzleStateManager; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.

public class Activity1_2DropSlot : DropSlot // Declares this type.
{ // Starts this code block.
    [Header("Puzzle 1 Settings")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] public List<string> correctAnswers = new(); // Exposes this private field in the Unity Inspector.

    // -----------------------------
    // FIX: mark acceptance + revert-to-origin on occupied slot
    // -----------------------------
    protected override void AcceptCard(DraggableCard card) // Overrides a Unity or Photon callback method.
    { // Starts this code block.
        // FIX: tell Activity1 card that this drag was accepted by a real slot
        card.MarkAcceptedThisDrag(); // Runs this line of puzzle logic.

        // Keep base behavior (parenting + snap pos/scale + trueHome assignment)
        base.AcceptCard(card); // Runs this line of puzzle logic.
    } // Ends this code block.

    protected override void TrySnapBack(GameObject droppedObj) // Overrides a Unity or Photon callback method.
    { // Starts this code block.
        // If slot occupied, revert to the drag origin (not SnapBack's special panel logic)
        var card = droppedObj != null ? droppedObj.GetComponent<DraggableCard>() : null; // Creates a local value used by the following logic.
        if (card != null) // Checks this condition before continuing.
        { // Starts this code block.
            card.RevertToDragOrigin(); // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        // Fallback
        base.TrySnapBack(droppedObj); // Runs this line of puzzle logic.
    } // Ends this code block.

    public override bool IsCardCorrect(DraggableCard card) // Declares a field, property, or member used by this script.
    { // Starts this code block.
        if (card != null) // Checks this condition before continuing.
        { // Starts this code block.
            if (PuzzleStateManager.Instance.CurrentStage == PuzzleStateManager.PuzzleStage.Puzzle2) // Checks this condition before continuing.
                return correctAnswers.Contains(card.symptomName); // Returns this result to the caller.
        } // Ends this code block.
        return false; // Returns this result to the caller.
    } // Ends this code block.
} // Ends this code block.
