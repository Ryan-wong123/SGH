using Photon.Pun; // Imports a namespace used by this script.
using Photon.Realtime; // Imports a namespace used by this script.
using ExitGames.Client.Photon; // Imports a namespace used by this script.
using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.

public class CharacterSelection : MonoBehaviourPunCallbacks // Declares this type.
{ // Starts this code block.
    [SerializeField] private Button[] characterButtons; // Exposes this private field in the Unity Inspector.

    [Header("Highlight Colors")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private Color selectedColor = Color.green; // Exposes this private field in the Unity Inspector.
    [SerializeField] private Color normalColor = Color.white; // Exposes this private field in the Unity Inspector.

    private int currentSelectedIndex = -1; // Declares a field, property, or member used by this script.

    public void SetCharacter(int characterIndex) // Declares a method used by this script.
    { // Starts this code block.
        Hashtable properties = new Hashtable // Creates a local value used by the following logic.
        { // Starts this code block.
            { "CharacterIndex", characterIndex } // Runs this line of logic.
        }; // Runs this line of logic.
        PhotonNetwork.LocalPlayer.SetCustomProperties(properties); // Runs this line of logic.
        Debug.Log("Selected Character Index: " + characterIndex); // Writes a diagnostic message to the Unity console.
        UpdateButtonColors(characterIndex); // Runs this line of logic.
    } // Ends this code block.

    private void UpdateButtonColors(int selectedIndex) // Declares a method used by this script.
    { // Starts this code block.
        for (int i = 0; i < characterButtons.Length; i++) // Repeats this block for a counted loop.
        { // Starts this code block.
            var image = characterButtons[i].GetComponent<Image>(); // Creates a local value used by the following logic.
            if (image != null) // Checks this condition before continuing.
            { // Starts this code block.
                image.color = (i == selectedIndex) ? selectedColor : normalColor; // Updates this value.
            } // Ends this code block.
        } // Ends this code block.

        currentSelectedIndex = selectedIndex; // Updates this value.
    } // Ends this code block.
} // Ends this code block.
