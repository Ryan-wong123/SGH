using UnityEngine; // Imports a namespace used by this UI script.
using TMPro; // Imports a namespace used by this UI script.
using System; // Imports a namespace used by this UI script.

public class DynamicDateText : MonoBehaviour // Declares this UI type.
{ // Starts this code block.
    [SerializeField] private TMP_Text dateText; // Exposes this private field in the Unity Inspector.
    [SerializeField] private int daysBack = 0; // 0 = today, 1 = yesterday, etc.
    [SerializeField] private string format = "dd MMM yyyy"; // Exposes this private field in the Unity Inspector.

    private void OnEnable() // Declares a method used by this UI script.
    { // Starts this code block.
        if (dateText == null) return; // Checks this condition before continuing.

        DateTime targetDate = DateTime.Today.AddDays(-daysBack); // Runs this line of UI logic.
        dateText.text = targetDate.ToString(format); // Updates this value.
    } // Ends this code block.
} // Ends this code block.
