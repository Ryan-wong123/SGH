using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

public class Activity6DropSlot : DropSlot
{
    [Header("Puzzle 6 answers")]
    [SerializeField] private string stepName = ""; // set in Inspector or auto-extract from name
    [SerializeField] private int requiredCorrect = 9; // set this in Inspector

    // FIX: If this slot is occupied, reject drop and revert card back to its own previous slot.
    public override void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null) return;

        // NETWORK: only owner can perform drop action (consistent with your base DropSlot logic)
        var pv = droppedObj.GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine)
            return;

        // If occupied -> revert to origin slot (NOT outside)
        if (transform.childCount > 0)
        {
            var cardA6 = droppedObj.GetComponent<Activity6DraggableCard>();
            if (cardA6 != null)
            {
                Debug.Log("[Activity6DropSlot] Occupied -> revert to drag origin slot");
                cardA6.RevertToDragOrigin();
            }
            else
            {
                // fallback to base behavior for other card types
                var baseCard = droppedObj.GetComponent<DraggableCard>();
                baseCard?.SnapBack();
            }
            return;
        }

        // Otherwise accept normally (base does parent+snap+trueHome)
        base.OnDrop(eventData);
    }

    public override bool IsCardCorrect(DraggableCard card)
    {
        if (card is Activity6DraggableCard a6Card)
        {
            string cardStep = a6Card.stepName?.Trim().ToLower() ?? "";
            string slotStep = stepName.Trim().ToLower();

            Debug.Log($"[IsCardCorrect - Puzzle8] Card Step: {cardStep}, Slot Expects: {slotStep}");

            return cardStep == slotStep;
        }

        return false;
    }
}
