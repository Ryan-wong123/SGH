using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using static PuzzleStateManager;
using UnityEngine.UI;

public class Activity1DropSlot : DropSlot
{
    [Header("Puzzle 1 Settings")]
    [SerializeField] public List<string> correctAnswers = new();

    [Header("Puzzle 2 Settings")]
    [SerializeField] private string slotColorName = ""; // set in Inspector or auto-extract from name
    [SerializeField] private int requiredCorrect = 10; // set this in Inspector

    // -----------------------------
    // FIX: mark acceptance + revert-to-origin on occupied slot
    // -----------------------------
    protected override void AcceptCard(DraggableCard card)
    {
        // FIX: tell Activity1 card that this drag was accepted by a real slot
        if (card is Activity1DraggableCard a1Card)
            a1Card.MarkAcceptedThisDrag();

        // Keep base behavior (parenting + snap pos/scale + trueHome assignment)
        base.AcceptCard(card);
    }

    protected override void TrySnapBack(GameObject droppedObj)
    {
        // If slot occupied, revert to the drag origin (not SnapBack's special panel logic)
        var a1Card = droppedObj != null ? droppedObj.GetComponent<Activity1DraggableCard>() : null;
        if (a1Card != null)
        {
            a1Card.RevertToDragOrigin();
            return;
        }

        // Fallback
        base.TrySnapBack(droppedObj);
    }

    public override bool IsCardCorrect(DraggableCard card)
    {
        if (card is Activity1DraggableCard a1Card)
        {
            if (PuzzleStateManager.Instance.CurrentStage == PuzzleStateManager.PuzzleStage.Puzzle1)
                return correctAnswers.Contains(a1Card.symptomName);
            else if (PuzzleStateManager.Instance.CurrentStage == PuzzleStateManager.PuzzleStage.Puzzle2)
            {
                string spriteName = a1Card.backSprite != null ? a1Card.backSprite.name.ToLower() : "";
                Debug.Log($"[IsCardCorrect - Puzzle2] Card Sprite: {spriteName}, Slot expects: {slotColorName}");
                return spriteName.Contains(slotColorName.ToLower());
            }
        }
        return false;
    }
}
