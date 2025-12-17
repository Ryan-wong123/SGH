using UnityEngine;
using TMPro;
using System;

public class DynamicDateText : MonoBehaviour
{
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private int daysBack = 0; // 0 = today, 1 = yesterday, etc.
    [SerializeField] private string format = "dd MMM yyyy";

    private void OnEnable()
    {
        if (dateText == null) return;

        DateTime targetDate = DateTime.Today.AddDays(-daysBack);
        dateText.text = targetDate.ToString(format);
    }
}
