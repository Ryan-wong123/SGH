using TMPro; // Imports a namespace used by this script.
using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.

public class CrosswordCell : MonoBehaviour // Declares this type.
{ // Starts this code block.
    public TMP_InputField inputField; // Declares a field, property, or member used by this script.
    public Vector2Int position; // Declares a field, property, or member used by this script.
    public bool isActive; // Declares a field, property, or member used by this script.

    public void SetLetter(char c) // Declares a method used by this script.
    { // Starts this code block.
        inputField.text = c.ToString(); // Updates this value.
    } // Ends this code block.

    public char GetLetter() // Declares a method used by this script.
    { // Starts this code block.
        return string.IsNullOrEmpty(inputField.text) ? ' ' : inputField.text[0]; // Returns this result to the caller.
    } // Ends this code block.

    public void SetInteractable(bool interactable) // Declares a method used by this script.
    { // Starts this code block.
        inputField.interactable = interactable; // Updates this value.
        inputField.GetComponent<Image>().enabled = interactable; // Runs this line of puzzle logic.
    } // Ends this code block.

} // Ends this code block.
