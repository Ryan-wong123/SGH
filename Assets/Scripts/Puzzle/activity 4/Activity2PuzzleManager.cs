using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using WebSocketSharp;

public class Activity2PuzzleManager : MonoBehaviour,IPuzzle
{
    [SerializeField] private int required;
    [SerializeField] private TMP_InputField input1;
    [SerializeField] private TMP_InputField input2;
    [SerializeField] private GameObject panel1;
    [SerializeField] private GameObject panel2;

    public enum Activity2SubmitResult
    {
        Incorrect,
        Advanced,
        Completed
    }

    public bool IsOnPart2 => panel2 != null && panel2.activeSelf;


    public bool AreAllPuzzle4CardsCorrect()
    {


        int correct = 0;

        // Reset all card visuals
        foreach (DropSlot slot in DropSlot.allDropSlots)
        {
            foreach (DraggableCard card in slot.GetCardsInSlot())
            {
                card.ResetSyncedFeedbackColor();
            }
        }

        // Evaluate placed cards only
        foreach (DropSlot slot in DropSlot.allDropSlots)
        {
            foreach (DraggableCard card in slot.GetCardsInSlot())
            {
                bool isCorrect = slot.IsCardCorrect(card);

                card.SetSyncedFeedbackColor(isCorrect ? Color.green : Color.red);

                if (isCorrect) correct++;
            }
        }

        StartCoroutine(ResetIncorrectCardColorsAfterDelay(1f));

        Debug.Log($"[Puzzle 4] Correct: {correct} / Required: {required}");

        return correct >= required;
    }

    public Activity2SubmitResult SubmitActivity2Stage()
    {
        if (!IsOnPart2)
        {
            return AreAllPuzzle4CardsCorrect()
                ? Activity2SubmitResult.Advanced
                : Activity2SubmitResult.Incorrect;
        }

        return CheckActivity2Part2Correct()
            ? Activity2SubmitResult.Completed
            : Activity2SubmitResult.Incorrect;
    }

    public void GoToPart2()
    {
        if (panel1 != null) panel1.SetActive(false);
        if (panel2 != null) panel2.SetActive(true);
    }

    public bool IsSolved()
    {
        return SubmitActivity2Stage() == Activity2SubmitResult.Completed;
    }

    public bool CheckActivity2Part2Correct()
    {
        if (input1.text.IsNullOrEmpty() || input2.text.IsNullOrEmpty())
        {
            return false;
        }
        input1.SetSyncedFeedbackColor(input1.text == "15" ? Color.green : Color.red);
        input2.SetSyncedFeedbackColor(input2.text == "4" ? Color.green : Color.red);
        return input1.text == "15" && input2.text == "4";
  
    }

    public void ResetActivity2Part2()
    {
        input1.ResetSyncedFeedbackColor();
        input2.ResetSyncedFeedbackColor();
        input1.text = "";
        input2.text = "";
    }

    private IEnumerator ResetIncorrectCardColorsAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        foreach (Activity2DropSlot slot in Activity2DropSlot.allDropSlots)
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
