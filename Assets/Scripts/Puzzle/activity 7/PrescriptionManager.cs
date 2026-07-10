using UnityEngine; // Imports a namespace used by this script.
using TMPro; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.
using System.Collections.Generic; // Imports a namespace used by this script.
using System; // Imports a namespace used by this script.

public class PrescriptionManager : MonoBehaviour, IPuzzle // Declares this type.
{ // Starts this code block.


    [SerializeField] TMP_InputField prescriptionAmount; // Exposes this private field in the Unity Inspector.

    [System.Serializable] // Adds an attribute that changes how Unity or C# treats the next declaration.
    public class DateTextEntry // Declares this type.
    { // Starts this code block.
        public TMP_Text dateText;      // The UI Text Component
        public int daysBack;           // How many days to go back from today
    } // Ends this code block.

    [SerializeField] private List<DateTextEntry> dateEntries; // Exposes this private field in the Unity Inspector.
    private void OnEnable() // Declares a method used by this script.
    { // Starts this code block.
        DateTime today = DateTime.Today; // Runs this line of puzzle logic.

        foreach (var entry in dateEntries) // Repeats this block for each item in a collection.
        { // Starts this code block.
            if (entry.dateText != null) // Checks this condition before continuing.
            { // Starts this code block.
                DateTime targetDate = today.AddDays(-entry.daysBack); // Runs this line of puzzle logic.
                entry.dateText.text = targetDate.ToString("dd MMM"); // Example format: 21 May 2025
            } // Ends this code block.
        } // Ends this code block.
    } // Ends this code block.
 
    public bool CheckPrescriptionAmount() // Declares a method used by this script.
    { // Starts this code block.
        string input = prescriptionAmount.text.Trim().ToLower(); // Creates a local value used by the following logic.

        bool correct1 = input == "28"; // Creates a local value used by the following logic.

        prescriptionAmount.SetSyncedFeedbackColor(correct1 ? Color.green : Color.red); // Runs this line of puzzle logic.

        return correct1; // Returns this result to the caller.
    } // Ends this code block.
    public bool IsSolved() // Declares a method used by this script.
    { // Starts this code block.
        return CheckPrescriptionAmount(); // Returns this result to the caller.
    } // Ends this code block.
    public void ResetPuzzle() // Declares a method used by this script.
    { // Starts this code block.
        // Reset Diary 1 Inputs
        prescriptionAmount.text = ""; // Updates this value.
        prescriptionAmount.ResetSyncedFeedbackColor(); // Runs this line of puzzle logic.

        Debug.Log("Prescription puzzle reset."); // Writes a diagnostic message to the Unity console.
    } // Ends this code block.
} // Ends this code block.
