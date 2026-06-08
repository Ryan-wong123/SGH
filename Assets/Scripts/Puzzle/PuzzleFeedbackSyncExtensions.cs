using TMPro; // Imports a namespace used by this script.
using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.

public static class PuzzleFeedbackSyncExtensions // Declares this type.
{ // Starts this code block.
    public static void SetSyncedFeedbackColor(this DraggableCard card, Color color) // Declares a method used by this script.
    { // Starts this code block.
        if (card == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        card.SetCardColorAndSync(color); // Runs this line of puzzle logic.
    } // Ends this code block.

    public static void ResetSyncedFeedbackColor(this DraggableCard card) // Declares a method used by this script.
    { // Starts this code block.
        if (card == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        card.ResetCardColorAndSync(); // Runs this line of puzzle logic.
    } // Ends this code block.

    public static void SetSyncedFeedbackColor(this TMP_InputField inputField, Color color) // Declares a method used by this script.
    { // Starts this code block.
        if (inputField == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        PhotonSyncedTMPInputField syncedInputField = inputField.GetComponent<PhotonSyncedTMPInputField>(); // Runs this line of puzzle logic.
        if (syncedInputField != null) // Checks this condition before continuing.
        { // Starts this code block.
            syncedInputField.SetBackgroundColorAndSync(color); // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        ApplyGraphicColor(inputField.image != null ? inputField.image : inputField.GetComponent<Graphic>(), color); // Runs this line of puzzle logic.
    } // Ends this code block.

    public static void ResetSyncedFeedbackColor(this TMP_InputField inputField) // Declares a method used by this script.
    { // Starts this code block.
        inputField.SetSyncedFeedbackColor(Color.white); // Runs this line of puzzle logic.
    } // Ends this code block.

    public static void SetSyncedFeedbackColor(this TMP_Dropdown dropdown, Color color) // Declares a method used by this script.
    { // Starts this code block.
        if (dropdown == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        PhotonSyncedTMPDropdown syncedDropdown = dropdown.GetComponent<PhotonSyncedTMPDropdown>(); // Runs this line of puzzle logic.
        if (syncedDropdown != null) // Checks this condition before continuing.
        { // Starts this code block.
            syncedDropdown.SetBackgroundColorAndSync(color); // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        ApplyGraphicColor(dropdown.targetGraphic as Graphic ?? dropdown.GetComponent<Graphic>(), color); // Runs this line of puzzle logic.
    } // Ends this code block.

    public static void ResetSyncedFeedbackColor(this TMP_Dropdown dropdown) // Declares a method used by this script.
    { // Starts this code block.
        dropdown.SetSyncedFeedbackColor(Color.white); // Runs this line of puzzle logic.
    } // Ends this code block.

    private static void ApplyGraphicColor(Graphic graphic, Color color) // Declares a method used by this script.
    { // Starts this code block.
        if (graphic != null) // Checks this condition before continuing.
            graphic.color = color; // Updates this value.
    } // Ends this code block.
} // Ends this code block.
