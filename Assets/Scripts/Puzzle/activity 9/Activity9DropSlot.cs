using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using static PuzzleStateManager;
using UnityEngine.UI;

public class Activity9DropSlot : DropSlot
{
    [Header("Puzzle 9 answers")]
    [SerializeField] private string lapsesAnswer = ""; // set in Inspector or auto-extract from name
    [SerializeField] private int requiredCorrect = 5; // set this in Inspector
    public override int GetRequiredCount() => requiredCorrect;

    public override bool IsCardCorrect(DraggableCard card)
    {
        if (card is Activity9DraggableCard a1Card)
        {
            if (PuzzleStateManager.Instance.CurrentStage == PuzzleStateManager.PuzzleStage.Puzzle11)
            {
                string cardStep = a1Card.lapsesAnswer?.Trim().ToLower() ?? "";
                string slotStep = lapsesAnswer.Trim().ToLower();

                Debug.Log($"[IsCardCorrect - Puzzle11] Card Step: {cardStep}, Slot Expects: {slotStep}");

                return cardStep == slotStep;
            }
        }

        return false;
    }

    protected override void AcceptCard(DraggableCard card)
    {
        card.transform.SetParent(transform);

        var rect = card.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(90, -90);
        rect.localScale = new Vector2(0.66f, 0.66f);

        card.trueHome = transform.parent;
        cardImage = card.GetComponent<Image>();
    }
}
