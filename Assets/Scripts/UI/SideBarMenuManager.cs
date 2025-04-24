using UnityEngine;
using UnityEngine.UI;

public class SidebarMenuManager : MonoBehaviour
{
    [Header("Popup Panels")]
    [SerializeField] private GameObject popupMenuPanel;
    [SerializeField] private GameObject popupInventoryPanel;
    [SerializeField] private GameObject popupCluePanel;
    [SerializeField] private GameObject popupSettingsPanel;
    [SerializeField] private GameObject popupBlocker;

    private GameObject currentPopup = null;
    private PuzzleTrigger currentPuzzle = null;

    public void SetActivePuzzle(PuzzleTrigger puzzle)
    {
        currentPuzzle = puzzle;
    }

    public void TogglePopup(GameObject popup)
    {
        if (currentPopup == popup)
        {
            // Close the popup if it's already open
            popup.SetActive(false);
            popupBlocker.SetActive(false);
            currentPopup = null;
        }
        else
        {
            if (currentPopup != null)
                currentPopup.SetActive(false);

            popup.SetActive(true);
            popupBlocker.SetActive(true);
            currentPopup = popup;
        }
    }

    public void CloseAllPopups()
    {
        if (currentPopup != null)
        {
            currentPopup.SetActive(false);
            currentPopup = null;
        }

        if (currentPuzzle != null)
        {
            currentPuzzle.ClosePuzzle();
            currentPuzzle = null;
        }

        popupBlocker.SetActive(false);
    }


}
