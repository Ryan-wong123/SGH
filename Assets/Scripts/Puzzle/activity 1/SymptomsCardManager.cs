using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class SymptomsCardManager : MonoBehaviour, IPuzzle
{
    [SerializeField] private int required;


    public bool AreAllPuzzle1CardsCorrect()
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

                var img = card.GetComponent<Image>();
                if (img != null) img.color = isCorrect ? Color.green : Color.red;

                if (isCorrect) correct++;
            }
        }
        StartCoroutine(ResetIncorrectCardColorsAfterDelay(1f));
        Debug.Log($"[Puzzle 1] Filled: {filled}/{totalSlots}, Correct: {correct}/{totalSlots}");
        return filled == totalSlots && correct == totalSlots;
    }

    public bool IsSolved()
    {
        if (AreAllPuzzle1CardsCorrect())
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
                    img.color = Color.white;
                }
            }
        }
    }


}
