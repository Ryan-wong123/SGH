using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class PictureGridManager : MonoBehaviour, IPuzzle
{
    [Header("Grid Settings")]
    public List<Button> pictureButtons; // Assign all 20 buttons
    public List<int> correctIndexes; // e.g. [0, 4, 7, 9, 15, 19]

    private HashSet<int> selectedIndexes = new HashSet<int>();
    private readonly List<PhotonSyncedUIButtonColor> syncedButtons = new List<PhotonSyncedUIButtonColor>();

    public Color defaultColor = Color.white;
    public Color selectedColor = Color.yellow;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    [SerializeField] GameObject submitButton;
    [SerializeField] private TMP_Text selectedCountText;

    public GameObject SubmitButton => submitButton;

    public void Start()
    {
        for (int i = 0; i < pictureButtons.Count; i++)
        {
            var syncedButton = pictureButtons[i].GetComponent<PhotonSyncedUIButtonColor>();
            if (syncedButton != null)
            {
                syncedButton.StateChanged += HandleSyncedButtonStateChanged;
                syncedButtons.Add(syncedButton);
                continue;
            }

            int index = i; // Needed to avoid closure issues
            pictureButtons[i].onClick.AddListener(() => OnPictureClick(index));
        }

        RebuildSelectedIndexesFromButtons();
        RefreshSubmitButtonVisibility();
        UpdateSelectedCountText();
    }

    private void OnDestroy()
    {
        foreach (var syncedButton in syncedButtons)
        {
            if (syncedButton != null)
                syncedButton.StateChanged -= HandleSyncedButtonStateChanged;
        }
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

        if (selectedIndexes.Count == correctIndexes.Count)
            Debug.Log($"{correctIndexes.Count} buttons selected. Ready to check answers!");

        RefreshSubmitButtonVisibility();
    }

    void UpdateSelectedCountText()
    {
        if (selectedCountText != null)
        {
            selectedCountText.text = $"Selected: {selectedIndexes.Count}/{correctIndexes.Count}";
        }
    }

    private void HandleSyncedButtonStateChanged(PhotonSyncedUIButtonColor _)
    {
        RebuildSelectedIndexesFromButtons();
        RefreshSubmitButtonVisibility();
        UpdateSelectedCountText();
    }

    private void RebuildSelectedIndexesFromButtons()
    {
        selectedIndexes.Clear();

        for (int i = 0; i < pictureButtons.Count; i++)
        {
            var syncedButton = pictureButtons[i].GetComponent<PhotonSyncedUIButtonColor>();
            if (syncedButton != null && syncedButton.IsSelected)
            {
                selectedIndexes.Add(i);
                continue;
            }

            var image = pictureButtons[i].GetComponent<Image>();
            if (image != null && (image.color == selectedColor || image.color == correctColor || image.color == wrongColor))
                selectedIndexes.Add(i);
        }
    }

    // ✅ FULL FIX IS HERE:
    // Instead of directly setting Image.color (local only),
    // we translate the color intention into a synced button-state call.
    void SetButtonColor(int index, Color color)
    {
        if (index < 0 || index >= pictureButtons.Count) return;

        var btnGO = pictureButtons[index].gameObject;
        var synced = btnGO.GetComponent<PhotonSyncedUIButtonColor>();

        // If this button doesn't have the sync component, fallback to local color (keeps your logic usable)
        if (synced == null)
        {
            var img = btnGO.GetComponent<Image>();
            if (img != null) img.color = color;
            return;
        }

        // Map your color usage to synced states:
        // - defaultColor => clear result (back to normal)
        // - selectedColor => selected state (yellow)
        // - correctColor => correct state (green)
        // - wrongColor => wrong state (red)
        if (color == defaultColor)
        {
            // back to normal (not selected, not correct/wrong)
            synced.ResetStateAndSync();
        }
        else if (color == selectedColor)
        {
            // selected highlight (yellow)
            synced.MarkSelectedAndSync();
        }
        else if (color == correctColor)
        {
            synced.MarkCorrectAndSync();
        }
        else if (color == wrongColor)
        {
            synced.MarkWrongAndSync();
        }
        else
        {
            // If you pass a custom color, just treat it as selected
            // (or you can add SetCustomColorAndSync to your button script if needed)
            synced.MarkSelectedAndSync();
        }
    }

    public bool CheckAnswers()
    {
        RebuildSelectedIndexesFromButtons();

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

        RefreshSubmitButtonVisibility();
        UpdateSelectedCountText();

        Debug.Log("Picture Grid Puzzle reset.");
    }

    public void RefreshSubmitButtonVisibility()
    {
        if (submitButton == null) return;

        bool hasRequiredSelectionCount = selectedIndexes.Count == correctIndexes.Count;
        bool canLocalPlayerSubmit = PuzzleStateManager.Instance == null ||
                                    PuzzleStateManager.Instance.CanLocalPlayerSubmitCurrentAttempt();

        submitButton.SetActive(canLocalPlayerSubmit && hasRequiredSelectionCount);
    }

    private void ResetSelection(int index)
    {
        if (index < 0 || index >= pictureButtons.Count) return;

        Button btn = pictureButtons[index];
        btn.interactable = true;

        // Use your existing reset flow, but route through SetButtonColor so it syncs
        SetButtonColor(index, defaultColor);
    }
}
