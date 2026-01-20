using UnityEngine;
using UnityEngine.UI;

public class SidebarMenuManager : MonoBehaviour
{
    [Header("Sidebar Popups")]
    [SerializeField] private SidebarPopup[] sidebarPopups;

    [Header("Blocker")]
    [SerializeField] private GameObject popupBlocker;

    private GameObject currentPopup = null;
    private PuzzleTrigger currentPuzzle = null;

    [System.Serializable]
    public struct SidebarPopup
    {
        public Button button;
        public GameObject panel;
    }

    private void Awake()
    {
        // Auto-bind button click listeners
        foreach (var popup in sidebarPopups)
        {
            SidebarPopup capturedPopup = popup; // important for closures
            popup.button.onClick.AddListener(() =>
            {
                TogglePopup(capturedPopup.panel);
            });
        }
    }

    public void TogglePopup(GameObject popup)
    {
        if (currentPopup == popup)
        {
            Debug.Log("Closing popup: " + popup.name);
            popup.SetActive(false);
            popupBlocker.SetActive(false);
            currentPopup = null;
        }
        else
        {
            if (currentPopup != null)
                currentPopup.SetActive(false);

            Debug.Log("Opening popup: " + popup.name);
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
