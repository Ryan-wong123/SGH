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

    public bool AreAllPuzzle11CardsCorrect()
    {
        if (!stage1Completed)
        {
            if (AreStage1CardsCorrect())
            {
                stage1Completed = true;
                ShowStage2();
                Debug.Log("✅ Stage 1 completed. Submit again to check Stage 2.");
                return false; // ⛔️ SKIP Stage 2 completely on first submit
            }
            Debug.Log("❌ Stage 1 incorrect.");
            return false;
        }
        else
        {
            Debug.Log("checking stage 2");
            return AreStage2CardsCorrect(); // ✅ only after stage1Completed = true
        }
    }


    public bool IsSolved() => AreAllPuzzle11CardsCorrect();

    private void ShowStage2()
    {
        answer2Cards?.SetActive(true);
        answer2Slots?.SetActive(true);
    }

    private bool AreStage1CardsCorrect()
    {
        int correct = 0;

        // ✅ Reset colors first
        foreach (Activity9DropSlot slot in Activity9DropSlot.allDropSlots)
        {
            if (!slot.CompareTag("Stage1Slot")) continue;
            foreach (Activity9DraggableCard card in slot.GetCardsInSlot())
            {
                card.GetComponent<Image>().color = Color.white;
            }
        }

        // ✅ Assign stage1Answer FIRST then check correctness
        foreach (Activity9DropSlot slot in Activity9DropSlot.allDropSlots)
        {
            if (!slot.CompareTag("Stage1Slot")) continue;

            var cards = slot.GetCardsInSlot();
            if (cards.Count > 0)
            {
                var card = cards[0] as Activity9DraggableCard;
                if (card == null) continue;

                // ✅ Save answer before checking
                slot.stage1Answer = card.lapsesAnswer.Trim().ToLower();

                bool isCorrect = slot.IsCardCorrect(card);

                var img = card.GetComponent<Image>();
                if (img != null) img.color = isCorrect ? Color.green : Color.red;

                if (isCorrect) correct++;

                Debug.Log($"[Stage 1] Slot {slot.name} stores '{slot.stage1Answer}' and check = {isCorrect}");
            }
            else slot.stage1Answer = "";
        }

        Debug.Log($"Stage 1 correct: {correct}");
        return correct >= requiredAnswer1;
    }


    private bool AreStage2CardsCorrect()
    {
        int correct = 0;

        foreach (Activity9DropSlot slot in Activity9DropSlot.allDropSlots)
        {
            if (!slot.CompareTag("Stage2Slot")) continue;
            foreach (Activity9DraggableCard card in slot.GetCardsInSlot())
            {
                card.GetComponent<Image>().color = Color.white;
            }
        }

        foreach (Activity9DropSlot slot in Activity9DropSlot.allDropSlots)
        {
            if (!slot.CompareTag("Stage2Slot")) continue;

            var cards = slot.GetCardsInSlot();
            if (cards.Count > 0)
            {
                var card = cards[0] as Activity9DraggableCard;
                if (card == null) continue;

                bool isCorrect = slot.IsCardCorrect(card);
                card.GetComponent<Image>().color = isCorrect ? Color.green : Color.red;
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
        foreach (Activity9DropSlot slot in Activity9DropSlot.allDropSlots)
        {
            foreach (Activity9DraggableCard card in slot.GetCardsInSlot())
            {
                if (card.GetComponent<Image>().color == Color.red)
                    card.GetComponent<Image>().color = Color.white;
            }
        }
    }

    public void ResetPuzzle()
    {
        Debug.Log("[DeathLapses] Resetting puzzle...");

        // 1) Reset stage flags
        stage1Completed = false;

        // 2) Hide Stage 2 UI
        if (answer2Cards != null) answer2Cards.SetActive(false);
        if (answer2Slots != null) answer2Slots.SetActive(false);

        Debug.Log("[DeathLapses] Reset complete.");
    }

}
