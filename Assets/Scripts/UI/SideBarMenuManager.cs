using UnityEngine; // Imports Unity engine types such as MonoBehaviour and GameObject.
using UnityEngine.UI; // Imports Unity UI types such as Button.

public class SidebarMenuManager : MonoBehaviour // Declares the component that manages sidebar popup panels.
{ // Starts the SidebarMenuManager class body.
    [Header("Sidebar Popups")] // Groups sidebar popup fields in the Unity inspector.
    [SerializeField] private SidebarPopup[] sidebarPopups; // Exposes the button-to-panel popup mappings in the inspector.

    [Header("Blocker")] // Groups the blocker field in the Unity inspector.
    [SerializeField] private GameObject popupBlocker; // Exposes the background blocker object used while a popup is open.

    private GameObject currentPopup = null; // Tracks the currently open popup panel, if any.
    private PuzzleTrigger currentPuzzle = null; // Tracks the currently open puzzle trigger, if any.

    [System.Serializable] // Allows Unity to show this struct in the inspector.
    public struct SidebarPopup // Defines one sidebar button and its matching popup panel.
    { // Starts the SidebarPopup struct body.
        public Button button; // References the sidebar button that opens or closes the panel.
        public GameObject panel; // References the popup panel controlled by the button.
    } // Ends the SidebarPopup struct body.

    private void Awake() // Runs before Start to connect sidebar button listeners.
    { // Starts the Awake method body.
        // Auto-bind button click listeners
        foreach (var popup in sidebarPopups) // Loops through each configured sidebar popup mapping.
        { // Starts the popup listener setup block.
            SidebarPopup capturedPopup = popup; // Stores this loop value so the click closure uses the correct panel.
            popup.button.onClick.AddListener(() => // Adds a click listener to the sidebar button.
            { // Starts the click listener body.
                TogglePopup(capturedPopup.panel); // Toggles the panel assigned to the clicked button.
            }); // Ends the click listener body and listener registration.
        } // Ends the popup listener setup block.
    } // Ends the Awake method body.

    public void TogglePopup(GameObject popup) // Opens the requested popup or closes it if it is already open.
    { // Starts the TogglePopup method body.
        if (currentPopup == popup) // Checks whether the requested popup is already open.
        { // Starts the close-current-popup block.
            Debug.Log("Closing popup: " + popup.name); // Logs which popup is being closed.
            popup.SetActive(false); // Hides the currently open popup panel.
            popupBlocker.SetActive(false); // Hides the blocker because no popup remains open.
            currentPopup = null; // Clears the current popup reference.
        } // Ends the close-current-popup block.
        else // Runs when a different popup should be opened.
        { // Starts the open-new-popup block.
            if (currentPopup != null) // Checks whether another popup is currently open.
                currentPopup.SetActive(false); // Hides the previously open popup before showing the new one.

            Debug.Log("Opening popup: " + popup.name); // Logs which popup is being opened.
            popup.SetActive(true); // Shows the requested popup panel.
            popupBlocker.SetActive(true); // Shows the blocker while a popup is open.
            currentPopup = popup; // Stores the requested popup as the current open panel.
        } // Ends the open-new-popup block.
    } // Ends the TogglePopup method body.

    public void CloseAllPopups() // Closes any open sidebar popup and puzzle overlay.
    { // Starts the CloseAllPopups method body.
        if (currentPopup != null) // Checks whether a sidebar popup is currently open.
        { // Starts the close-popup block.
            currentPopup.SetActive(false); // Hides the current popup panel.
            currentPopup = null; // Clears the current popup reference.
        } // Ends the close-popup block.

        if (currentPuzzle != null) // Checks whether a puzzle overlay is currently tracked.
        { // Starts the close-puzzle block.
            currentPuzzle.ClosePuzzle(); // Asks the active puzzle trigger to close its puzzle UI.
            currentPuzzle = null; // Clears the current puzzle reference.
        } // Ends the close-puzzle block.

        popupBlocker.SetActive(false); // Hides the blocker after all popups and puzzles are closed.
    } // Ends the CloseAllPopups method body.
} // Ends the SidebarMenuManager class body.
