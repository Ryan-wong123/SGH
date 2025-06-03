using UnityEngine;
using UnityEngine.UI;
public class Activity2DropSlot : DropSlot
{
    [Header("Puzzle 6 answers")]
    [SerializeField] private string stepName = ""; // set in Inspector or auto-extract from name
    [SerializeField] private int requiredCorrect = 20; // set this in Inspector
    public override int GetRequiredCount() => requiredCorrect;

    public override bool IsCardCorrect(DraggableCard card)
    {
        if (card is Activity2DraggableCard a1Card)
        {
            if (PuzzleStateManager.Instance.CurrentStage == PuzzleStateManager.PuzzleStage.Puzzle4)
            {
                string cardStep = a1Card.stepName?.Trim().ToLower() ?? "";
                string slotStep = stepName.Trim().ToLower();

                Debug.Log($"[IsCardCorrect - Puzzle4] Card Step: {cardStep}, Slot Expects: {slotStep}");

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
        rect.localScale = new Vector2(0.66f, 0.66f) ;

        card.trueHome = transform.parent;
        cardImage = card.GetComponent<Image>();
    }
}
