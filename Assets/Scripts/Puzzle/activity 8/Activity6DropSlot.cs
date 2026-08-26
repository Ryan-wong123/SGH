using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.EventSystems; // Imports a namespace used by this script.
using Photon.Pun; // Imports a namespace used by this script.

public class Activity6DropSlot : DropSlot // Declares this type.
{ // Starts this code block.
    [Header("Puzzle 6 answers")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private string stepName = ""; // set in Inspector or auto-extract from name
    [SerializeField] private int requiredCorrect = 9; // set this in Inspector

    // FIX: If this slot is occupied, reject drop and revert card back to its own previous slot.
    public override void OnDrop(PointerEventData eventData) // Overrides a Unity or Photon callback method.
    { // Starts this code block.
        GameObject droppedObj = eventData.pointerDrag; // Creates a local value used by the following logic.
        if (droppedObj == null) return; // Checks this condition before continuing.

        // NETWORK: only owner can perform drop action (consistent with your base DropSlot logic)
        var pv = droppedObj.GetComponent<PhotonView>(); // Creates a local value used by the following logic.
        if (pv != null && !pv.IsMine) // Checks this condition before continuing.
        { // Starts this code block.
            DeferDropUntilOwnershipTransferred(droppedObj); // Preserves a first drag while Photon ownership transfers.
            return; // Returns this result to the caller.
        } // Ends this code block.

        // If occupied -> revert to origin slot (NOT outside)
        if (transform.childCount > 0) // Checks this condition before continuing.
        { // Starts this code block.
            var card = droppedObj.GetComponent<DraggableCard>(); // Creates a local value used by the following logic.
            if (card != null) // Checks this condition before continuing.
            { // Starts this code block.
                Debug.Log("[Activity6DropSlot] Occupied -> revert to drag origin slot"); // Writes a diagnostic message to the Unity console.
                card.RevertToDragOrigin(); // Runs this line of puzzle logic.
            } // Ends this code block.
            else // Runs when the previous condition was false.
            { // Starts this code block.
                // fallback to base behavior for other card types
                var baseCard = droppedObj.GetComponent<DraggableCard>(); // Creates a local value used by the following logic.
                baseCard?.SnapBack(); // Runs this line of puzzle logic.
            } // Ends this code block.
            return; // Returns this result to the caller.
        } // Ends this code block.

        // Otherwise accept normally (base does parent+snap+trueHome)
        base.OnDrop(eventData); // Runs this line of puzzle logic.
    } // Ends this code block.

    protected override void TrySnapBack(GameObject droppedObj) // Preserves this puzzle's occupied-slot behavior for deferred drops.
    { // Starts this code block.
        DraggableCard card = droppedObj != null ? droppedObj.GetComponent<DraggableCard>() : null; // Creates a local value used by this script.
        if (card != null) // Checks this condition before continuing.
        { // Starts this code block.
            card.RevertToDragOrigin(); // Runs this line of puzzle logic.
            return; // Returns this method.
        } // Ends this code block.

        base.TrySnapBack(droppedObj); // Runs this line of puzzle logic.
    } // Ends this code block.

    public override bool IsCardCorrect(DraggableCard card) // Declares a field, property, or member used by this script.
    { // Starts this code block.
        if (card != null) // Checks this condition before continuing.
        { // Starts this code block.
            string cardStep = card.stepName?.Trim().ToLower() ?? ""; // Creates a local value used by the following logic.
            string slotStep = stepName.Trim().ToLower(); // Creates a local value used by the following logic.

            Debug.Log($"[IsCardCorrect - Puzzle8] Card Step: {cardStep}, Slot Expects: {slotStep}"); // Writes a diagnostic message to the Unity console.

            return cardStep == slotStep; // Returns this result to the caller.
        } // Ends this code block.

        return false; // Returns this result to the caller.
    } // Ends this code block.
} // Ends this code block.
