using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;
using TMPro;

public class PictureGridManager : MonoBehaviour, IPuzzle
{
    [Header("Grid Settings")]
    public List<Button> pictureButtons; // Assign all 20 buttons
    public List<int> correctIndexes; // e.g. [0, 4, 7, 9, 15, 19]

    private HashSet<int> selectedIndexes = new HashSet<int>();

    public Color defaultColor = Color.white;
    public Color selectedColor = Color.yellow;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    [SerializeField] GameObject submitButton;
    [SerializeField] private TMP_Text selectedCountText;

    public void Start()
    {
        for (int i = 0; i < pictureButtons.Count; i++)
        {
            int index = i; // Needed to avoid closure issues
            pictureButtons[i].onClick.AddListener(() => OnPictureClick(index));
        }

        submitButton.SetActive(false);
        UpdateSelectedCountText();

    }

    void OnPictureClick(int index)
    {
        if (selectedIndexes.Contains(index))
        {
            selectedIndexes.Remove(index);
            SetButtonColor(index, defaultColor);
        }
        else
        {
            selectedIndexes.Add(index);
            SetButtonColor(index, selectedColor);
        }
        UpdateSelectedCountText();
        // Log when exactly 6 buttons are selected
        if (selectedIndexes.Count == correctIndexes.Count)
        {
            Debug.Log($"{correctIndexes.Count} buttons selected. Ready to check answers!");

            submitButton.SetActive(true);
        }
    }
    void UpdateSelectedCountText()
    {
        if (selectedCountText != null)
        {
            selectedCountText.text = $"Selected: {selectedIndexes.Count}/{correctIndexes.Count}";
        }
    }

    void SetButtonColor(int index, Color color)
    {
        var img = pictureButtons[index].GetComponent<Image>();
        if (img != null)
            img.color = color;
    }

    public bool CheckAnswers()
    {
        bool allCorrect = true;

        foreach (int index in selectedIndexes)
        {
            if (correctIndexes.Contains(index))
            {
                SetButtonColor(index, correctColor);
            }
            else
            {
                SetButtonColor(index, wrongColor);
                allCorrect = false; // Found a wrong answer
            }
        }

        // Also ensure that the user didn't *miss* selecting correct answers
        if (selectedIndexes.Count != correctIndexes.Count)
        {
            allCorrect = false;
        }

        return allCorrect;
    }

    public bool IsSolved()
    {
        return CheckAnswers();
    }
    public void ResetGrid()
    {
        selectedIndexes.Clear();
        for (int i = 0; i < pictureButtons.Count; i++)
        {
            pictureButtons[i].interactable = true;
            SetButtonColor(i, defaultColor);
        }
    }

    public void ResetPuzzle()
    {
        selectedIndexes.Clear();

        for (int i = 0; i < pictureButtons.Count; i++)
        {
            ResetSelection(i); // Handles reset logic per button
        }

        submitButton.SetActive(false);

        Debug.Log("Picture Grid Puzzle reset.");
    }
    private void ResetSelection(int index)
    {
        if (index < 0 || index >= pictureButtons.Count) return;

        Button btn = pictureButtons[index];
        btn.interactable = true;

        var img = btn.GetComponent<Image>();
        if (img != null)
            img.color = defaultColor;
    }


}
