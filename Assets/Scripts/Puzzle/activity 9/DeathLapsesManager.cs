using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class DeathLapsesManager : MonoBehaviour
{
    [SerializeField] private int requiredAnswer1;
    [SerializeField] private int requiredAnswer2;

    public bool AreAllPuzzle9CardsCorrect()
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

        Debug.Log($"[Puzzle 2] Correct: {correct} / Required: {requiredAnswer1}");

        return correct >= requiredAnswer1;
    }

    private IEnumerator ResetIncorrectCardColorsAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        foreach (Activity9DropSlot slot in Activity9DropSlot.allDropSlots)
        {
            foreach (Activity9DraggableCard card in slot.GetCardsInSlot())
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
