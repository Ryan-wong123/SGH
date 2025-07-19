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

    public bool IsSolved()
    {
         if(AreAllPuzzle4CardsCorrect())
        {
            panel1.SetActive(false);
            panel2.SetActive(true);

            if (CheckActivity2Part2Correct())
            {
                return true;
            }
        }
        return false;
    }

    public bool CheckActivity2Part2Correct()
    {
        if (input1.text.IsNullOrEmpty() || input2.text.IsNullOrEmpty())
        {
            return false;
        }
        input1.GetComponent<Image>().color = input1.text == "15" ? Color.green : Color.red;
        input2.GetComponent<Image>().color = input2.text == "4" ? Color.green : Color.red;
        return input1.text == "15" && input2.text == "4";
  
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
