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

    public override int GetRequiredCount() => requiredCorrect;
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

                return spriteName.Contains(slotColorName.ToLower()); // fuzzy match: "greenbear" contains "green"
            }
              
        }
        return false;
    }

    protected override void AcceptCard(DraggableCard card)
    {
        card.transform.SetParent(transform);

        var rect = card.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(140, -160);
        rect.localScale = Vector2.one;

        card.trueHome = transform.parent;
        cardImage = card.GetComponent<Image>();
    }
    protected override void TrySnapBack(GameObject droppedObj)
    {
        Activity1DraggableCard card = droppedObj?.GetComponent<Activity1DraggableCard>();
        card?.SnapBack();
    }
}
