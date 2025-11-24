using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using TMPro;
public class InsulinPuzzleManager : MonoBehaviour, IPuzzle
{
    [SerializeField] private int required;

    [SerializeField] GameObject panel1;
    [SerializeField] GameObject panel2;

    public TMP_Dropdown syringeDropdown;
    public int correctSyringeIndex = 1;       // The correct option index in dropdown

    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    public Color defaultColor = Color.white;

    public void SubmitSyringeAnswer()
    {
        StartCoroutine(CheckCorrectSyringe());
    }

    public IEnumerator CheckCorrectSyringe()
    {
        // Get the user's selected option
        int selected = syringeDropdown.value;

        // Check correctness
        if (selected == correctSyringeIndex)
        {
            syringeDropdown.GetComponent<Image>().color = correctColor;
            // Optional delay before showing result (remove if not needed)
            yield return new WaitForSeconds(2f);
            panel1.SetActive(false);
            panel2.SetActive(true);
        }
           
        else
            syringeDropdown.GetComponent<Image>().color = wrongColor;
    }



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

    public void ResetPuzzle()
    {
        Debug.Log("[InsulinPuzzle] Resetting Puzzle");

        // -----------------------------
        // 1) Reset syringe dropdown
        // -----------------------------
        if (syringeDropdown != null)
        {
            syringeDropdown.value = 0;                   // Reset selection to first option
            syringeDropdown.RefreshShownValue();         // Update UI
            syringeDropdown.GetComponent<Image>().color = defaultColor;
        }

        // -----------------------------
        // 5) Reset panel visibility
        // -----------------------------
        if (panel1 != null) panel1.SetActive(true);
        if (panel2 != null) panel2.SetActive(false);

        Debug.Log("[InsulinPuzzle] RESET COMPLETE");
    }

}
