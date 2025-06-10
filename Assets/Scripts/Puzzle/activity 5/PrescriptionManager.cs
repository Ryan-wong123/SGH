using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class PrescriptionManager : MonoBehaviour, IPuzzle
{


    [SerializeField] TMP_InputField prescriptionAmount;

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
                entry.dateText.text = targetDate.ToString("dd MMM"); // Example format: 21 May 2025
            }
        }
    }
 
    public bool CheckPrescriptionAmount()
    {
        string input = prescriptionAmount.text.Trim().ToLower();

        bool correct1 = input == "28";

        prescriptionAmount.image.color = correct1 ? Color.green : Color.red;

        return correct1;
    }
    public bool IsSolved()
    {
        return CheckPrescriptionAmount();
    }
    public void ResetPuzzle()
    {
        // Reset Diary 1 Inputs
        prescriptionAmount.text = "";
        prescriptionAmount.image.color = Color.white;

        Debug.Log("Prescription puzzle reset.");
    }
}
