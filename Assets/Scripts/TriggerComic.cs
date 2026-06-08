using TMPro; // Imports a namespace used by this script.
using UnityEngine; // Imports a namespace used by this script.

public class TriggerComic : MonoBehaviour // Declares this type.
{ // Starts this code block.
    [Header("Assign the UI or Objects to activate")] // Adds a labeled section in the Unity Inspector.
    public GameObject[] objectsToActivate;   // array of objects

    private bool hasActivated = false; // Declares a field, property, or member used by this script.

    [SerializeField] private TextMeshProUGUI textUI = null; // Reference to the TextMeshProUGUI component
    [SerializeField] private string newText; // The new text to display

    private void OnTriggerEnter(Collider other) // Declares a method used by this script.
    { // Starts this code block.
        if (!hasActivated && other.CompareTag("Player")) // Checks this condition before continuing.
        { // Starts this code block.
            // Activate all objects in the array
            foreach (GameObject obj in objectsToActivate) // Repeats this block for each item in a collection.
            { // Starts this code block.
                if (obj != null) // Checks this condition before continuing.
                    obj.SetActive(true); // Runs this line of logic.
            } // Ends this code block.

            if(textUI != null) // Checks this condition before continuing.
            { // Starts this code block.
                textUI.text = newText; // Update the text
            } // Ends this code block.

            hasActivated = true; // prevent future activation
        } // Ends this code block.
    } // Ends this code block.
} // Ends this code block.
