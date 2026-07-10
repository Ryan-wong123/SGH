using System.Collections.Generic; // Imports a namespace used by this script.
using TMPro; // Imports a namespace used by this script.
using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.UI; // for Image

public class RiskTable : MonoBehaviour, IPuzzle // Declares this type.
{ // Starts this code block.
    [System.Serializable] // Adds an attribute that changes how Unity or C# treats the next declaration.
    public class DropdownAnswer // Declares this type.
    { // Starts this code block.
        [Tooltip("Reference to the dropdown UI element")] // Shows helper text for this field in the Unity Inspector.
        public TMP_Dropdown dropdown; // Declares a field, property, or member used by this script.

        [Tooltip("The correct option index for this dropdown")] // Shows helper text for this field in the Unity Inspector.
        public int correctIndex; // Declares a field, property, or member used by this script.
    } // Ends this code block.

    [Header("Dropdowns & Correct Answers")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private List<DropdownAnswer> dropdowns = new(); // Exposes this private field in the Unity Inspector.

    [Header("Colors")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private Color correctColor = Color.green; // Exposes this private field in the Unity Inspector.
    [SerializeField] private Color wrongColor = Color.red; // Exposes this private field in the Unity Inspector.
    [SerializeField] private Color defaultColor = Color.white; // Exposes this private field in the Unity Inspector.

    /// <summary>
    /// Evaluate all dropdowns. Returns true if all are correct.
    /// </summary>
    private bool CheckAnswers() // Declares a method used by this script.
    { // Starts this code block.
        bool allCorrect = true; // Creates a local value used by the following logic.

        foreach (var entry in dropdowns) // Repeats this block for each item in a collection.
        { // Starts this code block.
            if (entry.dropdown == null) continue; // Checks this condition before continuing.

            if (entry.dropdown.value == entry.correctIndex) // Checks this condition before continuing.
            { // Starts this code block.
                entry.dropdown.SetSyncedFeedbackColor(correctColor); // Runs this line of puzzle logic.
            } // Ends this code block.
            else // Runs when the previous condition was false.
            { // Starts this code block.
                entry.dropdown.SetSyncedFeedbackColor(wrongColor); // Runs this line of puzzle logic.
                allCorrect = false; // Updates this value.
            } // Ends this code block.
        } // Ends this code block.

        if (allCorrect) // Checks this condition before continuing.
            Debug.Log("✅ All dropdowns are correct!"); // Writes a diagnostic message to the Unity console.
        else // Runs when the previous condition was false.
            Debug.Log("❌ Some dropdowns are wrong."); // Writes a diagnostic message to the Unity console.

        return allCorrect; // Returns this result to the caller.
    } // Ends this code block.

    public bool IsSolved() // Declares a method used by this script.
    { // Starts this code block.
        return CheckAnswers(); // Returns this result to the caller.
    } // Ends this code block.

    public void ResetPuzzle() // Declares a method used by this script.
    { // Starts this code block.
        foreach (var entry in dropdowns) // Repeats this block for each item in a collection.
        { // Starts this code block.
            if (entry.dropdown == null) // Checks this condition before continuing.
                continue; // Skips to the next loop iteration.

            // Reset selection to the first option (index 0), or whatever default you want
            entry.dropdown.value = 0; // Updates this value.
            entry.dropdown.RefreshShownValue(); // Runs this line of puzzle logic.

            // Reset color back to default
            entry.dropdown.SetSyncedFeedbackColor(defaultColor); // Runs this line of puzzle logic.
        } // Ends this code block.

        Debug.Log("RiskTable puzzle reset."); // Writes a diagnostic message to the Unity console.
    } // Ends this code block.

} // Ends this code block.
