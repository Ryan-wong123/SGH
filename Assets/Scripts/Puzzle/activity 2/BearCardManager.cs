using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class BearCardManager : MonoBehaviour, IPuzzle
{
    [SerializeField] private int required;

    public bool AreAllPuzzle2CardsCorrect()
    {

        int totalSlots = DropSlot.allDropSlots.Count;
        int filled = 0;
        int correct = 0;

        // Evaluate each slot
        foreach (DropSlot slot in DropSlot.allDropSlots)
        {
            var cards = slot.GetCardsInSlot();
            if (cards.Count > 0)
            {
                filled++;

                DraggableCard card = cards[0];
                bool isCorrect = slot.IsCardCorrect(card);

                card.SetSyncedFeedbackColor(isCorrect ? Color.green : Color.red);

                if (isCorrect) correct++;
            }
        }
        StartCoroutine(ResetIncorrectCardColorsAfterDelay(1f));

        Debug.Log($"[Puzzle 2] Correct: {correct} / Required: {required}");

        return correct >= required;
    }
    public bool IsSolved()
    {
   
        if (AreAllPuzzle2CardsCorrect())
        {
            return true;
        }

        return false;
    }
    private IEnumerator ResetIncorrectCardColorsAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        foreach (DropSlot slot in DropSlot.allDropSlots)
        {
            foreach (DraggableCard card in slot.GetCardsInSlot())
            {
                Image img = card.GetComponent<Image>();
                if (img != null && img.color == Color.red)
                {
                    card.ResetSyncedFeedbackColor();
                }
            }
        }
    }


}
