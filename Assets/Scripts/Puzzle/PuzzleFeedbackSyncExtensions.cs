using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class PuzzleFeedbackSyncExtensions
{
    public static void SetSyncedFeedbackColor(this DraggableCard card, Color color)
    {
        if (card == null)
            return;

        card.SetCardColorAndSync(color);
    }

    public static void ResetSyncedFeedbackColor(this DraggableCard card)
    {
        if (card == null)
            return;

        card.ResetCardColorAndSync();
    }

    public static void SetSyncedFeedbackColor(this TMP_InputField inputField, Color color)
    {
        if (inputField == null)
            return;

        PhotonSyncedTMPInputField syncedInputField = inputField.GetComponent<PhotonSyncedTMPInputField>();
        if (syncedInputField != null)
        {
            syncedInputField.SetBackgroundColorAndSync(color);
            return;
        }

        ApplyGraphicColor(inputField.image != null ? inputField.image : inputField.GetComponent<Graphic>(), color);
    }

    public static void ResetSyncedFeedbackColor(this TMP_InputField inputField)
    {
        inputField.SetSyncedFeedbackColor(Color.white);
    }

    public static void SetSyncedFeedbackColor(this TMP_Dropdown dropdown, Color color)
    {
        if (dropdown == null)
            return;

        PhotonSyncedTMPDropdown syncedDropdown = dropdown.GetComponent<PhotonSyncedTMPDropdown>();
        if (syncedDropdown != null)
        {
            syncedDropdown.SetBackgroundColorAndSync(color);
            return;
        }

        ApplyGraphicColor(dropdown.targetGraphic as Graphic ?? dropdown.GetComponent<Graphic>(), color);
    }

    public static void ResetSyncedFeedbackColor(this TMP_Dropdown dropdown)
    {
        dropdown.SetSyncedFeedbackColor(Color.white);
    }

    private static void ApplyGraphicColor(Graphic graphic, Color color)
    {
        if (graphic != null)
            graphic.color = color;
    }
}
