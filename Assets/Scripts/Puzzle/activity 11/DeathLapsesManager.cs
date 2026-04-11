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

    private bool stage1Completed = false;

    public enum DeathLapsesSubmitResult
    {
        Incorrect,
        Advanced,
        Completed
    }

    public DeathLapsesSubmitResult SubmitDeathLapsesStage()
    {
        if (!stage1Completed)
        {
            if (AreStage1CardsCorrect())
            {
                Debug.Log("✅ Stage 1 completed. Submit again to check Stage 2.");
                return DeathLapsesSubmitResult.Advanced;
            }
            Debug.Log("❌ Stage 1 incorrect.");
            return DeathLapsesSubmitResult.Incorrect;
        }

        Debug.Log("checking stage 2");
        return AreStage2CardsCorrect()
            ? DeathLapsesSubmitResult.Completed
            : DeathLapsesSubmitResult.Incorrect;
    }

    public bool IsSolved() => SubmitDeathLapsesStage() == DeathLapsesSubmitResult.Completed;

    public void GoToStage2()
    {
        stage1Completed = true;
        answer2Cards?.SetActive(true);
        answer2Slots?.SetActive(true);
    }

    private bool AreStage1CardsCorrect()
    {
        int correct = 0;

        // ✅ Reset colors first
        foreach (DropSlot baseSlot in DropSlot.allDropSlots)
        {
            var slot = baseSlot as Activity9DropSlot;
            if (slot == null) continue;                    // skip other puzzles' slots
            if (!slot.CompareTag("Stage1Slot")) continue;

            foreach (var baseCard in slot.GetCardsInSlot())
            {
                var card = baseCard as DraggableCard;
                if (card == null) continue;                // skip non-Activity9 cards

                var img = card.GetComponent<Image>();
                if (img != null) img.color = Color.white;
            }
        }

        // ✅ Assign stage1Answer FIRST then check correctness
        foreach (DropSlot baseSlot in DropSlot.allDropSlots)
        {
            var slot = baseSlot as Activity9DropSlot;
            if (slot == null) continue;
            if (!slot.CompareTag("Stage1Slot")) continue;

            var cards = slot.GetCardsInSlot();
            if (cards.Count > 0)
            {
                var card = cards[0] as DraggableCard;
                if (card == null) continue;

                // Save the Stage 1 answer in the slot
                slot.stage1Answer = card.lapsesAnswer.Trim().ToLower();

                bool isCorrect = slot.IsCardCorrect(card);

                var img = card.GetComponent<Image>();
                if (img != null) img.color = isCorrect ? Color.green : Color.red;

                if (isCorrect) correct++;

                Debug.Log($"[Stage 1] Slot {slot.name} stores '{slot.stage1Answer}' and check = {isCorrect}");
            }
            else
            {
                slot.stage1Answer = "";
            }
        }

        Debug.Log($"Stage 1 correct: {correct}");
        return correct >= requiredAnswer1;
    }

    private bool AreStage2CardsCorrect()
    {
        int correct = 0;

        // Reset Stage 2 colors
        foreach (DropSlot baseSlot in DropSlot.allDropSlots)
        {
            var slot = baseSlot as Activity9DropSlot;
            if (slot == null) continue;
            if (!slot.CompareTag("Stage2Slot")) continue;

            foreach (var baseCard in slot.GetCardsInSlot())
            {
                var card = baseCard as DraggableCard;
                if (card == null) continue;

                var img = card.GetComponent<Image>();
                if (img != null) img.color = Color.white;
            }
        }

        // Check Stage 2 correctness
        foreach (DropSlot baseSlot in DropSlot.allDropSlots)
        {
            var slot = baseSlot as Activity9DropSlot;
            if (slot == null) continue;
            if (!slot.CompareTag("Stage2Slot")) continue;

            var cards = slot.GetCardsInSlot();
            if (cards.Count > 0)
            {
                var card = cards[0] as DraggableCard;
                if (card == null) continue;

                bool isCorrect = slot.IsCardCorrect(card);

                var img = card.GetComponent<Image>();
                if (img != null) img.color = isCorrect ? Color.green : Color.red;

                if (isCorrect) correct++;
            }
        }

        Debug.Log($"✅ Stage 2 correct {correct}/{requiredAnswer2}");
        StartCoroutine(ResetIncorrectCardColorsAfterDelay(1f));
        return correct >= requiredAnswer2;
    }

    private IEnumerator ResetIncorrectCardColorsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        foreach (DropSlot baseSlot in DropSlot.allDropSlots)
        {
            var slot = baseSlot as Activity9DropSlot;
            if (slot == null) continue;

            foreach (var baseCard in slot.GetCardsInSlot())
            {
                var card = baseCard as DraggableCard;
                if (card == null) continue;

                var img = card.GetComponent<Image>();
                if (img != null && img.color == Color.red)
                {
                    img.color = Color.white;
                }
            }
        }
    }

    public void ResetPuzzle()
    {
        Debug.Log("[DeathLapses] Resetting puzzle.");

        // Reset stage flag
        stage1Completed = false;

        // Hide Stage 2 UI
        if (answer2Cards != null) answer2Cards.SetActive(false);
        if (answer2Slots != null) answer2Slots.SetActive(false);

        Debug.Log("[DeathLapses] Reset complete.");
    }
}
