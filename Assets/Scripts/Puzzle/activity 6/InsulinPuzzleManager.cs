using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
public class InsulinPuzzleManager : MonoBehaviour, IPuzzle
{
    [SerializeField] private int required;

    public bool AreAllPuzzle8CardsCorrect()
    {
        

        int correct = 0;

        // Reset all card visuals
        foreach (DropSlot slot in DropSlot.allDropSlots)
        {
            foreach (DraggableCard card in slot.GetCardsInSlot())
            {
                var img = card.GetComponent<Image>();
                if (img != null) img.color = Color.white;
            }
        }

        // Evaluate placed cards only
        foreach (DropSlot slot in DropSlot.allDropSlots)
        {
            foreach (DraggableCard card in slot.GetCardsInSlot())
            {
                bool isCorrect = slot.IsCardCorrect(card);

                var img = card.GetComponent<Image>();
                if (img != null) img.color = isCorrect ? Color.green : Color.red;

                if (isCorrect) correct++;
            }
        }

        StartCoroutine(ResetIncorrectCardColorsAfterDelay(1f));

        Debug.Log($"[Puzzle 2] Correct: {correct} / Required: {required}");

        return correct >= required;
    }
    public bool IsSolved()
    {
        return AreAllPuzzle8CardsCorrect();
    }
    private IEnumerator ResetIncorrectCardColorsAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        foreach (Activity6DropSlot slot in Activity6DropSlot.allDropSlots)
        {
            foreach (Activity6DraggableCard card in slot.GetCardsInSlot())
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
