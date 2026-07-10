using UnityEngine; // Imports a namespace used by this script.

public class QuitButton : MonoBehaviour // Declares this type.
{ // Starts this code block.
    public void QuitGame() // Declares a method used by this script.
    { // Starts this code block.
#if UNITY_EDITOR // Starts a conditional compilation block.
        UnityEditor.EditorApplication.isPlaying = false; // Updates this value.
#else // Starts the alternate conditional compilation branch.
        Application.Quit(); // Runs this line of logic.
#endif // Ends the conditional compilation block.
    } // Ends this code block.
} // Ends this code block.
