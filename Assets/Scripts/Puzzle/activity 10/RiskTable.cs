using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // for Image

public class RiskTable : MonoBehaviour, IPuzzle
{
    [System.Serializable]
    public class DropdownAnswer
    {
        [Tooltip("Reference to the dropdown UI element")]
        public TMP_Dropdown dropdown;

        [Tooltip("The correct option index for this dropdown")]
        public int correctIndex;
    }

    [Header("Dropdowns & Correct Answers")]
    [SerializeField] private List<DropdownAnswer> dropdowns = new();

    [Header("Colors")]
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color wrongColor = Color.red;
    [SerializeField] private Color defaultColor = Color.white;

    /// <summary>
    /// Evaluate all dropdowns. Returns true if all are correct.
    /// </summary>
    private bool CheckAnswers()
    {
        bool allCorrect = true;

        foreach (var entry in dropdowns)
        {
            if (entry.dropdown == null) continue;

            Image img = entry.dropdown.targetGraphic as Image;

            if (entry.dropdown.value == entry.correctIndex)
            {
                if (img) img.color = correctColor;
            }
            else
            {
                if (img) img.color = wrongColor;
                allCorrect = false;
            }
        }

        if (allCorrect)
            Debug.Log("✅ All dropdowns are correct!");
        else
            Debug.Log("❌ Some dropdowns are wrong.");

        return allCorrect;
    }

    public bool IsSolved()
    {
        return CheckAnswers();
    }

    /// <summary>
    /// Reset all dropdown colors back to default.
    /// </summary>
    public void ResetColors()
    {
        foreach (var entry in dropdowns)
        {
            if (entry.dropdown == null) continue;

            Image img = entry.dropdown.targetGraphic as Image;
            if (img) img.color = defaultColor;
        }
    }
}
