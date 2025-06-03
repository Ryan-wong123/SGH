using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Activity6DropSlot : DropSlot
{
    [Header("Puzzle 6 answers")]
    [SerializeField] private string stepName = ""; // set in Inspector or auto-extract from name
    [SerializeField] private int requiredCorrect = 9; // set this in Inspector

    public override int GetRequiredCount() => requiredCorrect;

    public override bool IsCardCorrect(DraggableCard card)
    {
        if (card is Activity6DraggableCard a1Card)
        {
            if (PuzzleStateManager.Instance.CurrentStage == PuzzleStateManager.PuzzleStage.Puzzle8)
            {
                string cardStep = a1Card.stepName?.Trim().ToLower() ?? "";
                string slotStep = stepName.Trim().ToLower();

                Debug.Log($"[IsCardCorrect - Puzzle8] Card Step: {cardStep}, Slot Expects: {slotStep}");

                return cardStep == slotStep;
            }
        }

        return false;
    }

    protected override void AcceptCard(DraggableCard card)
    {
        card.transform.SetParent(transform);

        var rect = card.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(150, -150);
        rect.localScale = Vector2.one;

        card.trueHome = transform.parent;
        cardImage = card.GetComponent<Image>();
    }
}
