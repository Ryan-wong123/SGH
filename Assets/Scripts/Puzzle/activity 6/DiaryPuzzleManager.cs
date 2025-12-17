using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Globalization;

public class DiaryPuzzleManager : MonoBehaviour, IPuzzle
{
    [System.Serializable]
    public struct AnswerItem
    {
        public TMP_InputField field;   // The input to check & color
        public int stage;              // 1-based stage index (1,2,3,…)
        public bool caseSensitive;     // Toggle per field
        public string[] accepted;      // One or more accepted answers
    }

    [SerializeField] private List<AnswerItem> answers;
    [SerializeField] private GameObject[] stagePanels;

    private int currentStage = 1;

    [System.Serializable]
    public class DateTextEntry
    {
        public TMP_Text dateText;
        public int daysBack;
    }

    [SerializeField] private List<DateTextEntry> dateEntries;

    public enum DiarySubmitResult
    {
        Incorrect,
        Advanced,   // correct, moved to next diary page
        Completed   // correct, final page complete
    }

    public int CurrentStage => currentStage;
    public int MaxStage => stagePanels != null ? stagePanels.Length : 0;

    private void OnEnable()
    {
        DateTime today = DateTime.Today;

        foreach (var entry in dateEntries)
        {
            if (entry.dateText != null)
            {
                DateTime targetDate = today.AddDays(-entry.daysBack);
                entry.dateText.text = targetDate.ToString("dd MMM yyyy");
            }
        }
    }

    private static bool ValuesMatch(string user, string expected, bool caseSensitive, float floatEps = 1e-4f)
    {
        if (user == null) user = string.Empty;
        if (expected == null) expected = string.Empty;

        string u = user.Trim();
        string e = expected.Trim();

        if (!caseSensitive)
        {
            u = u.ToLowerInvariant();
            e = e.ToLowerInvariant();
        }

        if (u == e) return true;

        if (float.TryParse(u, NumberStyles.Float, CultureInfo.InvariantCulture, out var uf) &&
            float.TryParse(e, NumberStyles.Float, CultureInfo.InvariantCulture, out var ef))
        {
            return Math.Abs(uf - ef) <= floatEps;
        }

        return false;
    }

    private bool CheckStage(int stage)
    {
        bool allCorrect = true;

        foreach (var a in answers)
        {
            if (a.stage != stage || a.field == null) continue;

            string val = a.field.text ?? string.Empty;

            bool ok = false;
            if (a.accepted != null && a.accepted.Length > 0)
            {
                foreach (var acc in a.accepted)
                {
                    if (ValuesMatch(val, acc, a.caseSensitive))
                    {
                        ok = true;
                        break;
                    }
                }
            }

            a.field.image.color = ok ? Color.green : Color.red;
            if (!ok) allCorrect = false;
        }

        return allCorrect;
    }

    // ✅ New: no panel toggling here, just returns the result
    public DiarySubmitResult SubmitDiaryStage()
    {
        int maxStage = MaxStage;
        if (currentStage < 1 || currentStage > maxStage)
        {
            Debug.LogWarning($"Invalid currentStage={currentStage}");
            return DiarySubmitResult.Incorrect;
        }

        if (!CheckStage(currentStage))
        {
            Debug.Log($"Diary {currentStage} incorrect. Please fix it.");
            return DiarySubmitResult.Incorrect;
        }

        if (currentStage < maxStage)
        {
            Debug.Log($"Diary {currentStage} correct. Proceed to Diary {currentStage + 1}.");
            return DiarySubmitResult.Advanced;
        }

        Debug.Log($"Diary {currentStage} correct. All stages complete!");
        return DiarySubmitResult.Completed;
    }

    // ✅ New: PuzzleStateManager will call this via RPC to actually toggle panels
    public void GoToStage(int newStage)
    {
        int maxStage = MaxStage;
        if (maxStage <= 0) return;

        newStage = Mathf.Clamp(newStage, 1, maxStage);

        for (int i = 0; i < stagePanels.Length; i++)
            stagePanels[i].SetActive(i == (newStage - 1));

        currentStage = newStage;
    }

    public void ResetPuzzle()
    {
        foreach (var a in answers)
        {
            if (a.field == null) continue;
            a.field.text = string.Empty;
            a.field.image.color = Color.white;
        }

        // panels
        for (int i = 0; i < stagePanels.Length; i++)
            stagePanels[i].SetActive(i == 0);

        currentStage = 1;
        Debug.Log("Diary puzzle reset.");
    }

    // IPuzzle
    public bool IsSolved()
    {
        // Keep compatibility: only returns true when fully completed
        return SubmitDiaryStage() == DiarySubmitResult.Completed;
    }
}
