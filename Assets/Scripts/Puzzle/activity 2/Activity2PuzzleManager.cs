using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class Activity2PuzzleManager : MonoBehaviour
{
    [SerializeField] private int required;

    public bool AreAllPuzzle4CardsCorrect()
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

        Debug.Log($"[Puzzle 4] Correct: {correct} / Required: {required}");

        return correct >= required;
    }

    private IEnumerator ResetIncorrectCardColorsAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        foreach (Activity2DropSlot slot in Activity2DropSlot.allDropSlots)
        {
            foreach (Activity2DraggableCard card in slot.GetCardsInSlot())
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
