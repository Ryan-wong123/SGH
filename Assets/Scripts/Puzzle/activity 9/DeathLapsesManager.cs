using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class DeathLapsesManager : MonoBehaviour
{
    public bool AreAllPuzzle1CardsCorrect()
    {
        int totalSlots = DropSlot.allDropSlots.Count;
        int filled = 0;
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

    public bool AreAllPuzzle2CardsCorrect()
    {
        int required = DropSlot.allDropSlots.Count > 0
             ? DropSlot.allDropSlots[0].GetRequiredCount()
             : 0;

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
                string spriteName = (card is Activity1DraggableCard a1Card) ? a1Card.backSprite.name : "unknown";
                Debug.Log($"[Submit Puzzle 2] Card: {card.name}, Sprite: {spriteName}");

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
