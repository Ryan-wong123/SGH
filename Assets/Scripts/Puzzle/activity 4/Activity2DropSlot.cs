using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

public class Activity2DropSlot : DropSlot
{
    [Header("Puzzle 6 answers")]
    [SerializeField] private string stepName = ""; // set in Inspector
    [SerializeField] private int requiredCorrect = 20; // set this in Inspector

    // FIX: If this slot is occupied, reject the drop and revert card back to its own slot
    public override void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null) return;

        // NETWORK: only owner can perform the drop action (matches your base DropSlot behavior)
        var pv = droppedObj.GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine)
            return;

        // If already occupied -> revert to origin slot (NOT outside)
        if (transform.childCount > 0)
        {
            var cardA2 = droppedObj.GetComponent<Activity2DraggableCard>();
            if (cardA2 != null)
            {
                Debug.Log("[Activity2DropSlot] Occupied -> revert to drag origin slot");
                cardA2.RevertToDragOrigin();
            }
            else
            {
                // fallback to base behavior if some other card type
                var baseCard = droppedObj.GetComponent<DraggableCard>();
                baseCard?.SnapBack();
            }
            return;
        }

        // Otherwise, let base accept logic handle parenting + snap
        base.OnDrop(eventData);
    }

    public override bool IsCardCorrect(DraggableCard card)
    {
        if (card is Activity2DraggableCard a2Card)
        {
            if (PuzzleStateManager.Instance.CurrentStage == PuzzleStateManager.PuzzleStage.Puzzle4)
            {
                string cardStep = a2Card.stepName?.Trim().ToLower() ?? "";
                string slotStep = stepName.Trim().ToLower();

                Debug.Log($"[IsCardCorrect - Puzzle4] Card Step: {cardStep}, Slot Expects: {slotStep}");

                return cardStep == slotStep;
            }
        }

        return false;
    }
}
