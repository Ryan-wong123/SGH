using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.EventSystems; // Imports a namespace used by this script.
using Photon.Pun; // Imports a namespace used by this script.

public class Activity9DropSlot : DropSlot // Declares this type.
{ // Starts this code block.
    [Header("Puzzle 11 answers")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private int requiredCorrect = 5; // Exposes this private field in the Unity Inspector.
    [HideInInspector] public string stage1Answer = ""; // Adds an attribute that changes how Unity or C# treats the next declaration.

    [Header("Optional Stage 2 linking")] // Adds a labeled section in the Unity Inspector.
    public Activity9DropSlot linkedStage1Slot; // ✅ assign for Stage 2 slots

    // FIX: If slot is occupied, reject and return the card to its previous slot (not outside)
    public override void OnDrop(PointerEventData eventData) // Overrides a Unity or Photon callback method.
    { // Starts this code block.
        GameObject droppedObj = eventData.pointerDrag; // Creates a local value used by the following logic.
        if (droppedObj == null) return; // Checks this condition before continuing.

        // NETWORK: only owner can perform drop action (matches your base DropSlot behavior)
        var pv = droppedObj.GetComponent<PhotonView>(); // Creates a local value used by the following logic.
        if (pv != null && !pv.IsMine) // Checks this condition before continuing.
        { // Starts this code block.
            DeferDropUntilOwnershipTransferred(droppedObj); // Preserves a first drag while Photon ownership transfers.
            return; // Returns this result to the caller.
        } // Ends this code block.

        // Occupied slot => revert to origin slot
        if (transform.childCount > 0) // Checks this condition before continuing.
        { // Starts this code block.
            var card = droppedObj.GetComponent<DraggableCard>(); // Creates a local value used by the following logic.
            if (card != null) // Checks this condition before continuing.
            { // Starts this code block.
                Debug.Log("[Activity9DropSlot] Occupied -> revert to drag origin slot"); // Writes a diagnostic message to the Unity console.
                card.RevertToDragOrigin(); // Runs this line of puzzle logic.
            } // Ends this code block.
            else // Runs when the previous condition was false.
            { // Starts this code block.
                // fallback for non-Activity9 cards
                var baseCard = droppedObj.GetComponent<DraggableCard>(); // Creates a local value used by the following logic.
                baseCard?.SnapBack(); // Runs this line of puzzle logic.
            } // Ends this code block.
            return; // Returns this result to the caller.
        } // Ends this code block.

        // Otherwise accept normally (base handles parenting + snap + trueHome)
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
            string stage2Answer = card.lapsesAnswer.Trim().ToLower(); // Creates a local value used by the following logic.
            string expectedAnswer = stage1Answer.Trim().ToLower(); // Creates a local value used by the following logic.

            // ✅ For Stage 2, use linked Stage 1 slot
            if (linkedStage1Slot != null) // Checks this condition before continuing.
                expectedAnswer = linkedStage1Slot.stage1Answer.Trim().ToLower(); // Updates this value.

            Debug.Log($"[IsCardCorrect] Stage 2 Card '{stage2Answer}' vs Expected '{expectedAnswer}'"); // Writes a diagnostic message to the Unity console.
            return stage2Answer == expectedAnswer; // Returns this result to the caller.
        } // Ends this code block.
        return false; // Returns this result to the caller.
    } // Ends this code block.
} // Ends this code block.
