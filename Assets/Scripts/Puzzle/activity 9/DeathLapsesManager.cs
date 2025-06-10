using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class DeathLapsesManager : MonoBehaviour, IPuzzle
{
    [SerializeField] private int requiredAnswer1;
    [SerializeField] private int requiredAnswer2;

    [SerializeField] private GameObject answer2Cards;
    [SerializeField] private GameObject answer2Slots;
    public bool AreAllPuzzle11CardsCorrect()
    {
        int stage1Correct = 0;
        int stage2Correct = 0;

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
            if (!slot.CompareTag("Stage1Slot")) continue; // ✅ Filter by tag or assign explicitly

            var cards = slot.GetCardsInSlot();
            if (cards.Count > 0)
            {
                var card = cards[0] as Activity9DraggableCard;
                if (card == null) continue;

                bool isCorrect = slot.IsCardCorrect(card);

                Image img = card.GetComponent<Image>();
                if (img != null) img.color = isCorrect ? Color.green : Color.red;

                if (isCorrect) stage1Correct++;
            }
        }

        StartCoroutine(ResetIncorrectCardColorsAfterDelay(1f));

        Debug.Log($"[Puzzle 2] Correct: {stage1Correct} / Required: {requiredAnswer1}");

        if (stage1Correct >= requiredAnswer1)
        {
            Debug.Log("✅ Stage 1 complete — locking cards and showing Stage 2");

            // ✅ Show stage 2 cards and slots
            answer2Cards.SetActive(true);
            answer2Slots.SetActive(true);
        }

        foreach (DropSlot slot in DropSlot.allDropSlots)
        {
            if (!slot.CompareTag("Stage2Slot")) continue; // ✅ Tag slots for Stage 2
            var cards = slot.GetCardsInSlot();
            if (cards.Count > 0)
            {
                var card = cards[0] as Activity9DraggableCard;
                if (card == null) continue;

                bool isCorrect = slot.IsCardCorrect(card);

                Image img = card.GetComponent<Image>();
                if (img != null) img.color = isCorrect ? Color.green : Color.red;

                if (isCorrect) stage2Correct++;
            }
        }
        Debug.Log($"{stage2Correct} — {requiredAnswer2} Stage 2");

        return stage2Correct >= requiredAnswer2;
    }
    public bool IsSolved()
    {
        return AreAllPuzzle11CardsCorrect();
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
