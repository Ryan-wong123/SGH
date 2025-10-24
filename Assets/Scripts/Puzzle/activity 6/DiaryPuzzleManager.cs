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
        public TMP_Text dateText;      // The UI Text Component
        public int daysBack;           // How many days to go back from today
    }

    [SerializeField] private List<DateTextEntry> dateEntries;

    private void OnEnable()
    {
        DateTime today = DateTime.Today;

        foreach (var entry in dateEntries)
        {
            if (entry.dateText != null)
            {
                DateTime targetDate = today.AddDays(-entry.daysBack);
                entry.dateText.text = targetDate.ToString("dd MMM yyyy"); // Example format: 21 May 2025
            }
        }
    }


    // Treat answers as equal if:
    // 1) exact string match (with optional case-insensitive), OR
    // 2) numeric match within a small tolerance (handles "8", "08", "8.0")
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

        // Try numeric comparison
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



    public bool SubmitDiary()
    {
        int maxStage = stagePanels?.Length ?? 0;
        if (currentStage < 1 || currentStage > maxStage)
        {
            Debug.LogWarning($"Invalid currentStage={currentStage}");
            return false;
        }

        if (!CheckStage(currentStage))
        {
            Debug.Log($"Diary {currentStage} incorrect. Please fix it.");
            return false;
        }

        // advance or finish
        if (currentStage < maxStage)
        {
            Debug.Log($"Diary {currentStage} correct. Proceed to Diary {currentStage + 1}.");
            stagePanels[currentStage - 1].SetActive(false);
            stagePanels[currentStage].SetActive(true);
            currentStage++;
            return false;
        }

        Debug.Log($"Diary {currentStage} correct. All stages complete!");
        return true;
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
  
    public bool IsSolved()
    {
        return SubmitDiary();
    }
}
