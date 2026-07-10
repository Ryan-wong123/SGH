using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.

public class PuzzleTile : MonoBehaviour // Declares this type.
{ // Starts this code block.
    private int currentRotation = 0; // 0, 90, 180, 270
    private const int rotationStep = 90; // Declares a field, property, or member used by this script.

    private RectTransform rectTransform; // Declares a field, property, or member used by this script.

    [Tooltip("Set the correct rotation angle here (0, 90, 180, 270)")] // Shows helper text for this field in the Unity Inspector.
    public int correctRotation = 0; // Declares a field, property, or member used by this script.

    private void Awake() // Declares a method used by this script.
    { // Starts this code block.
        rectTransform = GetComponentInChildren<RectTransform>(); // Updates this value.
        // Random initial rotation
        currentRotation = 90 * Random.Range(0, 4); // Updates this value.
        rectTransform.rotation = Quaternion.Euler(0, 0, currentRotation); // Updates this value.
    } // Ends this code block.

    public void RotateTile() // Declares a method used by this script.
    { // Starts this code block.
        currentRotation = (currentRotation + rotationStep) % 360; // Updates this value.
        rectTransform.rotation = Quaternion.Euler(0, 0, currentRotation); // Updates this value.
    } // Ends this code block.

    public bool IsCorrect() // Declares a method used by this script.
    { // Starts this code block.
        return currentRotation == correctRotation; // Returns this result to the caller.
    } // Ends this code block.
} // Ends this code block.
