using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.

public class PuzzleTileManager : MonoBehaviour // Declares this type.
{ // Starts this code block.
    public static PuzzleTileManager Instance; // Declares a field, property, or member used by this script.

    public PuzzleTile[] tiles; // assign these in the Inspector

    private void Awake() // Declares a method used by this script.
    { // Starts this code block.
        Instance = this; // Updates this value.
    } // Ends this code block.

    public bool CheckPuzzleSolved() // Declares a method used by this script.
    { // Starts this code block.
        bool allCorrect = true; // Creates a local value used by the following logic.

        foreach (var tile in tiles) // Repeats this block for each item in a collection.
        { // Starts this code block.
            if (!tile.IsCorrect()) // Checks this condition before continuing.
            { // Starts this code block.
                Debug.LogWarning($"{tile.gameObject.name} is NOT correctly rotated. Current rotation is incorrect."); // Writes a warning message to the Unity console.
                allCorrect = false; // Updates this value.
            } // Ends this code block.
            else // Runs when the previous condition was false.
            { // Starts this code block.
                Debug.Log($"{tile.gameObject.name} is correctly rotated."); // Writes a diagnostic message to the Unity console.
            } // Ends this code block.
        } // Ends this code block.

        return allCorrect; // Returns this result to the caller.
    } // Ends this code block.

    public void ResetPuzzle() // Declares a method used by this script.
    { // Starts this code block.
        for (int i = 0; i < tiles.Length; i++) // Repeats this block for a counted loop.
        { // Starts this code block.
            float currentRotation = 90 * Random.Range(0, 4); // Creates a local value used by the following logic.
            tiles[i].transform.localRotation = Quaternion.Euler(0f, 0f, currentRotation); // Updates this value.
        } // Ends this code block.


        Debug.Log("Tile Rotation Puzzle reset."); // Writes a diagnostic message to the Unity console.
    } // Ends this code block.

} // Ends this code block.
