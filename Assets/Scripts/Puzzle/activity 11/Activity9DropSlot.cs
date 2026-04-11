using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

public class Activity9DropSlot : DropSlot
{
    [Header("Puzzle 11 answers")]
    [SerializeField] private int requiredCorrect = 5;
    [HideInInspector] public string stage1Answer = "";

    [Header("Optional Stage 2 linking")]
    public Activity9DropSlot linkedStage1Slot; // ✅ assign for Stage 2 slots

    // FIX: If slot is occupied, reject and return the card to its previous slot (not outside)
    public override void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null) return;

        // NETWORK: only owner can perform drop action (matches your base DropSlot behavior)
        var pv = droppedObj.GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine)
            return;

        // Occupied slot => revert to origin slot
        if (transform.childCount > 0)
        {
            var card = droppedObj.GetComponent<DraggableCard>();
            if (card != null)
            {
                Debug.Log("[Activity9DropSlot] Occupied -> revert to drag origin slot");
                card.RevertToDragOrigin();
            }
            else
            {
                // fallback for non-Activity9 cards
                var baseCard = droppedObj.GetComponent<DraggableCard>();
                baseCard?.SnapBack();
            }
            return;
        }

        // Otherwise accept normally (base handles parenting + snap + trueHome)
        base.OnDrop(eventData);
    }

    public override bool IsCardCorrect(DraggableCard card)
    {
        if (card != null)
        {
            string stage2Answer = card.lapsesAnswer.Trim().ToLower();
            string expectedAnswer = stage1Answer.Trim().ToLower();

            // ✅ For Stage 2, use linked Stage 1 slot
            if (linkedStage1Slot != null)
                expectedAnswer = linkedStage1Slot.stage1Answer.Trim().ToLower();

            Debug.Log($"[IsCardCorrect] Stage 2 Card '{stage2Answer}' vs Expected '{expectedAnswer}'");
            return stage2Answer == expectedAnswer;
        }
        return false;
    }
}
